using System;
using System.Collections;
using System.Reflection;
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
    [TestFixture]
    public class AssetPreviewTests
    {
        const string k_PreviewFolderPrefix = "Assets/Tests/EditMode/Preview";

        static TestCaseData[] s_TestCaseData = {
            new TestCaseData(new MaterialFactory($"TestMaterial-{nameof(UniversalRenderPipelineAsset)}"), typeof(UniversalRenderPipelineAsset))
                .SetName($"Preview generation for Material {nameof(UniversalRenderPipelineAsset)}")
                .Returns(null),
            new TestCaseData(new MaterialFactory($"TestMaterial-{nameof(HDRenderPipelineAsset)}"), typeof(HDRenderPipelineAsset))
                .SetName($"Preview generation for Material {nameof(HDRenderPipelineAsset)}")
                .Returns(null)
                .Ignore("HD Render Pipeline thumbnails doesn't render properly when rendering requested from C++ worker in case thumbnail was scheduled to be rendered later. Possible cause that burstified Job required for proper feature flags determination is not running.")
        };

        string m_CreatedObjectPath;

        [UnityTest]
        [TestCaseSource(nameof(s_TestCaseData))]
        public IEnumerator CreatePreview(AssetFactory objectFactory, Type renderPipelineAssetType)
        {
            using (new RenderPipelineScope(renderPipelineAssetType, forceInitialization: true))
            {
                yield return null;

                //Arrange
                var testObject = ProduceNewObject(objectFactory, out m_CreatedObjectPath);
                var loadIcon = LoadIcon(TestContext.CurrentContext.Test.Name);

                //Act
                var tex = CreatePreviewForAsset(testObject, m_CreatedObjectPath, 128, 128);

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

            if(!string.IsNullOrEmpty(m_CreatedObjectPath) && AssetDatabase.AssetPathExists(m_CreatedObjectPath))
                AssetDatabase.DeleteAsset(m_CreatedObjectPath);

            yield return null;
        }

        Texture2D LoadIcon(string name)
        {
            var testedIconPath = $"Assets/ReferenceImages/{TestUtils.GetCurrentTestResultsFolderPath()}/{name}.png";
            var loadIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(testedIconPath);
            return loadIcon;
        }

        Object ProduceNewObject(AssetFactory objectFactory, out string path)
        {
            var producedObject = objectFactory.GetObjectWithPath(k_PreviewFolderPrefix);
            path = producedObject.Item2;
            var testObject = producedObject.Item1;
            AssetDatabase.CreateAsset(testObject, path);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            return testObject;
        }

        public Texture2D CreatePreviewForAsset(Object testObject, string path, int width, int height)
        {
            var assetPreviewUpdaterType = Type.GetType("UnityEditor.AssetPreviewUpdater, UnityEditor.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            var method = assetPreviewUpdaterType.GetMethod("CreatePreview", BindingFlags.Static | BindingFlags.Public);
            return method.Invoke(null, new object[] { testObject, null, path, width, height }) as Texture2D;
        }
    }

    public abstract class AssetFactory
    {
        public abstract (Object, string) GetObjectWithPath(string prefix);

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

        public override (Object, string) GetObjectWithPath(string prefix)
        {
            string shaderName = "";
            if(GraphicsSettings.currentRenderPipelineAssetType == typeof(UniversalRenderPipelineAsset))
                shaderName = "Universal Render Pipeline/Lit";
            else if(GraphicsSettings.currentRenderPipelineAssetType == typeof(HDRenderPipelineAsset))
                shaderName = "HDRP/Lit";

            Assert.That(shaderName, Is.Not.Empty, $"Can not test Material Preview outside of predefined list of SRPs. Current RenderPipelineType is {GraphicsSettings.currentRenderPipelineAssetType?.FullName}");

            var shader = Shader.Find(shaderName);
            var newMaterial = new Material(shader);
            var materialPath = $"{prefix}/{name}.mat";
            return (newMaterial, materialPath);
        }
    }
}
