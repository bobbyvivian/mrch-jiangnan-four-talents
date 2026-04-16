using TMPro;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class TextParticleEffect : MonoBehaviour
{
    [Header("References")]
    public TextMeshPro tmpText;

    [Header("Particle Density")]
    [Range(5, 50)]
    public int particlesPerCharacter = 20;

    [Header("Wiggle Settings")]
    [Range(0f, 0.1f)]
    public float wiggleRadius = 0.03f;
    [Range(0.5f, 5f)]
    public float wiggleSpeed = 1.5f;

    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    private Vector3[] basePositions;
    private float[] timeOffsets;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        BuildParticles();
    }

    void BuildParticles()
    {
        tmpText.ForceMeshUpdate();
        TMP_TextInfo textInfo = tmpText.textInfo;

        var posList = new System.Collections.Generic.List<Vector3>();

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mi = charInfo.materialReferenceIndex;
            int vi = charInfo.vertexIndex;
            Vector3[] verts = textInfo.meshInfo[mi].vertices;

            Vector3 bl = tmpText.transform.TransformPoint(verts[vi]);
            Vector3 tl = tmpText.transform.TransformPoint(verts[vi + 1]);
            Vector3 tr = tmpText.transform.TransformPoint(verts[vi + 2]);
            Vector3 br = tmpText.transform.TransformPoint(verts[vi + 3]);

            for (int j = 0; j < particlesPerCharacter; j++)
            {
                float u = Random.value;
                float v = Random.value;
                Vector3 point = Vector3.Lerp(
                    Vector3.Lerp(bl, br, u),
                    Vector3.Lerp(tl, tr, u), v
                );
                point += Camera.main.transform.forward * 0.1f; // nudge toward camera
                posList.Add(point);
            }
        }

        // Set up particle system
        var main = ps.main;
        main.maxParticles = posList.Count;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = Mathf.Infinity;
        main.startSpeed = 0;
        main.startSize = 0.025f;

        // Stop auto-emission
        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;

        ps.Play();

        // Emit all particles at once
        var emitParams = new ParticleSystem.EmitParams();
        emitParams.startLifetime = float.MaxValue;
        emitParams.velocity = Vector3.zero;

        foreach (var pos in posList)
        {
            emitParams.position = pos;
            ps.Emit(emitParams, 1);
        }

        // Cache base positions and random time offsets for wiggle
        basePositions = posList.ToArray();
        timeOffsets = new float[basePositions.Length];
        particles = new ParticleSystem.Particle[basePositions.Length];

        for (int i = 0; i < timeOffsets.Length; i++)
            timeOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (particles == null || basePositions == null) return;

        int count = ps.GetParticles(particles);
        Vector3 camOffset = Camera.main.transform.forward * -0.1f; // recalculated every frame


        for (int i = 0; i < count && i < basePositions.Length; i++)
        {
            // Each particle wiggles on its own phase offset so they don't
            // all move together (that would look like a single mass shifting)
            float t = Time.time * wiggleSpeed + timeOffsets[i];

            // Use two sine waves on different axes + frequencies for
            // organic-looking movement rather than a circular orbit
            float ox = Mathf.Sin(t * 1.00f + timeOffsets[i] * 1.3f) * wiggleRadius;
            float oy = Mathf.Sin(t * 1.37f + timeOffsets[i] * 0.9f) * wiggleRadius;
            float oz = Mathf.Sin(t * 0.83f + timeOffsets[i] * 1.7f) * wiggleRadius * 0.3f;

            particles[i].position = basePositions[i] + new Vector3(ox, oy, oz);
        }

        ps.SetParticles(particles, count);
    }
}