using System;
using System.Linq;
using nadena.dev.ndmf.animator;
using UnityEngine;

namespace Azipa.DistanceFade.Editor
{
    public static class DistanceFadeAnimationProcessor
    {
        public static void Process(AnimatorServicesContext services, DistanceFadeController controller,
            DistanceFadeMaterialProcessor processor, Action<string, UnityEngine.Object> warn,
            Func<DistanceFadeController, Renderer, Material, bool> includes = null)
        {
            includes = includes ?? DistanceFadeTargetResolver.Includes;
            foreach (var clip in services.AnimationIndex.ClipsWithObjectCurves.ToArray())
            foreach (var binding in clip.GetObjectCurveBindings().ToArray())
            {
                var frames = clip.GetObjectCurve(binding);
                if (frames == null || !frames.Any(f => f.value is Material)) continue;
                var obj = services.ObjectPathRemapper.GetObjectForPath(binding.path);
                var renderer = obj && typeof(Renderer).IsAssignableFrom(binding.type)
                    ? obj.GetComponent(binding.type) as Renderer : null;
                if (!renderer || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer) ||
                    !binding.propertyName.StartsWith("m_Materials.Array.data[", StringComparison.Ordinal) || clip.IsMarkerClip)
                {
                    warn($"Material Swap unsupported: {clip.Name}, {binding.path}, {binding.propertyName} / 未対応の参照", obj);
                    continue;
                }
                var changed = false;
                // GetObjectCurve returns cached data; copy before editing.
                frames = frames.ToArray();
                for (var i = 0; i < frames.Length; i++)
                {
                    var material = frames[i].value as Material;
                    if (!material || !includes(controller, renderer, material)) continue;
                    var replacement = processor.Process(material);
                    if (replacement == material) continue;
                    frames[i].value = replacement;
                    changed = true;
                }
                if (changed) clip.SetObjectCurve(binding, frames);
            }
            foreach (var clip in services.ControllerContext.GetAllControllers().SelectMany(c => c.AllReachableNodes()).OfType<VirtualClip>().Distinct())
            foreach (var binding in clip.GetFloatCurveBindings())
            {
                if (binding.propertyName.IndexOf("_DistanceFade", StringComparison.Ordinal) < 0) continue;
                var obj = services.ObjectPathRemapper.GetObjectForPath(binding.path);
                warn($"Animated fade property may override build settings: {clip.Name}, {binding.path}, {binding.propertyName} / フェード値のアニメーションがあります", obj);
            }
        }
    }
}
