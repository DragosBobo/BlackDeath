using UnityEngine;

public class CursorHider : MonoBehaviour
{
    [Header("Cursor Settings")]
    public bool hideCursorOnStart;

    void Start()
    {
        ApplyCursorState(hideCursorOnStart);
    }

    public void ApplyCursorState(bool hide)
    {
        Cursor.visible = !hide;
    }
}