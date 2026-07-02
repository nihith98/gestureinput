using System;
using System.Reflection;
using UnityEngine;
using GestureInput.Core;

namespace GestureInput.Unity.Registration
{
    /// <summary>Reflection-based recognizer discovery. See <see cref="GestureRecognizerAttribute"/> for the AOT caveat.</summary>
    public static class AttributeDiscovery
    {
        /// <summary>
        /// Scan all loaded assemblies for [GestureRecognizer] classes and register
        /// an instance of each. Returns how many were registered. Failures on
        /// individual types are logged and skipped.
        /// </summary>
        public static int RegisterAll(GestureRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));

            int registered = 0;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }
                catch (Exception) { continue; }

                foreach (var type in types)
                {
                    if (type == null || !type.IsClass || type.IsAbstract) continue;
                    if (type.GetCustomAttribute<GestureRecognizerAttribute>() == null) continue;

                    try
                    {
                        if (!typeof(IGestureRecognizer).IsAssignableFrom(type))
                        {
                            Debug.LogWarning($"[GestureInput] {type.FullName} has [GestureRecognizer] but does not implement IGestureRecognizer.");
                            continue;
                        }

                        registry.Register((IGestureRecognizer)Activator.CreateInstance(type));
                        registered++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[GestureInput] Failed to auto-register {type.FullName}: {e.Message}");
                    }
                }
            }
            return registered;
        }
    }
}
