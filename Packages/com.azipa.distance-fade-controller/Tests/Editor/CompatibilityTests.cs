using Azipa.DistanceFade.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using nadena.dev.ndmf;
using VRC.SDK3.Avatars.Components;

namespace Azipa.DistanceFade.Tests
{
    public sealed class CompatibilityTests : DistanceFadeTestBase
    {
        [TestCase("lilToon")]
        [TestCase("Hidden/lilToonCutout")]
        [TestCase("Hidden/lilToonTransparent")]
        [TestCase("_lil/lilToonMulti")]
        public void SupportedLilToonShaders(string name)
        {
            var shader = Shader.Find(name);
            Assert.IsNotNull(shader, name);
            var material = Own(new Material(shader));
            Assert.IsTrue(DistanceFadeMaterialValidator.IsSupported(material));
            var clone = new DistanceFadeMaterialProcessor(new DistanceFadeSettings(), obj => owned.Add(obj)).Process(material);
            Assert.AreEqual(1, clone.GetVector("_DistanceFade").z);
        }
        [Test] public void UnsupportedShaderSkipped()
        {
            var material = Own(new Material(Shader.Find("Standard")));
            Assert.IsFalse(DistanceFadeMaterialValidator.IsSupported(material));
            Assert.AreEqual(material, new DistanceFadeMaterialProcessor(new DistanceFadeSettings(), null).Process(material));
        }
        [TestCase("Hidden/lilToonLite")]
        [TestCase("DistanceFadeControllerTests/Unrelated")]
        public void ShaderWithPropertyButWithoutFadeSupportIsSkipped(string name)
        {
            var shader = Shader.Find(name); Assert.IsNotNull(shader);
            var material = Own(new Material(shader));
            if (name == "DistanceFadeControllerTests/Unrelated") Assert.IsTrue(material.HasProperty("_DistanceFade"));
            Assert.IsFalse(DistanceFadeMaterialValidator.IsSupported(material));
        }
        [TestCase(false)] [TestCase(true)]
        public void ModularAvatarMergedInactiveOutfitRetainsTargetScope(bool excluded)
        {
            var mergeType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("nadena.dev.modular_avatar.core.ModularAvatarMergeArmature")).FirstOrDefault(t => t != null);
            var toggleType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("nadena.dev.modular_avatar.core.ModularAvatarObjectToggle")).FirstOrDefault(t => t != null);
            if (mergeType == null || toggleType == null) Assert.Ignore("Modular Avatar is optional and not installed");
            var avatar = Own(new GameObject("MA Avatar")); avatar.AddComponent<VRCAvatarDescriptor>();
            var armature = new GameObject("Armature"); armature.transform.SetParent(avatar.transform);
            var outfit = new GameObject("Outfit"); outfit.transform.SetParent(avatar.transform);
            var outfitArmature = new GameObject("Armature"); outfitArmature.transform.SetParent(outfit.transform);
            var body = new GameObject("OutfitMesh"); body.transform.SetParent(outfitArmature.transform);
            var source = Material(); body.AddComponent<MeshRenderer>().sharedMaterial = source; body.SetActive(false);
            var merge = outfitArmature.AddComponent(mergeType);
            var serializedMerge = new SerializedObject(merge);
            serializedMerge.FindProperty("mergeTarget.referencePath").stringValue = "Armature";
            serializedMerge.FindProperty("mergeTarget.targetObject").objectReferenceValue = armature;
            serializedMerge.ApplyModifiedPropertiesWithoutUndo();
            var toggle = outfit.AddComponent(toggleType);
            var serializedToggle = new SerializedObject(toggle);
            var objects = serializedToggle.FindProperty("m_objects"); objects.arraySize = 1;
            objects.GetArrayElementAtIndex(0).FindPropertyRelative("Object.referencePath").stringValue = "Outfit/Armature/OutfitMesh";
            objects.GetArrayElementAtIndex(0).FindPropertyRelative("Object.targetObject").objectReferenceValue = body;
            objects.GetArrayElementAtIndex(0).FindPropertyRelative("Active").boolValue = false;
            serializedToggle.ApplyModifiedPropertiesWithoutUndo();
            var controllerObject = new GameObject("Controller"); controllerObject.transform.SetParent(avatar.transform);
            var c = controllerObject.AddComponent<DistanceFadeController>();
            c.targetMode = excluded ? TargetMode.Exclude : TargetMode.IncludeOnly; c.objects = new[] { outfitArmature };
            var clone = Own(UnityEngine.Object.Instantiate(avatar)); AvatarProcessor.ProcessAvatar(clone);
            var built = clone.GetComponentsInChildren<MeshRenderer>(true).Single();
            Assert.AreEqual(excluded ? 0 : 1, built.sharedMaterial.GetVector("_DistanceFade").z);
            Assert.AreEqual(source, body.GetComponent<MeshRenderer>().sharedMaterial);
            Assert.AreEqual(0, source.GetVector("_DistanceFade").z);
        }
    }
}
