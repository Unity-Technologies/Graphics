using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.VFX;

using UnityEditor.VFX.Block;
using UnityEditor.VFX.Block.Test;
using UnityEditor.VFX.UI;
using System.Reflection;

using static UnityEditor.VFX.Test.VFXControllerTestHelper;

namespace UnityEditor.VFX.Test.Integration.Controllers
{
    [TestFixture]
    public class VFXControllerHDRPTests
    {
        private VFXViewController m_ViewController;
        private int m_StartUndoGroupId;

        [SetUp]
        public void CreateTestAsset()
        {
            m_ViewController = VFXControllerTestHelper.CreateTestAsset();
            m_StartUndoGroupId = Undo.GetCurrentGroup();
        }

        [OneTimeSetUp]
        public void Init()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
        }

        [OneTimeTearDown]
        public void OnTimeCleanup()
        {
            VFXTestCommon.CloseAllVFXWindow();
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [TearDown]
        public void DestroyTestAsset()
        {
            VFXControllerTestHelper.DestroyTestAsset(m_ViewController, m_StartUndoGroupId, null);
        }

        [UnityTest, Description("Repro from UUM-84060")]
        public IEnumerator CustomHLSL_Usage_In_Sample_Water_Unexpected_Dirty()
        {
            string vfxPath = null;

            //Prepare Asset
            {
                var vfxGraph = VFXTestCommon.CreateGraph_And_System();
                vfxPath = AssetDatabase.GetAssetPath(vfxGraph);

                var sampleWaterDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Sample Water Surface");
                Assert.IsNotNull(sampleWaterDesc);
                var sampleWater = sampleWaterDesc.CreateInstance();
                vfxGraph.AddChild(sampleWater);

                var output = vfxGraph.children.OfType<VFXAbstractRenderedOutput>().SingleOrDefault();
                Assert.IsNotNull(output);

                var setPosition = ScriptableObject.CreateInstance<SetAttribute>();
                setPosition.SetSettingValue("attribute", "position");
                output.AddChild(setPosition);
                Assert.IsTrue(sampleWater.outputSlots[0].Link(setPosition.inputSlots[0]));
                AssetDatabase.ImportAsset(vfxPath);
                yield return null;
            }

            //Prepare Controller
            VFXViewWindow window = null;
            {
                var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
                Assert.IsNotNull(asset);
                Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetEntityId(), 0));

                window = VFXTestCommon.GetWindow(asset);
                window.LoadAsset(asset, null);
                var viewController = window.graphView.controller;
                Assert.IsNotNull(viewController);
                yield return null;
            }

            for (int i = 0; i < 4; i++)
                yield return null;

            //A failure would log "Expression graph was marked as dirty after compiling context for UI. Discard to avoid infinite compilation loop." is logged
            window.Close();
            yield return null;
        }
    }
}
