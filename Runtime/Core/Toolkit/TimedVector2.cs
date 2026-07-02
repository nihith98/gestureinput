using UnityEngine;

namespace GestureInput.Core
{
    /// <summary>A 2D sample paired with the frame timestamp it was observed at.</summary>
    public readonly struct TimedVector2
    {
        public Vector2 Position { get; }
        public long TimestampMs { get; }

        public TimedVector2(Vector2 position, long timestampMs)
        {
            Position = position;
            TimestampMs = timestampMs;
        }
    }
}
