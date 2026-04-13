using UnityEngine;
using UnityEngine.Video;

public class TheaterVideoTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Behavior")]
    [SerializeField] private bool triggerOnce = true;

    private bool _hasTriggered;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && _hasTriggered) return;
        if (!IsPlayerCollider(other)) return;
        if (videoPlayer == null) return;

        _hasTriggered = true;
        videoPlayer.Stop();
        videoPlayer.Play();
    }

    private static bool IsPlayerCollider(Collider other)
    {
        return other.CompareTag("Player") || other.CompareTag("MainCamera");
    }

    public void ResetTriggerState()
    {
        _hasTriggered = false;
    }
}
