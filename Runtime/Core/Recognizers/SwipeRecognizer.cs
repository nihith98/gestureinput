using System.Collections.Generic;
using UnityEngine;

namespace GestureInput.Core
{
    /// <summary>
    /// Detects fast, mostly-straight palm motion in the four cardinal directions.
    /// Keeps a short sliding window of palm positions; fires a discrete
    /// swipeLeft/Right/Up/Down when, within the window, the hand moved far enough,
    /// fast enough, and straight enough. A cooldown makes each swipe a single event.
    /// All units are normalized image space ([0,1]², +y down) and per second.
    /// </summary>
    public sealed class SwipeRecognizer : IGestureRecognizer
    {
        public const string SwipeLeft = "swipeLeft";
        public const string SwipeRight = "swipeRight";
        public const string SwipeUp = "swipeUp";
        public const string SwipeDown = "swipeDown";

        private readonly long _windowMs;
        private readonly float _minDistance;
        private readonly float _minVelocity;
        private readonly float _minStraightness;
        private readonly RingBuffer<TimedVector2> _path;
        private readonly Cooldown _cooldown;

        /// <param name="windowMs">Sliding evaluation window in ms.</param>
        /// <param name="minDistance">Minimum net displacement within the window (normalized units).</param>
        /// <param name="minVelocity">Minimum mean speed within the window (units/second).</param>
        /// <param name="minStraightness">Minimum displacement/path-length ratio (1 = perfectly straight).</param>
        /// <param name="cooldownMs">Refractory period after a fired swipe.</param>
        public SwipeRecognizer(
            long windowMs = 250,
            float minDistance = 0.25f,
            float minVelocity = 1.0f,
            float minStraightness = 0.8f,
            long cooldownMs = 400)
        {
            _windowMs = windowMs;
            _minDistance = minDistance;
            _minVelocity = minVelocity;
            _minStraightness = minStraightness;
            _path = new RingBuffer<TimedVector2>(32);
            _cooldown = new Cooldown(cooldownMs);

            Descriptors = new[]
            {
                new GestureDescriptor(SwipeLeft, GestureKind.Discrete),
                new GestureDescriptor(SwipeRight, GestureKind.Discrete),
                new GestureDescriptor(SwipeUp, GestureKind.Discrete),
                new GestureDescriptor(SwipeDown, GestureKind.Discrete)
            };
        }

        public IReadOnlyList<GestureDescriptor> Descriptors { get; }

        public void Reset()
        {
            _path.Clear();
            _cooldown.Reset();
        }

        public void Process(in GestureFrame frame, IGestureSink sink)
        {
            if (!frame.Hand.IsPresent)
            {
                // Hand lost: forget the motion window but keep the cooldown, so a
                // hand blinking out and back cannot double-fire the same swipe.
                _path.Clear();
                return;
            }

            _path.Add(new TimedVector2(frame.Hand.Palm, frame.TimestampMs));
            if (_path.Count < 2 || !_cooldown.Ready(frame.TimestampMs)) return;

            // Evaluate only samples inside the time window, oldest kept sample first.
            long newestTs = _path.Latest.TimestampMs;
            int start = 0;
            while (start < _path.Count - 1 && newestTs - _path[start].TimestampMs > _windowMs)
                start++;

            long dtMs = newestTs - _path[start].TimestampMs;
            if (dtMs <= 0) return;

            Vector2 displacement = _path.Latest.Position - _path[start].Position;
            float distance = displacement.magnitude;
            if (distance < _minDistance) return;

            float speed = distance * 1000f / dtMs;
            if (speed < _minVelocity) return;

            float pathLength = 0f;
            for (int i = start + 1; i < _path.Count; i++)
                pathLength += (_path[i].Position - _path[i - 1].Position).magnitude;
            float straightness = pathLength > 0f ? distance / pathLength : 0f;
            if (straightness < _minStraightness) return;

            var direction = Motion.DominantDirection(displacement, deadZone: 0f);
            string id = IdFor(direction);
            if (id == null) return;

            _cooldown.Trigger(frame.TimestampMs);
            sink.Emit(new GestureEvent(id, GesturePhase.Began, 1f,
                confidence: Mathf.Clamp01(straightness), timestampMs: frame.TimestampMs));
        }

        private static string IdFor(SwipeDirection direction)
        {
            switch (direction)
            {
                case SwipeDirection.Left: return SwipeLeft;
                case SwipeDirection.Right: return SwipeRight;
                case SwipeDirection.Up: return SwipeUp;
                case SwipeDirection.Down: return SwipeDown;
                default: return null;
            }
        }
    }
}
