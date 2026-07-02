using System.Collections.Generic;

namespace GestureInput.Core
{
    /// <summary>
    /// The single extension point of the library. Implement this to add a gesture —
    /// built-in recognizers use the exact same contract.
    ///
    /// Contract (verified by the RecognizerConformanceSuite):
    /// - <see cref="Descriptors"/> is non-empty, stable, and every emitted event id
    ///   is one of the declared descriptor ids.
    /// - <see cref="Reset"/> clears all temporal state; replaying the same frames
    ///   after a Reset yields identical events.
    /// - <see cref="Process"/> is called once per frame on the main thread and must
    ///   tolerate hand-absent/empty frames without throwing.
    /// </summary>
    public interface IGestureRecognizer
    {
        /// <summary>Declared up front so the system can build input controls and validate bindings.</summary>
        IReadOnlyList<GestureDescriptor> Descriptors { get; }

        /// <summary>Clear temporal state — called when the hand is lost or the scene resets.</summary>
        void Reset();

        /// <summary>Examine one frame and emit zero or more events into <paramref name="sink"/>.</summary>
        void Process(in GestureFrame frame, IGestureSink sink);
    }
}
