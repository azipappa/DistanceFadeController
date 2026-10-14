using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Azipa.DistanceFade.Editor
{
    [CustomEditor(typeof(DistanceFadeController))]
    public sealed class DistanceFadeControllerEditor : UnityEditor.Editor
    {
        private static bool Japanese => Application.systemLanguage == SystemLanguage.Japanese;
        private static string L(string en, string ja) => Japanese ? ja : en;
        private bool showDistanceFade = true;
        private bool showOverrides;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Field("enableOverride", "Enable Distance Fade Override", "距離フェード上書きを有効化");
            EditorGUILayout.Space();
            showDistanceFade = EditorGUILayout.BeginFoldoutHeaderGroup(showDistanceFade, L("Distance Fade", "距離フェード"));
            if (showDistanceFade)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    OverrideRow("overrideColor", rect => DrawColor(rect, "settings.color", true));
                    using (new EditorGUI.IndentLevelScope())
                    {
                        DrawParameters();
                        OverrideRow("overrideMode", rect =>
                        {
                            var mode = serializedObject.FindProperty("settings.mode");
                            EditorGUI.BeginProperty(rect, GUIContent.none, mode);
                            EditorGUI.BeginChangeCheck();
                            var selected = EditorGUI.Popup(rect, Label("Mode", "モード", "_DistanceFadeMode"), mode.intValue,
                                new[] { new GUIContent(L("Vertex", "頂点")), new GUIContent(L("Position", "位置")) });
                            if (EditorGUI.EndChangeCheck()) mode.intValue = selected;
                            EditorGUI.EndProperty();
                        });
                    }
                    var line = EditorGUILayout.GetControlRect(false, 1);
                    EditorGUI.DrawRect(line, new Color(0.5f, 0.5f, 0.5f, 0.4f));
                    EditorGUILayout.LabelField(L("Rim Light", "リムライト"));
                    using (new EditorGUI.IndentLevelScope())
                    {
                        OverrideRow("overrideRimColor", rect => DrawColor(rect, "settings.rimColor", false));
                        using (new EditorGUI.DisabledScope(!serializedObject.FindProperty("settings.overrideRimColor").boolValue))
                        {
                            var color = serializedObject.FindProperty("settings.rimColor");
                            var rect = EditorGUILayout.GetControlRect();
                            EditorGUI.BeginProperty(rect, GUIContent.none, color);
                            EditorGUI.BeginChangeCheck();
                            var value = color.colorValue;
                            value.a = EditorGUI.Slider(rect, Label("Opacity", "不透明度", "_DistanceFadeRimColor.a"), value.a, 0, 1);
                            if (EditorGUI.EndChangeCheck()) color.colorValue = value;
                            EditorGUI.EndProperty();
                        }
                        OverrideRow("overrideRimPower", DrawRimPower);
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            showOverrides = EditorGUILayout.Foldout(showOverrides, L("Properties to Override", "上書きする項目"), true);
            if (showOverrides)
            {
                Field("settings.overrideParameters", "Override Distance Parameters", "距離設定を上書き");
                Field("settings.overrideBackface", "Force Backface Shadow", "裏面を影にする");
                Field("settings.fadeEnabled", "Enable Fade", "フェード有効");
                Field("settings.overrideColor", "Override Color", "色を上書き");
                Field("settings.overrideMode", "Override Mode", "モードを上書き");
                Field("settings.overrideRimColor", "Override Rim Light Color / Opacity", "リムライトの色・不透明度を上書き");
                Field("settings.overrideRimPower", "Override Rim Light Sharpness", "リムライトの細さを上書き");
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(L("Target Settings", "対象設定"), EditorStyles.boldLabel);
            Field("targetMode", "Target Mode", "対象モード");
            if (serializedObject.FindProperty("targetMode").enumValueIndex != 0)
            {
                Field("objects", "Objects (with descendants)", "オブジェクト（子階層を含む）");
                Field("renderers", "Renderers", "Renderer");
                Field("materials", "Materials", "マテリアル");
            }
            serializedObject.ApplyModifiedProperties();
            Diagnostics((DistanceFadeController)target);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(L("Supported lilToon Version: 2.3.4", "対応lilToonバージョン：2.3.4"), EditorStyles.miniLabel);
        }
        private static GUIContent Label(string en, string ja, string tooltip) => new GUIContent(L(en, ja), tooltip);
        private void OverrideRow(string name, System.Action<Rect> draw)
        {
            var rect = EditorGUILayout.GetControlRect();
            var toggleRect = new Rect(rect.xMax - 16, rect.y, 16, rect.height);
            rect.width -= 22;
            var property = serializedObject.FindProperty("settings." + name);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(toggleRect, property, new GUIContent("", L("Override this property at build time", "チェックするとこの項目をビルド時に上書きします")));
            EditorGUI.indentLevel = indent;
            using (new EditorGUI.DisabledScope(!property.boolValue)) draw(rect);
        }
        private void DrawColor(Rect rect, string path, bool alpha)
        {
            var property = serializedObject.FindProperty(path);
            EditorGUI.BeginProperty(rect, GUIContent.none, property);
            EditorGUI.BeginChangeCheck();
            var value = EditorGUI.ColorField(rect, Label("Color", "色", path == "settings.color" ? "_DistanceFadeColor (HDR)" : "_DistanceFadeRimColor (HDR)"),
                property.colorValue, true, alpha, true);
            // Rim opacity has its own row, matching the official inspector.
            if (!alpha) value.a = property.colorValue.a;
            if (EditorGUI.EndChangeCheck()) property.colorValue = value;
            EditorGUI.EndProperty();
        }
        private void DrawParameters()
        {
            var property = serializedObject.FindProperty("settings.parameters");
            var enabled = serializedObject.FindProperty("settings.fadeEnabled");
            var height = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;
            var rect = EditorGUILayout.GetControlRect(false, height * 3 + spacing * 2);
            EditorGUI.BeginProperty(rect, GUIContent.none, property);
            using (new EditorGUI.DisabledScope(!serializedObject.FindProperty("settings.overrideParameters").boolValue))
            {
                var row = new Rect(rect.x, rect.y, rect.width, height);
                var value = property.vector4Value;
                EditorGUI.BeginChangeCheck();
                value.x = EditorGUI.FloatField(row, Label("Start Distance", "開始距離", "_DistanceFade.x (m)"), value.x);
                row.y += height + spacing;
                value.y = EditorGUI.FloatField(row, Label("End Distance", "終了距離", "_DistanceFade.y (m)"), value.y);
                if (EditorGUI.EndChangeCheck()) property.vector4Value = value;
                row.y += height + spacing;
                EditorGUI.BeginChangeCheck();
                var strength = EditorGUI.FloatField(row, Label("Strength", "強度", L("0 disables distance fade", "0で距離フェード無効")), enabled.boolValue ? value.z : 0);
                if (EditorGUI.EndChangeCheck())
                {
                    value.z = strength;
                    property.vector4Value = value;
                    enabled.boolValue = value.z != 0;
                }
            }
            EditorGUI.EndProperty();
            OverrideRow("overrideBackface", row =>
            {
                EditorGUI.BeginProperty(row, GUIContent.none, property);
                var value = property.vector4Value;
                EditorGUI.BeginChangeCheck();
                var backface = EditorGUI.Toggle(row, Label("Force Backface Shadow", "裏面を影にする", "_DistanceFade.w"), value.w != 0);
                if (EditorGUI.EndChangeCheck())
                {
                    value.w = backface ? 1 : 0;
                    property.vector4Value = value;
                }
                EditorGUI.EndProperty();
            });
        }
        private void DrawRimPower(Rect rect)
        {
            var property = serializedObject.FindProperty("settings.rimPower");
            var label = Label("Rim Light Sharpness", "リムライトの細さ", "_DistanceFadeRimFresnelPower");
            EditorGUI.BeginProperty(rect, label, property);
            var control = EditorGUI.PrefixLabel(rect, label);
            var number = new Rect(control.xMax - 50, control.y, 50, control.height);
            var slider = new Rect(control.x, control.y, Mathf.Max(0, control.width - 55), control.height);
            var value = property.floatValue;
            // Same power-3 response as lilToon's [PowerSlider(3.0)].
            EditorGUI.BeginChangeCheck();
            var position = GUI.HorizontalSlider(slider, Mathf.Pow(Mathf.Clamp(value, 0.01f, 50f), 1f / 3f),
                Mathf.Pow(0.01f, 1f / 3f), Mathf.Pow(50f, 1f / 3f));
            if (EditorGUI.EndChangeCheck()) value = property.floatValue = Mathf.Clamp(Mathf.Pow(position, 3), 0.01f, 50f);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.BeginChangeCheck();
            value = EditorGUI.FloatField(number, value);
            if (EditorGUI.EndChangeCheck()) property.floatValue = Mathf.Clamp(value, 0.01f, 50f);
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
        private void Field(string path, string en, string ja) => EditorGUILayout.PropertyField(serializedObject.FindProperty(path),
            new GUIContent(L(en, ja), en + " / " + ja + ": applied to build copies only / ビルド用複製にのみ適用"), true);
        private static void Diagnostics(DistanceFadeController c)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(L("Preview & Diagnostics", "プレビューと診断"), EditorStyles.boldLabel);
            var error = c.settings.ValidationError();
            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Warning);
            if (!DistanceFadeMaterialValidator.LilToonInstalled) EditorGUILayout.HelpBox("lilToon not installed / lilToonが未導入です", MessageType.Warning);
            var avatar = c.GetComponentInParent<VRCAvatarDescriptor>();
            if (!avatar) { EditorGUILayout.HelpBox("Place under an avatar / アバター配下に配置してください", MessageType.Warning); return; }
            if (avatar.GetComponentsInChildren<DistanceFadeController>(true).Count(x => x.enabled && x.enableOverride) > 1)
                EditorGUILayout.HelpBox("Multiple enabled controllers stop the build / 有効なControllerが複数あるためビルドできません", MessageType.Error);
            var processor = new DistanceFadeMaterialProcessor(c.settings, null);
            var affected = new HashSet<Material>();
            var unsupported = new HashSet<Material>();
            var excluded = new HashSet<Material>();
            var rendererCount = 0;
            foreach (var r in DistanceFadeTargetResolver.Collect(avatar.gameObject))
            {
                var selected = false;
                foreach (var m in r.sharedMaterials.Where(m => m))
                {
                    if (!DistanceFadeTargetResolver.Includes(c, r, m)) { excluded.Add(m); continue; }
                    selected = true;
                    if (!DistanceFadeMaterialValidator.IsSupported(m)) unsupported.Add(m);
                    else if (processor.HasOverrides(m) && c.enabled && c.enableOverride && error == null) affected.Add(m);
                }
                if (selected) rendererCount++;
            }
            EditorGUILayout.HelpBox($"Renderers: {rendererCount}\nMaterials to change / 変更マテリアル: {affected.Count}\nExcluded / 除外: {excluded.Count}\nUnsupported / 非対応: {unsupported.Count}\n" +
                L("Scene assignment counts. Animation and MA additions are resolved during build. Originals are unchanged.", "現在の割当を集計。アニメーションとMAの追加はビルド時に解決します。元アセットは変更しません。"), MessageType.Info);
            foreach (var obj in (c.objects ?? new GameObject[0]).Where(o => o && !o.transform.IsChildOf(avatar.transform)))
                EditorGUILayout.HelpBox("Target outside avatar / アバター外の対象: " + obj.name, MessageType.Warning);
        }
    }
}
