using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using GestureInput.Core;

namespace GestureInput.Tests
{
    public static class SyntheticHand
    {
        public static GestureFrame At(long ts, float x, float y)
        {
            var landmarks = new Vector3[21];
            for (int i = 0; i < 21; i++) landmarks[i] = new Vector3(x, y, 0f);
            return new GestureFrame(ts,
                new HandData(Handedness.Right, new Vector2(x, y), landmarks),
                PoseData.Absent, BuiltinGesture.None);
        }

        public static GestureFrame Absent(long ts) =>
            new GestureFrame(ts, HandData.Absent, PoseData.Absent, BuiltinGesture.None);

        /// <summary>Linear move from (x0,y0) to (x1,y1) over `frames` frames at ~30fps.</summary>
        public static IEnumerable<GestureFrame> Sweep(long startTs, float x0, float y0, float x1, float y1, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                float t = frames == 1 ? 1f : i / (float)(frames - 1);
                yield return At(startTs + i * 33, x0 + (x1 - x0) * t, y0 + (y1 - y0) * t);
            }
        }
    }

    [TestFixture]
    public class SwipeRecognizerTests
    {
        private static (TestSink sink, SwipeRecognizer r) Run(IEnumerable<GestureFrame> frames)
        {
            var r = new SwipeRecognizer();
            var sink = new TestSink();
            foreach (var f in frames) r.Process(f, sink);
            return (sink, r);
        }

        [Test]
        public void Declares_FourSwipeDirections()
        {
            CollectionAssert.AreEquivalent(
                new[] { "swipeLeft", "swipeRight", "swipeUp", "swipeDown" },
                new SwipeRecognizer().Descriptors.Select(d => d.Id).ToArray());
        }

        [Test]
        public void FastRightMotion_FiresSwipeRightExactlyOnce()
        {
            // 0.5 normalized units in ~200 ms — a decisive swipe
            var (sink, _) = Run(SyntheticHand.Sweep(0, 0.2f, 0.5f, 0.7f, 0.5f, 7));
            Assert.AreEqual(1, sink.Count("swipeRight"), string.Join("\n", sink.Events));
            Assert.AreEqual(GesturePhase.Began, sink.Events.Single().Phase);
        }

        [TestCase(0.7f, 0.5f, 0.2f, 0.5f, "swipeLeft")]
        [TestCase(0.5f, 0.7f, 0.5f, 0.2f, "swipeUp")]
        [TestCase(0.5f, 0.2f, 0.5f, 0.7f, "swipeDown")]
        public void EachDirection_IsDetected(float x0, float y0, float x1, float y1, string expected)
        {
            var (sink, _) = Run(SyntheticHand.Sweep(0, x0, y0, x1, y1, 7));
            Assert.AreEqual(1, sink.Count(expected));
        }

        [Test]
        public void SlowDrift_DoesNotFire()
        {
            // same distance but over ~3.3 seconds — velocity too low
            var (sink, _) = Run(SyntheticHand.Sweep(0, 0.2f, 0.5f, 0.7f, 0.5f, 100));
            Assert.IsEmpty(sink.Events);
        }

        [Test]
        public void SmallMotion_DoesNotFire()
        {
            var (sink, _) = Run(SyntheticHand.Sweep(0, 0.45f, 0.5f, 0.55f, 0.5f, 7));
            Assert.IsEmpty(sink.Events);
        }

        [Test]
        public void ZigZag_DoesNotFire()
        {
            // fast but reverses direction — net displacement and straightness reject it
            var frames = SyntheticHand.Sweep(0, 0.3f, 0.5f, 0.5f, 0.5f, 4)
                .Concat(SyntheticHand.Sweep(4 * 33, 0.5f, 0.5f, 0.28f, 0.5f, 4));
            var (sink, _) = Run(frames);
            Assert.IsEmpty(sink.Events);
        }

        [Test]
        public void Cooldown_SuppressesImmediateSecondFire()
        {
            // one long fast sweep: without cooldown the window would trigger repeatedly
            var (sink, _) = Run(SyntheticHand.Sweep(0, 0.05f, 0.5f, 0.95f, 0.5f, 12));
            Assert.AreEqual(1, sink.Count("swipeRight"));
        }

        [Test]
        public void SecondSwipe_AfterCooldown_Fires()
        {
            var frames = new List<GestureFrame>();
            frames.AddRange(SyntheticHand.Sweep(0, 0.2f, 0.5f, 0.7f, 0.5f, 7));
            // hold still past the cooldown window
            for (int i = 0; i < 20; i++) frames.Add(SyntheticHand.At(7 * 33 + i * 33, 0.7f, 0.5f));
            frames.AddRange(SyntheticHand.Sweep(27 * 33, 0.7f, 0.5f, 0.2f, 0.5f, 7));

            var (sink, _) = Run(frames);
            Assert.AreEqual(1, sink.Count("swipeRight"));
            Assert.AreEqual(1, sink.Count("swipeLeft"));
        }

        [Test]
        public void HandLoss_ClearsWindow()
        {
            var frames = new List<GestureFrame>();
            frames.AddRange(SyntheticHand.Sweep(0, 0.2f, 0.5f, 0.4f, 0.5f, 3)); // partial motion
            frames.Add(SyntheticHand.Absent(99));
            // reappears far right immediately — must NOT read as a swipe (window was cleared)
            frames.Add(SyntheticHand.At(132, 0.7f, 0.5f));
            frames.Add(SyntheticHand.At(165, 0.7f, 0.5f));

            var (sink, _) = Run(frames);
            Assert.IsEmpty(sink.Events);
        }

        [Test]
        public void FixtureReplay_SwipeRight_Fires()
        {
            var frames = FixtureCodec.Read(System.IO.File.ReadAllText(FixturePaths.Resolve("swipe_right.gframes")));
            var (sink, _) = Run(frames);
            Assert.AreEqual(1, sink.Count("swipeRight"));
        }
    }

    public class SwipeRecognizerConformance : RecognizerConformanceSuite
    {
        protected override IGestureRecognizer CreateRecognizer() => new SwipeRecognizer();

        protected override IEnumerable<GestureFrame> GetSampleFrames()
        {
            foreach (var f in SyntheticHand.Sweep(0, 0.2f, 0.5f, 0.7f, 0.5f, 7)) yield return f;
            for (int i = 0; i < 10; i++) yield return SyntheticHand.Absent(300 + i * 33);
            foreach (var f in SyntheticHand.Sweep(700, 0.5f, 0.7f, 0.5f, 0.2f, 7)) yield return f;
        }
    }
}
