using System.Collections.Concurrent;
using System.Threading;

namespace GestureInput.Core
{
    /// <summary>
    /// The thread-safe handoff between the perception callback (which may run on
    /// a native thread) and the main-thread runtime. Bounded: when the consumer
    /// falls behind, the oldest frames are dropped and counted — fresh perception
    /// data always wins over stale.
    /// </summary>
    public sealed class FrameInbox
    {
        private readonly ConcurrentQueue<GestureFrame> _queue = new ConcurrentQueue<GestureFrame>();
        private readonly int _capacity;
        private long _dropped;

        public FrameInbox(int capacity = 4)
        {
            _capacity = capacity < 1 ? 1 : capacity;
        }

        /// <summary>Total frames evicted because the consumer fell behind.</summary>
        public long DroppedFrames => Interlocked.Read(ref _dropped);

        /// <summary>Safe to call from any thread.</summary>
        public void Enqueue(in GestureFrame frame)
        {
            _queue.Enqueue(frame);
            while (_queue.Count > _capacity && _queue.TryDequeue(out _))
                Interlocked.Increment(ref _dropped);
        }

        public bool TryDequeue(out GestureFrame frame) => _queue.TryDequeue(out frame);

        public void Clear()
        {
            while (_queue.TryDequeue(out _)) { }
        }
    }
}
