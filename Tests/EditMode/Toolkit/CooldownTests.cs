using System;
using NUnit.Framework;
using GestureInput.Core;

namespace GestureInput.Tests
{
    [TestFixture]
    public class CooldownTests
    {
        [Test]
        public void Ctor_RejectsNegativeDuration()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Cooldown(-1));
        }

        [Test]
        public void ReadyBeforeFirstTrigger()
        {
            var c = new Cooldown(600);
            Assert.IsTrue(c.Ready(0));
            Assert.IsTrue(c.Ready(12345));
        }

        [Test]
        public void NotReadyDuringWindow_ReadyAfter()
        {
            var c = new Cooldown(600);
            c.Trigger(1000);
            Assert.IsFalse(c.Ready(1000));
            Assert.IsFalse(c.Ready(1599));
            Assert.IsTrue(c.Ready(1600));
        }

        [Test]
        public void Retrigger_RestartsWindow()
        {
            var c = new Cooldown(600);
            c.Trigger(1000);
            c.Trigger(1600);
            Assert.IsFalse(c.Ready(2100));
            Assert.IsTrue(c.Ready(2200));
        }

        [Test]
        public void Reset_MakesImmediatelyReady()
        {
            var c = new Cooldown(600);
            c.Trigger(1000);
            c.Reset();
            Assert.IsTrue(c.Ready(1001));
        }
    }
}
