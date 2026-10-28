using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    class SurfaceCacheGIRendererFeatureMigrationTests
    {
        const int k_LatestVersion = 1;

        SurfaceCacheGIRendererFeature m_Feature;
        BuildTarget m_BuildTarget;
        bool m_PreviousStaticBatching;
        DefaultMeshBufferTarget m_PreviousMeshBufferTarget;

        // Creating the feature under URP validates these project settings and logs an error when they are unmet.
        [SetUp]
        public void Setup()
        {
            m_BuildTarget = EditorUserBuildSettings.activeBuildTarget;
            m_PreviousStaticBatching = PlayerSettings.GetStaticBatchingForPlatform(m_BuildTarget);
            m_PreviousMeshBufferTarget = EditorGraphicsSettings.defaultMeshBufferTarget;
            PlayerSettings.SetStaticBatchingForPlatform(m_BuildTarget, false);
            EditorGraphicsSettings.defaultMeshBufferTarget = DefaultMeshBufferTarget.Raw;

            m_Feature = ScriptableObject.CreateInstance<SurfaceCacheGIRendererFeature>();
        }

        [TearDown]
        public void TearDown()
        {
            m_Feature.Dispose();
            Object.DestroyImmediate(m_Feature);
            EditorGraphicsSettings.defaultMeshBufferTarget = m_PreviousMeshBufferTarget;
            PlayerSettings.SetStaticBatchingForPlatform(m_BuildTarget, m_PreviousStaticBatching);
        }

        static int GetSerializedVersion(Object feature)
        {
            using var serializedObject = new SerializedObject(feature);
            return serializedObject.FindProperty("m_Version").intValue;
        }

        [Test]
        public void NewFeature_DefaultsToMediumAndLatestVersion()
        {
            EditorJsonUtility.ToJson(m_Feature);

            Assert.AreEqual(SurfaceCacheGIRendererFeature.MaterialResolution.Medium, m_Feature.materialResolution);
            Assert.AreEqual(k_LatestVersion, GetSerializedVersion(m_Feature));
        }

        [Test]
        public void FeatureSerializedBeforeMaterialResolution_MigratesToVeryHigh()
        {
            var json = EditorJsonUtility.ToJson(m_Feature);
            json = Regex.Replace(json, "\"m_MaterialResolution\":\\s*-?\\d+,?", "");
            json = Regex.Replace(json, "\"m_Version\":\\s*-?\\d+,?", "");
            json = Regex.Replace(json, ",\\s*}", "}");

            var legacyFeature = ScriptableObject.CreateInstance<SurfaceCacheGIRendererFeature>();
            try
            {
                EditorJsonUtility.FromJsonOverwrite(json, legacyFeature);

                Assert.AreEqual(SurfaceCacheGIRendererFeature.MaterialResolution.VeryHigh, legacyFeature.materialResolution);
                Assert.AreEqual(k_LatestVersion, GetSerializedVersion(legacyFeature));
            }
            finally
            {
                legacyFeature.Dispose();
                Object.DestroyImmediate(legacyFeature);
            }
        }

        [Test]
        public void FeatureAtLatestVersion_KeepsMaterialResolution()
        {
            m_Feature.materialResolution = SurfaceCacheGIRendererFeature.MaterialResolution.Low;
            var json = EditorJsonUtility.ToJson(m_Feature);

            var copy = ScriptableObject.CreateInstance<SurfaceCacheGIRendererFeature>();
            try
            {
                EditorJsonUtility.FromJsonOverwrite(json, copy);
                Assert.AreEqual(SurfaceCacheGIRendererFeature.MaterialResolution.Low, copy.materialResolution);
            }
            finally
            {
                copy.Dispose();
                Object.DestroyImmediate(copy);
            }
        }
    }
}
