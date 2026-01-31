using UnityEngine;

public class CursorSetter : MonoBehaviour
{
    [Header("Cursor")]
    public Texture2D cursorTexture;

    [Tooltip("Punctul exact al click-ului în cursor (pixeli).")]
    public Vector2 hotspot = Vector2.zero;

    public CursorMode cursorMode = CursorMode.Auto;

    void Start()
    {
        SetCursor();
    }

    public void SetCursor()
    {
        Cursor.SetCursor(cursorTexture, hotspot, cursorMode);
    }

    public void ResetCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, cursorMode);
    }
}
