using UnityEngine;

public class CharacterProximity : MonoBehaviour
{
    [SerializeField] RingDrawer myRing;
    [SerializeField] bool isPlayer = false;

    Transform myRoot;

    void Start()
    {
        myRoot = transform.root;

        // Find my ring anywhere under my character root (siblings included)
        if (!myRing)
            myRing = myRoot.GetComponentInChildren<RingDrawer>(true);

        if (myRing)
        {
            myRing.SetColor(isPlayer ? Color.green : Color.red);
            myRing.SetVisible(false); // hidden until collision
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var otherProx = other.GetComponent<CharacterProximity>();
        if (!otherProx) return;

        var otherRoot = otherProx.transform.root;

        // ignore self
        if (otherRoot == myRoot) return;

        // Only player drives show/hide
        if (!isPlayer) return;

        // Show player's ring (green)
        if (myRing)
        {
            myRing.SetColor(Color.green);
            myRing.SetVisible(true);
        }

        var npcFill = otherRoot.GetComponentInChildren<RingFillController>(true);
        var npcRing = otherRoot.GetComponentInChildren<RingDrawer>(true);
        if (npcRing)
        {
            npcRing.SetColor(Color.red);
            npcRing.SetVisible(true);
        }

        if (npcFill && npcFill.Completed)
        {
            // Already completed: show NPC green ring, do NOT retrigger
            if (npcRing)
            {
                npcRing.SetColor(Color.green);
                npcRing.SetVisible(true);
            }
        }
        else
        {
            // Not completed: NPC ring red and start fill
            if (npcRing)
            {
                npcRing.SetColor(Color.red);
                npcRing.SetVisible(true);
            }

            if (npcFill)
            {
                npcFill.startColor = Color.red;
                npcFill.endColor = Color.green;

                npcFill.ResetVisual(); // allowed because not completed
                npcFill.OnFillComplete = () =>
                {
                    if (npcRing) npcRing.SetColor(Color.green);
                };
                npcFill.StartFill();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        var otherProx = other.GetComponent<CharacterProximity>();
        if (!otherProx) return;

        var otherRoot = otherProx.transform.root;
        if (otherRoot == myRoot) return;

        if (!isPlayer) return;

        // Hide player's ring
        if (myRing) myRing.SetVisible(false);

        // Hide NPC ring
        var npcRing = otherRoot.GetComponentInChildren<RingDrawer>(true);
        if (npcRing) npcRing.SetVisible(false);

        var npcFill = otherRoot.GetComponentInChildren<RingFillController>(true);
        if (npcFill) npcFill.ResetVisual();
    }
}
