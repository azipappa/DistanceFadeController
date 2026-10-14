using System.Linq;
using Azipa.DistanceFade.Editor;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Azipa.DistanceFade.Tests
{
    public class BuildTests : DistanceFadeTestBase
    {
        [TestCase(false)] [TestCase(true)]
        public void NdmfBuildReplacesRendererAndAnimationWithoutChangingOriginals(bool excluded)
        {
            var source = Material();
            var swap = Material();
            var avatar = Own(new GameObject("Test Avatar"));
            avatar.AddComponent<VRCAvatarDescriptor>();
            var body = new GameObject("Body");
            body.transform.SetParent(avatar.transform);
            var renderer = body.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = source;
            body.SetActive(false);
            var controllerObject = new GameObject("DistanceFadeController");
            controllerObject.transform.SetParent(avatar.transform);
            var controller = controllerObject.AddComponent<DistanceFadeController>();
            if (excluded) { controller.targetMode = TargetMode.Exclude; controller.objects = new[] { body }; }
            var clip = Own(new AnimationClip());
            var binding = EditorCurveBinding.PPtrCurve("Body", typeof(MeshRenderer), "m_Materials.Array.data[0]");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, new[] { new ObjectReferenceKeyframe { time = 0, value = swap } });
            var animatorController = Own(new AnimatorController());
            animatorController.AddLayer("Swap");
            owned.Add(animatorController.layers[0].stateMachine);
            var state = animatorController.layers[0].stateMachine.AddState("Swap");
            owned.Add(state);
            state.motion = clip;
            avatar.AddComponent<Animator>().runtimeAnimatorController = animatorController;
            var clone = Own(Object.Instantiate(avatar));
            AvatarProcessor.ProcessAvatar(clone);
            var builtRenderer = clone.transform.Find("Body").GetComponent<MeshRenderer>();
            Assert.AreEqual(excluded, builtRenderer.sharedMaterial == source);
            Assert.AreEqual(0, source.GetVector("_DistanceFade").z);
            Assert.AreEqual(0, swap.GetVector("_DistanceFade").z);
            Assert.AreEqual(source, renderer.sharedMaterial);
            Assert.AreEqual(swap, AnimationUtility.GetObjectReferenceCurve(clip, binding)[0].value);
            var builtClip = clone.GetComponent<Animator>().runtimeAnimatorController.animationClips.First(c => AnimationUtility.GetObjectReferenceCurveBindings(c).Length > 0);
            var builtMaterial = (Material)AnimationUtility.GetObjectReferenceCurve(builtClip, binding)[0].value;
            Assert.AreEqual(excluded ? 0 : 1, builtMaterial.GetVector("_DistanceFade").z);
            Assert.IsNull(clone.GetComponentInChildren<DistanceFadeController>(true));
        }
        [TestCase(false)] [TestCase(true)]
        public void BlendTreeAndOverrideControllerSwapUsesBuildCopies(bool useOverride)
        {
            var avatar = Own(new GameObject("Avatar"));
            avatar.AddComponent<VRCAvatarDescriptor>();
            var child = new GameObject("Body"); child.transform.SetParent(avatar.transform);
            var source = Material(); var swap = Material();
            child.AddComponent<SkinnedMeshRenderer>().sharedMaterial = source;
            var c = new GameObject("Controller"); c.transform.SetParent(avatar.transform);
            c.AddComponent<DistanceFadeController>();
            var clip = Own(new AnimationClip { name = "Swap" });
            var binding = EditorCurveBinding.PPtrCurve("Body", typeof(SkinnedMeshRenderer), "m_Materials.Array.data[0]");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, new[] { new ObjectReferenceKeyframe { time = 0, value = swap } });
            var controller = Own(new AnimatorController()); controller.AddLayer("Swap");
            owned.Add(controller.layers[0].stateMachine);
            var state = controller.layers[0].stateMachine.AddState("Swap"); owned.Add(state);
            var tree = Own(new BlendTree { blendType = BlendTreeType.Simple1D, blendParameter = "Blend" });
            controller.AddParameter("Blend", AnimatorControllerParameterType.Float);
            tree.AddChild(clip); state.motion = tree;
            RuntimeAnimatorController assigned = controller;
            if (useOverride)
            {
                var overrideClip = Own(Object.Instantiate(clip)); overrideClip.name = "Override";
                var overrideController = Own(new AnimatorOverrideController(controller));
                overrideController[clip.name] = overrideClip; assigned = overrideController;
            }
            avatar.AddComponent<Animator>().runtimeAnimatorController = assigned;
            var clone = Own(Object.Instantiate(avatar));
            AvatarProcessor.ProcessAvatar(clone);
            var built = clone.GetComponent<Animator>().runtimeAnimatorController.animationClips
                .First(x => AnimationUtility.GetObjectReferenceCurveBindings(x).Length > 0);
            var result = (Material)AnimationUtility.GetObjectReferenceCurve(built, binding)[0].value;
            Assert.AreNotEqual(swap, result);
            Assert.AreEqual(1, result.GetVector("_DistanceFade").z);
            Assert.AreEqual(swap, AnimationUtility.GetObjectReferenceCurve(clip, binding)[0].value);
            Assert.AreEqual(assigned, avatar.GetComponent<Animator>().runtimeAnimatorController);
        }
        [TestCase(false)] [TestCase(true)]
        public void DisabledOrDeletedControllerLeavesSourceReference(bool deleted)
        {
            var avatar = Own(new GameObject("Avatar")); avatar.AddComponent<VRCAvatarDescriptor>();
            var source = Material(); avatar.AddComponent<MeshRenderer>().sharedMaterial = source;
            var controller = avatar.AddComponent<DistanceFadeController>(); controller.enableOverride = false;
            if (deleted) Object.DestroyImmediate(controller);
            var clone = Own(Object.Instantiate(avatar)); AvatarProcessor.ProcessAvatar(clone);
            Assert.AreEqual(source, clone.GetComponent<MeshRenderer>().sharedMaterial);
            Assert.AreEqual(0, source.GetVector("_DistanceFade").z);
        }
        [Test] public void IncludeOnlyKeepsOtherRenderersOriginal()
        {
            var avatar = Own(new GameObject("Avatar"));
            var c = avatar.AddComponent<DistanceFadeController>();
            var first = new GameObject("First"); first.transform.SetParent(avatar.transform);
            var second = new GameObject("Second"); second.transform.SetParent(avatar.transform);
            var a = first.AddComponent<MeshRenderer>(); var b = second.AddComponent<MeshRenderer>();
            var material = Material();
            c.targetMode = TargetMode.IncludeOnly; c.objects = new[] { first };
            Assert.IsTrue(DistanceFadeTargetResolver.Includes(c, a, material));
            Assert.IsFalse(DistanceFadeTargetResolver.Includes(c, b, material));
            first.SetActive(false);
            Assert.AreEqual(2, DistanceFadeTargetResolver.Collect(avatar).Length);
        }
        [Test] public void MaterialOnlyIncludeProcessesSwapWithoutChangingRendererAssignment()
        {
            var avatar = Own(new GameObject("Avatar")); avatar.AddComponent<VRCAvatarDescriptor>();
            var body = new GameObject("Body"); body.transform.SetParent(avatar.transform);
            var source = Material(); var swap = Material();
            body.AddComponent<MeshRenderer>().sharedMaterial = source;
            var c = avatar.AddComponent<DistanceFadeController>();
            c.targetMode = TargetMode.IncludeOnly; c.materials = new[] { swap };
            var clip = Own(new AnimationClip());
            var binding = EditorCurveBinding.PPtrCurve("Body", typeof(MeshRenderer), "m_Materials.Array.data[0]");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, new[] { new ObjectReferenceKeyframe { time = 0, value = swap } });
            var controller = Own(new AnimatorController()); controller.AddLayer("Swap");
            owned.Add(controller.layers[0].stateMachine);
            var state = controller.layers[0].stateMachine.AddState("Swap"); owned.Add(state); state.motion = clip;
            avatar.AddComponent<Animator>().runtimeAnimatorController = controller;
            var sourceText = System.IO.File.ReadAllText(AssetDatabase.GetAssetPath(source));
            var swapText = System.IO.File.ReadAllText(AssetDatabase.GetAssetPath(swap));
            var clone = Own(Object.Instantiate(avatar)); AvatarProcessor.ProcessAvatar(clone);
            Assert.AreEqual(source, clone.transform.Find("Body").GetComponent<MeshRenderer>().sharedMaterial);
            var builtClip = clone.GetComponent<Animator>().runtimeAnimatorController.animationClips.First(x => AnimationUtility.GetObjectReferenceCurveBindings(x).Length > 0);
            var builtSwap = (Material)AnimationUtility.GetObjectReferenceCurve(builtClip, binding)[0].value;
            Assert.AreEqual(1, builtSwap.GetVector("_DistanceFade").z);
            Assert.AreEqual(sourceText, System.IO.File.ReadAllText(AssetDatabase.GetAssetPath(source)));
            Assert.AreEqual(swapText, System.IO.File.ReadAllText(AssetDatabase.GetAssetPath(swap)));
        }
    }
}
