using GestureInput.Core;

namespace GestureInput.Unity
{
    /// <summary>
    /// Supplies normalized perception frames to the <see cref="GestureRuntime"/>.
    /// The MediaPipe driver is the production implementation; PlayMode tests use
    /// a mock. Implementations must make <see cref="TryDequeue"/> safe to call
    /// from the main thread even while frames are produced on another thread
    /// (see <see cref="Core.FrameInbox"/>).
    /// </summary>
    public interface IGestureFrameSource
    {
        /// <summary>Pop the next pending frame; false when none are queued.</summary>
        bool TryDequeue(out GestureFrame frame);

        /// <summary>Discard any queued frames (scene reset, camera restart).</summary>
        void Clear();
    }
}
