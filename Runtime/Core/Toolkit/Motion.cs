using UnityEngine;

namespace GestureInput.Core
{
    /// <summary>Cardinal screen direction in normalized image space (+y is down).</summary>
    public enum SwipeDirection
    {
        None,
        Left,
        Right,
        Up,
        Down
    }

    /// <summary>Motion analysis helpers over sliding windows of samples.</summary>
    public static class Motion
    {
        /// <summary>Newest position minus oldest; zero with fewer than two samples.</summary>
        public static Vector2 Displacement(RingBuffer<TimedVector2> path)
        {
            if (path.Count < 2) return Vector2.zero;
            return path.Latest.Position - path[0].Position;
        }

        /// <summary>Average velocity over the window in units per second; zero when undefined.</summary>
        public static Vector2 Velocity(RingBuffer<TimedVector2> path)
        {
            if (path.Count < 2) return Vector2.zero;
            long dtMs = path.Latest.TimestampMs - path[0].TimestampMs;
            if (dtMs <= 0) return Vector2.zero;
            return Displacement(path) * (1000f / dtMs);
        }

        /// <summary>Sum of segment lengths along the window (≥ displacement magnitude).</summary>
        public static float PathLength(RingBuffer<TimedVector2> path)
        {
            float total = 0f;
            for (int i = 1; i < path.Count; i++)
                total += (path[i].Position - path[i - 1].Position).magnitude;
            return total;
        }

        /// <summary>
        /// The cardinal direction of <paramref name="displacement"/>, or
        /// <see cref="SwipeDirection.None"/> if the dominant axis is within
        /// <paramref name="deadZone"/>. +y is down (image space).
        /// </summary>
        public static SwipeDirection DominantDirection(Vector2 displacement, float deadZone)
        {
            float ax = Mathf.Abs(displacement.x);
            float ay = Mathf.Abs(displacement.y);
            if (ax >= ay)
            {
                if (ax <= deadZone) return SwipeDirection.None;
                return displacement.x > 0f ? SwipeDirection.Right : SwipeDirection.Left;
            }
            if (ay <= deadZone) return SwipeDirection.None;
            return displacement.y > 0f ? SwipeDirection.Down : SwipeDirection.Up;
        }

        /// <summary>
        /// Number of direction reversals in a scalar series, ignoring movements
        /// smaller than <paramref name="minDelta"/>. The heart of a wave detector.
        /// </summary>
        public static int CountReversals(RingBuffer<float> series, float minDelta)
        {
            int reversals = 0;
            int direction = 0; // -1 falling, +1 rising, 0 undetermined
            if (series.Count < 2) return 0;

            float anchor = series[0];
            for (int i = 1; i < series.Count; i++)
            {
                float delta = series[i] - anchor;
                if (Mathf.Abs(delta) < minDelta) continue; // jitter — keep the anchor

                int newDirection = delta > 0f ? 1 : -1;
                if (direction != 0 && newDirection != direction) reversals++;
                direction = newDirection;
                anchor = series[i];
            }
            return reversals;
        }
    }
}
