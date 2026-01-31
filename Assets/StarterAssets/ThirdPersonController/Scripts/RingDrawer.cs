using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RingDrawer : MonoBehaviour
{
    public int segments = 64;
    public float radius = 0.8f;
    public Color color = Color.green;

    LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = false;
        lr.positionCount = segments;

        // Make material unique per ring (avoids shared-state weirdness)
        if (lr.material) lr.material = new Material(lr.material);
        lr.enabled = false;

        ApplyColor(color);
        Draw();
    }

    public void SetColor(Color c)
    {
        color = c;
        ApplyColor(c);
    }

    public void SetVisible(bool visible)
    {
        if (!lr) lr = GetComponent<LineRenderer>();
        lr.enabled = visible;
    }

    void ApplyColor(Color c)
    {
        // Force gradient to a solid color
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        lr.colorGradient = grad;

        // Also set material color (some shaders rely on this)
        if (lr.material && lr.material.HasProperty("_Color"))
            lr.material.SetColor("_Color", c);
    }

    void Draw()
    {
        for (int i = 0; i < segments; i++)
        {
            float a = (float)i / segments * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }
    }
}
