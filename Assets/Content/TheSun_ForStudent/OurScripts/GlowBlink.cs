using UnityEngine;

public class GlowBlink : MonoBehaviour
{
    [Header("Intensity Settings")]
    public float maxIntensity = 2f;
    public float minIntensity = 0.2f;

    [Header("Speed Settings")]
    public float pulseSpeed = 1f;

    private Light _light;

    void Start()
    {
        _light = GetComponent<Light>();
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) / 2f;
        _light.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
    }
}