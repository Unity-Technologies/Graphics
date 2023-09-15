using NUnit.Framework;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.SceneManagement;

namespace UnityEditor.Rendering.HighDefinition.Tests
{
    class HDRPBuildDataTests
    {
        private static readonly string k_AssetPath =
            $"Assets/{nameof(CheckLabeledHDRenderPipelineAreIncluded)}/{nameof(CheckLabeledHDRenderPipelineAreIncluded)}_RP.asset";

        private HDRenderPipelineAsset m_Asset;

        private static readonly string k_ScenePath =
            $"Assets/{nameof(CheckLabeledHDRenderPipelineAreIncluded)}/{nameof(CheckLabeledHDRenderPipelineAreIncluded)}_Scene.unity";

        class ReferenceHDRPAssetComponent : MonoBehaviour
        {
            public HDRenderPipelineAsset m_ReferencedAsset;
        }

        [SetUp]
        public void Setup()
        {
            if (GraphicsSettings.currentRenderPipeline is not HDRenderPipelineAsset)
                Assert.Ignore("This is an HDRP Tests, and the current pipeline is not HDRP.");

            m_Asset = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
            m_Asset.name = nameof(CheckLabeledHDRenderPipelineAreIncluded);

            CoreUtils.EnsureFolderTreeInAssetFilePath(k_AssetPath);

            AssetDatabase.CreateAsset(m_Asset, k_AssetPath);
            AssetDatabase.SaveAssets();

            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Instantiate a GameObject and add components
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = Vector3.zero;

            var referenceHdrpAsset = cube.AddComponent<ReferenceHDRPAssetComponent>();
            referenceHdrpAsset.m_ReferencedAsset = m_Asset;

            // Save the scene
            EditorSceneManager.SaveScene(newScene, k_ScenePath);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Asset != null)
            {
                AssetDatabase.DeleteAsset(k_AssetPath);

                EditorSceneManager.SetActiveScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single));

                AssetDatabase.DeleteAsset(k_ScenePath);
            }
        }

        static bool CompareObjects<T>(object obj1, object obj2)
        {
            Type type = obj1.GetType();

            // Get all fields of the class
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (FieldInfo field in fields)
            {
                object value1 = field.GetValue(obj1);
                object value2 = field.GetValue(obj2);

                if (field.GetValue(obj1) is ICollection enumerable1)
                {
                    var enumerable2 = field.GetValue(obj1) as ICollection;

                    if (enumerable1.Count != enumerable2.Count)
                    {
                        UnityEngine.Debug.LogError($"Field {field.Name} did not rollback to its default state");
                        return false;
                    }

                }
                else if (!object.Equals(value1, value2))
                {
                    UnityEngine.Debug.LogError($"Field {field.Name} did not rollback to its default state");
                    return false;
                }
            }

            return true;
        }

        [Test]
        public void CheckDisposeClearsAllData()
        {
            var instance = new HDRPBuildData(EditorUserBuildSettings.activeBuildTarget, Debug.isDebugBuild);
            instance.Dispose();
            Assert.IsTrue(CompareObjects<HDRPBuildData>(instance, new HDRPBuildData()));
        }


        [Test]
        public void CheckLabeledHDRenderPipelineAreIncluded()
        {
            using (UnityEngine.Pool.ListPool<HDRenderPipelineAsset>.Get(out var list))
            {
                AssetDatabase.SetLabels(m_Asset, new[] { HDEditorUtils.HDRPAssetBuildLabel });
                AssetDatabase.SaveAssets();

                HDRPBuildData.AddAdditionalHDRenderPipelineAssetsIncludedForBuild(list);

                Assert.Contains(m_Asset, list);
            }
        }

        [Test]
        public void CheckLabeledHDRenderPipelineAreNotIncluded()
        {
            using (UnityEngine.Pool.ListPool<HDRenderPipelineAsset>.Get(out var list))
            {
                HDRPBuildData.AddAdditionalHDRenderPipelineAssetsIncludedForBuild(list);

                Assert.That(list, Has.No.Member(m_Asset));
            }
        }

        [Test]
        public void CheckDependentAssetInSceneIsIncludedForBuild()
        {
            using (UnityEngine.Pool.ListPool<HDRenderPipelineAsset>.Get(out var list))
            {
                var buildScenes = EditorBuildSettings.scenes;
                
                EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene(k_ScenePath, true) };
                
                HDRPBuildData.AddAdditionalHDRenderPipelineAssetsIncludedForBuild(list);

                Assert.Contains(m_Asset, list);

                EditorBuildSettings.scenes = buildScenes;
            }
        }

        [Test]
        public void CheckDependentAssetInSceneIsNotIncludedForBuild()
        {
            using (UnityEngine.Pool.ListPool<HDRenderPipelineAsset>.Get(out var list))
            {
                HDRPBuildData.AddAdditionalHDRenderPipelineAssetsIncludedForBuild(list);

                Assert.That(list, Has.No.Member(m_Asset));
            }
        }

        [Test]
        public void CheckRemoveDuplicatesWorks()
        {
            using (UnityEngine.Pool.ListPool<HDRenderPipelineAsset>.Get(out var list))
            {
                list.Add(m_Asset);
                list.Add(m_Asset);

                HDRPBuildData.RemoveDuplicateAssets(list);

                Assert.AreEqual(1, list.Count);
            }
        }
    }
}
