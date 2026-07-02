using System.Collections.Generic;
using UnityEngine;
using GestureInput.Core;
using GestureInput.Unity;

namespace GestureInput.Tests.PlayMode
{
    /// <summary>Preloadable frame source so PlayMode tests run without a camera or MediaPipe.</summary>
    public sealed class MockFrameSource : IGestureFrameSource
    {
        private readonly Queue<GestureFrame> _frames = new Queue<GestureFrame>();

        public void Enqueue(GestureFrame frame) => _frames.Enqueue(frame);

        public void EnqueueBuiltin(long ts, BuiltinGestureType type, float confidence)
        {
            var hand = new HandData(Handedness.Right, new Vector2(0.5f, 0.5f), new Vector3[21]);
            _frames.Enqueue(new GestureFrame(ts, hand, PoseData.Absent, new BuiltinGesture(type, confidence)));
        }

        public bool TryDequeue(out GestureFrame frame)
        {
            if (_frames.Count > 0)
            {
                frame = _frames.Dequeue();
                return true;
            }
            frame = default;
            return false;
        }

        public void Clear() => _frames.Clear();
    }
}
