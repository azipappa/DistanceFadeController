using Azipa.DistanceFade.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Azipa.DistanceFade.Tests
{
    public sealed class MenuTests : DistanceFadeTestBase
    {
        [Test] public void AddsPrefabOnceAndUndoRemovesIt()
        {
            var avatar = Own(new GameObject("Test Avatar"));
            avatar.AddComponent<VRCAvatarDescriptor>();
            Undo.IncrementCurrentGroup();
            var controller = DistanceFadeMenu.AddController(avatar);
            Assert.IsNotNull(controller);
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(controller));
            Assert.AreEqual(Vector3.zero, controller.transform.localPosition);
            Assert.AreEqual(controller, DistanceFadeMenu.AddController(avatar));
            Assert.AreEqual(1, avatar.GetComponentsInChildren<DistanceFadeController>(true).Length);
            Undo.PerformUndo();
            Assert.AreEqual(0, avatar.GetComponentsInChildren<DistanceFadeController>(true).Length);
        }
        [Test] public void RejectsNonAvatarAndPrefabAsset()
        {
            Assert.IsNull(DistanceFadeMenu.AddController(Own(new GameObject("Not Avatar"))));
            Assert.IsFalse(DistanceFadeMenu.IsValidAvatar(AssetDatabase.LoadAssetAtPath<GameObject>(DistanceFadeMenu.PrefabPath)));
        }
        [Test] public void SerializedSettingsRecordPrefabOverride()
        {
            var avatar = Own(new GameObject("Test Avatar"));
            avatar.AddComponent<VRCAvatarDescriptor>();
            var controller = DistanceFadeMenu.AddController(avatar);
            var so = new SerializedObject(controller);
            so.FindProperty("settings.overrideColor").boolValue = true;
            so.ApplyModifiedProperties();
            Assert.IsTrue(controller.settings.overrideColor);
            Assert.IsTrue(new SerializedObject(controller).FindProperty("settings.overrideColor").prefabOverride);
        }
    }
}
