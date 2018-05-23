using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;
using UnityEditor;
using UnityEngine.TestTools;
using System.Linq;
using UnityEditor.VFX.UI;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    public class VFXDebugExpressionTest
    {
        string tempFilePath = "Assets/TmpTests/vfxTest.vfx";

        VFXGraph MakeTemporaryGraph()
        {
            if (System.IO.File.Exists(tempFilePath))
            {
                AssetDatabase.DeleteAsset(tempFilePath);
            }

            var asset = VisualEffectAsset.Create(tempFilePath);

            VisualEffectResource resource = asset.GetResource(); // force resource creation

            VFXGraph graph = ScriptableObject.CreateInstance<VFXGraph>();

            graph.visualEffectResource = resource;

            return graph;
        }

        [TearDown]
        public void CleanUp()
        {
            AssetDatabase.DeleteAsset(tempFilePath);
        }

        [UnityTest]
        [Timeout(1000 * 10)]
        public IEnumerator CreateAssetAndComponentTotalTime()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            var graph = MakeTemporaryGraph();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var constantRate = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();

            // Attach to a valid particle system so that spawner is compiled
            var initContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var outputContext = ScriptableObject.CreateInstance<VFXPointOutput>();
            spawnerContext.LinkTo(initContext);
            initContext.LinkTo(outputContext);

            var slotRate = constantRate.GetInputSlot(0);
            var totalTime = VFXLibrary.GetOperators().First(o => o.name == VFXExpressionOperation.TotalTime.ToString()).CreateInstance();
            slotRate.Link(totalTime.GetOutputSlot(0));

            spawnerContext.AddChild(constantRate);
            graph.AddChild(spawnerContext);

            graph.RecompileIfNeeded();
            var expressionIndex = graph.FindReducedExpressionIndexFromSlotCPU(slotRate);

            var gameObj = new GameObject("CreateAssetAndComponentDebugExpressionTest");
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            var cameraObj = new GameObject("CreateAssetAndComponentSpawner_Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(vfxComponent.transform);

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            maxFrame = 512;
            while (!(vfxComponent.DebugExpressionGetFloat(expressionIndex) > 0.01f) && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            UnityEngine.Object.DestroyImmediate(gameObj);
            UnityEngine.Object.DestroyImmediate(cameraObj);
        }
    }
}
