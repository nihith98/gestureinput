namespace GestureInput.Core
{
    /// <summary>
    /// The shape of the value a gesture produces, used to pick the Input System
    /// control type when the <c>GestureDevice</c> layout is built.
    /// </summary>
    public enum GestureKind
    {
        /// <summary>Fires like a button (e.g. a swipe or a wave).</summary>
        Discrete,

        /// <summary>Produces a continuous scalar (e.g. a pinch amount).</summary>
        Continuous1D,

        /// <summary>Produces a continuous 2D value (e.g. a palm position).</summary>
        Continuous2D
    }
}
