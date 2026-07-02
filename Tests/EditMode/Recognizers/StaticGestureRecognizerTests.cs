using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using GestureInput.Core;

namespace GestureInput.Tests
{
    [TestFixture]
    public class StaticGestureRecognizerTests
    {
        private static GestureFrame Frame(long ts, BuiltinGestureType type, float conf)
        {
            var hand = new HandData(Handedness.Right, new Vector2(0.5f, 0.5f), new Vector3[21]);
            return new GestureFrame(ts, hand, PoseData.Absent, new BuiltinGesture(type, conf));
        }

        private static GestureFrame Empty(long ts) =>
            new GestureFrame(ts, HandData.Absent, PoseData.Absent, BuiltinGesture.None);

        [Test]
        public void Declares_AllSevenBuiltinGestures_AsDiscrete()
        {
            var ids = new StaticGestureRecognizer().Descriptors;
            CollectionAssert.AreEquivalent(
                new[] { "openPalm", "closedFist", "thumbUp", "thumbDown", "victory", "pointingUp", "iLoveYou" },
                ids.Select(d => d.Id).ToArray());
            Assert.IsTrue(ids.All(d => d.Kind == GestureKind.Discrete));
        }

        [Test]
        public void RisingConfidence_EmitsBeganOnce_ThenUpdated()
        {
            var r = new StaticGestureRecognizer();
            var sink = new TestSink();

            r.Process(Frame(0, BuiltinGestureType.OpenPalm, 0.4f), sink);   // below enter
            Assert.IsEmpty(sink.Events);

            r.Process(Frame(33, BuiltinGestureType.OpenPalm, 0.8f), sink);  // enters
            r.Process(Frame(66, BuiltinGestureType.OpenPalm, 0.85f), sink); // holds
            r.Process(Frame(99, BuiltinGestureType.OpenPalm, 0.9f), sink);  // holds

            var events = sink.Events;
            Assert.AreEqual(GesturePhase.Began, events[0].Phase);
            Assert.AreEqual("openPalm", events[0].Id);
            Assert.AreEqual(1, events.Count(e => e.Phase == GesturePhase.Began));
            Assert.AreEqual(2, events.Count(e => e.Phase == GesturePhase.Updated));
            Assert.AreEqual(0.9f, events.Last().Confidence, 1e-4f);
        }

        [Test]
        public void MidBandFlicker_DoesNotRetrigger()
        {
            var r = new StaticGestureRecognizer(enterConfidence: 0.7f, exitConfidence: 0.5f);
            var sink = new TestSink();

            r.Process(Frame(0, BuiltinGestureType.Victory, 0.9f), sink);
            for (int i = 1; i <= 10; i++)
                r.Process(Frame(i * 33, BuiltinGestureType.Victory, i % 2 == 0 ? 0.55f : 0.65f), sink);

            Assert.AreEqual(1, sink.Events.Count(e => e.Phase == GesturePhase.Began));
            Assert.AreEqual(0, sink.Events.Count(e => e.Phase == GesturePhase.Ended));
        }

        [Test]
        public void ConfidenceDrop_BelowExit_EmitsEnded()
        {
            var r = new StaticGestureRecognizer(enterConfidence: 0.7f, exitConfidence: 0.5f);
            var sink = new TestSink();

            r.Process(Frame(0, BuiltinGestureType.ThumbUp, 0.9f), sink);
            r.Process(Frame(33, BuiltinGestureType.ThumbUp, 0.3f), sink);

            Assert.AreEqual(GesturePhase.Ended, sink.Events.Last().Phase);
            Assert.AreEqual("thumbUp", sink.Events.Last().Id);
            Assert.AreEqual(0f, sink.Events.Last().Value);
        }

        [Test]
        public void HandLost_WhileActive_EmitsEnded()
        {
            var r = new StaticGestureRecognizer();
            var sink = new TestSink();

            r.Process(Frame(0, BuiltinGestureType.ClosedFist, 0.9f), sink);
            r.Process(Empty(33), sink);

            Assert.IsTrue(sink.Contains("closedFist", GesturePhase.Ended));
            r.Process(Empty(66), sink);
            Assert.AreEqual(1, sink.Events.Count(e => e.Phase == GesturePhase.Ended)); // only once
        }

        [Test]
        public void GestureSwitch_EndsOld_BeginsNew()
        {
            var r = new StaticGestureRecognizer();
            var sink = new TestSink();

            r.Process(Frame(0, BuiltinGestureType.OpenPalm, 0.9f), sink);
            r.Process(Frame(33, BuiltinGestureType.ClosedFist, 0.9f), sink);

            var last2 = sink.Events.Skip(sink.Events.Count - 2).ToList();
            Assert.IsTrue(sink.Contains("openPalm", GesturePhase.Ended));
            Assert.IsTrue(sink.Contains("closedFist", GesturePhase.Began));
            Assert.IsTrue(last2.Any(e => e.Id == "openPalm" && e.Phase == GesturePhase.Ended));
        }

        [Test]
        public void Reset_ClearsActiveState_WithoutEmitting()
        {
            var r = new StaticGestureRecognizer();
            var sink = new TestSink();

            r.Process(Frame(0, BuiltinGestureType.OpenPalm, 0.9f), sink);
            sink.Clear();

            r.Reset();
            Assert.IsEmpty(sink.Events);

            // after reset the same gesture begins again
            r.Process(Frame(33, BuiltinGestureType.OpenPalm, 0.9f), sink);
            Assert.IsTrue(sink.Contains("openPalm", GesturePhase.Began));
        }

        [Test]
        public void NoneClassification_NeverEmits()
        {
            var r = new StaticGestureRecognizer();
            var sink = new TestSink();
            for (int i = 0; i < 30; i++)
                r.Process(Frame(i * 33, BuiltinGestureType.None, 0.99f), sink);
            Assert.IsEmpty(sink.Events);
        }
    }

    public class StaticGestureRecognizerConformance : RecognizerConformanceSuite
    {
        protected override IGestureRecognizer CreateRecognizer() => new StaticGestureRecognizer();

        protected override IEnumerable<GestureFrame> GetSampleFrames()
        {
            var hand = new HandData(Handedness.Right, new Vector2(0.5f, 0.5f), new Vector3[21]);
            for (int i = 0; i < 30; i++)
            {
                var type = i < 15 ? BuiltinGestureType.OpenPalm : BuiltinGestureType.Victory;
                yield return new GestureFrame(i * 33, hand, PoseData.Absent, new BuiltinGesture(type, 0.9f));
            }
            for (int i = 30; i < 40; i++)
                yield return new GestureFrame(i * 33, HandData.Absent, PoseData.Absent, BuiltinGesture.None);
        }
    }
}
