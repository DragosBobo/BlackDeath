using System.Collections;
using UnityEngine;
using System;



public class RingFillController : MonoBehaviour
{
    [Header("Timing")]
    public float fillTime = 1.0f;

    [Header("Ring Shape (shader units)")]
    [Range(0f, 0.5f)] public float innerRadius = 0.18f;
    [Range(0f, 0.5f)] public float outerRadiusMax = 0.28f;

    [Header("Colors")]
    public Color startColor = Color.red;
    public Color endColor = Color.green;

    MeshRenderer mr;
    Material mat;
    Coroutine routine;

    public bool Completed { get; private set; } = false;
    public Action OnFillComplete;


    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int InnerId = Shader.PropertyToID("_Inner");
    static readonly int OuterId = Shader.PropertyToID("_Outer");

    void Awake()
    {
        mr = GetComponent<MeshRenderer>();
        mat = mr.material; // unique instance
        ResetVisual();
    }

    public void ResetVisual(bool force = false)
    {
        if (Completed && !force) return; // don't reset if completed unless forced

        if (routine != null) StopCoroutine(routine);
        routine = null;

        Completed = false;
        mr.enabled = false;

        mat.SetFloat(InnerId, innerRadius);
        mat.SetFloat(OuterId, innerRadius); // empty
        mat.SetColor(ColorId, startColor);
    }

    public void StartFill()
    {
        if (Completed) return; // don't retrigger if completed

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FillRoutine());
    }

    IEnumerator FillRoutine()
    {
        mr.enabled = true;
        mat.SetFloat(InnerId, innerRadius);

        Debug.Log(fillTime);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, fillTime);
            float u = Mathf.Clamp01(t);

            float outer = Mathf.Lerp(innerRadius, outerRadiusMax, u);
            mat.SetFloat(OuterId, outer);

            Color c = Color.Lerp(startColor, endColor, u);
            mat.SetColor(ColorId, c);

            yield return null;
        }

        // fully filled and green at the end
        mat.SetFloat(OuterId, outerRadiusMax);
        mat.SetColor(ColorId, endColor);

        Completed = true;
        routine = null;

        OnFillComplete?.Invoke();
    }
}
