using UnityEngine;
using UnityEngine.UI;

public class TransparentClickIgnore : MonoBehaviour, ICanvasRaycastFilter
{
    private Image image;
    private RectTransform rectTransform;

    void Awake()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        // Convert screen point to local rect position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, screenPoint, eventCamera, out Vector2 localPoint
        );

        // Get the sprite's texture coordinates
        Rect rect = rectTransform.rect;
        Vector2 normalized = new Vector2(
            (localPoint.x - rect.x) / rect.width,
            (localPoint.y - rect.y) / rect.height
        );

        // Sample the pixel alpha at that point
        Texture2D texture = image.sprite.texture;
        float alpha = texture.GetPixelBilinear(normalized.x, normalized.y).a;

        return alpha > 0.1f; // only register hit if pixel is visible
    }
}