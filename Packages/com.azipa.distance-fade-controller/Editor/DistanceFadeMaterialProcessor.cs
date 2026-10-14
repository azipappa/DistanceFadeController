using System;
using System.Collections.Generic;
using UnityEngine;

namespace Azipa.DistanceFade.Editor
{
    public sealed class DistanceFadeMaterialProcessor
    {
        private readonly DistanceFadeSettings settings;
        private readonly Action<UnityEngine.Object> save;
        private readonly Action<string, UnityEngine.Object> warn;
        // One immutable settings snapshot per processor; source is sufficient within this scope.
        private readonly Dictionary<Material, Material> cache = new Dictionary<Material, Material>();
        public DistanceFadeMaterialProcessor(DistanceFadeSettings settings, Action<UnityEngine.Object> save,
            Action<string, UnityEngine.Object> warn = null)
        {
            this.settings = JsonUtility.FromJson<DistanceFadeSettings>(JsonUtility.ToJson(settings));
            this.save = save;
            this.warn = warn;
        }
        public bool HasOverrides(Material source) => DistanceFadeMaterialValidator.IsSupported(source) && (
            settings.overrideParameters || settings.overrideBackface || settings.overrideColor && source.HasProperty("_DistanceFadeColor") ||
            settings.overrideMode && source.HasProperty("_DistanceFadeMode") ||
            settings.overrideRimColor && source.HasProperty("_DistanceFadeRimColor") ||
            settings.overrideRimPower && source.HasProperty("_DistanceFadeRimFresnelPower"));

        public Material Process(Material source)
        {
            if (!HasOverrides(source)) return source;
            var error = settings.ValidationError();
            if (error != null) throw new InvalidOperationException(error);
            if (cache.TryGetValue(source, out var existing)) return existing;
            var clone = new Material(source) { name = source.name + " (DistanceFadeController)" };
            try
            {
                if (settings.overrideParameters || settings.overrideBackface)
                {
                    var value = source.GetVector("_DistanceFade");
                    if (settings.overrideParameters)
                    {
                        var parameters = settings.EffectiveParameters;
                        value.x = parameters.x;
                        value.y = parameters.y;
                        value.z = parameters.z;
                    }
                    if (settings.overrideBackface) value.w = settings.parameters.w;
                    clone.SetVector("_DistanceFade", value);
                }
                Set(clone, "_DistanceFadeColor", settings.overrideColor, () => clone.SetColor("_DistanceFadeColor", settings.color));
                Set(clone, "_DistanceFadeMode", settings.overrideMode, () => clone.SetInt("_DistanceFadeMode", (int)settings.mode));
                Set(clone, "_DistanceFadeRimColor", settings.overrideRimColor, () => clone.SetColor("_DistanceFadeRimColor", settings.rimColor));
                Set(clone, "_DistanceFadeRimFresnelPower", settings.overrideRimPower, () => clone.SetFloat("_DistanceFadeRimFresnelPower", settings.rimPower));
                DistanceFadeMaterialValidator.SetupClone(clone);
                save?.Invoke(clone);
                cache.Add(source, clone);
                return clone;
            }
            catch { UnityEngine.Object.DestroyImmediate(clone); throw; }
        }
        private void Set(Material clone, string property, bool enabled, Action apply)
        {
            if (!enabled) return;
            if (clone.HasProperty(property)) apply();
            else warn?.Invoke($"{clone.name}: unsupported property {property} / 非対応プロパティをスキップ", clone);
        }
    }
}
