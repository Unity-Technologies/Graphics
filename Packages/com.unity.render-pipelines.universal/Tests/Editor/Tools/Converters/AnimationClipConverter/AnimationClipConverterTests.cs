using System.Collections;
using NUnit.Framework;
using UnityEditor.Rendering.Converter;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tools
{
    [TestFixture]
    [Category("Graphics Tools")]
    class AnimationClipConverterTests
    {
        const string k_TestsPath = "Assets/AnimationClipConverterTests/";

        static AnimationClip CreateAnimationClip()
        {
            // Create a new animation clip
            AnimationClip clip = new AnimationClip();
            clip.legacy = true;
            clip.frameRate = 60;

            // Create keyframes for the color animation
            Keyframe[] redKeys = new Keyframe[3];
            Keyframe[] greenKeys = new Keyframe[3];
            Keyframe[] blueKeys = new Keyframe[3];
            Keyframe[] alphaKeys = new Keyframe[3];

            // Start color (Red) at time 0
            redKeys[0] = new Keyframe(0f, 1f);
            greenKeys[0] = new Keyframe(0f, 0f);
            blueKeys[0] = new Keyframe(0f, 0f);
            alphaKeys[0] = new Keyframe(0f, 1f);

            // Middle color (Green) at time 1
            redKeys[1] = new Keyframe(1f, 0f);
            greenKeys[1] = new Keyframe(1f, 1f);
            blueKeys[1] = new Keyframe(1f, 0f);
            alphaKeys[1] = new Keyframe(1f, 1f);

            // End color (Blue) at time 2
            redKeys[2] = new Keyframe(2f, 0f);
            greenKeys[2] = new Keyframe(2f, 0f);
            blueKeys[2] = new Keyframe(2f, 1f);
            alphaKeys[2] = new Keyframe(2f, 1f);

            // Create animation curves
            AnimationCurve redCurve = new AnimationCurve(redKeys);
            AnimationCurve greenCurve = new AnimationCurve(greenKeys);
            AnimationCurve blueCurve = new AnimationCurve(blueKeys);
            AnimationCurve alphaCurve = new AnimationCurve(alphaKeys);

            // Set curves to the clip with correct binding format
            // Empty string "" means the animated object itself
            clip.SetCurve("", typeof(MeshRenderer), "material._Color.r", redCurve);
            clip.SetCurve("", typeof(MeshRenderer), "material._Color.g", greenCurve);
            clip.SetCurve("", typeof(MeshRenderer), "material._Color.b", blueCurve);
            clip.SetCurve("", typeof(MeshRenderer), "material._Color.a", alphaCurve);

            // Set the clip to loop
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // Save the animation clip as an asset
            string path = k_TestsPath + "ColorAnimation.anim";
            CoreUtils.EnsureFolderTreeInAssetFilePath(path);
            AssetDatabase.CreateAsset(clip, path);
            AssetDatabase.SaveAssets();

            return clip;
        }

        public static GameObject CreatePrefabForTest(Material[] materials, AnimationClip clip, string assetPath)
        {
            // Create a temporary GameObject
            var go = new GameObject("TestingPrefabGO");

            try
            {
                // Add components
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = materials;

                var anim = go.AddComponent<Animation>();
                anim.enabled = true;
                anim.clip = clip;

                string localPath = assetPath + go.name + ".prefab";
                CoreUtils.EnsureFolderTreeInAssetFilePath(localPath);

                // Save as prefab
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, localPath, out bool success);
                if (!success || prefab == null)
                {
                    Debug.LogError("Failed to save prefab at: " + assetPath);
                    return null;
                }

                AssetDatabase.ImportAsset(localPath);
                AssetDatabase.SaveAssets();

                return prefab;
            }
            finally
            {
                // Cleanup temporary instance
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static Material GetDefaultDiffuse()
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                return null;

            return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
        }

        private static Material GetURPDefaultMaterial()
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                return null;

            // Get URP default material from graphics settings
            return GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>().defaultMaterial;
        }

        AnimationClipConverter m_Converter;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {   
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
                Assert.Ignore("Project without URP. Skipping test");

            var universalRenderer = urpAsset.scriptableRenderer as UniversalRenderer;
            if (universalRenderer == null)
                Assert.Ignore("Project without URP - Universal Renderer. Skipping test");

            m_Converter = new AnimationClipConverter();
            Assume.That(m_Converter.m_UpgradePathsToNewShaders == null, "Upgraders is not null, when it should be null");
            m_Converter.BeforeConvert();
            Assume.That(m_Converter.m_UpgradePathsToNewShaders != null, "Upgraders was not initialized");
        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            m_Converter.AfterConvert();
            Assume.That(m_Converter.m_UpgradePathsToNewShaders == null, "Upgraders was not disposed");
            AssetDatabase.DeleteAsset(k_TestsPath);
        }

        AnimationClip m_AnimationClip;

        [SetUp]
        public void Setup()
        {
            m_AnimationClip = CreateAnimationClip();
            Assume.That(m_AnimationClip != null, "Unable to create animation clip");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m_AnimationClip));
        }

        private static IEnumerable MaterialTestCases()
        {
            yield return new TestCaseData(
                new Material[] { GetDefaultDiffuse() },
                Status.Warning,
                "material._Color"
            ).SetName("GivenBuiltInMaterial_WhenConverting_ThenNoUpgradeNeeded");

            yield return new TestCaseData(
                new Material[] { GetURPDefaultMaterial() },
                Status.Success,
                "material._BaseColor"
            ).SetName("GivenURPMaterial_WhenConverting_ThenUpgradeSucceeds");

            yield return new TestCaseData(
                new Material[] { GetDefaultDiffuse(), GetURPDefaultMaterial() },
                Status.Error,
                "material._Color"
            ).SetName("GivenMixedMaterials_WhenConverting_ThenReturnsError");
        }

        [Test, TestCaseSource(nameof(MaterialTestCases))]
        public void PerformAnimationClipConversion(Material[] materials, Status expectedStatus, string expectedBindingPropertyName)
        {
            AnimationClipConverterItem animationClipConverterItem = new(GlobalObjectId.GetGlobalObjectIdSlow(m_AnimationClip), AssetDatabase.GetAssetPath(m_AnimationClip));

            var go = CreatePrefabForTest(materials, m_AnimationClip, k_TestsPath + $"{TestContext.CurrentContext.Test.Name}/");

            animationClipConverterItem.dependencies.Add(
                new RenderPipelineConverterAssetItem(
                    GlobalObjectId.GetGlobalObjectIdSlow(go),
                    AssetDatabase.GetAssetPath(go))
            );

            var status = m_Converter.Convert(animationClipConverterItem, out var message);

            Assert.AreEqual(expectedStatus, status, $"Expected status {expectedStatus} but got {status} with {message}");

            foreach (var b in AnimationUtility.GetCurveBindings(m_AnimationClip))
            {
                Assert.IsTrue(b.propertyName.Contains(expectedBindingPropertyName));
            }
        }
    }

}
