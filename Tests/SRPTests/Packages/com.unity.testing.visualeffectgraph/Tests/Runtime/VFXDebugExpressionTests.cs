#if UNITY_EDITOR && (!UNITY_EDITOR_OSX || MAC_FORCE_TESTS)
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.TestTools;
using System.Linq;
using System.Collections;
using UnityEngine.SceneManagement;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXDebugExpressionTest
    {
        [OneTimeSetUp]
        public void Init()
        {
        }

        Scene m_SceneToUnload;
        GameObject m_mainObject;
        [SetUp]
        public void Setup()
        {
            m_SceneToUnload = SceneManager.CreateScene("EmptyDebugExpression_" + Guid.NewGuid());
            SceneManager.SetActiveScene(m_SceneToUnload);

            var mainObjectName = "VFX_Test_Main_Object";
            m_mainObject = new GameObject(mainObjectName);

            var mainCameraName = "VFX_Test_Main_Camera";
            var mainCamera = new GameObject(mainCameraName);
            var camera = mainCamera.AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(m_mainObject.transform.position);
        }

        [TearDown]
        public void Teardown()
        {
            SceneManager.UnloadSceneAsync(m_SceneToUnload);
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        static VFXModelDescriptor<VFXOperator> GetTotalTimeOperator()
        {
            string opName = ObjectNames.NicifyVariableName(VFXExpressionOperation.TotalTime.ToString());
            return VFXLibrary.GetOperators().First(o => o.name.StartsWith(opName));
        }

        [UnityTest]
        public IEnumerator Create_Asset_And_Component_Check_Expected_TotalTime()
        {
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
            var totalTime = GetTotalTimeOperator().CreateInstance();
            slotRate.Link(totalTime.GetOutputSlot(0));

            spawnerContext.AddChild(constantRate);
            graph.AddChild(spawnerContext);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
            var expressionIndex = graph.FindReducedExpressionIndexFromSlotCPU(slotRate);

            while (m_mainObject.GetComponent<VisualEffect>() != null)
                UnityEngine.Object.DestroyImmediate(m_mainObject.GetComponent<VisualEffect>());
            var vfxComponent = m_mainObject.AddComponent<VisualEffect>();
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

        [UnityTest]
        public IEnumerator Check_Total_Time_Is_Always_The_Sum_of_DeltaTime()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();

            var blockCustomSpawner = ScriptableObject.CreateInstance<VFXSpawnerCustomWrapper>();
            blockCustomSpawner.SetSettingValue("m_customType", new SerializableType(typeof(VFXCustomSpawnerTimeCheckerTest)));

            var initContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var outputContext = ScriptableObject.CreateInstance<VFXPointOutput>();

            spawnerContext.LinkTo(initContext);
            initContext.LinkTo(outputContext);

            spawnerContext.AddChild(blockCustomSpawner);
            graph.AddChild(spawnerContext);
            graph.AddChild(initContext);
            graph.AddChild(outputContext);

            //plug total time into custom spawn total time
            var builtInParameter = ScriptableObject.CreateInstance<VFXBuiltInParameter>();
            builtInParameter.SetSettingValue("m_expressionOp", VFXExpressionOperation.TotalTime);
            blockCustomSpawner.inputSlots[0].Link(builtInParameter.outputSlots[0]);

            graph.AddChild(builtInParameter);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            var vfxComponent = m_mainObject.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            int maxFrame = 64;
            while (vfxComponent.culled && --maxFrame > 0)
                yield return null;
            Assert.IsTrue(maxFrame > 0);

            while (--maxFrame > 0 && VFXCustomSpawnerTimeCheckerTest.s_ReadInternalTotalTime < 0.2f)
                yield return null;
            Assert.IsTrue(maxFrame > 0);

            //Moved the object until culled
            var backupPosition = vfxComponent.transform.position;

            maxFrame = 64;
            while (--maxFrame > 0 && !vfxComponent.culled)
            {
                vfxComponent.transform.position = vfxComponent.transform.position + Vector3.up * 5.0f;
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            for (int i = 0; i < 8; ++i)
                yield return null;

            vfxComponent.transform.position = backupPosition;

            for (int i = 0; i < 8; ++i)
                yield return null;

            Assert.AreEqual((double)VFXCustomSpawnerTimeCheckerTest.s_ReadInternalTotalTime, (double)VFXCustomSpawnerTimeCheckerTest.s_ReadTotalTimeThroughInput, 0.0001f);
        }
    }
}
#endif
