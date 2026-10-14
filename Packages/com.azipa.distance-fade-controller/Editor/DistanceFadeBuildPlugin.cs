using System;
using System.Linq;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEngine;

[assembly: ExportsPlugin(typeof(Azipa.DistanceFade.Editor.DistanceFadeBuildPlugin))]
namespace Azipa.DistanceFade.Editor
{
    public sealed class DistanceFadeBuildPlugin : Plugin<DistanceFadeBuildPlugin>
    {
        public override string QualifiedName => "com.azipa.distance-fade-controller";
        public override string DisplayName => "DistanceFadeController";
        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving).BeforePlugin("nadena.dev.modular-avatar")
                .Run("Capture target hierarchy scopes", context =>
                    context.GetState<DistanceFadeTargetResolver.BuildTargets>().Capture(context.AvatarRootObject));
            InPhase(BuildPhase.Transforming)
                .AfterPlugin("nadena.dev.modular-avatar")
                .AfterPlugin("nadena.dev.modular-avatar.late-transform-stages")
                .WithRequiredExtension(typeof(AnimatorServicesContext), sequence =>
                    sequence.Run("Apply distance fade and Material Swap", Apply));
        }
        public static void Apply(BuildContext context)
        {
            var controllers = context.AvatarRootObject.GetComponentsInChildren<DistanceFadeController>(true);
            var active = controllers.Where(c => c.enabled && c.enableOverride).ToArray();
            if (active.Length > 1)
                throw new InvalidOperationException("Multiple DistanceFadeControllers: keep one per avatar / Controllerはアバターに1つだけ配置してください");
            if (active.Length == 1)
            {
                var controller = active[0];
                var error = controller.settings?.ValidationError() ?? (controller.settings == null ? "Settings missing" : null);
                if (error != null) throw new InvalidOperationException(controller.name + ": " + error);
                Action<string, UnityEngine.Object> warn = (message, obj) => Debug.LogWarning("[DistanceFadeController] " + message, obj);
                var processor = new DistanceFadeMaterialProcessor(controller.settings, context.AssetSaver.SaveAsset, warn);
                var services = context.Extension<AnimatorServicesContext>();
                var targets = context.GetState<DistanceFadeTargetResolver.BuildTargets>();
                // Resolve animated materials before renderer replacements so Material-only filters use source identities.
                DistanceFadeAnimationProcessor.Process(services, controller, processor, warn, targets.Includes);
                foreach (var renderer in DistanceFadeTargetResolver.Collect(context.AvatarRootObject))
                {
                    var slots = renderer.sharedMaterials;
                    var changed = false;
                    for (var i = 0; i < slots.Length; i++)
                    {
                        if (!slots[i] || !targets.Includes(controller, renderer, slots[i])) continue;
                        var replacement = processor.Process(slots[i]);
                        changed |= replacement != slots[i];
                        slots[i] = replacement;
                    }
                    if (changed) renderer.sharedMaterials = slots;
                }
                foreach (var animation in context.AvatarRootObject.GetComponentsInChildren<Animation>(true))
                    warn("Legacy Animation is unsupported: " + animation.name + " / Legacy AnimationのMaterial Swapは未対応", animation);
            }
            // Strip only our component, retaining any user-created children/components on its object.
            foreach (var controller in controllers) UnityEngine.Object.DestroyImmediate(controller);
        }
    }
}
