using System;
using NUnit.Framework;
using UnityEngine;
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

            var slotRate = constantRate.GetInputSlot(0);
            var totalTime = VFXLibrary.GetBuiltInParameters().First(o => o.name == VFXExpressionOp.kVFXTotalTimeOp.ToString()).CreateInstance();
            slotRate.Link(totalTime.GetOutputSlot(0));

            spawnerContext.AddChild(constantRate);
            graph.AddChild(spawnerContext);

            graph.vfxAsset = new VFXAsset();
            graph.RecompileIfNeeded();
            var expressionIndex = graph.FindReducedExpressionIndexFromSlotCPU(slotRate);
            graph.vfxAsset.bounds = new Bounds(Vector3.zero, Vector3.positiveInfinity);

            var gameObj = new GameObject("CreateAssetAndComponentDebugExpressionTest");
            var vfxComponent = gameObj.AddComponent<VFXComponent>();
            vfxComponent.vfxAsset = graph.vfxAsset;

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
        }
    }
}
