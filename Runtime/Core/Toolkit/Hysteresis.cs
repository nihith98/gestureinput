using System;

namespace GestureInput.Core
{
    /// <summary>
    /// Enter-high / exit-low gate: activates when the value reaches
    /// <c>enter</c>, deactivates only when it drops below <c>exit</c>.
    /// Stops a near-threshold signal from flickering on and off.
    /// </summary>
    public sealed class Hysteresis
    {
        private readonly float _enter;
        private readonly float _exit;

        public Hysteresis(float enter, float exit)
        {
            if (enter <= exit)
                throw new ArgumentException($"enter ({enter}) must be greater than exit ({exit}).");
            _enter = enter;
            _exit = exit;
        }

        public bool IsActive { get; private set; }

        /// <summary>Feed the next sample; returns the gate state after applying it.</summary>
        public bool Update(float value)
        {
            if (IsActive)
            {
                if (value < _exit) IsActive = false;
            }
            else
            {
                if (value >= _enter) IsActive = true;
            }
            return IsActive;
        }

        public void Reset() => IsActive = false;
    }
}
