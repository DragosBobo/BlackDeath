using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.Rendering.DebugUI;

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
    [SerializeField] private float graceRemaining = 0f;
    [SerializeField] private float graceDuration = 0f;
    [SerializeField] private RingFillController graceRing;
    public static int TotalPeasants { get; private set; }
    public static int SickPeasants { get; private set; }
    public static int ContagiousPeasants { get; private set; }


    [Header("Spreading (Only when contagious)")]
    [SerializeField] private float spreadRadius = 1.6f;
    [SerializeField] private float spreadRatePerTick = 0.04f;
    [SerializeField] private float spreadTickInterval = 0.5f;

    [Header("Reveal (Called by lamp)")]
    [SerializeField] private float revealHoldSeconds = 0.15f;
    public bool IsRevealed { get; private set; }
    public bool IsGraceActive => graceRemaining > 0f;


    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string isWalkingParam = "isWalking";
    [SerializeField] private float walkSpeedThreshold = 0.08f;
    [SerializeField] private float walkSpeedHysteresis = 0.03f;

    public bool IsSick => infectionProgress >= sickThreshold;
    public bool IsContagious => infectionProgress >= contagiousThreshold;
    public float InfectionProgress => infectionProgress;

    private bool countedSick;
    private bool countedContagious;



    private NavMeshAgent agent;
    private float nextRepathTime;
    private float nextSpreadTickTime;
    private float revealTimer;

    private readonly Collider[] spreadHits = new Collider[32];

    private int isWalkingHash;
    private bool isWalkingCached;

    // Cached renderer for debug coloring
    private Renderer cachedRenderer;

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

        if (countedSick) SickPeasants = Mathf.Max(0, SickPeasants - 1);
        if (countedContagious) ContagiousPeasants = Mathf.Max(0, ContagiousPeasants - 1);
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
        cachedRenderer = GetComponentInChildren<Renderer>();

        isWalkingHash = Animator.StringToHash(isWalkingParam);

        if (startRandomized)
            infectionProgress = Mathf.Clamp01(infectionProgress + Random.Range(0f, 0.15f));

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
        //UpdateDebugColor(); // <-- NEW
    }

    // -----------------------------------------------------
    // DEBUG: Infection Color Visualization
    // -----------------------------------------------------
    //private void UpdateDebugColor()
    //{
        //if (!cachedRenderer) return;

        //if (IsContagious)
        //{
           // cachedRenderer.material.color = Color.red;
        //}
        //else if (IsSick)
        //{
            //cachedRenderer.material.color = Color.yellow;
        //}
        //else
        //{
          //  cachedRenderer.material.color = Color.white;
        //}
    //}

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
                graceRing.SetVisible(false); // optional: hide when grace ends

            return; // grace active -> stop infection tick
        }

        infectionProgress = Mathf.Clamp01(infectionProgress + baseInfectionRatePerSecond * dt);
        UpdateCountersIfCrossed();

    }

    public void AddInfection(float amount)
    {
        if (amount <= 0f) return;
        infectionProgress = Mathf.Clamp01(infectionProgress + amount);
        UpdateCountersIfCrossed();

    }

    public void Cure(float seconds)
    {
        if (seconds <= 0f) return;

        // Start/extend grace
        graceDuration = Mathf.Max(graceDuration, seconds);
        graceRemaining = Mathf.Max(graceRemaining, seconds);

        Debug.Log(graceDuration + " " + graceRemaining);

        // show full ring immediately
        if (graceRing)
        {
            graceRing.SetVisible(true);     // if you added SetVisible
            graceRing.SetFill01(1f);
        }
    }


    private void UpdateCountersIfCrossed()
    {
        if (!countedSick && IsSick)
        {
            countedSick = true;
            SickPeasants++;
        }

        if (!countedContagious && IsContagious)
        {
            countedContagious = true;
            ContagiousPeasants++;
        }
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

        for (int i = 0; i < count; i++)
        {
            Collider c = spreadHits[i];
            if (!c) continue;

            PeasantController other = c.GetComponentInParent<PeasantController>();
            if (!other || other == this) continue;

            other.AddInfection(spreadRatePerTick);
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
        {
            IsRevealed = false;
        }
    }
}
