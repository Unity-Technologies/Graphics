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
using UnityEditor.VFX.Block;

namespace UnityEditor.VFX.Test
{
    public class VFXSpawnerTest
    {
        string tempFilePath = "Assets/Temp_vfxTest.vfx";

        VFXGraph MakeTemporaryGraph()
        {
            if (System.IO.File.Exists(tempFilePath))
            {
                AssetDatabase.DeleteAsset(tempFilePath);
            }

            var asset = VisualEffectAssetEditorUtility.CreateNewAsset(tempFilePath);

            VisualEffectResource resource = asset.GetResource(); // force resource creation

            VFXGraph graph = ScriptableObject.CreateInstance<VFXGraph>();

            graph.visualEffectResource = resource;

            return graph;
        }

        int m_previousCaptureFrameRate;
        float m_previousFixedTimeStep;
        float m_previousMaxDeltaTime;

        [OneTimeSetUp]
        public void Init()
        {
            m_previousCaptureFrameRate = Time.captureFramerate;
            m_previousFixedTimeStep = UnityEngine.Experimental.VFX.VFXManager.fixedTimeStep;
            m_previousMaxDeltaTime = UnityEngine.Experimental.VFX.VFXManager.maxDeltaTime;
            Time.captureFramerate = 10;
            UnityEngine.Experimental.VFX.VFXManager.fixedTimeStep = 0.1f;
            UnityEngine.Experimental.VFX.VFXManager.maxDeltaTime = 0.1f;
        }

        [TearDown]
        public void CleanUp()
        {
            Time.captureFramerate = m_previousCaptureFrameRate;
            UnityEngine.Experimental.VFX.VFXManager.fixedTimeStep = m_previousFixedTimeStep;
            UnityEngine.Experimental.VFX.VFXManager.maxDeltaTime = m_previousMaxDeltaTime;
            AssetDatabase.DeleteAsset(tempFilePath);
        }

        [UnityTest]
        public IEnumerator CreateAssetAndComponentSpawner()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");

            var graph = MakeTemporaryGraph();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockConstantRate = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();
            var slotCount = blockConstantRate.GetInputSlot(0);

            var spawnerInit = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var spawnerOutput = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();

            var spawnCountValue = 753.0f;
            slotCount.value = spawnCountValue;

            spawnerContext.AddChild(blockConstantRate);
            graph.AddChild(spawnerContext);
            graph.AddChild(spawnerInit);
            graph.AddChild(spawnerOutput);
            spawnerInit.LinkFrom(spawnerContext);
            spawnerOutput.LinkFrom(spawnerInit);

            graph.RecompileIfNeeded();

            var gameObj = new GameObject("CreateAssetAndComponentSpawner");
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
            yield return null; //wait for exactly one more update if visible

            var spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);

