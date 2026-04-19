using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIMagnifier : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Serializable]
    public class HotspotStep
    {
        [Tooltip("Delay in seconds before this step runs.")]
        public float delay;
        [Tooltip("The UI object to affect in this step.")]
        public GameObject target;
        [Tooltip("The active state to apply to the target in this step.")]
        public bool setActive = true;
    }

    [Serializable]
    public class Hotspot
    {
        public string name;
        public RectTransform point;
        public float radius = 40f;
        [Tooltip("How many seconds the lens must stay on this hotspot before it is recognized.")]
        public float dwellTime = 0f;
        [Tooltip("These UI objects will be shown when the lens focuses this hotspot.")]
        public GameObject[] targets;
        public bool hideWhenNotFocused = true;
        [Tooltip("After this many seconds, the objects below will be turned off. Set 0 to disable timed hiding.")]
        public float autoHideDelay = 0f;
        [Tooltip("These UI objects will be turned off after the delay. If left empty, the Targets array will be used instead.")]
        public GameObject[] autoHideTargets;
        [Tooltip("Optional ordered sequence to run when the lens enters this hotspot.")]
        public HotspotStep[] onFocusSequence;
        [Tooltip("Optional ordered sequence to run when the lens leaves this hotspot.")]
        public HotspotStep[] onLoseFocusSequence;

        [NonSerialized] public bool isActive;
        [NonSerialized] public bool hasBeenRecognized;
        [NonSerialized] public bool hasTriggeredOnce;
        [NonSerialized] public Coroutine autoHideRoutine;
        [NonSerialized] public Coroutine sequenceRoutine;
        [NonSerialized] public float dwellTimer;
    }

    [Header("Drag")]
    [SerializeField] private bool bringToFrontOnDrag = true;
    [SerializeField] private float returnDelay = 5f;

    [Header("Magnify")]
    [SerializeField] private Image sourceImage;
    [SerializeField] private RectTransform sourceRect;
    [SerializeField] private Image magnifiedImage;
    [SerializeField] private float zoom = 2f;
    [SerializeField] private bool syncSpriteOnAwake = true;

    [Header("Hotspots")]
    [SerializeField] private Hotspot[] hotspots;
    [SerializeField] private bool enforceHotspotOrder = true;

    private RectTransform _rectTransform;
    private RectTransform _parentRect;
    private Canvas _rootCanvas;
    private Vector2 _startAnchoredPosition;
    private Vector2 _dragOffset;
    private float _lastMoveTime;
    private bool _isDragging;
    private int _nextHotspotIndexToRecognize;
    private int _runningHotspotIndex = -1;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _parentRect = _rectTransform.parent as RectTransform;
        _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        _startAnchoredPosition = _rectTransform.anchoredPosition;
        _lastMoveTime = Time.unscaledTime;

        if (sourceImage != null && sourceRect == null)
        {
            sourceRect = sourceImage.rectTransform;
        }

        if (syncSpriteOnAwake && sourceImage != null && magnifiedImage != null)
        {
            magnifiedImage.sprite = sourceImage.sprite;
            magnifiedImage.type = sourceImage.type;
            magnifiedImage.preserveAspect = sourceImage.preserveAspect;
        }

        UpdateMagnifiedView();
        UpdateHotspots();
    }

    private void Update()
    {
        if (!_isDragging && returnDelay > 0f && Time.unscaledTime - _lastMoveTime >= returnDelay)
        {
            if ((_rectTransform.anchoredPosition - _startAnchoredPosition).sqrMagnitude > 0.01f)
            {
                _rectTransform.anchoredPosition = _startAnchoredPosition;
                _lastMoveTime = Time.unscaledTime;
                UpdateMagnifiedView();
                UpdateHotspots();
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_parentRect == null) return;

        _isDragging = true;
        if (bringToFrontOnDrag)
        {
            transform.SetAsLastSibling();
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        _dragOffset = _rectTransform.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_parentRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        _rectTransform.anchoredPosition = localPoint + _dragOffset;
        _lastMoveTime = Time.unscaledTime;

        UpdateMagnifiedView();
        UpdateHotspots();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        _lastMoveTime = Time.unscaledTime;
    }

    private void UpdateMagnifiedView()
    {
        if (sourceRect == null || magnifiedImage == null) return;
        if (zoom < 1f) zoom = 1f;

        RectTransform magnifiedRect = magnifiedImage.rectTransform;
        Vector2 focusLocalPoint = GetLensCenterOnSource();
        Vector2 normalized = LocalPointToNormalized(sourceRect.rect, focusLocalPoint);

        Vector2 sourceSize = sourceRect.rect.size;
        Vector2 magnifiedSize = sourceSize * zoom;

        magnifiedRect.anchorMin = new Vector2(0.5f, 0.5f);
        magnifiedRect.anchorMax = new Vector2(0.5f, 0.5f);
        magnifiedRect.pivot = new Vector2(0.5f, 0.5f);
        magnifiedRect.sizeDelta = magnifiedSize;
        magnifiedRect.anchoredPosition = -new Vector2(
            (normalized.x - 0.5f) * magnifiedSize.x,
            (normalized.y - 0.5f) * magnifiedSize.y
        );
    }

    private void UpdateHotspots()
    {
        if (sourceRect == null || hotspots == null) return;
        if (_runningHotspotIndex != -1) return;
        if (_nextHotspotIndexToRecognize < 0 || _nextHotspotIndexToRecognize >= hotspots.Length) return;

        Vector2 focusLocalPoint = GetLensCenterOnSource();
        Hotspot hotspot = hotspots[_nextHotspotIndexToRecognize];
        if (hotspot == null || hotspot.point == null) return;
        if (hotspot.hasTriggeredOnce) return;

        Vector2 hotspotLocalPoint = WorldPointToSourceLocal(hotspot.point.position);
        bool inRange = Vector2.Distance(focusLocalPoint, hotspotLocalPoint) <= hotspot.radius;
        if (!inRange)
        {
            hotspot.dwellTimer = 0f;
            return;
        }

        if (hotspot.dwellTime > 0f)
        {
            hotspot.dwellTimer += Time.unscaledDeltaTime;
            if (hotspot.dwellTimer < hotspot.dwellTime) return;
        }

        hotspot.dwellTimer = 0f;
        StartHotspotFlow(_nextHotspotIndexToRecognize, hotspot);
    }

    private void StartHotspotFlow(int hotspotIndex, Hotspot hotspot)
    {
        hotspot.isActive = true;
        hotspot.hasBeenRecognized = true;
        hotspot.hasTriggeredOnce = true;
        _runningHotspotIndex = hotspotIndex;

        if (hotspot.sequenceRoutine != null)
        {
            StopCoroutine(hotspot.sequenceRoutine);
            hotspot.sequenceRoutine = null;
        }

        if (hotspot.autoHideRoutine != null)
        {
            StopCoroutine(hotspot.autoHideRoutine);
            hotspot.autoHideRoutine = null;
        }

        hotspot.sequenceRoutine = StartCoroutine(RunHotspotFlow(hotspotIndex, hotspot));
    }

    private IEnumerator RunHotspotFlow(int hotspotIndex, Hotspot hotspot)
    {
        if (HasSequence(hotspot.onFocusSequence))
        {
            yield return RunSequence(hotspot.onFocusSequence);
        }
        else
        {
            SetTargetsActive(hotspot.targets, true);

            if (hotspot.autoHideDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(hotspot.autoHideDelay);

                GameObject[] delayedTargets = hotspot.autoHideTargets != null && hotspot.autoHideTargets.Length > 0
                    ? hotspot.autoHideTargets
                    : hotspot.targets;

                SetTargetsActive(delayedTargets, false);
            }
        }

        hotspot.isActive = false;
        hotspot.sequenceRoutine = null;
        hotspot.autoHideRoutine = null;
        _runningHotspotIndex = -1;

        if (enforceHotspotOrder)
        {
            _nextHotspotIndexToRecognize = hotspotIndex + 1;
        }
    }

    private static bool HasSequence(HotspotStep[] sequence)
    {
        return sequence != null && sequence.Length > 0;
    }

    private IEnumerator RunSequence(HotspotStep[] sequence)
    {
        for (int i = 0; i < sequence.Length; i++)
        {
            HotspotStep step = sequence[i];
            if (step == null) continue;

            if (step.delay > 0f)
            {
                yield return new WaitForSecondsRealtime(step.delay);
            }

            if (step.target != null)
            {
                step.target.SetActive(step.setActive);
            }
        }
    }

    private static void SetTargetsActive(GameObject[] targets, bool active)
    {
        if (targets == null) return;

        for (int i = 0; i < targets.Length; i++)
        {
            GameObject target = targets[i];
            if (target == null) continue;
            target.SetActive(active);
        }
    }

    private Vector2 GetLensCenterOnSource()
    {
        Vector3 lensCenterWorld = _rectTransform.TransformPoint(_rectTransform.rect.center);
        return WorldPointToSourceLocal(lensCenterWorld);
    }

    private Vector2 WorldPointToSourceLocal(Vector3 worldPoint)
    {
        Camera eventCamera = GetEventCamera();
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPoint);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            sourceRect,
            screenPoint,
            eventCamera,
            out Vector2 localPoint
        );

        return localPoint;
    }

    private Camera GetEventCamera()
    {
        if (_rootCanvas == null) return null;
        return _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;
    }

    private static Vector2 LocalPointToNormalized(Rect rect, Vector2 localPoint)
    {
        float x = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float y = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
        return new Vector2(x, y);
    }

    public void ResetToStartPosition()
    {
        _rectTransform.anchoredPosition = _startAnchoredPosition;
        _lastMoveTime = Time.unscaledTime;
        UpdateMagnifiedView();
        UpdateHotspots();
    }

    public void ResetHotspotRecognitionOrder()
    {
        if (hotspots == null) return;

        _nextHotspotIndexToRecognize = 0;
        _runningHotspotIndex = -1;

        for (int i = 0; i < hotspots.Length; i++)
        {
            Hotspot hotspot = hotspots[i];
            if (hotspot == null) continue;

            hotspot.hasBeenRecognized = false;
            hotspot.hasTriggeredOnce = false;
            hotspot.isActive = false;
            hotspot.dwellTimer = 0f;

            if (hotspot.sequenceRoutine != null)
            {
                StopCoroutine(hotspot.sequenceRoutine);
                hotspot.sequenceRoutine = null;
            }

            if (hotspot.autoHideRoutine != null)
            {
                StopCoroutine(hotspot.autoHideRoutine);
                hotspot.autoHideRoutine = null;
            }
        }
    }
}
