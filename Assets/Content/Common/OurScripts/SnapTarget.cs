using UnityEngine;

public class SnapTarget : MonoBehaviour
{
    public RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetCompleted()
    {
        // optional: play a sound, particle effect, etc.
        Debug.Log(gameObject.name + " completed!");
    }
}