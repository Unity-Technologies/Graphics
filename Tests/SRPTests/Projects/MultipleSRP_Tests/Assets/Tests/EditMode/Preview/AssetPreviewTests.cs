using System;
using System.Collections;
using Common;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using Object = UnityEngine.Object;

namespace Preview
{
    public class AssetPreviewTests
    {
        public const string k_PreviewFolderPrefix = "Assets/GraphicsTests/0x_Base/0003_Preview";

        static TestCaseData[] s_TestCaseData =
        {
            new TestCaseData(new MaterialFactory($"TestMaterial-Built-In"), null)
                .SetName($"Preview generation for Material Built-In")
                .Returns(null),
            new TestCaseData(new MaterialFactory($"TestMaterial-{nameof(UniversalRenderPipelineAsset)}"), typeof(UniversalRenderPipelineAsset))
                .SetName($"Preview generation for Material {nameof(UniversalRenderPipelineAsset)}")
                .Returns(null),
            new TestCaseData(new MaterialFactory($"TestMaterial-{nameof(HDRenderPipelineAsset)}"), typeof(HDRenderPipelineAsset))
                .SetName($"Preview generation for Material {nameof(HDRenderPipelineAsset)}")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.fbx"), null)
                .SetName($"Preview generation for Model Built-In")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.fbx"), typeof(UniversalRenderPipelineAsset))
                .SetName($"Preview generation for Model {nameof(UniversalRenderPipelineAsset)}")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.fbx"), typeof(HDRenderPipelineAsset))
                .SetName($"Preview generation for Model {nameof(HDRenderPipelineAsset)}")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.prefab"), null)
                .SetName($"Preview generation for Prefab Built-In")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.prefab"), typeof(UniversalRenderPipelineAsset))
                .SetName($"Preview generation for Prefab {nameof(UniversalRenderPipelineAsset)}")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.prefab"), typeof(HDRenderPipelineAsset))
                .SetName($"Preview generation for Prefab {nameof(HDRenderPipelineAsset)}")
                .Returns(null),
        };

        string m_CreatedObjectPath;
        AssetFactoryResultStatus m_AssetFactoryAssetFactoryResultStatus;

        [UnityTest]
        [TestCaseSource(nameof(s_TestCaseData))]
        public IEnumerator CreatePreview(AssetFactory objectFactory, Type renderPipelineAssetType)
        {
            using (new RenderPipelineScope(renderPipelineAssetType))
            {
                yield return null;

                //Arrange
                var testObject = ProduceNewObject(objectFactory, renderPipelineAssetType, out m_CreatedObjectPath);
                var loadIcon = LoadIcon(TestContext.CurrentContext.Test.Name);

                //Act

                yield return WaitForShadersToCompile(testObject, m_CreatedObjectPath);
                var tex = AssetPreviewUpdater.CreatePreviewForAsset(testObject, null, m_CreatedObjectPath);

                //Assert
                ImageAssert.AreEqual(loadIcon, tex, new ImageComparisonSettings
                {
                    TargetWidth = 128,
                    TargetHeight = 128,
                    AverageCorrectnessThreshold = 0.005f,
                    PerPixelCorrectnessThreshold = 0.005f
                }, saveFailedImage: true);
            }
        }

        [UnityTearDown]
        // ReSharper disable once UnusedMember.Global
        public IEnumerator TearDown()
        {
            yield return null;

            if (m_AssetFactoryAssetFactoryResultStatus == AssetFactoryResultStatus.Created && !string.IsNullOrEmpty(m_CreatedObjectPath) && AssetDatabase.AssetPathExists(m_CreatedObjectPath))
            {
                AssetDatabase.DeleteAsset(m_CreatedObjectPath);
                AssetDatabase.Refresh();
            }
        }

        Texture2D LoadIcon(string name)
        {
            var testedIconPath = $"Assets/ReferenceImages/{TestUtils.GetCurrentTestResultsFolderPath()}/{name}.png";
            var loadIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(testedIconPath);
            return loadIcon;
        }

        Object ProduceNewObject(AssetFactory objectFactory, Type renderPipelineType, out string path)
        {
            m_AssetFactoryAssetFactoryResultStatus = objectFactory.GetObjectWithPath(k_PreviewFolderPrefix, renderPipelineType, out var testObject, out path);
            return testObject;
        }

        IEnumerator WaitForShadersToCompile(Object testObject, string path, float maxTime = 300)
        {
            AssetPreviewUpdater.CreatePreviewForAsset(testObject, null, path);
            yield return null;
            yield return WaitForChange(() => !ShaderUtil.anythingCompiling, maxTime);
        }

        static IEnumerator WaitForChange(Func<bool> changeCheck, double maxWaitTime = 1)
        {
            var startTime = EditorApplication.timeSinceStartup;
            while (!changeCheck.Invoke() && startTime + maxWaitTime > EditorApplication.timeSinceStartup)
            {
                yield return null;
            }
        }
    }

    public enum AssetFactoryResultStatus
    {
        Created,
        Loaded
    }

    public abstract class AssetFactory
    {
        public abstract AssetFactoryResultStatus GetObjectWithPath(string prefix, Type renderPipelineType, out Object newObject, out string path);

        public string name;

        public AssetFactory(string name)
        {
            this.name = name;
        }
    }

    public class MaterialFactory : AssetFactory
    {
        public MaterialFactory(string name) : base(name)
        {
        }

        public override AssetFactoryResultStatus GetObjectWithPath(string prefix, Type renderPipelineType, out Object newObject, out string path)
        {
            string shaderName;
            if (renderPipelineType != null)
            {
                var asset = RenderPipelineUtils.LoadAsset(renderPipelineType);
                shaderName = asset.defaultShader.name;
            }
            else
                shaderName = "Standard";

            Assert.That(shaderName, Is.Not.Empty,
                $"Can not test Material Preview outside of predefined list of SRPs. Current RenderPipelineType is {GraphicsSettings.currentRenderPipelineAssetType?.FullName}");

            var shader = Shader.Find(shaderName);
            newObject = new Material(shader);
            path = $"{prefix}/{name}.mat";

            AssetDatabase.CreateAsset(newObject, path);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            return AssetFactoryResultStatus.Created;
        }
    }

    public class GameObjectFactory : AssetFactory
    {
        public GameObjectFactory(string name) : base(name)
        {
        }

        public override AssetFactoryResultStatus GetObjectWithPath(string prefix, Type renderPipelineType, out Object newObject, out string path)
        {
            path = $"{AssetPreviewTests.k_PreviewFolderPrefix}/{name}";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            newObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            Assert.That(newObject, Is.Not.Null, $"Couldn't load a {name} by this {path} for testing.");

            return AssetFactoryResultStatus.Loaded;
        }
    }
}