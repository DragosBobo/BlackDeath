using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class PeasantController : MonoBehaviour
{
    [Header("Movement (NavMesh)")]
    [SerializeField] private bool enableWander = true;
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float repathIntervalMin = 2.0f;
    [SerializeField] private float repathIntervalMax = 4.0f;
    [SerializeField] private float waypointReachedDistance = 0.6f;

    [Header("Detection / Identity")]
    [Tooltip("Set this to the Peasant layer (used by infection spreading).")]
    [SerializeField] private LayerMask peasantLayerMask;

    [Header("Infection")]
    [SerializeField] private bool startRandomized = true;
    [SerializeField, Range(0f, 1f)] public float infectionProgress = 0f;
    [SerializeField] private float baseInfectionRatePerSecond = 0.01f;
    [SerializeField, Range(0f, 1f)] private float sickThreshold = 0.6f;
    [SerializeField, Range(0f, 1f)] private float contagiousThreshold = 0.85f;

    [Header("Cure / Grace")]
    [SerializeField] private float graceRemaining = 0f;
    [SerializeField] private float graceDuration = 0f;
    [SerializeField] private RingFillController graceRing;
    public bool IsGraceActive => graceRemaining > 0f;

    // Global counters (CURRENT, can go up and down)
    public static int TotalPeasants { get; private set; }
    public static int SickPeasants { get; private set; }
    public static int ContagiousPeasants { get; private set; }

    [Header("Spreading (Only when contagious)")]
    [SerializeField] private float spreadRadius = 1.6f;
    [SerializeField] private float spreadRatePerTick = 0.04f;
    [SerializeField] private float spreadTickInterval = 0.5f;

    [Header("Spread Randomization (per NPC instance)")]
    [SerializeField, Range(1, 10)] private int spreadPowerMin = 1;
    [SerializeField, Range(1, 10)] private int spreadPowerMax = 10;

    // Exposed in Inspector (runtime assigned)
    [SerializeField, Tooltip("Randomized on spawn. Multiplies spreadRatePerTick. (1..10)")]
    private int spreadPower = 1;
    public int SpreadPower => spreadPower;

    [Header("Reveal (Called by lamp)")]
    [SerializeField] private float revealHoldSeconds = 0.15f;
    public bool IsRevealed { get; private set; }

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string isWalkingParam = "isWalking";
    [SerializeField] private float walkSpeedThreshold = 0.08f;
    [SerializeField] private float walkSpeedHysteresis = 0.03f;

    [Header("VFX")]
    [SerializeField] private ParticleSystem sickVfx;
    [SerializeField] private bool playSickVfxOnce = true;
    private bool sickVfxPlayed;

    public bool IsSick => infectionProgress >= sickThreshold;
    public bool IsContagious => infectionProgress >= contagiousThreshold;
    public float InfectionProgress => infectionProgress;

    // Current state tracking (so counters can go DOWN after healing)
    private bool isSickCurrent;
    private bool isContagiousCurrent;

    private NavMeshAgent agent;
    private float nextRepathTime;
    private float nextSpreadTickTime;
    private float revealTimer;

    private readonly Collider[] spreadHits = new Collider[32];

    private int isWalkingHash;
    private bool isWalkingCached;

    public static void ResetCounts()
    {
        TotalPeasants = 0;
        SickPeasants = 0;
        ContagiousPeasants = 0;
    }

    private void OnEnable()
    {
        TotalPeasants++;
    }

    private void OnDisable()
    {
        TotalPeasants = Mathf.Max(0, TotalPeasants - 1);

        // Remove from CURRENT counters if this peasant was currently in those states
        if (isSickCurrent) SickPeasants = Mathf.Max(0, SickPeasants - 1);
        if (isContagiousCurrent) ContagiousPeasants = Mathf.Max(0, ContagiousPeasants - 1);
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!agent)
        {
            enabled = false;
            return;
        }

        if (!animator) animator = GetComponentInChildren<Animator>();
        isWalkingHash = Animator.StringToHash(isWalkingParam);

        if (!sickVfx) sickVfx = GetComponentInChildren<ParticleSystem>(true);
        if (sickVfx)
        {
            // Prevent any "plays on spawn" behavior
            sickVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (startRandomized)
            infectionProgress = Mathf.Clamp01(infectionProgress + Random.Range(0f, 0.15f));

        // Per-instance spread strength (inclusive)
        int min = Mathf.Clamp(spreadPowerMin, 1, 10);
        int max = Mathf.Clamp(spreadPowerMax, 1, 10);
        if (max < min) (min, max) = (max, min);
        spreadPower = Random.Range(min, max + 1);

        // Initialize CURRENT state + counters WITHOUT firing VFX
        SyncStateAndCounts(forceInit: true);

        ScheduleNextRepath();
        nextSpreadTickTime = Time.time + Random.Range(0f, spreadTickInterval);

        if (animator)
        {
            isWalkingCached = false;
            animator.SetBool(isWalkingHash, false);
        }
    }

    private void Update()
    {
        HandleRevealTimer();
        TickInfection(Time.deltaTime);

        if (enableWander)
            HandleWander();

        if (IsContagious)
            HandleSpread();

        UpdateAnimation();
    }

    // -----------------------------------------------------
    // Public: Called by lamp
    // -----------------------------------------------------
    public void SetRevealed(bool on)
    {
        if (!IsSick) return;

        if (on)
        {
            IsRevealed = true;
            revealTimer = revealHoldSeconds;
        }
    }

    // -----------------------------------------------------
    // Infection
    // -----------------------------------------------------
    private void TickInfection(float dt)
    {
        if (graceRemaining > 0f)
        {
            graceRemaining -= dt;

            float t01 = (graceDuration <= 0f) ? 0f : Mathf.Clamp01(graceRemaining / graceDuration);

            if (graceRing)
                graceRing.SetFill01(t01);

            if (graceRemaining <= 0f && graceRing)
                graceRing.SetVisible(false);

            return; // grace active -> stop infection tick
        }

        infectionProgress = Mathf.Clamp01(infectionProgress + baseInfectionRatePerSecond * dt);
        SyncStateAndCounts(); // can increase counts + trigger VFX when crossing sick threshold
    }

    public void AddInfection(float amount)
    {
        if (amount <= 0f) return;

        infectionProgress = Mathf.Clamp01(infectionProgress + amount);
        SyncStateAndCounts(); // can increase counts + trigger VFX when crossing sick threshold
    }

    /// <summary>
    /// Healing that actually reduces infectionProgress (so Sick/Contagious can go DOWN).
    /// Call this when the player heals.
    /// </summary>
    public void HealInfection(float amount)
    {
        if (amount <= 0f) return;

        infectionProgress = Mathf.Clamp01(infectionProgress - amount);
        SyncStateAndCounts(); // can decrease counts if we drop below thresholds
    }

    /// <summary>
    /// Optional: keep your grace mechanic (pauses infection progression).
    /// Does not reduce infection by itself.
    /// </summary>
    public void Cure(float seconds)
    {
        if (seconds <= 0f) return;

        graceDuration = Mathf.Max(graceDuration, seconds);
        graceRemaining = Mathf.Max(graceRemaining, seconds);

        if (graceRing)
        {
            graceRing.SetVisible(true);
            graceRing.SetFill01(1f);
        }
    }

    // -----------------------------------------------------
    // State + counters that can go UP/DOWN
    // -----------------------------------------------------
    private void SyncStateAndCounts(bool forceInit = false)
    {
        bool sickNow = IsSick;
        bool contagiousNow = IsContagious;

        if (forceInit)
        {
            isSickCurrent = sickNow;
            isContagiousCurrent = contagiousNow;

            if (isSickCurrent) SickPeasants++;
            if (isContagiousCurrent) ContagiousPeasants++;
            return;
        }

        // Sick transitions
        if (sickNow != isSickCurrent)
        {
            // transition only
            if (!isSickCurrent && sickNow)
                TriggerSickVfx(); // only when becoming sick during gameplay

            isSickCurrent = sickNow;
            SickPeasants = Mathf.Max(0, SickPeasants + (isSickCurrent ? 1 : -1));
        }

        // Contagious transitions
        if (contagiousNow != isContagiousCurrent)
        {
            isContagiousCurrent = contagiousNow;
            ContagiousPeasants = Mathf.Max(0, ContagiousPeasants + (isContagiousCurrent ? 1 : -1));
        }
    }

    private void TriggerSickVfx()
    {
        if (!sickVfx) return;
        if (playSickVfxOnce && sickVfxPlayed) return;

        sickVfxPlayed = true;
        sickVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        sickVfx.Play(true);
    }

    // -----------------------------------------------------
    // Spread infection
    // -----------------------------------------------------
    private void HandleSpread()
    {
        if (Time.time < nextSpreadTickTime) return;
        nextSpreadTickTime = Time.time + spreadTickInterval;

        int count = Physics.OverlapSphereNonAlloc(transform.position, spreadRadius, spreadHits, peasantLayerMask);
        if (count <= 0) return;

        float amount = spreadRatePerTick * spreadPower;

        for (int i = 0; i < count; i++)
        {
            Collider c = spreadHits[i];
            if (!c) continue;

            PeasantController other = c.GetComponentInParent<PeasantController>();
            if (!other || other == this) continue;

            other.AddInfection(amount);
        }
    }

    // -----------------------------------------------------
    // Wandering
    // -----------------------------------------------------
    private void HandleWander()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;

        bool reached =
            !agent.pathPending &&
            agent.remainingDistance <= Mathf.Max(waypointReachedDistance, agent.stoppingDistance) &&
            (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f);

        if (reached && Time.time >= nextRepathTime)
        {
            if (TryGetRandomNavMeshPoint(transform.position, wanderRadius, out Vector3 target))
                agent.SetDestination(target);

            ScheduleNextRepath();
        }
    }

    private static bool TryGetRandomNavMeshPoint(Vector3 origin, float radius, out Vector3 result)
    {
        for (int i = 0; i < 12; i++)
        {
            Vector3 random = origin + Random.insideUnitSphere * radius;
            random.y = origin.y;

            if (NavMesh.SamplePosition(random, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = origin;
        return false;
    }

    private void ScheduleNextRepath()
    {
        nextRepathTime = Time.time + Random.Range(repathIntervalMin, repathIntervalMax);
    }

    // -----------------------------------------------------
    // Animation
    // -----------------------------------------------------
    private void UpdateAnimation()
    {
        if (!animator || !agent) return;

        float speed = agent.velocity.magnitude;
        bool shouldWalk;

        if (isWalkingCached)
            shouldWalk = speed > Mathf.Max(0.001f, walkSpeedThreshold - walkSpeedHysteresis);
        else
            shouldWalk = speed > walkSpeedThreshold;

        if (shouldWalk != isWalkingCached)
        {
            isWalkingCached = shouldWalk;
            animator.SetBool(isWalkingHash, isWalkingCached);
        }
    }

    // -----------------------------------------------------
    // Reveal timer
    // -----------------------------------------------------
    private void HandleRevealTimer()
    {
        if (!IsRevealed) return;

        revealTimer -= Time.deltaTime;
        if (revealTimer <= 0f)
            IsRevealed = false;
    }
}
