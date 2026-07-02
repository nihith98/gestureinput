using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using GestureInput.Core;

namespace GestureInput.Tests
{
    internal sealed class FakeRecognizer : IGestureRecognizer
    {
        private readonly GestureDescriptor[] _descriptors;
        public int ProcessCalls;
        public int ResetCalls;
        public Func<GestureFrame, GestureEvent?> OnProcess;

        public FakeRecognizer(params string[] ids)
        {
            _descriptors = ids.Select(id => new GestureDescriptor(id, GestureKind.Discrete)).ToArray();
        }

        public IReadOnlyList<GestureDescriptor> Descriptors => _descriptors;
        public void Reset() => ResetCalls++;

        public void Process(in GestureFrame frame, IGestureSink sink)
        {
            ProcessCalls++;
            var e = OnProcess?.Invoke(frame);
            if (e.HasValue) sink.Emit(e.Value);
        }
    }

    internal sealed class ThrowingRecognizer : IGestureRecognizer
    {
        public IReadOnlyList<GestureDescriptor> Descriptors { get; } =
            new[] { new GestureDescriptor("boom", GestureKind.Discrete) };

        public void Reset() { }
        public void Process(in GestureFrame frame, IGestureSink sink) => throw new InvalidOperationException("boom");
    }

    [TestFixture]
    public class GestureRegistryTests
    {
        private static GestureFrame Frame(long ts = 0) =>
            new GestureFrame(ts, HandData.Absent, PoseData.Absent, BuiltinGesture.None);

        [Test]
        public void Register_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GestureRegistry().Register(null));
        }

        [Test]
        public void Register_DuplicateIdAcrossRecognizers_Throws()
        {
            var reg = new GestureRegistry();
            reg.Register(new FakeRecognizer("wave"));
            Assert.Throws<ArgumentException>(() => reg.Register(new FakeRecognizer("wave", "other")));
            Assert.AreEqual(1, reg.Recognizers.Count);
        }

        [Test]
        public void AllDescriptors_IsUnionInRegistrationOrder()
        {
            var reg = new GestureRegistry();
            reg.Register(new FakeRecognizer("a", "b"));
            reg.Register(new FakeRecognizer("c"));
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, reg.AllDescriptors.Select(d => d.Id).ToArray());
        }

        [Test]
        public void Remove_UnregistersAndFreesIds()
        {
            var reg = new GestureRegistry();
            var r = new FakeRecognizer("wave");
            reg.Register(r);
            Assert.IsTrue(reg.Remove(r));
            Assert.AreEqual(0, reg.Recognizers.Count);
            Assert.DoesNotThrow(() => reg.Register(new FakeRecognizer("wave")));
            Assert.IsFalse(reg.Remove(r)); // already gone
        }

        [Test]
        public void ProcessFrame_RunsAllRecognizers_CollectsEvents()
        {
            var reg = new GestureRegistry();
            var r1 = new FakeRecognizer("a") { OnProcess = f => new GestureEvent("a", GesturePhase.Began, 1f) };
            var r2 = new FakeRecognizer("b");
            reg.Register(r1);
            reg.Register(r2);

            var sink = new TestSink();
            reg.ProcessFrame(Frame(), sink);

            Assert.AreEqual(1, r1.ProcessCalls);
            Assert.AreEqual(1, r2.ProcessCalls);
            Assert.IsTrue(sink.Contains("a", GesturePhase.Began));
            Assert.AreEqual(1, sink.Events.Count);
        }

        [Test]
        public void ProcessFrame_ThrowingRecognizer_IsIsolated_OthersStillRun()
        {
            var reg = new GestureRegistry();
            var good = new FakeRecognizer("a") { OnProcess = f => new GestureEvent("a", GesturePhase.Began, 1f) };
            reg.Register(new ThrowingRecognizer());
            reg.Register(good);

            IGestureRecognizer errored = null;
            Exception captured = null;
            reg.OnRecognizerError += (r, e) => { errored = r; captured = e; };

            var sink = new TestSink();
            Assert.DoesNotThrow(() => reg.ProcessFrame(Frame(), sink));

            Assert.IsInstanceOf<ThrowingRecognizer>(errored);
            Assert.IsInstanceOf<InvalidOperationException>(captured);
            Assert.AreEqual(1, good.ProcessCalls);
            Assert.IsTrue(sink.Contains("a", GesturePhase.Began));
        }

        [Test]
        public void ResetAll_ResetsEveryRecognizer()
        {
            var reg = new GestureRegistry();
            var r1 = new FakeRecognizer("a");
            var r2 = new FakeRecognizer("b");
            reg.Register(r1);
            reg.Register(r2);
            reg.ResetAll();
            Assert.AreEqual(1, r1.ResetCalls);
            Assert.AreEqual(1, r2.ResetCalls);
        }

        [Test]
        public void Clear_RemovesEverything()
        {
            var reg = new GestureRegistry();
            reg.Register(new FakeRecognizer("a"));
            reg.Clear();
            Assert.AreEqual(0, reg.Recognizers.Count);
            Assert.IsEmpty(reg.AllDescriptors);
        }
    }
}
