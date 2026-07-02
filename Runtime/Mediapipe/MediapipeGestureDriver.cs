// The driver compiles only when the MediaPipe Unity Plugin
// (com.github.homuler.mediapipe) is installed — the asmdef defines
// GESTUREINPUT_HAS_MEDIAPIPE via versionDefines when the package is present.
// Without it, this file compiles to a stub that explains the missing setup.
//
// NOTE: the plugin's Tasks C# API has shifted across releases. The type and
// member names below target the GestureRecognizer task as used by the plugin's
// official sample scene (Samples/Scenes/Tasks/Gesture Recognition). If they
// drift in a newer plugin, that sample scene is the source of truth (SCOPE §11).

using UnityEngine;
using GestureInput.Core;
using GestureInput.Unity;

#if GESTUREINPUT_HAS_MEDIAPIPE
using System;
using System.Collections;
using System.Collections.Generic;
using Mediapipe;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.Core;
using Mediapipe.Tasks.Vision.GestureRecognizer;
using Mediapipe.Unity.Experimental;   // TextureFrame
#endif

namespace GestureInput.Mediapipe
{
#if GESTUREINPUT_HAS_MEDIAPIPE
    /// <summary>
    /// Production frame source: webcam → MediaPipe GestureRecognizer task
    /// (LIVE_STREAM) → normalized <see cref="GestureFrame"/>s.
    ///
    /// Threading: MediaPipe invokes the result callback on a non-Unity thread.
    /// The callback only copies plain data into a <see cref="FrameInbox"/> and
    /// returns; <see cref="GestureRuntime"/> drains the inbox on the main thread.
    /// Never touch Unity APIs inside the callback.
    /// </summary>
    [RequireComponent(typeof(GestureRuntime))]
    public sealed class MediapipeGestureDriver : MonoBehaviour, IGestureFrameSource, IDisposable
    {
        [Tooltip("GestureRecognizer model bundle, relative to StreamingAssets (download from the MediaPipe model zoo).")]
        [SerializeField] private string modelAssetPath = "gesture_recognizer.task";

        [Tooltip("Requested webcam resolution/rate; the OS picks the closest supported mode.")]
        [SerializeField] private int requestedWidth = 640;
        [SerializeField] private int requestedHeight = 480;
        [SerializeField] private int requestedFps = 30;

        [Tooltip("Run inference at most this often (ms). 0 = every rendered frame. Decouples model cost from render rate on CPU-only machines.")]
        [SerializeField] private int inferenceIntervalMs = 0;

        [Tooltip("Flip the webcam image before inference. Adjust while watching the landmark overlay: the skeleton should track your real hand, not a mirror.")]
        [SerializeField] private bool flipHorizontally = false;
        [SerializeField] private bool flipVertically = true;

        [Tooltip("Pooled input textures. Must exceed the number of inferences that can be in flight at once; each pooled frame is aliased by its Image until MediaPipe finishes with it.")]
        [SerializeField] private int texturePoolSize = 10;

        private readonly FrameInbox _inbox = new FrameInbox();
        private WebCamTexture _webcam;
        private GestureRecognizer _recognizer;
        private TextureFrame[] _frames;   // round-robin pool; BuildCPUImage aliases these
        private int _frameCursor;
        private long _lastInferenceMs = long.MinValue;
        private long _lastTimestampMs = long.MinValue;
        private long _framesSubmitted;

        /// <summary>Frames dropped because the main thread fell behind the camera.</summary>
        public long DroppedFrames => _inbox.DroppedFrames;
        public long FramesSubmitted => _framesSubmitted;

        private void Awake()
        {
            GetComponent<GestureRuntime>().FrameSource = this;
        }

        private IEnumerator Start()
        {
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogError("[GestureInput] No webcam found.", this);
                yield break;
            }

            _webcam = new WebCamTexture(WebCamTexture.devices[0].name, requestedWidth, requestedHeight, requestedFps);
            _webcam.Play();
            // wait until the camera actually delivers sized frames
            while (_webcam.width <= 16) yield return null;

            // A pool of textures the webcam is copied into. BuildCPUImage() wraps
            // (does not copy) a frame's texture, so a frame must not be reused while
            // its Image is still being processed asynchronously — hence a round-robin
            // pool larger than the number of in-flight inferences.
            _frames = new TextureFrame[Mathf.Max(2, texturePoolSize)];
            for (int i = 0; i < _frames.Length; i++)
                _frames[i] = new TextureFrame(_webcam.width, _webcam.height, TextureFormat.RGBA32);

            var options = new GestureRecognizerOptions(
                new BaseOptions(BaseOptions.Delegate.CPU, modelAssetPath: modelAssetPath),
                runningMode: RunningMode.LIVE_STREAM,
                numHands: 1,
                resultCallback: OnRecognitionResult);
            _recognizer = GestureRecognizer.CreateFromOptions(options);

