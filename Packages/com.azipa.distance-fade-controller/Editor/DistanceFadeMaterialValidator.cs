using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Azipa.DistanceFade.Editor
{
    public static class DistanceFadeMaterialValidator
    {
        // Optional lilToon dependency. Use its public validator for custom derivatives when installed.
        private static Type LilUtils => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("lilToon.lilMaterialUtils")).FirstOrDefault(t => t != null);
        public static bool IsSupported(Material material)
        {
            if (!material || !material.shader || !material.HasProperty("_DistanceFade")) return false;
            // Lite derivatives must not opt in merely by adding a distance property.
            if (material.shader.name.IndexOf("lilToonLite", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (material.shader.name.IndexOf("lilToon", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            var check = LilUtils?.GetMethod("CheckShaderIslilToon", new[] { typeof(Material) });
            return check != null && (bool)check.Invoke(null, new object[] { material });
        }
        public static bool LilToonInstalled => LilUtils != null;
        public static void SetupClone(Material clone)
        {
            // The official multi-material helper keeps _FADING_ON consistent with vector Z.
            if (clone.shader.name.IndexOf("lilToonMulti", StringComparison.OrdinalIgnoreCase) < 0) return;
            LilUtils?.GetMethod("SetupMultiMaterial", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(Material) }, null)?.Invoke(null, new object[] { clone });
        }
    }
}
