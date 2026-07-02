using System.Threading;
using NUnit.Framework;
using GestureInput.Core;

namespace GestureInput.Tests
{
    [TestFixture]
    public class FrameInboxTests
    {
        private static GestureFrame Frame(long ts) =>
            new GestureFrame(ts, HandData.Absent, PoseData.Absent, BuiltinGesture.None);

        [Test]
        public void DequeueOnEmpty_ReturnsFalse()
        {
            var inbox = new FrameInbox();
            Assert.IsFalse(inbox.TryDequeue(out _));
        }

        [Test]
        public void Fifo_Order()
        {
            var inbox = new FrameInbox(4);
            inbox.Enqueue(Frame(1));
            inbox.Enqueue(Frame(2));

            Assert.IsTrue(inbox.TryDequeue(out var a));
            Assert.IsTrue(inbox.TryDequeue(out var b));
            Assert.AreEqual(1, a.TimestampMs);
            Assert.AreEqual(2, b.TimestampMs);
            Assert.IsFalse(inbox.TryDequeue(out _));
        }

        [Test]
        public void Overflow_DropsOldest_AndCounts()
        {
            var inbox = new FrameInbox(2);
            inbox.Enqueue(Frame(1));
            inbox.Enqueue(Frame(2));
            inbox.Enqueue(Frame(3)); // evicts 1

            Assert.AreEqual(1, inbox.DroppedFrames);
            Assert.IsTrue(inbox.TryDequeue(out var a));
            Assert.AreEqual(2, a.TimestampMs);
            Assert.IsTrue(inbox.TryDequeue(out var b));
            Assert.AreEqual(3, b.TimestampMs);
        }

        [Test]
        public void Clear_EmptiesQueue()
        {
            var inbox = new FrameInbox(4);
            inbox.Enqueue(Frame(1));
            inbox.Clear();
            Assert.IsFalse(inbox.TryDequeue(out _));
        }

        [Test]
        public void ConcurrentProducerConsumer_LosesNothingUnaccounted()
        {
            const int total = 10_000;
            var inbox = new FrameInbox(64);
            long consumed = 0;

            var producer = new Thread(() =>
            {
                for (int i = 0; i < total; i++) inbox.Enqueue(Frame(i));
            });

            var consumer = new Thread(() =>
            {
                while (Interlocked.Read(ref consumed) + inbox.DroppedFrames < total)
                {
                    if (inbox.TryDequeue(out _)) Interlocked.Increment(ref consumed);
                }
            });

            producer.Start();
            consumer.Start();
            Assert.IsTrue(producer.Join(5000), "producer hung");
            Assert.IsTrue(consumer.Join(5000), "consumer hung");

            Assert.AreEqual(total, consumed + inbox.DroppedFrames);
        }
    }
}
