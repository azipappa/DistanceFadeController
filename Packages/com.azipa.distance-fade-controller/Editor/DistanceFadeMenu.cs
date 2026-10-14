using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Azipa.DistanceFade.Editor
{
    public static class DistanceFadeMenu
    {
        public const string PrefabPath = "Packages/com.azipa.distance-fade-controller/Prefabs/DistanceFadeController.prefab";
        public static bool IsValidAvatar(GameObject obj) => obj && !EditorUtility.IsPersistent(obj)
            && obj.scene.IsValid() && PrefabStageUtility.GetCurrentPrefabStage() == null
            && obj.GetComponent<VRCAvatarDescriptor>();
        private static GameObject Target(MenuCommand command) => command.context as GameObject ?? Selection.activeGameObject;
        [MenuItem("GameObject/DistanceFadeController/Add Controller", false, 49)]
        private static void Add(MenuCommand command) { if (Selection.gameObjects.Length <= 1) AddController(Target(command)); }
        [MenuItem("GameObject/DistanceFadeController/Add Controller", true)]
        private static bool ValidateAdd(MenuCommand command) => Selection.gameObjects.Length <= 1 && IsValidAvatar(Target(command));
        public static DistanceFadeController AddController(GameObject avatar)
        {
            if (!IsValidAvatar(avatar)) return null;
            var existing = avatar.GetComponentInChildren<DistanceFadeController>(true);
            if (existing) { Selection.activeGameObject = existing.gameObject; return existing; }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!prefab) { Debug.LogError("DistanceFadeController prefab missing: " + PrefabPath, avatar); return null; }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, avatar.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            Undo.RegisterCreatedObjectUndo(instance, "Add DistanceFadeController");
            Selection.activeGameObject = instance;
            return instance.GetComponent<DistanceFadeController>();
        }
        [MenuItem("GameObject/DistanceFadeController/Select Controller", false, 50)]
        private static void Select(MenuCommand command)
        {
            var avatar = Target(command);
            if (!IsValidAvatar(avatar)) return;
            var controller = avatar.GetComponentInChildren<DistanceFadeController>(true);
            if (controller) Selection.activeGameObject = controller.gameObject;
            else Debug.LogWarning("DistanceFadeController not found / Controllerが見つかりません", avatar);
        }
        [MenuItem("GameObject/DistanceFadeController/Select Controller", true)]
        private static bool ValidateSelect(MenuCommand command) => ValidateAdd(command);
    }
}
