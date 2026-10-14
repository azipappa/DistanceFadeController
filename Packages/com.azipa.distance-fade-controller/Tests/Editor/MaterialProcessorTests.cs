using System.Collections.Generic;
using Azipa.DistanceFade.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Azipa.DistanceFade.Tests
{
    public abstract class DistanceFadeTestBase
    {
        protected readonly List<Object> owned = new List<Object>();
        private string testAssetRoot;
        [SetUp] public void Setup()
        {
            testAssetRoot = "Assets/DistanceFadeTests_" + System.Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", testAssetRoot.Substring("Assets/".Length));
        }
        protected T SaveTestAsset<T>(T asset) where T : Object
        {
            AssetDatabase.CreateAsset(asset, testAssetRoot + "/" + System.Guid.NewGuid().ToString("N") + ".asset");
            return asset;
        }
        protected T Own<T>(T obj) where T : Object { owned.Add(obj); return obj; }
        protected Material Material()
        {
            var shader = Shader.Find("lilToon");
            Assert.IsNotNull(shader, "Install lilToon for compatibility tests");
            return Own(SaveTestAsset(new Material(shader)));
        }
        [TearDown] public void Cleanup()
        {
            foreach (var obj in owned) if (obj && !EditorUtility.IsPersistent(obj)) Object.DestroyImmediate(obj);
            owned.Clear();
            if (!string.IsNullOrEmpty(testAssetRoot) && testAssetRoot.StartsWith("Assets/DistanceFadeTests_"))
                AssetDatabase.DeleteAsset(testAssetRoot);
        }
    }
    public class MaterialProcessorTests : DistanceFadeTestBase
    {
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void DistanceAndBackfaceOverridesAreIndependent(bool distance, bool backface)
        {
            var source = Material();
            var original = new Vector4(.3f, .05f, .6f, 1);
            source.SetVector("_DistanceFade", original);
            var settings = new DistanceFadeSettings { overrideParameters = distance, overrideBackface = backface,
                parameters = new Vector4(.2f, .03f, .8f, 0) };
            var result = new DistanceFadeMaterialProcessor(settings, obj => owned.Add(obj)).Process(source);
            Assert.AreEqual(new Vector4(distance ? .2f : .3f, distance ? .03f : .05f, distance ? .8f : .6f, backface ? 0 : 1), result.GetVector("_DistanceFade"));
            Assert.AreEqual(original, source.GetVector("_DistanceFade"));
            if (!distance && !backface) Assert.AreEqual(source, result);
        }
        [Test] public void SourceUnchangedAndCloneCached()
        {
            var source = Material();
            var original = source.GetVector("_DistanceFade");
            var processor = new DistanceFadeMaterialProcessor(new DistanceFadeSettings(), obj => owned.Add(obj));
            var result = processor.Process(source);
            Assert.AreNotEqual(source, result);
            Assert.AreEqual(result, processor.Process(source));
            Assert.AreEqual(original, source.GetVector("_DistanceFade"));
            Assert.AreEqual(1, result.GetVector("_DistanceFade").z);
        }
        [Test] public void ColorOnlyRetainsParameters()
        {
            var source = Material();
            var settings = new DistanceFadeSettings { overrideParameters = false, overrideColor = true, color = Color.red };
            var result = new DistanceFadeMaterialProcessor(settings, obj => owned.Add(obj)).Process(source);
            Assert.AreEqual(source.GetVector("_DistanceFade"), result.GetVector("_DistanceFade"));
            Assert.AreEqual(Color.red, result.GetColor("_DistanceFadeColor"));
        }
        [Test] public void DisabledFadeSetsStrengthZero()
        {
            var source = Material();
            var result = new DistanceFadeMaterialProcessor(new DistanceFadeSettings { fadeEnabled = false }, obj => owned.Add(obj)).Process(source);
            Assert.AreEqual(0, result.GetVector("_DistanceFade").z);
        }
        [Test] public void NoOverridesDoesNotClone()
        {
            var source = Material();
            Assert.AreEqual(source, new DistanceFadeMaterialProcessor(new DistanceFadeSettings { overrideParameters = false }, null).Process(source));
        }
        [Test] public void InvalidSettingsDoNotChangeSource()
        {
            var source = Material();
            var settings = new DistanceFadeSettings { parameters = Vector4.zero };
            Assert.Throws<System.InvalidOperationException>(() => new DistanceFadeMaterialProcessor(settings, null).Process(source));
            Assert.AreEqual(new Vector4(.1f, .01f, 0, 0), source.GetVector("_DistanceFade"));
        }
        [Test] public void SettingsSnapshotIsIndependent()
        {
            var source = Material();
            var settings = new DistanceFadeSettings();
            var processor = new DistanceFadeMaterialProcessor(settings, obj => owned.Add(obj));
            settings.fadeEnabled = false;
            Assert.AreEqual(1, processor.Process(source).GetVector("_DistanceFade").z);
        }
        [Test] public void OptionalOverridesMatchShaderProperties()
        {
            var source = Material();
            var settings = new DistanceFadeSettings { overrideMode = true, mode = FadeMode.ObjectPosition,
                overrideRimColor = true, rimColor = Color.green, overrideRimPower = true, rimPower = 10 };
            var clone = new DistanceFadeMaterialProcessor(settings, obj => owned.Add(obj)).Process(source);
            Assert.AreEqual(1, clone.GetInt("_DistanceFadeMode"));
            Assert.AreEqual(Color.green, clone.GetColor("_DistanceFadeRimColor"));
            Assert.AreEqual(10, clone.GetFloat("_DistanceFadeRimFresnelPower"));
            Assert.AreEqual(0, source.GetInt("_DistanceFadeMode"));
        }
        [Test] public void MultiKeywordMatchesFadeStrength()
        {
            var source = Own(new Material(Shader.Find("_lil/lilToonMulti")));
            var on = new DistanceFadeMaterialProcessor(new DistanceFadeSettings(), obj => owned.Add(obj)).Process(source);
            Assert.IsTrue(on.IsKeywordEnabled("_FADING_ON"));
            var off = new DistanceFadeMaterialProcessor(new DistanceFadeSettings { fadeEnabled = false }, obj => owned.Add(obj)).Process(on);
            Assert.IsFalse(off.IsKeywordEnabled("_FADING_ON"));
        }
    }
}
