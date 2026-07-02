using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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
                    GUILayout.Label($"  <GestureDevice>/{descriptor.Id} = {ReadControl(control, descriptor.Kind)}");
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("<b>Recent events</b>", Rich());
            foreach (var line in _recentEvents) GUILayout.Label(line);

            GUILayout.EndArea();
        }

        // Read a control's current value without the non-generic ReadValueAsObject()
        // (absent from some Input System versions): controls are always typed as
        // InputControl<float> (Button/Axis) or InputControl<Vector2>.
        private static string ReadControl(InputControl control, GestureKind kind)
        {
            if (control == null) return "-";
            switch (kind)
            {
                case GestureKind.Continuous2D:
                    return control is InputControl<Vector2> v ? v.ReadValue().ToString() : "-";
                default:
                    return control is InputControl<float> f ? f.ReadValue().ToString("F2") : "-";
            }
        }

        private static GUIStyle Rich() => new GUIStyle(GUI.skin.label) { richText = true };
    }
}
