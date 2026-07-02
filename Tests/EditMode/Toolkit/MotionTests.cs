using NUnit.Framework;
using UnityEngine;
using GestureInput.Core;

namespace GestureInput.Tests
{
    [TestFixture]
    public class MotionTests
    {
        private static RingBuffer<TimedVector2> Path(params (float x, float y, long t)[] samples)
        {
            var buf = new RingBuffer<TimedVector2>(System.Math.Max(1, samples.Length));
            foreach (var (x, y, t) in samples)
                buf.Add(new TimedVector2(new Vector2(x, y), t));
            return buf;
        }

        [Test]
        public void Displacement_IsNewestMinusOldest()
        {
            var path = Path((0.1f, 0.5f, 0), (0.3f, 0.5f, 100), (0.6f, 0.5f, 200));
            var d = MotionMath.Displacement(path);
            Assert.AreEqual(0.5f, d.x, 1e-4f);
            Assert.AreEqual(0f, d.y, 1e-4f);
        }

        [Test]
        public void Displacement_FewerThanTwoSamples_IsZero()
        {
            Assert.AreEqual(Vector2.zero, MotionMath.Displacement(Path()));
            Assert.AreEqual(Vector2.zero, MotionMath.Displacement(Path((0.5f, 0.5f, 0))));
        }

        [Test]
        public void Velocity_IsDisplacementOverSeconds()
        {
            // 0.4 normalized units in 200 ms => 2.0 units/sec to the right
            var path = Path((0.1f, 0.5f, 0), (0.5f, 0.5f, 200));
            var v = MotionMath.Velocity(path);
            Assert.AreEqual(2f, v.x, 1e-4f);
            Assert.AreEqual(0f, v.y, 1e-4f);
        }

        [Test]
        public void Velocity_ZeroDt_IsZero()
        {
            var path = Path((0.1f, 0.5f, 50), (0.5f, 0.5f, 50));
            Assert.AreEqual(Vector2.zero, MotionMath.Velocity(path));
        }

        [Test]
        public void PathLength_SumsSegmentLengths()
        {
            var path = Path((0f, 0f, 0), (0.3f, 0f, 100), (0.3f, 0.4f, 200));
            Assert.AreEqual(0.7f, MotionMath.PathLength(path), 1e-4f);
        }

        [TestCase(0.5f, 0f, SwipeDirection.Right)]
        [TestCase(-0.5f, 0.1f, SwipeDirection.Left)]
        [TestCase(0.1f, 0.5f, SwipeDirection.Down)]  // +y is down in image space
        [TestCase(0f, -0.5f, SwipeDirection.Up)]
        public void DominantDirection_PicksLargestAxis(float x, float y, SwipeDirection expected)
        {
            Assert.AreEqual(expected, MotionMath.DominantDirection(new Vector2(x, y), deadZone: 0.05f));
        }

        [Test]
        public void DominantDirection_InsideDeadZone_IsNone()
        {
            Assert.AreEqual(SwipeDirection.None, MotionMath.DominantDirection(new Vector2(0.01f, -0.02f), deadZone: 0.05f));
        }

        [Test]
        public void CountReversals_CountsDirectionChanges()
        {
            // left-right wave: x goes up, down, up, down => 3 reversals
            var xs = new RingBuffer<float>(16);
            foreach (var x in new[] { 0.2f, 0.4f, 0.6f, 0.4f, 0.2f, 0.4f, 0.6f, 0.4f, 0.2f })
                xs.Add(x);
            Assert.AreEqual(3, MotionMath.CountReversals(xs, minDelta: 0.05f));
        }

        [Test]
        public void CountReversals_IgnoresJitterBelowMinDelta()
        {
            var xs = new RingBuffer<float>(16);
            foreach (var x in new[] { 0.5f, 0.51f, 0.5f, 0.51f, 0.5f, 0.51f })
                xs.Add(x);
            Assert.AreEqual(0, MotionMath.CountReversals(xs, minDelta: 0.05f));
        }

        [Test]
        public void CountReversals_MonotonicSeries_IsZero()
        {
            var xs = new RingBuffer<float>(8);
            foreach (var x in new[] { 0.1f, 0.3f, 0.5f, 0.7f, 0.9f })
                xs.Add(x);
            Assert.AreEqual(0, MotionMath.CountReversals(xs, minDelta: 0.05f));
        }
    }
}
