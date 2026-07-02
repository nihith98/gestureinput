using System;

namespace GestureInput.Core
{
    /// <summary>
    /// Refractory period keyed on frame timestamps: after <see cref="Trigger"/>,
    /// <see cref="Ready"/> stays false until <c>milliseconds</c> have elapsed.
    /// Timestamp-driven (not wall clock) so replayed fixtures behave identically.
    /// </summary>
    public sealed class Cooldown
    {
        private readonly long _milliseconds;
        private long _lastTriggerMs;
        private bool _triggered;

        public Cooldown(long milliseconds)
        {
            if (milliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(milliseconds), milliseconds, "Duration cannot be negative.");
            _milliseconds = milliseconds;
        }

        public bool Ready(long nowMs) => !_triggered || nowMs - _lastTriggerMs >= _milliseconds;

        public void Trigger(long nowMs)
        {
            _triggered = true;
            _lastTriggerMs = nowMs;
        }

        public void Reset() => _triggered = false;
    }
}