            // Capture at end of frame so the webcam texture is fully updated before
            // the CPU readback (ReadTextureOnCPU), matching the plugin's own samples.
            var waitForEndOfFrame = new WaitForEndOfFrame();
            while (true)
            {
                yield return waitForEndOfFrame;
                TrySubmitFrame();
            }
        }

        private void TrySubmitFrame()
        {
            if (_recognizer == null || _webcam == null || !_webcam.didUpdateThisFrame) return;

            long nowMs = (long)(Time.realtimeSinceStartupAsDouble * 1000.0);
            if (inferenceIntervalMs > 0 && nowMs - _lastInferenceMs < inferenceIntervalMs) return;
            // MediaPipe's LIVE_STREAM requires strictly increasing timestamps.
            if (nowMs <= _lastTimestampMs) nowMs = _lastTimestampMs + 1;
            _lastInferenceMs = nowMs;
            _lastTimestampMs = nowMs;

            var textureFrame = _frames[_frameCursor];
            _frameCursor = (_frameCursor + 1) % _frames.Length;

            textureFrame.ReadTextureOnCPU(_webcam, flipHorizontally, flipVertically);
            var image = textureFrame.BuildCPUImage();
            _recognizer.RecognizeAsync(image, nowMs, imageProcessingOptions: null);
            _framesSubmitted++;
        }

        // ---- native/worker thread from here ------------------------------------

        private void OnRecognitionResult(GestureRecognizerResult result, Image image, long timestampMs)
        {
            _inbox.Enqueue(Normalize(result, timestampMs));
        }

        private static GestureFrame Normalize(GestureRecognizerResult result, long timestampMs)
        {
            bool hasHand = result.handLandmarks != null && result.handLandmarks.Count > 0;
            if (!hasHand)
                return new GestureFrame(timestampMs, HandData.Absent, PoseData.Absent, BuiltinGesture.None);

            var source = result.handLandmarks[0].landmarks;
            var landmarks = new Vector3[source.Count];
            for (int i = 0; i < source.Count; i++)
                landmarks[i] = new Vector3(source[i].x, source[i].y, source[i].z);

            // palm reference: wrist landmark (index 0 in MediaPipe's hand topology)
            var palm = landmarks.Length > 0 ? new Vector2(landmarks[0].x, landmarks[0].y) : Vector2.zero;

            var handedness = Handedness.Unknown;
            if (result.handedness != null && result.handedness.Count > 0 && result.handedness[0].categories.Count > 0)
            {
                var name = result.handedness[0].categories[0].categoryName;
                if (name == "Left") handedness = Handedness.Left;
                else if (name == "Right") handedness = Handedness.Right;
            }

            var builtin = BuiltinGesture.None;
            if (result.gestures != null && result.gestures.Count > 0 && result.gestures[0].categories.Count > 0)
            {
                var top = result.gestures[0].categories[0];
                builtin = new BuiltinGesture(MapCategory(top.categoryName), top.score);
            }

            return new GestureFrame(timestampMs, new HandData(handedness, palm, landmarks), PoseData.Absent, builtin);
        }

        private static BuiltinGestureType MapCategory(string categoryName)
        {
            switch (categoryName)
            {
                case "Open_Palm": return BuiltinGestureType.OpenPalm;
                case "Closed_Fist": return BuiltinGestureType.ClosedFist;
                case "Thumb_Up": return BuiltinGestureType.ThumbUp;
                case "Thumb_Down": return BuiltinGestureType.ThumbDown;
                case "Victory": return BuiltinGestureType.Victory;
                case "Pointing_Up": return BuiltinGestureType.PointingUp;
                case "ILoveYou": return BuiltinGestureType.ILoveYou;
                default: return BuiltinGestureType.None;
            }
        }

        // ---- main thread again --------------------------------------------------

        public bool TryDequeue(out GestureFrame frame) => _inbox.TryDequeue(out frame);

        public void Clear() => _inbox.Clear();

        public void Dispose()
        {
            _recognizer?.Close();
            _recognizer = null;

            if (_webcam != null)
            {
                _webcam.Stop();
                Destroy(_webcam);
                _webcam = null;
            }

            if (_frames != null)
            {
                foreach (var frame in _frames) frame?.Dispose();
                _frames = null;
            }
        }

        private void OnDestroy() => Dispose();
    }
#else
    /// <summary>
    /// Placeholder shown when the MediaPipe Unity Plugin is not installed.
    /// Install com.github.homuler.mediapipe (GitHub Releases — it is not on a
    /// package registry) to enable the real driver; see the package README.
    /// </summary>
    public sealed class MediapipeGestureDriver : MonoBehaviour, IGestureFrameSource
    {
        private void Awake()
        {
            Debug.LogError(
                "[GestureInput] MediapipeGestureDriver is inert: the MediaPipe Unity Plugin " +
                "(com.github.homuler.mediapipe) is not installed. Install it from its GitHub " +
                "Releases page, then this component compiles into the real webcam driver.", this);
        }

        public bool TryDequeue(out GestureFrame frame)
        {
            frame = default;
            return false;
        }

        public void Clear() { }
    }
#endif
}
