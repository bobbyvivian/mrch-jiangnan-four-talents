using TMPro;
using UnityEngine;

[ExecuteInEditMode]
public class CurvedText : MonoBehaviour
{
    public TextMeshProUGUI tmpText;

    [Range(-1000f, 1000f)]
    public float radius = 300f;

    void Start()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        CurveText();
    }

    void CurveText()
    {
        tmpText.ForceMeshUpdate();
        TMP_TextInfo textInfo = tmpText.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mi = charInfo.materialReferenceIndex;
            int vi = charInfo.vertexIndex;
            Vector3[] verts = textInfo.meshInfo[mi].vertices;

            // Find the center of this character
            Vector3 charCenter = (verts[vi] + verts[vi + 1] + verts[vi + 2] + verts[vi + 3]) / 4f;

            // How far along the arc is this character (based on its X position)
            float angle = -(charCenter.x / radius) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            // Where should this character sit on the arc
            float rad = angle * Mathf.Deg2Rad;
            Vector3 arcPos = new Vector3(
                Mathf.Sin(-rad) * radius,
                radius * (Mathf.Cos(rad) - 1f),
                0f
            );

            // Rotate each vertex around the character center, then move to arc position
            for (int j = 0; j < 4; j++)
            {
                Vector3 offset = verts[vi + j] - charCenter;
                verts[vi + j] = rotation * offset + charCenter + arcPos - new Vector3(charCenter.x, 0f, 0f);
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            TMP_MeshInfo meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            tmpText.UpdateGeometry(meshInfo.mesh, i);
        }
    }
}