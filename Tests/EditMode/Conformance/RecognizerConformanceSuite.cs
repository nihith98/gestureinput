using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using GestureInput.Core;

namespace GestureInput.Tests
{
    /// <summary>
    /// Reusable contract tests for ANY <see cref="IGestureRecognizer"/> — built-in
    /// or third-party. Subclass, implement <see cref="CreateRecognizer"/>, and
    /// (ideally) override <see cref="GetSampleFrames"/> with a stream that makes
    /// your recognizer actually fire; every SPI rule below is then verified.
    ///
    /// <code>
    /// public class WaveRecognizerConformance : RecognizerConformanceSuite
    /// {
    ///     protected override IGestureRecognizer CreateRecognizer() => new WaveRecognizer();
    ///     protected override IEnumerable&lt;GestureFrame&gt; GetSampleFrames() => Fixtures.Load("wave.gframes");
    /// }
    /// </code>
    /// </summary>
    [TestFixture]
    public abstract class RecognizerConformanceSuite
    {
        /// <summary>A fresh recognizer instance. Must not be shared between calls.</summary>
        protected abstract IGestureRecognizer CreateRecognizer();

        /// <summary>
        /// A representative frame stream. Override with frames that exercise your
        /// recognizer (e.g. a recorded fixture); the default is 60 hand-absent frames,
        /// which only proves absence-safety.
        /// </summary>
        protected virtual IEnumerable<GestureFrame> GetSampleFrames()
        {
            for (int i = 0; i < 60; i++)
                yield return new GestureFrame(i * 33, HandData.Absent, PoseData.Absent, BuiltinGesture.None);
        }

        [Test]
        public void Descriptors_AreNonEmpty_Unique_AndValid()
        {
            var descriptors = CreateRecognizer().Descriptors;
            Assert.IsNotNull(descriptors, "Descriptors must not be null.");
            Assert.IsNotEmpty(descriptors, "A recognizer must declare at least one gesture.");

            var ids = descriptors.Select(d => d.Id).ToList();
            CollectionAssert.AllItemsAreUnique(ids, "Descriptor ids must be unique within a recognizer.");
            foreach (var id in ids)
                Assert.IsTrue(GestureDescriptor.IsValidId(id), $"Descriptor id '{id}' is not control-name safe.");
        }

        [Test]
        public void Descriptors_AreStableAcrossInstances()
        {
            var a = CreateRecognizer().Descriptors.Select(d => d.Id).ToList();
            var b = CreateRecognizer().Descriptors.Select(d => d.Id).ToList();
            CollectionAssert.AreEqual(a, b, "Descriptors must be deterministic across instances.");
        }

        [Test]
        public void EmittedIds_AreSubsetOfDescriptors()
        {
            var recognizer = CreateRecognizer();
            var declared = new HashSet<string>(recognizer.Descriptors.Select(d => d.Id));
            var sink = new TestSink();

            foreach (var frame in GetSampleFrames())
                recognizer.Process(in frame, sink);

            foreach (var e in sink.Events)
                Assert.IsTrue(declared.Contains(e.Id),
                    $"Recognizer emitted undeclared gesture id '{e.Id}'.");
        }

        [Test]
        public void Reset_MakesReplayDeterministic()
        {
            var recognizer = CreateRecognizer();
            var frames = GetSampleFrames().ToList();

            var first = new TestSink();
            foreach (var frame in frames) recognizer.Process(in frame, first);

            recognizer.Reset();

            var second = new TestSink();
            foreach (var frame in frames) recognizer.Process(in frame, second);

            Assert.AreEqual(first.Events.Count, second.Events.Count,
                "Replaying the same frames after Reset must produce the same number of events.");
            for (int i = 0; i < first.Events.Count; i++)
            {
                Assert.AreEqual(first.Events[i].Id, second.Events[i].Id, $"event {i} id differs after Reset");
                Assert.AreEqual(first.Events[i].Phase, second.Events[i].Phase, $"event {i} phase differs after Reset");
            }
        }

        [Test]
        public void HandAbsentFrames_DoNotThrow_AndEmitNoBegan()
        {
            var recognizer = CreateRecognizer();
            var sink = new TestSink();

            for (int i = 0; i < 120; i++)
            {
                var frame = new GestureFrame(i * 33, HandData.Absent, PoseData.Absent, BuiltinGesture.None);
                Assert.DoesNotThrow(() => recognizer.Process(in frame, sink));
            }

            Assert.IsFalse(sink.Events.Any(e => e.Phase == GesturePhase.Began),
                "No gesture should begin while no hand is present.");
        }

        [Test]
        public void ProcessOnFreshInstance_DefaultFrame_DoesNotThrow()
        {
            var recognizer = CreateRecognizer();
            var frame = default(GestureFrame);
            Assert.DoesNotThrow(() => recognizer.Process(in frame, new TestSink()));
        }

        [Test]
        public void Reset_OnFreshInstance_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => CreateRecognizer().Reset());
        }
    }
}
