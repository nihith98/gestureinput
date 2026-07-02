using System;
using System.Collections.Generic;
using UnityEngine;
using GestureInput.Core;
using GestureInput.Unity.Registration;

namespace GestureInput.Unity
{
    /// <summary>
    /// The main-thread heart of the library. Each frame it drains the frame
    /// source, runs every registered recognizer over each perception frame, and
    /// fans the resulting events out to (a) the <see cref="OnGesture"/> C# event
    /// stream and (b) the bindable <see cref="GestureDevice"/>.
    ///
    /// Typical setup: add this and a driver component (e.g. MediapipeGestureDriver)
    /// to one GameObject; register extra recognizers in Awake/Start of your own
    /// script; the device is created automatically at the end of the first Update
    /// unless <see cref="autoCreateDevice"/> is off.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class GestureRuntime : MonoBehaviour, IGestureSink
    {
        [Tooltip("Register the built-in StaticGestureRecognizer and SwipeRecognizer on Awake.")]
        [SerializeField] private bool registerBuiltinRecognizers = true;

        [Tooltip("Create the GestureDevice automatically on the first Update. Disable to call CreateDevice() yourself after late registrations.")]
        [SerializeField] private bool autoCreateDevice = true;

        [Tooltip("Recognizers registered from ScriptableObject assets (designer-tunable).")]
        [SerializeField] private List<GestureRecognizerAsset> recognizerAssets = new List<GestureRecognizerAsset>();

        [Tooltip("Consecutive hand-absent frames before all recognizers are Reset().")]
        [SerializeField] private int handLossResetFrames = 15;

        private readonly List<GestureEvent> _collected = new List<GestureEvent>(16);
        private GestureDeviceBridge _bridge;
        private int _absentStreak;
        private bool _deviceCreated;

        public static GestureRuntime Instance { get; private set; }

        /// <summary>Every gesture event, regardless of when its recognizer was registered.</summary>
        public event Action<GestureEvent> OnGesture;

        public GestureRegistry Registry { get; } = new GestureRegistry();

        /// <summary>The frame producer. Assign before frames should flow (a driver does this in its Awake).</summary>
        public IGestureFrameSource FrameSource { get; set; }

        public UnityEngine.InputSystem.InputDevice Device => _bridge?.Device;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GestureInput] Multiple GestureRuntime instances; destroying the newer one.", this);
                Destroy(this);
                return;
            }
            Instance = this;

            _bridge = new GestureDeviceBridge();
            Registry.OnRecognizerError += (recognizer, exception) =>
                Debug.LogError($"[GestureInput] Recognizer {recognizer.GetType().Name} threw: {exception}", this);

            if (registerBuiltinRecognizers)
            {
                Registry.Register(new StaticGestureRecognizer());
                Registry.Register(new SwipeRecognizer());
            }

            foreach (var asset in recognizerAssets)
            {
                if (asset == null) continue;
                var recognizer = asset.CreateRecognizer();
                if (recognizer != null) Registry.Register(recognizer);
                else Debug.LogWarning($"[GestureInput] Recognizer asset '{asset.name}' returned null.", asset);
            }
        }

        /// <summary>
        /// Build the GestureDevice layout from all currently registered descriptors.
        /// After this, the control set is fixed; later registrations reach only the
        /// C# event stream until the device is recreated.
        /// </summary>
        public void CreateDevice()
        {
            _bridge.CreateDevice(Registry.AllDescriptors);
            _deviceCreated = true;
        }

        private void Update()
        {
            if (!_deviceCreated && autoCreateDevice && Registry.AllDescriptors.Count > 0)
                CreateDevice();

            if (FrameSource == null) return;

            while (FrameSource.TryDequeue(out var frame))
            {
                _collected.Clear();
                Registry.ProcessFrame(in frame, this);

                for (int i = 0; i < _collected.Count; i++)
                {
                    var e = _collected[i];
                    _bridge.Push(in e);
                    OnGesture?.Invoke(e);
                }

                TrackHandLoss(in frame);
            }
        }

        private void TrackHandLoss(in GestureFrame frame)
        {
            if (frame.Hand.IsPresent)
            {
                _absentStreak = 0;
                return;
            }

            _absentStreak++;
            if (_absentStreak == handLossResetFrames)
                Registry.ResetAll();
        }

        // IGestureSink — recognizers emit into this collector during ProcessFrame.
        void IGestureSink.Emit(in GestureEvent e) => _collected.Add(e);

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            _bridge?.Dispose();
            (FrameSource as IDisposable)?.Dispose();
        }
    }
}
