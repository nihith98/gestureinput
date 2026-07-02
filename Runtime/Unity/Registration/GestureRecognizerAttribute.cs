using System;

namespace GestureInput.Unity.Registration
{
    /// <summary>
    /// Opt-in attribute discovery: mark an <c>IGestureRecognizer</c> with a
    /// parameterless constructor and call
    /// <see cref="AttributeDiscovery.RegisterAll"/> at startup to register every
    /// marked type.
    ///
    /// CAVEAT: this relies on reflection, which IL2CPP/AOT stripping can break on
    /// mobile. Explicit registration and <see cref="GestureRecognizerAsset"/> are
    /// the guaranteed paths; use this only where you control the stripping level.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class GestureRecognizerAttribute : Attribute
    {
    }
}
