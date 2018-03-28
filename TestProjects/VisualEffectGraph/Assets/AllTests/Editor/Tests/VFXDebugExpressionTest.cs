using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.VFX;
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
        [UnityTest]
        [Timeout(1000 * 10)]
        public IEnumerator CreateAssetAndComponentTotalTime()
        {
            EditorApplication.ExecuteMenuItem("Window/Game");
            var graph = ScriptableObject.CreateInstance<VFXGraph>();

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

            graph.visualEffectAsset = new VisualEffectAsset();
            graph.RecompileIfNeeded();
            var expressionIndex = graph.FindReducedExpressionIndexFromSlotCPU(slotRate);

            var gameObj = new GameObject("CreateAssetAndComponentDebugExpressionTest");
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectAsset;

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
