using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using GestureInput.Core;
using GestureInput.Samples;

namespace GestureInput.Tests
{
    [TestFixture]
    public class WaveRecognizerTests
    {
        private static List<GestureFrame> Fixture(string name) =>
            FixtureCodec.Read(System.IO.File.ReadAllText(FixturePaths.Resolve(name)));

        [Test]
        public void Wave_Fixture_EmitsWave()
        {
            var r = new WaveRecognizer();
            var sink = new TestSink();
            foreach (var f in Fixture("wave_left_right.gframes"))
                r.Process(f, sink);
            Assert.IsTrue(sink.Contains("wave", GesturePhase.Began));
        }

        [Test]
        public void Swipe_Fixture_DoesNotWave()
        {
            var r = new WaveRecognizer();
            var sink = new TestSink();
            foreach (var f in Fixture("swipe_right.gframes"))
                r.Process(f, sink);
            Assert.IsEmpty(sink.Events);
        }

        [Test]
        public void StillHand_DoesNotWave()
        {
            var r = new WaveRecognizer();
            var sink = new TestSink();
            for (int i = 0; i < 100; i++)
                r.Process(SyntheticHand.At(i * 33, 0.5f, 0.5f), sink);
            Assert.IsEmpty(sink.Events);
        }

        [Test]
        public void ContinuousWaving_RespectsCooldown()
        {
            var r = new WaveRecognizer(cooldownMs: 600);
            var sink = new TestSink();

            // wave continuously for ~4.7s: legs of 4 frames, 0.1/frame
            float x = 0.3f;
            int dir = 1;
            for (int i = 0; i < 144; i++)
            {
                r.Process(SyntheticHand.At(i * 33, x, 0.5f), sink);
                x += 0.1f * dir;
                if (x >= 0.7f || x <= 0.3f) dir = -dir;
            }

            int waves = sink.Count("wave");
            Assert.Greater(waves, 1, "sustained waving should fire repeatedly");
            // events must be at least cooldownMs apart
            var times = sink.Events.Select(e => e.TimestampMs).ToList();
            for (int i = 1; i < times.Count; i++)
                Assert.GreaterOrEqual(times[i] - times[i - 1], 600);
        }
    }

    public class WaveRecognizerConformance : RecognizerConformanceSuite
    {
        protected override IGestureRecognizer CreateRecognizer() => new WaveRecognizer();

        protected override IEnumerable<GestureFrame> GetSampleFrames() =>
            FixtureCodec.Read(System.IO.File.ReadAllText(FixturePaths.Resolve("wave_left_right.gframes")));
    }
}
