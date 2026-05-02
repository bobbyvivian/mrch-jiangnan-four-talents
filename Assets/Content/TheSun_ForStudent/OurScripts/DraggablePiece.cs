using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class DraggablePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 startPosition;
    private bool isPlaced = false;
    private Vector2 dragOffset;   // ← added here as a class field

    [Header("Snap Settings")]
    public SnapTarget snapTarget;
    public float snapDistance = 80f;
    public PuzzleManager puzzleManager;

    [Header("Events")]
    public UnityEvent onPiecePlaced;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>().rootCanvas;
        startPosition = rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced) return;
        transform.SetAsLastSibling();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            null,
            out Vector2 localPoint
        );
        dragOffset = rectTransform.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            null,
            out Vector2 localPoint
        );
        rectTransform.anchoredPosition = localPoint + dragOffset;  // ← apply offset here
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        float dist = Vector2.Distance(
            rectTransform.position,
            snapTarget.rectTransform.position
        );
        if (dist <= snapDistance)
        {
            SnapToTarget();
        }
    }

    void SnapToTarget()
    {
        isPlaced = true;
        rectTransform.position = snapTarget.rectTransform.position;
        snapTarget.SetCompleted();
        onPiecePlaced.Invoke();
        puzzleManager.PieceCompleted();
    }
}