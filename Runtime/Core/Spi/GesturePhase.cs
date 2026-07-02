namespace GestureInput.Core
{
    /// <summary>Lifecycle phase of an emitted gesture event.</summary>
    public enum GesturePhase
    {
        /// <summary>The gesture was just detected.</summary>
        Began,

        /// <summary>The gesture is ongoing and its value may have changed.</summary>
        Updated,

        /// <summary>The gesture finished or was cancelled (e.g. hand lost).</summary>
        Ended
    }
}
