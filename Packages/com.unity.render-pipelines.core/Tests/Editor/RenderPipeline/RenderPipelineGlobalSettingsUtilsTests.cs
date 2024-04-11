using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [TestFixture]
    public class RenderPipelineGlobalSettingsTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach(var globalsetting in CoreUtils.LoadAllAssets<DummyRenderPipelineGlobalSettings>())
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(globalsetting));
            }

            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<DummyRenderPipeline>(null);
        }

        static TestCaseData[] s_TestsCaseDatas =
        {
            new TestCaseData(string.Empty, false, AssetState.Null)
                .SetName(
                    "Given an empty project, when ensuring a global settings without canCreateNewAsset, the asset is not created")
                .Returns(string.Empty),
            // Disabled due to instabilities in the test
            //new TestCaseData(string.Empty, true, AssetState.NotNull)
            //    .SetName(
            //        "Given an empty project, when ensuring a global settings, the asset is created with the type name")
            //    .Returns("Assets/DummyRenderPipelineGlobalSettings.asset"),
            new TestCaseData(DummyRenderPipelineGlobalSettings.defaultPath, false, AssetState.NotNull)
                .SetName(
                    "Given a project with an asset already created in the default path, when ensuring a global settings, the asset returned is the one at default path")
                .Returns(DummyRenderPipelineGlobalSettings.defaultPath),
            new TestCaseData("Assets/Tests/AnotherDummyRenderPipelineGlobalSettings.asset", false, AssetState.NotNull)
                .SetName(
                    "Given a project with an asset already created somewhere, when ensuring a global settings, the asset returned is that one")
                .Returns("Assets/Tests/AnotherDummyRenderPipelineGlobalSettings.asset"),
        };

        public enum AssetState
        {
            Null,
            NotNull
        }

        [Test, TestCaseSource(nameof(s_TestsCaseDatas))]
        public string Ensure(string path, bool canCreateNewAsset, AssetState assetState)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var instance = RenderPipelineGlobalSettingsUtils.Create<DummyRenderPipelineGlobalSettings>(path);
                Assert.IsNotNull(instance);
            }

            DummyRenderPipelineGlobalSettings instanceEnsured = null;
            var ensureResult = RenderPipelineGlobalSettingsUtils.
                TryEnsure<DummyRenderPipelineGlobalSettings, DummyRenderPipeline>(ref instanceEnsured, path, canCreateNewAsset, out _);

            switch (assetState)
            {
                case AssetState.Null:
                    Assert.IsFalse(ensureResult);
                    Assert.IsNull(instanceEnsured);
                    break;
                case AssetState.NotNull:
                    Assert.IsTrue(ensureResult);
                    Assert.IsNotNull(instanceEnsured);
                    Assert.IsTrue(instanceEnsured.initializedCalled, "Initialize method has not been called");
                    break;
            }

            if (instanceEnsured == null) return string.Empty;

            var instanceInGraphics = GraphicsSettings.GetSettingsForRenderPipeline<DummyRenderPipeline>();
            Assert.AreEqual(instanceInGraphics.GetInstanceID(), instanceEnsured.GetInstanceID());
            return AssetDatabase.GetAssetPath(instanceEnsured);

        }

        [Test]
        public void EnsureWithAValidInstanceReturnsTheCurrentInstance()
        {
            var path = "Assets/Tests/DummyRenderPipelineGlobalSettings.asset";
            var instanceEnsured = RenderPipelineGlobalSettingsUtils.Create<DummyRenderPipelineGlobalSettings>(path);
            Assert.IsNotNull(instanceEnsured);
            Assert.AreEqual(path, AssetDatabase.GetAssetPath(instanceEnsured));

            var instanceIDExpected = instanceEnsured.GetInstanceID();
            var ensureResult = RenderPipelineGlobalSettingsUtils.
                TryEnsure<DummyRenderPipelineGlobalSettings, DummyRenderPipeline>(ref instanceEnsured, DummyRenderPipelineGlobalSettings.defaultPath, true, out _);

            Assert.IsTrue(ensureResult);
            Assert.IsNotNull(instanceEnsured);
            Assert.AreEqual(instanceIDExpected, instanceEnsured.GetInstanceID());
        }
    }
}
