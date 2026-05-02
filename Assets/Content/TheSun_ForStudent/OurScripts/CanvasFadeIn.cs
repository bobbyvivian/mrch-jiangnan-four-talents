using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CanvasFadeIn : MonoBehaviour
{
    [Header("Fade Settings")]
    public float fadeDuration = 1f;
    public float delayBeforeFade = 0f;
    public bool playOnAwake = true;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        // CanvasGroup is the cleanest way to fade UI elements
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Start()
    {
        if (playOnAwake)
        {
            canvasGroup.alpha = 0f;
            StartCoroutine(FadeInRoutine());
        }
    }

    public void TriggerFadeIn()
    {
        StopAllCoroutines();
        canvasGroup.alpha = 0f;
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        if (delayBeforeFade > 0f)
            yield return new WaitForSeconds(delayBeforeFade);

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }
}