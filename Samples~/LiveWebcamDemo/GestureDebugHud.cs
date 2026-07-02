using System.Collections.Generic;
using UnityEngine;
using GestureInput.Core;
using GestureInput.Unity;

namespace GestureInput.Samples.LiveDemo
{
    /// <summary>
    /// IMGUI overlay showing live pipeline state: recent gesture events with
    /// confidence, current GestureDevice control values, inference FPS, and
    /// dropped-frame count. This is the manual QA harness (SCOPE §8.4) — use it
    /// to judge latency and false positives while the recorder captures fixtures.
    /// </summary>
    [RequireComponent(typeof(GestureRuntime))]
    public sealed class GestureDebugHud : MonoBehaviour
    {
        private const int MaxLog = 8;

        private readonly Queue<string> _recentEvents = new Queue<string>();
        private GestureRuntime _runtime;
        private float _fps;
        private long _lastFrameTs;

        private void Awake()
        {
            _runtime = GetComponent<GestureRuntime>();
            _runtime.OnGesture += OnGesture;
        }

        private void OnDestroy()
        {
            if (_runtime != null) _runtime.OnGesture -= OnGesture;
        }

        private void OnGesture(GestureEvent e)
        {
            if (e.Phase == GesturePhase.Updated) return; // keep the log readable
            _recentEvents.Enqueue($"{e.TimestampMs,8} ms  {e.Id,-12} {e.Phase,-7} conf={e.Confidence:F2}");
            while (_recentEvents.Count > MaxLog) _recentEvents.Dequeue();

            if (_lastFrameTs > 0 && e.TimestampMs > _lastFrameTs)
                _fps = 1000f / (e.TimestampMs - _lastFrameTs);
            _lastFrameTs = e.TimestampMs;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 460, 480), GUI.skin.box);
            GUILayout.Label("<b>GestureInput — live debug</b>", Rich());

            var driver = GetComponent<GestureInput.Mediapipe.MediapipeGestureDriver>();
#if GESTUREINPUT_HAS_MEDIAPIPE
            if (driver != null)
                GUILayout.Label($"submitted: {driver.FramesSubmitted}   dropped: {driver.DroppedFrames}");
#else
            GUILayout.Label("<color=red>MediaPipe plugin not installed — no live frames.</color>", Rich());
#endif

            GUILayout.Space(6);
            GUILayout.Label("<b>Device controls</b>", Rich());
            var device = _runtime.Device;
            if (device != null)
            {
                foreach (var descriptor in _runtime.Registry.AllDescriptors)
                {
                    var control = device.TryGetChildControl(descriptor.Id);
                    GUILayout.Label($"  <GestureDevice>/{descriptor.Id} = {control?.ReadValueAsObject() ?? "-"}");
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("<b>Recent events</b>", Rich());
            foreach (var line in _recentEvents) GUILayout.Label(line);

            GUILayout.EndArea();
        }

        private static GUIStyle Rich() => new GUIStyle(GUI.skin.label) { richText = true };
    }
}
