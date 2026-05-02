using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace YourProject.AR
{
    /// <summary>
    /// Drives the "Old TV / 1930s" effect on the modified ARKitBackground shader.
    /// Provides immediate toggling and smooth UniTask-based transitions in either direction.
    ///
    /// Usage:
    ///   1. Modify the ARCameraBackground material (or any material using the modified shader).
    ///   2. Drag that material into _material on this component.
    ///   3. Call EnableImmediate / DisableImmediate / TransitionToEffectAsync / TransitionToNormalAsync.
    /// </summary>
    [DisallowMultipleComponent]
    public class ARKitOldTVEffectController : MonoBehaviour
    {
        // -------------------- Material --------------------
        [TitleGroup("Material")]
        [SerializeField, Required]
        [InfoBox("Assign the material instance used by ARCameraBackground (or any material using the modified ARKitBackground shader).")]
        private Material _material;

        // -------------------- Effect Parameters --------------------
        [TitleGroup("Effect Parameters")]
        [LabelText("Sepia (0=Gray, 1=Sepia)"), PropertyRange(0f, 1f)]
        [OnValueChanged(nameof(PushParameters), IncludeChildren = true)]
        [SerializeField] private float _sepiaTint = 0.7f;

        [LabelText("Contrast"), PropertyRange(0f, 3f)]
        [OnValueChanged(nameof(PushParameters))]
        [SerializeField] private float _contrast = 1.4f;

        [LabelText("Brightness"), PropertyRange(-1f, 1f)]
        [OnValueChanged(nameof(PushParameters))]
        [SerializeField] private float _brightness = -0.05f;

        [LabelText("Grain Strength"), PropertyRange(0f, 1f)]
        [OnValueChanged(nameof(PushParameters))]
        [SerializeField] private float _grainStrength = 0.45f;

        [LabelText("Grain Size"), PropertyRange(50f, 2000f)]
        [OnValueChanged(nameof(PushParameters))]
        [SerializeField] private float _grainSize = 600f;

        [LabelText("Vignette Strength"), PropertyRange(0f, 2f)]
        [OnValueChanged(nameof(PushParameters))]
        [SerializeField] private float _vignetteStrength = 1.2f;

        [LabelText("Vignette Softness"), PropertyRange(0.05f, 2f)]
        [OnValueChanged(nameof(PushParameters))]
        [SerializeField] private float _vignetteSoftness = 0.55f;

        [LabelText("Scanline Strength"), PropertyRange(0f, 1f)]
        [OnValueChanged(nameof(PushParameters))]
        [SerializeField] private float _scanlineStrength = 0.25f;

        [LabelText("Scanline Count"), PropertyRange(50f, 2000f)]
        [OnValueChanged(nameof(PushParameters))]
        [SerializeField] private float _scanlineCount = 600f;

        [LabelText("Flicker Strength"), PropertyRange(0f, 1f)]
        [OnValueChanged(nameof(PushParameters))]
        [SerializeField] private float _flickerStrength = 0.18f;

        [LabelText("Flicker Speed"), PropertyRange(0f, 30f)]
        [OnValueChanged(nameof(PushParameters))]
        [SerializeField] private float _flickerSpeed = 14f;

        [LabelText("Scratch Strength"), PropertyRange(0f, 1f)]
        [OnValueChanged(nameof(PushParameters))]
        [SerializeField] private float _scratchStrength = 0.35f;

        [LabelText("Vertical Jitter"), PropertyRange(0f, 0.05f)]
        [OnValueChanged(nameof(PushParameters))]
        [SerializeField] private float _jitterStrength = 0.004f;

        // -------------------- Transition --------------------
        [TitleGroup("Transition")]
        [SerializeField, MinValue(0.01f), SuffixLabel("sec", true)]
        private float _defaultDuration = 1.5f;

        [SerializeField]
        private AnimationCurve _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField, ToggleLeft]
        private bool _useUnscaledTime = false;

        // -------------------- Runtime State --------------------
        [TitleGroup("Runtime State")]
        [ShowInInspector, ReadOnly, ProgressBar(0f, 1f)]
        private float _currentStrength;

        [ShowInInspector, ReadOnly]
        private bool IsKeywordOn => _material != null && _material.IsKeywordEnabled(KEYWORD);

        [ShowInInspector, ReadOnly]
        private bool IsTransitioning => _transitionCts != null;

        // -------------------- Constants / IDs --------------------
        private const string KEYWORD = "ARKIT_OLD_TV_EFFECT";

        private static readonly int ID_Strength          = Shader.PropertyToID("_OldTVStrength");
        private static readonly int ID_SepiaTint         = Shader.PropertyToID("_SepiaTint");
        private static readonly int ID_Contrast          = Shader.PropertyToID("_Contrast");
        private static readonly int ID_Brightness        = Shader.PropertyToID("_Brightness");
        private static readonly int ID_GrainStrength     = Shader.PropertyToID("_GrainStrength");
        private static readonly int ID_GrainSize         = Shader.PropertyToID("_GrainSize");
        private static readonly int ID_VignetteStrength  = Shader.PropertyToID("_VignetteStrength");
        private static readonly int ID_VignetteSoftness  = Shader.PropertyToID("_VignetteSoftness");
        private static readonly int ID_ScanlineStrength  = Shader.PropertyToID("_ScanlineStrength");
        private static readonly int ID_ScanlineCount     = Shader.PropertyToID("_ScanlineCount");
        private static readonly int ID_FlickerStrength   = Shader.PropertyToID("_FlickerStrength");
        private static readonly int ID_FlickerSpeed      = Shader.PropertyToID("_FlickerSpeed");
        private static readonly int ID_ScratchStrength   = Shader.PropertyToID("_ScratchStrength");
        private static readonly int ID_JitterStrength    = Shader.PropertyToID("_JitterStrength");

        private CancellationTokenSource _transitionCts;

        // -------------------- Lifecycle --------------------
        private void Awake()
        {
            PushParameters();
            ApplyStrength(0f);
            DisableKeyword();
        }

        private void OnDestroy()
        {
            CancelTransition();
        }

        // -------------------- Inspector buttons --------------------
        [TitleGroup("Controls")]
        [HorizontalGroup("Controls/Immediate"), Button("ON (Immediate)"), GUIColor(0.6f, 1f, 0.6f)]
        public void EnableImmediate()
        {
            CancelTransition();
            EnableKeyword();
            ApplyStrength(1f);
        }

        [HorizontalGroup("Controls/Immediate"), Button("OFF (Immediate)"), GUIColor(1f, 0.6f, 0.6f)]
        public void DisableImmediate()
        {
            CancelTransition();
            ApplyStrength(0f);
            DisableKeyword();
        }

        [HorizontalGroup("Controls/Transition"), Button("Fade IN"), GUIColor(0.7f, 0.9f, 1f)]
        private void Btn_FadeIn() => TransitionToEffectAsync().Forget();

        [HorizontalGroup("Controls/Transition"), Button("Fade OUT"), GUIColor(1f, 0.9f, 0.7f)]
        private void Btn_FadeOut() => TransitionToNormalAsync().Forget();

        // -------------------- Public API --------------------
        /// <summary>Smoothly transition from current state to fully active effect.</summary>
        public UniTask TransitionToEffectAsync(float duration = -1f, CancellationToken ct = default)
            => TransitionToAsync(1f, duration, ct);

        /// <summary>Smoothly transition from current state back to no effect.</summary>
        public UniTask TransitionToNormalAsync(float duration = -1f, CancellationToken ct = default)
            => TransitionToAsync(0f, duration, ct);

        /// <summary>
        /// Smoothly transition strength to <paramref name="targetStrength"/> (0..1).
        /// Pass duration &lt; 0 to use the inspector default. Cancellable.
        /// </summary>
        public async UniTask TransitionToAsync(float targetStrength, float duration = -1f, CancellationToken ct = default)
        {
            if (_material == null)
            {
                Debug.LogError($"[{nameof(ARKitOldTVEffectController)}] Material is not assigned.", this);
                return;
            }

            targetStrength = Mathf.Clamp01(targetStrength);
            if (duration < 0f) duration = _defaultDuration;

            CancelTransition();
            _transitionCts = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy(), ct);
            var token = _transitionCts.Token;

            // Make sure the keyword is on while there's any non-zero strength to render.
            if (targetStrength > 0f || _currentStrength > 0f)
                EnableKeyword();

            float startStrength = _currentStrength;

            try
            {
                if (duration <= 0f)
                {
                    ApplyStrength(targetStrength);
                }
                else
                {
                    float elapsed = 0f;
                    while (elapsed < duration)
                    {
                        token.ThrowIfCancellationRequested();
                        elapsed += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                        float t      = Mathf.Clamp01(elapsed / duration);
                        float eased  = _curve.Evaluate(t);
                        ApplyStrength(Mathf.Lerp(startStrength, targetStrength, eased));
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                    }
                    ApplyStrength(targetStrength);
                }

                // Clean up keyword if we landed at zero so the shader skips the work.
                if (Mathf.Approximately(targetStrength, 0f))
                    DisableKeyword();
            }
            catch (OperationCanceledException)
            {
                // Expected on cancel: leave state where it is so a new transition can pick up smoothly.
            }
            finally
            {
                // Only clear if THIS run's CTS is still the active one.
                if (_transitionCts != null && _transitionCts.Token == token)
                {
                    _transitionCts.Dispose();
                    _transitionCts = null;
                }
            }
        }

        /// <summary>Push all inspector-configured parameters to the material in one go.</summary>
        [TitleGroup("Controls"), Button("Push Parameters To Material")]
        public void PushParameters()
        {
            if (_material == null) return;
            _material.SetFloat(ID_SepiaTint,        _sepiaTint);
            _material.SetFloat(ID_Contrast,         _contrast);
            _material.SetFloat(ID_Brightness,       _brightness);
            _material.SetFloat(ID_GrainStrength,    _grainStrength);
            _material.SetFloat(ID_GrainSize,        _grainSize);
            _material.SetFloat(ID_VignetteStrength, _vignetteStrength);
            _material.SetFloat(ID_VignetteSoftness, _vignetteSoftness);
            _material.SetFloat(ID_ScanlineStrength, _scanlineStrength);
            _material.SetFloat(ID_ScanlineCount,    _scanlineCount);
            _material.SetFloat(ID_FlickerStrength,  _flickerStrength);
            _material.SetFloat(ID_FlickerSpeed,     _flickerSpeed);
            _material.SetFloat(ID_ScratchStrength,  _scratchStrength);
            _material.SetFloat(ID_JitterStrength,   _jitterStrength);
        }

        // -------------------- Internals --------------------
        private void ApplyStrength(float value)
        {
            _currentStrength = Mathf.Clamp01(value);
            if (_material != null)
                _material.SetFloat(ID_Strength, _currentStrength);
        }

        private void EnableKeyword()
        {
            if (_material != null && !_material.IsKeywordEnabled(KEYWORD))
                _material.EnableKeyword(KEYWORD);
        }

        private void DisableKeyword()
        {
            if (_material != null && _material.IsKeywordEnabled(KEYWORD))
                _material.DisableKeyword(KEYWORD);
        }

        private void CancelTransition()
        {
            if (_transitionCts == null) return;
            try { _transitionCts.Cancel(); } catch { /* ignore */ }
            _transitionCts.Dispose();
            _transitionCts = null;
        }
    }
}
