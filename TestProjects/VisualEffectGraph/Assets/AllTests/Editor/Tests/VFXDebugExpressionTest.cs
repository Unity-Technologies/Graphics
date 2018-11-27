#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
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

            var asset = VisualEffectResource.CreateNewAsset(tempFilePath);

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
            while (!(VisualEffectUtility.GetExpressionFloat(vfxComponent, expressionIndex) > 0.01f) && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            UnityEngine.Object.DestroyImmediate(gameObj);
            UnityEngine.Object.DestroyImmediate(cameraObj);
        }

        //TEMP disable LogAssert.Expect, still failing running on katana
#if _ENABLE_LOG_EXCEPT_TEST
        [UnityTest]
        public IEnumerator CreateAsset_And_Check_Exception_On_Invalid_Graph()
        {
            var graph = MakeTemporaryGraph();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            graph.AddChild(spawnerContext);
            var constantRate = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();
            graph.AddChild(constantRate);

            // Attach to a valid particle system so that spawner is compiled
            var initContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            graph.AddChild(initContext);
            var quadOutput = ScriptableObject.CreateInstance<VFXQuadOutput>();
            graph.AddChild(quadOutput);
            var meshOutput = ScriptableObject.CreateInstance<VFXMeshOutput>();
            graph.AddChild(meshOutput);
            spawnerContext.LinkTo(initContext);
            initContext.LinkTo(quadOutput);
            initContext.LinkTo(meshOutput);

            var branch = ScriptableObject.CreateInstance<Operator.Branch>();
            graph.AddChild(branch);
            branch.SetOperandType((SerializableType)typeof(Mesh));
            Assert.IsTrue(branch.outputSlots[0].Link(meshOutput.inputSlots.First(s => s.property.type == typeof(Mesh))));

            var compare = ScriptableObject.CreateInstance<Operator.Condition>();
            graph.AddChild(compare);
            Assert.IsTrue(compare.outputSlots[0].Link(branch.inputSlots[0]));

            var modulo = ScriptableObject.CreateInstance<Operator.Modulo>();
            graph.AddChild(modulo);
            modulo.SetOperandType((SerializableType)typeof(uint));
            modulo.inputSlots[1].value = 2u;
            Assert.IsTrue(modulo.outputSlots[0].Link(compare.inputSlots[0]));

            var particleId = ScriptableObject.CreateInstance<VFXAttributeParameter>();
            graph.AddChild(particleId);
            particleId.SetSettingValue("attribute", VFXAttribute.ParticleId.name);

            graph.RecompileIfNeeded(); //at this point, compilation is still legal
            particleId.outputSlots[0].Link(modulo.inputSlots[0]);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("InvalidOperationException"));
            graph.RecompileIfNeeded();

            branch.outputSlots[0].UnlinkAll();
            graph.RecompileIfNeeded(); //Back to a legal state

            quadOutput.SetSettingValue("blendMode", VFXAbstractParticleOutput.BlendMode.Masked);
            Assert.IsTrue(modulo.outputSlots[0].Link(quadOutput.inputSlots.First(o => o.name.Contains("alphaThreshold"))));
            graph.RecompileIfNeeded(); //Should also be legal (alphaTreshold relying on particleId)

            yield return null;
        }
#endif
    }
}
#endif
