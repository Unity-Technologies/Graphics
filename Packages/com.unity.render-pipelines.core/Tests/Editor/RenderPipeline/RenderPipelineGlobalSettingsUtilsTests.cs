using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
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

        public class DummyRenderPipelineAsset : RenderPipelineAsset<DummyRenderPipeline>
        {
            protected override RenderPipeline CreatePipeline()
            {
                throw new System.NotImplementedException();
            }
        }

        public class DummyRenderPipeline : RenderPipeline
        {
            protected override void Render(ScriptableRenderContext context, Camera[] cameras)
            {
                throw new System.NotImplementedException();
            }
        }

        [SupportedOnRenderPipeline(typeof(DummyRenderPipelineAsset))]
        public class DummyRenderPipelineGlobalSettings : RenderPipelineGlobalSettings<DummyRenderPipelineGlobalSettings, DummyRenderPipeline>
        {
            internal static string defaultPath => "Assets/Tests/DummyRenderPipelineGlobalSettings.asset";

            public bool initializedCalled = false;

            public override void Initialize(RenderPipelineGlobalSettings source = null)
            {
                initializedCalled = true;
            }
        }

        static TestCaseData[] s_TestsCaseDatas =
        {
            new TestCaseData(string.Empty, false, AssetState.Null)
                .SetName(
                    "Given an empty project, when ensuring a global settings without canCreateNewAsset, the asset is not created")
                .Returns(string.Empty),
            new TestCaseData(string.Empty, true, AssetState.NotNull)
                .SetName(
                    "Given an empty project, when ensuring a global settings, the asset is created with the type name")
                .Returns("Assets/DummyRenderPipelineGlobalSettings.asset"),
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
            bool ensureResult = RenderPipelineGlobalSettingsUtils.
                TryEnsure<DummyRenderPipelineGlobalSettings, DummyRenderPipeline>(ref instanceEnsured, path, canCreateNewAsset, out var _);

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

            if (instanceEnsured != null)
            {
                var instanceInGraphics = GraphicsSettings.GetSettingsForRenderPipeline<DummyRenderPipeline>();
                Assert.AreEqual(instanceInGraphics.GetInstanceID(), instanceEnsured.GetInstanceID());
                return AssetDatabase.GetAssetPath(instanceEnsured);
            }

            return string.Empty;
        }

        [Test]
        public void EnsureWithAValidInstanceReturnsTheCurrentInstance()
        {
            string path = "Assets/Tests/DummyRenderPipelineGlobalSettings.asset";
            var instanceEnsured = RenderPipelineGlobalSettingsUtils.Create<DummyRenderPipelineGlobalSettings>(path);
            Assert.IsNotNull(instanceEnsured);
            Assert.AreEqual(path, AssetDatabase.GetAssetPath(instanceEnsured));

            int instanceIDExpected = instanceEnsured.GetInstanceID();
            bool ensureResult = RenderPipelineGlobalSettingsUtils.
                TryEnsure<DummyRenderPipelineGlobalSettings, DummyRenderPipeline>(ref instanceEnsured, DummyRenderPipelineGlobalSettings.defaultPath, true, out var _);

            Assert.IsTrue(ensureResult);
            Assert.IsNotNull(instanceEnsured);
            Assert.AreEqual(instanceIDExpected, instanceEnsured.GetInstanceID());
        }
    }
}
