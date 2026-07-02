// Sanity checks for the UnityEngine shim itself (harness-local, never compiled by Unity).
using NUnit.Framework;
using UnityEngine;

namespace GestureInput.DevTests
{
    [TestFixture]
    public class ShimSmokeTests
    {
        [Test]
        public void Vector2_Arithmetic_Works()
        {
            var v = new Vector2(3f, 4f);
            Assert.AreEqual(5f, v.magnitude, 1e-5f);
            Assert.AreEqual(new Vector2(6f, 8f), v * 2f);
            Assert.AreEqual(new Vector2(1f, 1f), new Vector2(4f, 5f) - v);
            Assert.AreEqual(1f, v.normalized.magnitude, 1e-5f);
        }

        [Test]
        public void Mathf_Clamp_And_Approximately_Work()
        {
            Assert.AreEqual(0.5f, Mathf.Clamp01(0.5f));
            Assert.AreEqual(1f, Mathf.Clamp01(7f));
            Assert.IsTrue(Mathf.Approximately(0.1f + 0.2f, 0.3f));
        }
    }
}
