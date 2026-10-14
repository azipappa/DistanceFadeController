using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Azipa.DistanceFade.Editor
{
    public static class DistanceFadeTargetResolver
    {
        // Preserve object scopes if MA moves a renderer out of a merged hierarchy.
        public sealed class BuildTargets
        {
            public readonly Dictionary<DistanceFadeController, HashSet<Renderer>> Scopes =
                new Dictionary<DistanceFadeController, HashSet<Renderer>>();
            public void Capture(GameObject root)
            {
                foreach (var c in root.GetComponentsInChildren<DistanceFadeController>(true))
                    Scopes[c] = new HashSet<Renderer>(Collect(root).Where(r => ObjectMatches(c, r)));
            }
            public bool Includes(DistanceFadeController c, Renderer r, Material m)
            {
                if (c.targetMode == TargetMode.All) return true;
                var original = nadena.dev.ndmf.ObjectRegistry.GetReference(m).Object as Material;
                var matched = Scopes.TryGetValue(c, out var scope) && scope.Contains(r)
                    || ObjectMatches(c, r) || (c.materials ?? new Material[0]).Contains(m)
                    || original && (c.materials ?? new Material[0]).Contains(original);
                return c.targetMode == TargetMode.IncludeOnly ? matched : !matched;
            }
        }
        public static Renderer[] Collect(GameObject root) => root.GetComponentsInChildren<Renderer>(true)
            .Where(r => r is MeshRenderer || r is SkinnedMeshRenderer).ToArray();

        public static bool Includes(DistanceFadeController controller, Renderer renderer, Material material)
        {
            if (controller.targetMode == TargetMode.All) return true;
            var matched = ObjectMatches(controller, renderer) || (controller.materials ?? new Material[0]).Contains(material);
            return controller.targetMode == TargetMode.IncludeOnly ? matched : !matched;
        }
        private static bool ObjectMatches(DistanceFadeController controller, Renderer renderer) =>
            (controller.objects ?? new GameObject[0]).Any(o => o && renderer.transform.IsChildOf(o.transform))
            || (controller.renderers ?? new Renderer[0]).Contains(renderer);
    }
}
