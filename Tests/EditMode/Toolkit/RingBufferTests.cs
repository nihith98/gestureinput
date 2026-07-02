using System;
using NUnit.Framework;
using GestureInput.Core;

namespace GestureInput.Tests
{
    [TestFixture]
    public class RingBufferTests
    {
        [Test]
        public void Ctor_RejectsNonPositiveCapacity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<int>(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<int>(-1));
        }

        [Test]
        public void Add_GrowsCountUntilFull()
        {
            var buf = new RingBuffer<int>(3);
            Assert.AreEqual(0, buf.Count);
            Assert.IsFalse(buf.IsFull);

            buf.Add(1);
            buf.Add(2);
            Assert.AreEqual(2, buf.Count);
            Assert.IsFalse(buf.IsFull);

            buf.Add(3);
            Assert.AreEqual(3, buf.Count);
            Assert.IsTrue(buf.IsFull);
            Assert.AreEqual(3, buf.Capacity);
        }

        [Test]
        public void Wraparound_KeepsLastCapacityItems_OldestFirst()
        {
            var buf = new RingBuffer<int>(3);
            for (int i = 1; i <= 5; i++) buf.Add(i);

            Assert.AreEqual(3, buf.Count);
            Assert.AreEqual(3, buf[0]); // oldest
            Assert.AreEqual(4, buf[1]);
            Assert.AreEqual(5, buf[2]); // newest
            Assert.AreEqual(5, buf.Latest);
        }

        [Test]
        public void Indexer_OutOfRange_Throws()
        {
            var buf = new RingBuffer<int>(3);
            buf.Add(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = buf[1]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = buf[-1]; });
        }

        [Test]
        public void Latest_OnEmpty_Throws()
        {
            var buf = new RingBuffer<int>(3);
            Assert.Throws<InvalidOperationException>(() => { var _ = buf.Latest; });
        }

        [Test]
        public void Clear_EmptiesBuffer()
        {
            var buf = new RingBuffer<int>(2);
            buf.Add(1);
            buf.Add(2);
            buf.Clear();
            Assert.AreEqual(0, buf.Count);
            Assert.IsFalse(buf.IsFull);
            buf.Add(9);
            Assert.AreEqual(9, buf[0]);
        }
    }
}
