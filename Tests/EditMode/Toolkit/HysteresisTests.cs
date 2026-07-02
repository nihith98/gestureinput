using System;
using NUnit.Framework;
using GestureInput.Core;

namespace GestureInput.Tests
{
    [TestFixture]
    public class HysteresisTests
    {
        [Test]
        public void Ctor_RequiresEnterAboveExit()
        {
            Assert.Throws<ArgumentException>(() => new Hysteresis(0.5f, 0.5f));
            Assert.Throws<ArgumentException>(() => new Hysteresis(0.4f, 0.6f));
        }

        [Test]
        public void StartsInactive_ActivatesOnlyAtEnterThreshold()
        {
            var h = new Hysteresis(enter: 0.7f, exit: 0.5f);
            Assert.IsFalse(h.IsActive);
            Assert.IsFalse(h.Update(0.69f));
            Assert.IsTrue(h.Update(0.7f));
            Assert.IsTrue(h.IsActive);
        }

        [Test]
        public void MidBandOscillation_DoesNotFlicker()
        {
            var h = new Hysteresis(enter: 0.7f, exit: 0.5f);
            h.Update(0.9f); // activate

            // oscillate between exit and enter thresholds — must stay active
            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(h.Update(0.55f));
                Assert.IsTrue(h.Update(0.65f));
            }
        }

        [Test]
        public void DropsOnlyBelowExitThreshold()
        {
            var h = new Hysteresis(enter: 0.7f, exit: 0.5f);
            h.Update(0.9f);
            Assert.IsTrue(h.Update(0.5f));   // at exit — still active
            Assert.IsFalse(h.Update(0.49f)); // below exit — deactivates
            Assert.IsFalse(h.Update(0.6f));  // mid-band from below — stays inactive
        }

        [Test]
        public void Reset_ReturnsToInactive()
        {
            var h = new Hysteresis(enter: 0.7f, exit: 0.5f);
            h.Update(0.9f);
            h.Reset();
            Assert.IsFalse(h.IsActive);
            Assert.IsFalse(h.Update(0.6f)); // mid-band must not re-activate after reset
        }
    }
}
