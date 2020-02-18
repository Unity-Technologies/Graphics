#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using UnityEditor;
using UnityEngine.TestTools;
using System.Linq;
using UnityEditor.VFX.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace UnityEditor.VFX.Test
{
    public class VFXDebugExpressionTest
    {
        private float m_previousFixedTimeStep;
        private float m_previousMaxDeltaTime;
        private GameObject m_gameObject;
        private GameObject m_camera;

        [SetUp]
        public void Init()
        {
            m_previousFixedTimeStep = UnityEngine.VFX.VFXManager.fixedTimeStep;
            m_previousMaxDeltaTime = UnityEngine.VFX.VFXManager.maxDeltaTime;

            m_gameObject = new GameObject("MainGameObject");

            m_camera = new GameObject("CreateAssetAndComponentSpawner_Camera");
            var camera = m_camera.AddComponent<Camera>();
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(m_gameObject.transform);
        }

        [TearDown]
        public void CleanUp()
        {
            UnityEngine.VFX.VFXManager.fixedTimeStep = m_previousFixedTimeStep;
            UnityEngine.VFX.VFXManager.maxDeltaTime = m_previousMaxDeltaTime;
            VFXTestCommon.DeleteAllTemporaryGraph();

            UnityEngine.Object.DestroyImmediate(m_gameObject);
            UnityEngine.Object.DestroyImmediate(m_camera);
        }

        [UnityTest]
        public IEnumerator Create_Asset_And_Component_Check_Expected_TotalTime()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            var graph = VFXTestCommon.MakeTemporaryGraph();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var constantRate = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();

            // Attach to a valid particle system so that spawner is compiled
            var initContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var outputContext = ScriptableObject.CreateInstance<VFXPointOutput>();
            graph.AddChild(initContext);
            graph.AddChild(outputContext);
            spawnerContext.LinkTo(initContext);
            initContext.LinkTo(outputContext);

            var slotRate = constantRate.GetInputSlot(0);
            string opName = ObjectNames.NicifyVariableName(VFXExpressionOperation.TotalTime.ToString());

            var totalTime = VFXLibrary.GetOperators().First(o => o.name == opName).CreateInstance();
            slotRate.Link(totalTime.GetOutputSlot(0));

            spawnerContext.AddChild(constantRate);
            graph.AddChild(spawnerContext);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
            var expressionIndex = graph.FindReducedExpressionIndexFromSlotCPU(slotRate);

            while (m_gameObject.GetComponent<VisualEffect>() != null) UnityEngine.Object.DestroyImmediate(m_gameObject.GetComponent<VisualEffect>());
            var vfxComponent = m_gameObject.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

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
        }

#pragma warning disable 0414
        public static object[] updateModes = { VFXUpdateMode.FixedDeltaTime, VFXUpdateMode.DeltaTime };
#pragma warning restore 0414

        [UnityTest]
        public IEnumerator Create_Asset_And_Component_Check_Overflow_MaxDeltaTime([ValueSource("updateModes")] object updateMode)
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            graph.visualEffectResource.updateMode = (VFXUpdateMode)updateMode;

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var constantRate = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();
            var initContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var outputContext = ScriptableObject.CreateInstance<VFXPointOutput>();
            graph.AddChild(initContext);
            graph.AddChild(outputContext);

            spawnerContext.LinkTo(initContext);
            initContext.LinkTo(outputContext);

            spawnerContext.AddChild(constantRate);
            graph.AddChild(spawnerContext);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            var vfxComponent = m_gameObject.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            float fixedTimeStep = 1.0f / 20.0f;
            float maxTimeStep = 1.0f / 10.0f;

            UnityEngine.VFX.VFXManager.fixedTimeStep = fixedTimeStep;
            UnityEngine.VFX.VFXManager.maxDeltaTime = maxTimeStep;

            /* waiting for culling (simulating big delay between each frame) */
            int maxFrame = 512;
            VFXSpawnerState spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0u);
            float sleepTimeInSeconds = maxTimeStep * 5.0f;
            while (--maxFrame > 0 && spawnerState.deltaTime != maxTimeStep)
            {
                System.Threading.Thread.Sleep((int)(sleepTimeInSeconds * 1000.0f));
                yield return null;
                spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0u);
            }
            Assert.IsTrue(maxFrame > 0);
            if (graph.visualEffectResource.updateMode == VFXUpdateMode.FixedDeltaTime)
            {
                Assert.AreEqual(maxTimeStep, spawnerState.deltaTime);
            }
            else
            {
                Assert.AreEqual(maxTimeStep, spawnerState.deltaTime); //< There is clamp even in delta time mode
                //Assert.AreEqual((double)sleepTimeInSeconds, spawnerState.deltaTime, 0.01f);
            }
            yield return null;
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

            quadOutput.SetSettingValue("blendMode", VFXAbstractParticleOutput.BlendMode.Opaque);
            quadOutput.SetSettingValue("useAlphaClipping", true);
            Assert.IsTrue(modulo.outputSlots[0].Link(quadOutput.inputSlots.First(o => o.name.Contains("alphaThreshold"))));
            graph.RecompileIfNeeded(); //Should also be legal (alphaTreshold relying on particleId)

            yield return null;
        }
#endif
    }
}
#endif