            var spawnCountRead = spawnerState.spawnCount / spawnerState.deltaTime;
            Assert.LessOrEqual(Mathf.Abs(spawnCountRead - spawnCountValue), 0.01f);
            UnityEngine.Object.DestroyImmediate(gameObj);
            UnityEngine.Object.DestroyImmediate(cameraObj);
        }

        [UnityTest]
        public IEnumerator CreateEventStartAndStop()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            var graph = MakeTemporaryGraph();

            var eventStart = ScriptableObject.CreateInstance<VFXBasicEvent>();
            eventStart.eventName = "Custom_Start";
            var eventStop = ScriptableObject.CreateInstance<VFXBasicEvent>();
            eventStop.eventName = "Custom_Stop";

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockConstantRate = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();
            var slotCount = blockConstantRate.GetInputSlot(0);

            var spawnerInit = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var spawnerOutput = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();

            var spawnCountValue = 1984.0f;
            slotCount.value = spawnCountValue;

            spawnerContext.AddChild(blockConstantRate);
            graph.AddChild(eventStart);
            graph.AddChild(eventStop);
            graph.AddChild(spawnerContext);
            graph.AddChild(spawnerInit);
            graph.AddChild(spawnerOutput);
            spawnerInit.LinkFrom(spawnerContext);
            spawnerOutput.LinkFrom(spawnerInit);
            spawnerContext.LinkFrom(eventStart, 0, 0);
            spawnerContext.LinkFrom(eventStop, 0, 1);

            graph.RecompileIfNeeded();

            var gameObj = new GameObject("CreateEventStartAndStop");
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            var cameraObj = new GameObject("CreateEventStartAndStop_Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(vfxComponent.transform);

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            yield return null; //wait for exactly one more update if visible

            var spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);
            var spawnCountRead = spawnerState.spawnCount / spawnerState.deltaTime;
            Assert.LessOrEqual(Mathf.Abs(spawnCountRead), 0.01f);

            vfxComponent.SendEvent("Custom_Start");
            for (int i = 0; i < 16; ++i) yield return null;

            spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);
            spawnCountRead = spawnerState.spawnCount / spawnerState.deltaTime;
            Assert.LessOrEqual(Mathf.Abs(spawnCountRead - spawnCountValue), 0.01f);

            vfxComponent.SendEvent("Custom_Stop");
            for (int i = 0; i < 16; ++i) yield return null;

            spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);
            spawnCountRead = spawnerState.spawnCount / spawnerState.deltaTime;
            Assert.LessOrEqual(Mathf.Abs(spawnCountRead), 0.01f);

            UnityEngine.Object.DestroyImmediate(gameObj);
            UnityEngine.Object.DestroyImmediate(cameraObj);
        }

        /*
        [UnityTest]
        [Timeout(1000 * 10)]
        public IEnumerator CreateEventAttributeAndStart()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            var graph = ScriptableObject.CreateInstance<VFXGraph>();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockBurst = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();

            spawnerContext.AddChild(blockBurst);
            graph.AddChild(spawnerContext);

            graph.visualEffectAsset = new VisualEffectAsset();
            graph.RecompileIfNeeded();
            graph.visualEffectAsset.bounds = new Bounds(Vector3.zero, Vector3.positiveInfinity);

            var gameObj = new GameObject("CreateEventAttributeAndStart");
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectAsset;

            var lifeTimeIn = 28.0f;
            var vfxEventAttr = vfxComponent.CreateVFXEventAttribute();
            vfxEventAttr.SetFloat("lifeTime", lifeTimeIn);
            vfxComponent.Start(vfxEventAttr);

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            yield return null; //wait for exactly one more update if visible

            var spawnerState = vfxComponent.GetSpawnerState(0);
            var lifeTimeOut = spawnerState.vfxEventAttribute.GetFloat("lifeTime");
            Assert.AreEqual(lifeTimeIn, lifeTimeOut);

            UnityEngine.Object.DestroyImmediate(gameObj);
        }
        */

        [UnityTest]
        public IEnumerator CreateCustomSpawnerAndComponent()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            var graph = MakeTemporaryGraph();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockCustomSpawner = ScriptableObject.CreateInstance<VFXSpawnerCustomWrapper>();
            blockCustomSpawner.SetSettingValue("m_customType", new SerializableType(typeof(VFXCustomSpawnerTest)));

            var spawnerInit = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var spawnerOutput = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();
            var blockSetAttribute = ScriptableObject.CreateInstance<SetAttribute>();
            blockSetAttribute.SetSettingValue("attribute", "lifetime");
            spawnerInit.AddChild(blockSetAttribute);

            var blockAttributeSource = ScriptableObject.CreateInstance<VFXAttributeParameter>();
            blockAttributeSource.SetSettingValue("location", VFXAttributeLocation.Source);
            blockAttributeSource.SetSettingValue("attribute", "lifetime");

            spawnerContext.AddChild(blockCustomSpawner);
            graph.AddChild(spawnerContext);
            graph.AddChild(spawnerInit);
            graph.AddChild(spawnerOutput);
            graph.AddChild(blockAttributeSource);
            blockAttributeSource.outputSlots[0].Link(blockSetAttribute.inputSlots[0]);

            spawnerInit.LinkFrom(spawnerContext);
            spawnerOutput.LinkFrom(spawnerInit);

            var valueTotalTime = 187.0f;
            blockCustomSpawner.GetInputSlot(0).value = valueTotalTime;

            graph.RecompileIfNeeded();

            var gameObj = new GameObject("CreateCustomSpawnerAndComponent");
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            var cameraObj = new GameObject("CreateCustomSpawnerAndComponent_Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(vfxComponent.transform);

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            yield return null; //wait for exactly one more update if visible

            var spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);
            Assert.GreaterOrEqual(spawnerState.totalTime, valueTotalTime);
            Assert.AreEqual(VFXCustomSpawnerTest.s_LifeTime, spawnerState.vfxEventAttribute.GetFloat("lifetime"));
            Assert.AreEqual(VFXCustomSpawnerTest.s_SpawnCount, spawnerState.spawnCount);

            UnityEngine.Object.DestroyImmediate(gameObj);
            UnityEngine.Object.DestroyImmediate(cameraObj);
        }

        /*
        [UnityTest]
        public IEnumerator CreateCustomSpawnerLinkedWithSourceAttribute()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            var graph = ScriptableObject.CreateInstance<VFXGraph>();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            graph.AddChild(spawnerContext);

            var initContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            graph.AddChild(initContext);

            spawnerContext.LinkTo(initContext);

            var initBlock = ScriptableObject.CreateInstance<VFXAllType>();
            initContext.AddChild(initBlock);

            var attributeVelocity = VFXLibrary.GetSourceAttributeParameters().FirstOrDefault(o => o.name == "velocity").CreateInstance();
            graph.AddChild(attributeVelocity);

            var targetSlot = initBlock.inputSlots.FirstOrDefault(o => o.property.type == attributeVelocity.outputSlots[0].property.type);
            attributeVelocity.outputSlots[0].Link(targetSlot);

            graph.visualEffectAsset = new VisualEffectAsset();
            graph.RecompileIfNeeded();
            graph.visualEffectAsset.bounds = new Bounds(Vector3.zero, Vector3.positiveInfinity);

            var gameObj = new GameObject("CreateCustomSpawnerLinkedWithSourceAttribute");
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectAsset;

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            yield return null; //wait for exactly one more update if visible

            var vfxEventAttribute = vfxComponent.CreateVFXEventAttribute();
            Assert.IsTrue(vfxEventAttribute.HasVector3("velocity"));
        }
        */

        [UnityTest]
        public IEnumerator CreateSpawner_Single_Burst_With_Delay()
        {
            //This test cover a regression : 1154292
            EditorApplication.ExecuteMenuItem("Window/General/Game");

            var graph = MakeTemporaryGraph();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockSpawnerBurst = ScriptableObject.CreateInstance<VFXSpawnerBurst>();
            var slotCount = blockSpawnerBurst.GetInputSlot(0);
            var delay = blockSpawnerBurst.GetInputSlot(1);

            var spawnerInit = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var spawnerOutput = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();

            var spawnCountValue = 456.0f;
            slotCount.value = spawnCountValue;

            var delayValue = 1.2f;
            delay.value = delayValue;

            spawnerContext.AddChild(blockSpawnerBurst);
            graph.AddChild(spawnerContext);
            graph.AddChild(spawnerInit);
            graph.AddChild(spawnerOutput);
            spawnerInit.LinkFrom(spawnerContext);
            spawnerOutput.LinkFrom(spawnerInit);

            //Force issue due to uninitialized expression (otherwise, constant folding resolve it)
            graph.SetCompilationMode(VFXCompilationMode.Edition); 
            graph.RecompileIfNeeded();

            var gameObj = new GameObject("CreateSpawner_Single_Burst_With_Delay");
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            var cameraObj = new GameObject("CreateSpawner_Single_Burst_With_Delay_Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(vfxComponent.transform);

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            var spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);

            //Sleeping state
            maxFrame = 512;
            while (--maxFrame > 0)
            {
                spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);
                if (spawnerState.totalTime < delayValue)
                    Assert.AreEqual(0.0f, spawnerState.spawnCount);
                else
                    break;
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            //Spawning supposed to occur
            maxFrame = 512;
            while (--maxFrame > 0)
            {
                spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);
                if (spawnerState.spawnCount == spawnCountValue)
                    break;
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            UnityEngine.Object.DestroyImmediate(gameObj);
            UnityEngine.Object.DestroyImmediate(cameraObj);
        }
    }
}
#endif
