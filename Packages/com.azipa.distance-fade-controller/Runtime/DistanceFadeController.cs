using UnityEngine;
using nadena.dev.ndmf;

namespace Azipa.DistanceFade
{
    [DisallowMultipleComponent]
    [AddComponentMenu("DistanceFadeController/Distance Fade Controller")]
    public sealed class DistanceFadeController : MonoBehaviour, INDMFEditorOnly
    {
        [Tooltip("Enable build-time overrides / ビルド時の一括上書きを有効化")]
        public bool enableOverride = true;
        public DistanceFadeSettings settings = new DistanceFadeSettings();
        [Tooltip("All / Exclude / Include Only")]
        public TargetMode targetMode;
        [Tooltip("Objects and all descendants / 指定オブジェクトとその子階層")]
        public GameObject[] objects = new GameObject[0];
        [Tooltip("Individual renderers / Renderer単位の指定")]
        public Renderer[] renderers = new Renderer[0];
        [Tooltip("Individual material assets / Material単位の指定")]
        public Material[] materials = new Material[0];
    }
}
