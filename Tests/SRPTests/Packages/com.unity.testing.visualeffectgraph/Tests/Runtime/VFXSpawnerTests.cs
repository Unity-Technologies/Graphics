#if UNITY_EDITOR && (!UNITY_EDITOR_OSX || MAC_FORCE_TESTS)
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.TestTools;

using UnityEditor.VFX.Block;
using UnityEngine.SceneManagement;

namespace UnityEditor.VFX.Test
{
    [TestFixture
#if VFX_TESTS_HAS_URP
     , Ignore("VFXSpawnerTests doesn't need to be launched on both SRP.")
#endif
    ]
    public class VFXSpawnerTests
    {
        [OneTimeSetUp]
        public void Init()
        {
        }

        Scene m_SceneToUnload;
        [SetUp]
        public void Setup()
        {
            m_SceneToUnload = SceneManager.CreateScene("EmptySpawnerScene_" + Guid.NewGuid());
            SceneManager.SetActiveScene(m_SceneToUnload);

            Time.captureFramerate = 10;
            VFXManager.fixedTimeStep = 0.1f;
            VFXManager.maxDeltaTime = 0.1f;
        }

        [TearDown]
        public void Teardown()
        {
            SceneManager.UnloadSceneAsync(m_SceneToUnload);
            Time.captureFramerate = 0;
            VFXManager.fixedTimeStep = 1.0f / 60.0f;
            VFXManager.maxDeltaTime = 1.0f / 20.0f;
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }
        
        private void CreateAssetAndComponent(float spawnCountValue, string playEventName, out VFXGraph graph, out VisualEffect vfxComponent, out GameObject gameObj, out GameObject cameraObj)
        {
            graph = VFXTestCommon.MakeTemporaryGraph();

            var eventStart = ScriptableObject.CreateInstance<VFXBasicEvent>();
            eventStart.eventName = playEventName;

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockConstantRate = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();
            var slotCount = blockConstantRate.GetInputSlot(0);

            var spawnerInit = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var spawnerOutput = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();

            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.modelType == typeof(Block.SetAttribute));
            var blockAttribute = blockAttributeDesc.CreateInstance();
            blockAttribute.SetSettingValue("attribute", "position");
            spawnerInit.AddChild(blockAttribute);

            slotCount.value = spawnCountValue;

            spawnerContext.AddChild(blockConstantRate);
            graph.AddChild(eventStart);
            graph.AddChild(spawnerContext);
            graph.AddChild(spawnerInit);
            graph.AddChild(spawnerOutput);

            spawnerContext.LinkFrom(eventStart, 0, 0);
            spawnerInit.LinkFrom(spawnerContext);
            spawnerOutput.LinkFrom(spawnerInit);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            gameObj = new GameObject("CreateAssetAndComponentSpawner");
            vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            cameraObj = new GameObject("CreateAssetAndComponentSpawner_Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(vfxComponent.transform);
        }

        [UnityTest]
        public IEnumerator Sanitize_VFXSpawnerCustomCallback_Namespace()
        {
            string kSourceAsset = "Packages/com.unity.testing.visualeffectgraph/Tests/Runtime/VFXSpawnerCustomCallbackBuiltin.vfx_";
            var graph = VFXTestCommon.CopyTemporaryGraph(kSourceAsset);

            Assert.AreEqual(1, graph.children.OfType<VFXBasicSpawner>().Count());
            var basicSpawner = graph.children.OfType<VFXBasicSpawner>().FirstOrDefault();
            Assert.AreEqual(4, basicSpawner.GetNbChildren());
            Assert.IsNotNull(basicSpawner.children.FirstOrDefault(o => o.name == ObjectNames.NicifyVariableName("SpawnOverDistance")));
            Assert.IsNotNull(basicSpawner.children.FirstOrDefault(o => o.name == ObjectNames.NicifyVariableName("SetSpawnTime")));
            Assert.IsNotNull(basicSpawner.children.FirstOrDefault(o => o.name == ObjectNames.NicifyVariableName("LoopAndDelay")));
            Assert.IsNotNull(basicSpawner.children.FirstOrDefault(o => o.name == ObjectNames.NicifyVariableName("IncrementStripIndexOnStart")));

            foreach (var sanitizeSpawn in basicSpawner.children)
            {
                Assert.IsFalse(sanitizeSpawn.inputSlots.Any(o => !o.HasLink()));
                Assert.IsNotNull(sanitizeSpawn.GetSettingValue("m_customScript"));
                Assert.IsNotNull((sanitizeSpawn as VFXSpawnerCustomWrapper).customBehavior);
            }

            yield return null;
        }

        //Cover case 1122404
        [UnityTest]
        public IEnumerator Create_Asset_And_Set_Really_High_SpawnRate()
        {
            VisualEffect vfxComponent;
            GameObject cameraObj, gameObj;
            VFXGraph graph;

            var reallyBigFloat = 3e+38f;
            CreateAssetAndComponent(reallyBigFloat, "OnPlay", out graph, out vfxComponent, out gameObj, out cameraObj);

            var init = graph.children.OfType<VFXBasicInitialize>().First();
            var setLifetime = ScriptableObject.CreateInstance<SetAttribute>();
            setLifetime.SetSettingValue("attribute", "lifetime"); //Issue 1122404 only occurs when hasKill
            setLifetime.inputSlots[0].value = 1.0f;
            init.AddChild(setLifetime);

            var update = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            graph.AddChild(update);

            init.LinkTo(update);
            update.LinkTo(graph.children.OfType<VFXPlanarPrimitiveOutput>().First());

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            int maxFrame = 256;
            while (vfxComponent.culled && --maxFrame > 0)
                yield return null;
            Assert.IsTrue(maxFrame > 0);

            //Assertion failed on expression: 'nbGroups.x > 0 && nbGroups.y > 0' is logged before 1122404 resolution.
            yield return null;

            var spawnSystems = new List<string>();
            vfxComponent.GetSpawnSystemNames(spawnSystems);
            var spawnState = vfxComponent.GetSpawnSystemInfo(spawnSystems[0]);
            Assert.IsTrue(spawnState.spawnCount >= reallyBigFloat * 0.01f);

            var spawnCountCastInt = (int)spawnState.spawnCount; //expecting an overflow
            Assert.IsTrue(spawnCountCastInt < 0);
        }

        static string[] k_Create_Asset_And_Check_Event_ListCases = new[] { "OnPlay", "Test_Event" };

        [UnityTest]
        public IEnumerator Create_Asset_And_Check_Event_List([ValueSource("k_Create_Asset_And_Check_Event_ListCases")] string playEventName)
        {
            VisualEffect vfxComponent;
            GameObject cameraObj, gameObj;
            VFXGraph graph;
            CreateAssetAndComponent(123.0f, playEventName, out graph, out vfxComponent, out gameObj, out cameraObj);

            var visualEffectAsset = vfxComponent.visualEffectAsset;
            Assert.IsNotNull(visualEffectAsset);

            var vfxEvents = new List<string>();
            visualEffectAsset.GetEvents(vfxEvents);

            Assert.IsTrue(vfxEvents.Contains(playEventName));
            Assert.IsTrue(vfxEvents.Contains("OnStop"));
            Assert.AreEqual(2, vfxEvents.Count);

            yield return null;
        }

        [Retry(3)]
        [UnityTest]
        public IEnumerator Create_Asset_And_Component_Spawner_Check_Initial_Event()
        {
            var spawnCountValue = 666.0f;
            VisualEffect vfxComponent;
            GameObject cameraObj, gameObj;
            VFXGraph graph;

            var initialEventName = "CustomInitialEvent";
            CreateAssetAndComponent(spawnCountValue, initialEventName, out graph, out vfxComponent, out gameObj, out cameraObj);
            gameObj.name = "Create_Asset_And_Component_Spawner_Check_Initial_Event";

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            yield return null; //wait for exactly one more update if visible

            //Default event state is supposed to be "OnPlay"
            var spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
            Assert.AreEqual(0.0, spawnerState.spawnCount);

            var editor = Editor.CreateEditor(graph.GetResource().asset);
            editor.serializedObject.Update();
            var initialEventProperty = editor.serializedObject.FindProperty("m_Infos.m_InitialEventName");
            initialEventProperty.stringValue = initialEventName;
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            GameObject.DestroyImmediate(editor);

            yield return null;
            spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
            var spawnCountRead = spawnerState.spawnCount / spawnerState.deltaTime;
            Assert.LessOrEqual(Mathf.Abs(spawnCountRead - spawnCountValue), 0.01f);

            //Now, do it on vfxComponent (override automatically taken into account)
            vfxComponent.initialEventName = "OnPlay";
            vfxComponent.Reinit(); //Automatic while changing it through serialized property, here, it's a runtime behavior
            yield return null;

            spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
            Assert.AreEqual(0.0f, spawnerState.spawnCount);

            //Try setting the correct value
            vfxComponent.initialEventName = initialEventName;
            vfxComponent.Reinit();
            yield return null;

            spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
            spawnCountRead = spawnerState.spawnCount / spawnerState.deltaTime;
            Assert.LessOrEqual(Mathf.Abs(spawnCountRead - spawnCountValue), 0.01f);

            
        }

        static List<Vector3> s_RecordedPositions = new List<Vector3>();
        static void OnEventReceived_SavePosition(VFXOutputEventArgs evt)
        {
            s_RecordedPositions.Add(evt.eventAttribute.GetVector3("position"));
        }

        static bool[] s_Verify_Reseed_OnPlay_Behavior_options = new bool[] { false, true };

        [UnityTest]
        public IEnumerator Verify_Reseed_OnPlay_Behavior([ValueSource("s_Verify_Reseed_OnPlay_Behavior_options")] bool reseed, [ValueSource("s_Verify_Reseed_OnPlay_Behavior_options")] bool useSendEvent)
        {
            var spawnCountValue = 1.0f;
            VisualEffect vfxComponent;
            GameObject cameraObj, gameObj;
            VFXGraph graph;
            CreateAssetAndComponent(spawnCountValue, "OnPlay", out graph, out vfxComponent, out gameObj, out cameraObj);

            var outputEvent = ScriptableObject.CreateInstance<VFXOutputEvent>();
            var eventName = "qsdf";
            outputEvent.SetSettingValue("eventName", eventName);
            var basicSpawner = graph.children.OfType<VFXBasicSpawner>().FirstOrDefault();
            graph.AddChild(outputEvent);
            outputEvent.LinkFrom(basicSpawner);

            //Add constant random to inspect the current seed
            var setAttributePosition = ScriptableObject.CreateInstance<VFXSpawnerSetAttribute>();
            setAttributePosition.SetSettingValue("attribute", "position");
            basicSpawner.AddChild(setAttributePosition);

            for (int i = 0; i < 3; ++i)
            {
                var random = ScriptableObject.CreateInstance<Operator.Random>();
                random.SetSettingValue("seed", VFXSeedMode.PerVFXComponent);
                random.SetSettingValue("constant", true);
                graph.AddChild(outputEvent);
                random.outputSlots.First().Link(setAttributePosition.inputSlots.First()[i]);
            }
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            s_RecordedPositions = new List<Vector3>();
            vfxComponent.outputEventReceived += OnEventReceived_SavePosition;
            vfxComponent.resetSeedOnPlay = reseed;

            int maxFrame = 256;
            while (s_RecordedPositions.Count < 3 && --maxFrame > 0)
                yield return null;

            Assert.IsTrue(maxFrame > 0);
            Assert.AreEqual(1, s_RecordedPositions.Distinct().Count());

            for (int i = 0; i < 3; ++i)
            {
                //The seed should change depending on resetSeedOnPlay settings
                if (useSendEvent)
                    vfxComponent.SendEvent(VisualEffectAsset.PlayEventID);
                else
                    vfxComponent.Play();

                maxFrame = 256;
                while (s_RecordedPositions.Count < 3 + i * 3 && --maxFrame > 0)
                    yield return null;
                Assert.IsTrue(maxFrame > 0);
            }

            var distinctCount = s_RecordedPositions.Distinct().Count();
            if (reseed)
                Assert.AreNotEqual(1, distinctCount);
            else
                Assert.AreEqual(1, distinctCount);

            
        }

        [UnityTest]
        public IEnumerator Send_Event_And_Reinit_Expecting_Clear()
        {
            var eventName = "fghjkl";
            CreateAssetAndComponent(0.0f, eventName, out var graph, out var vfxComponent, out var gameObj, out var cameraObj);

            var blockCustomSpawner = ScriptableObject.CreateInstance<VFXSpawnerCustomWrapper>();
            blockCustomSpawner.SetSettingValue("m_customType", new SerializableType(typeof(VFXCustomSpawnerRecordSpawnCount)));
            var basicSpawner = graph.children.OfType<VFXBasicSpawner>().FirstOrDefault();
            basicSpawner.AddChild(blockCustomSpawner);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            VFXCustomSpawnerRecordSpawnCount.ClearReceivedSpawnCount();
            var attr = vfxComponent.CreateVFXEventAttribute();
            attr.SetFloat("spawnCount", 0);
            vfxComponent.SendEvent(eventName, attr);
            attr.SetFloat("spawnCount", 1);
            vfxComponent.SendEvent(eventName, attr);
            attr.SetFloat("spawnCount", 2);
            vfxComponent.SendEvent(eventName, attr);

            vfxComponent.Reinit();

            attr.SetFloat("spawnCount", 3);
            vfxComponent.SendEvent(eventName, attr);
            attr.SetFloat("spawnCount", 4);
            vfxComponent.SendEvent(eventName, attr);
            attr.SetFloat("spawnCount", 5);
            vfxComponent.SendEvent(eventName, attr);

            int maxFrame = 64;
            while (!VFXCustomSpawnerRecordSpawnCount.GetReceivedSpawnCount().Any() && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            Assert.AreEqual(3, VFXCustomSpawnerRecordSpawnCount.GetReceivedSpawnCount().Count());
            Assert.IsFalse(VFXCustomSpawnerRecordSpawnCount.GetReceivedSpawnCount().Any(o => o < 3));

            
        }

        static List<int> s_ReceivedEventNamedId;
        static void OnEventReceived_RegisterNameID(VFXOutputEventArgs evt)
        {
            s_ReceivedEventNamedId.Add(evt.nameId);
        }

        [UnityTest]
        public IEnumerator Create_Asset_And_Component_Spawner_And_Output_Event()
        {
            //This mainly cover return value & expected behavior, event attribute values are covered by a graphic test
            var spawnCountValue = 1.0f; //We running these test at 10FPS
            VisualEffect vfxComponent;
            GameObject cameraObj, gameObj;
            VFXGraph graph;
            CreateAssetAndComponent(spawnCountValue, "OnPlay", out graph, out vfxComponent, out gameObj, out cameraObj);

            var outputEvent = ScriptableObject.CreateInstance<VFXOutputEvent>();
            var eventName = "wxcvbn";
            outputEvent.SetSettingValue("eventName", eventName);
            var basicSpawner = graph.children.OfType<VFXBasicSpawner>().FirstOrDefault();
            graph.AddChild(outputEvent);
            outputEvent.LinkFrom(basicSpawner);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            s_ReceivedEventNamedId = new List<int>();
            vfxComponent.outputEventReceived += OnEventReceived_RegisterNameID;

            int maxFrame = 64;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            var outputEventNames = new List<string>();
            vfxComponent.GetOutputEventNames(outputEventNames);
            Assert.AreEqual(1u, outputEventNames.Count);
            var outputEventName = outputEventNames[0];
            Assert.AreEqual(outputEventName, eventName);

            //Checking invalid event (waiting for the first event)
            Assert.AreEqual(0u, s_ReceivedEventNamedId.Count);

            //Checking on valid event while there is an event
            maxFrame = 64; s_ReceivedEventNamedId.Clear();
            while (s_ReceivedEventNamedId.Count == 0u && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            Assert.IsTrue(s_ReceivedEventNamedId.Count > 0);
            Assert.AreEqual(Shader.PropertyToID(eventName), s_ReceivedEventNamedId.FirstOrDefault());

            s_ReceivedEventNamedId.Clear();

            
        }

        [UnityTest]
        public IEnumerator Create_Asset_And_Component_Spawner_And_Output_Event_Expected_Count()
        {
            //This mainly cover return value & expected behavior, event attribute values are covered by a graphic test
            var spawnCountValue = 1.0f; //We running these test at 10FPS
            VisualEffect vfxComponent;
            GameObject cameraObj, gameObj;
            VFXGraph graph;
            CreateAssetAndComponent(spawnCountValue, "OnPlay", out graph, out vfxComponent, out gameObj, out cameraObj);

            var outputEvent = ScriptableObject.CreateInstance<VFXOutputEvent>();
            var basicSpawner = graph.children.OfType<VFXBasicSpawner>().FirstOrDefault();
            graph.AddChild(outputEvent);
            outputEvent.LinkFrom(basicSpawner);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            s_ReceivedEventNamedId = new List<int>();
            vfxComponent.outputEventReceived += OnEventReceived_RegisterNameID;

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            vfxComponent.Reinit();
            float deltaTime = 0.1f;
            uint count = 32;
            vfxComponent.Simulate(deltaTime, count);
            Assert.AreEqual(0u, s_ReceivedEventNamedId.Count); //The simulate is asynchronous

            float simulateTime = deltaTime * count;
            uint expectedEventCount = (uint)Mathf.Floor(simulateTime / spawnCountValue);

            maxFrame = 64; s_ReceivedEventNamedId.Clear();
            cameraObj.SetActive(false);
            while (s_ReceivedEventNamedId.Count == 0u && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.AreEqual(expectedEventCount, (uint)s_ReceivedEventNamedId.Count);
            yield return null;

        }

        [UnityTest]
        public IEnumerator Create_Asset_And_Component_Spawner_With_Manual_Set_SpawnCount_And_Output_Event_Check_Expected_Count()
        {

            //This mainly cover return value & expected behavior, event attribute values are covered by a graphic test
            var spawnCountValue = 1.0f; //We running these test at 10FPS, half of rate provided by constantRate, the other comes from manaul spawnCount
            VisualEffect vfxComponent;
            GameObject cameraObj, gameObj;
            VFXGraph graph;
            CreateAssetAndComponent(spawnCountValue / 2.0f, "OnPlay", out graph, out vfxComponent, out gameObj, out cameraObj);

            //Manual constant rate
            var spawnState = ScriptableObject.CreateInstance<Operator.SpawnState>();
            graph.AddChild(spawnState);

            var currentSpawnCount = ScriptableObject.CreateInstance<VFXAttributeParameter>(); //Also available in spawn state
            currentSpawnCount.SetSettingValue("location", VFXAttributeLocation.Current);
            currentSpawnCount.SetSettingValue("attribute", VFXAttribute.SpawnCount.name);
            graph.AddChild(currentSpawnCount);

            graph.AddChild(spawnState);

            var multiply = ScriptableObject.CreateInstance<Operator.Multiply>();
            multiply.SetOperandType(0, typeof(float));
            multiply.SetOperandType(1, typeof(float));
            graph.AddChild(multiply);

            var add = ScriptableObject.CreateInstance<Operator.Add>();
            add.SetOperandType(0, typeof(float));
            add.SetOperandType(1, typeof(float));
            graph.AddChild(add);

            var setSpawnCount = ScriptableObject.CreateInstance<Block.VFXSpawnerSetAttribute>();
            setSpawnCount.SetSettingValue("attribute", VFXAttribute.SpawnCount.name);
            graph.children.OfType<VFXBasicSpawner>().First().AddChild(setSpawnCount);

            Assert.IsTrue(spawnState.outputSlots.First(o => o.name == "SpawnDeltaTime").Link(multiply.inputSlots[0]));
            multiply.inputSlots[1].value = spawnCountValue / 2.0f;

            Assert.IsTrue(multiply.outputSlots[0].Link(add.inputSlots[0]));
            Assert.IsTrue(currentSpawnCount.outputSlots[0].Link(add.inputSlots[1]));
            Assert.IsTrue(add.outputSlots[0].Link(setSpawnCount.inputSlots[0]));

            //Create output event
            var outputEvent = ScriptableObject.CreateInstance<VFXOutputEvent>();
            var basicSpawner = graph.children.OfType<VFXBasicSpawner>().FirstOrDefault();
            graph.AddChild(outputEvent);
            outputEvent.LinkFrom(basicSpawner);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            s_ReceivedEventNamedId = new List<int>();
            vfxComponent.outputEventReceived += OnEventReceived_RegisterNameID;

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            vfxComponent.Reinit();
            float deltaTime = 0.1f;
            uint count = 32;
            vfxComponent.Simulate(deltaTime, count);
            Assert.AreEqual(0u, s_ReceivedEventNamedId.Count); //The simulate is asynchronous

            float simulateTime = deltaTime * count;
            uint expectedEventCount = (uint)Mathf.Floor(simulateTime / spawnCountValue);

            maxFrame = 64; s_ReceivedEventNamedId.Clear();
            cameraObj.SetActive(false);
            while (s_ReceivedEventNamedId.Count == 0u && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.AreEqual(expectedEventCount, (uint)s_ReceivedEventNamedId.Count);
            yield return null;

            
        }

        [UnityTest]
        public IEnumerator Create_Asset_And_Component_Spawner()
        {
            var spawnCountValue = 753.0f;
            VisualEffect vfxComponent;
            GameObject cameraObj, gameObj;
            VFXGraph graph;
            CreateAssetAndComponent(spawnCountValue, "OnPlay", out graph, out vfxComponent, out gameObj, out cameraObj);

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            yield return null; //wait for exactly one more update if visible

            var spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
            var spawnCountRead = spawnerState.spawnCount / spawnerState.deltaTime;
            Assert.LessOrEqual(Mathf.Abs(spawnCountRead - spawnCountValue), 0.01f);
        }

        public struct VFXTimeModeTest
        {
            public override string ToString()
            {
                return name;
            }

            public string name { get; }
            public uint vfxUpdateMode { get; }
            public uint expectedUpdateCount { get; }
            public float expectedDeltaTime { get; }

            public VFXTimeModeTest(string name, uint vfxUpdateMode, uint expectedUpdateCount, float expectedDeltaTime)
            {
                this.name = name;
                this.vfxUpdateMode = vfxUpdateMode;
                this.expectedUpdateCount = expectedUpdateCount;
                this.expectedDeltaTime = expectedDeltaTime;
            }
        }

        const float s_Check_Time_Mode_SleepingTimeInSecond = 1.0f;
        const float s_Check_Time_Mode_FixedDeltaTime = 0.1f;
        const float s_Check_Time_Mode_MaxDeltaTime = 0.7f;

        static VFXTimeModeTest[] s_CheckTimeMode = new[]
        {
            new VFXTimeModeTest("FixedDeltaTime", (uint)VFXUpdateMode.FixedDeltaTime, 1u, s_Check_Time_Mode_MaxDeltaTime),
            new VFXTimeModeTest("ExactFixedDeltaTime", (uint)VFXUpdateMode.ExactFixedTimeStep, (uint)Mathf.Floor(s_Check_Time_Mode_MaxDeltaTime / s_Check_Time_Mode_FixedDeltaTime), s_Check_Time_Mode_FixedDeltaTime),
        };

        //Fix 1216631 : Check Exact time has actually an effect in low fps condition
        [UnityTest]
        public IEnumerator Create_Spawner_Check_Time_Mode_Update_Count([ValueSource("s_CheckTimeMode")] VFXTimeModeTest timeMode)
        {
            var spawnCountValue = 651.0f;
            VisualEffect vfxComponent;
            GameObject cameraObj, gameObj;
            VFXGraph graph;
            CreateAssetAndComponent(spawnCountValue, "OnPlay", out graph, out vfxComponent, out gameObj, out cameraObj);

            var basicSpawner = graph.children.OfType<VFXBasicSpawner>().FirstOrDefault();
            var blockCustomSpawner = ScriptableObject.CreateInstance<VFXSpawnerCustomWrapper>();
            blockCustomSpawner.SetSettingValue("m_customType", new SerializableType(typeof(VFXCustomSpawnerUpdateCounterTest)));
            basicSpawner.AddChild(blockCustomSpawner);

            graph.GetResource().updateMode = (VFXUpdateMode)timeMode.vfxUpdateMode;
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
            Assert.AreEqual(graph.GetResource().updateMode, (VFXUpdateMode)timeMode.vfxUpdateMode);
            
            VFXManager.fixedTimeStep = s_Check_Time_Mode_FixedDeltaTime;
            VFXManager.maxDeltaTime = s_Check_Time_Mode_MaxDeltaTime;
            Time.captureDeltaTime = 0;

            VFXCustomSpawnerUpdateCounterTest.s_UpdateCount = 0;
            //Wait for the first warm up
            int maxFrame = 128;
            while (VFXCustomSpawnerUpdateCounterTest.s_UpdateCount == 0 && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.AreNotEqual(0u, VFXCustomSpawnerUpdateCounterTest.s_UpdateCount);

            //Force Capture Delta Time to large number
            Time.captureDeltaTime = s_Check_Time_Mode_SleepingTimeInSecond;
            while (Time.deltaTime < s_Check_Time_Mode_SleepingTimeInSecond - 0.1f && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.False(maxFrame < 0);

            vfxComponent.Reinit();
            VFXCustomSpawnerUpdateCounterTest.s_UpdateCount = 0;
            VFXCustomSpawnerUpdateCounterTest.s_LastDeltaTime = 0.0f;
            while (VFXCustomSpawnerUpdateCounterTest.s_UpdateCount == 0)
            {
                yield return null;
            }
            Assert.AreEqual(timeMode.expectedUpdateCount, VFXCustomSpawnerUpdateCounterTest.s_UpdateCount);
            Assert.AreEqual(timeMode.expectedDeltaTime, VFXCustomSpawnerUpdateCounterTest.s_LastDeltaTime);
        }

        //Cover fix from 1268360 : Simple usage of exact fixed time step, it should not throw any error from the renderer
        [UnityTest]
        public IEnumerator Create_Spawner_Check_No_Incorrect_Thread_Count([ValueSource("s_CheckTimeMode")] VFXTimeModeTest timeMode)
        {
            var spawnCountValue = 651.0f;
            VisualEffect vfxComponent;
            GameObject cameraObj, gameObj;
            VFXGraph graph;
            CreateAssetAndComponent(spawnCountValue, "OnPlay", out graph, out vfxComponent, out gameObj, out cameraObj);
            graph.GetResource().updateMode = (VFXUpdateMode)timeMode.vfxUpdateMode;
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
            
            UnityEngine.VFX.VFXManager.fixedTimeStep = 0.1f;
            UnityEngine.VFX.VFXManager.maxDeltaTime = 0.5f;
            Time.captureDeltaTime = 1.0f;

            int maxFrame = 128;
            while (vfxComponent.culled && --maxFrame > 0)
                yield return null;

            //Wait a few frame to verify if we are getting any warning from the rendering
            for (int i = 0; i < 5; ++i)
                yield return null;

            UnityEngine.Object.DestroyImmediate(gameObj);
            UnityEngine.Object.DestroyImmediate(cameraObj);
        }


        //Fix case 1217876
        static VFXTimeModeTest[] s_Change_Fixed_Time_Step_To_A_Large_Value_Then_Back_To_Default = new[]
        {
            new VFXTimeModeTest("FixedDeltaTime", (uint)VFXUpdateMode.FixedDeltaTime, 0u, 0),
            new VFXTimeModeTest("FixedDeltaTime_And_IgnoreTimeScale", (uint)VFXUpdateMode.IgnoreTimeScale, 0u, 0),
        };

        [UnityTest]
        public IEnumerator Change_Fixed_Time_Step_To_A_Large_Value_Then_Back_To_Default([ValueSource("s_Change_Fixed_Time_Step_To_A_Large_Value_Then_Back_To_Default")] VFXTimeModeTest timeMode)
        {
            VisualEffect vfxComponent;
            GameObject cameraObj, gameObj;
            VFXGraph graph;
            CreateAssetAndComponent(3615.0f, "OnPlay", out graph, out vfxComponent, out gameObj, out cameraObj);
            graph.GetResource().updateMode = (VFXUpdateMode)timeMode.vfxUpdateMode;
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
            Assert.AreEqual(graph.GetResource().updateMode, (VFXUpdateMode)timeMode.vfxUpdateMode);

            var previousCaptureFrameRate = Time.captureFramerate;
            var previousFixedTimeStep = UnityEngine.VFX.VFXManager.fixedTimeStep;
            var previousMaxDeltaTime = UnityEngine.VFX.VFXManager.maxDeltaTime;

            //Set default
            UnityEngine.VFX.VFXManager.fixedTimeStep = 0.01f;

            var maxFrame = 64;
            Time.captureFramerate = 10;
            while (Mathf.Abs(Time.deltaTime - Time.captureDeltaTime) > 0.0001f && --maxFrame > 0)
                yield return null; //wait capture deltaTime setting effective

            //Change_Fixed_Time_Step_To_A_Large_Value
            UnityEngine.VFX.VFXManager.fixedTimeStep = 2.0f;
            UnityEngine.VFX.VFXManager.maxDeltaTime = 5.0f;

            //wait a few frame
            for (int frame = 0; frame < 6; ++frame)
                yield return null;

            //Then_Back_To_Default (actually, a really small value)
            Time.captureFramerate = 600; //Not round delta time, easily failing on small value
            UnityEngine.VFX.VFXManager.fixedTimeStep = 0.001f; //Can be 0.01f too

            //Failure should occurs within these frame
            for (int frame = 0; frame < 8; ++frame)
            {
                var spawnState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
                Assert.AreNotEqual(spawnState.deltaTime, UnityEngine.VFX.VFXManager.maxDeltaTime); //Overflow in step count
                yield return null;
            }

            Time.captureFramerate = previousCaptureFrameRate;
            UnityEngine.VFX.VFXManager.fixedTimeStep = previousFixedTimeStep;
            UnityEngine.VFX.VFXManager.maxDeltaTime = previousMaxDeltaTime;

            
        }

        [UnityTest]
        public IEnumerator Create_Asset_And_Component_Spawner_Plugging_OnStop_Into_Start_Input_Flow()
        {
            //Cover regression introduced at b76b691db3313ca06f157580e954116eca1473fa
            VisualEffect vfxComponent;
            GameObject cameraObj, gameObj;
            VFXGraph graph;
            CreateAssetAndComponent(457.0f, "OnStop", out graph, out vfxComponent, out gameObj, out cameraObj);

            //Plug a Dummy Event on "Stop" entry (otherwise OnStop is implicitly plugged)
            var eventStop = ScriptableObject.CreateInstance<VFXBasicEvent>();
            eventStop.eventName = "Dummy";
            graph.AddChild(eventStop);
            graph.children.OfType<VFXBasicSpawner>().First().LinkFrom(eventStop, 0, 1);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            yield return null; //wait for exactly one more update if visible

            var spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
            Assert.AreEqual(VFXSpawnerLoopState.Finished, spawnerState.loopState);

            //Now send event Stop, we expect to wake up
            vfxComponent.Stop();
            maxFrame = 512;
            while (--maxFrame > 0)
            {
                yield return null;
                spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
                if (spawnerState.loopState != VFXSpawnerLoopState.Finished)
                    break;
            }
            Assert.AreNotEqual(VFXSpawnerLoopState.Finished, spawnerState.loopState);
            Assert.IsTrue(maxFrame > 0);

            
        }

        [UnityTest]
        public IEnumerator Create_CustomEvent_For_StartAndStop_And_Send_Them_Manually()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();

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

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

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

            var spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
            var spawnCountRead = spawnerState.spawnCount / spawnerState.deltaTime;
            Assert.LessOrEqual(Mathf.Abs(spawnCountRead), 0.01f);

            vfxComponent.SendEvent("Custom_Start");
            yield return null;

            spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
            spawnCountRead = spawnerState.spawnCount / spawnerState.deltaTime;
            Assert.LessOrEqual(Mathf.Abs(spawnCountRead - spawnCountValue), 0.01f);

            vfxComponent.SendEvent("Custom_Stop");
            yield return null;

            spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
            spawnCountRead = spawnerState.spawnCount / spawnerState.deltaTime;
            Assert.LessOrEqual(Mathf.Abs(spawnCountRead), 0.01f);
        }

        [UnityTest]
        public IEnumerator Create_CustomSpawner_And_Component()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();

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

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            var gameObj = new GameObject("CreateCustomSpawnerAndComponent");
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            var cameraObj = new GameObject("CreateCustomSpawnerAndComponent_Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(vfxComponent.transform);

            int maxFrame = 512;
            var spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
            vfxComponent.Simulate(0.5f);
            while ((spawnerState.playing == false || spawnerState.deltaTime == 0.0f) && --maxFrame > 0)
            {
                yield return null;
                spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
            }
            Assert.IsTrue(maxFrame > 0);

            Assert.GreaterOrEqual(spawnerState.totalTime, valueTotalTime);
            Assert.AreEqual(VFXCustomSpawnerTest.s_LifeTime, spawnerState.vfxEventAttribute.GetFloat("lifetime"));
            Assert.AreEqual(VFXCustomSpawnerTest.s_SpawnCount, spawnerState.spawnCount);
        }
        
        [UnityTest]
        public IEnumerator CreateSpawner_Set_Attribute_With_ContextDelay()
        {
            //This test cover an issue : 1205329

            var graph = VFXTestCommon.MakeTemporaryGraph();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockSpawnerBurst = ScriptableObject.CreateInstance<VFXSpawnerBurst>();
            var setSpawnEventAttribute = ScriptableObject.CreateInstance<VFXSpawnerSetAttribute>();
            setSpawnEventAttribute.SetSettingValue("attribute", "color");
            var colorSlot = setSpawnEventAttribute.GetInputSlot(0);
            Assert.AreEqual(VFXValueType.Float3, colorSlot.valueType);

            var expectedColor = new Vector3(0.1f, 0.2f, 0.3f);
            colorSlot.value = expectedColor;
            blockSpawnerBurst.GetInputSlot(0).value = 23.0f;

            var inheritColor = ScriptableObject.CreateInstance<SetAttribute>();
            inheritColor.SetSettingValue("Source", SetAttribute.ValueSource.Source);
            inheritColor.SetSettingValue("attribute", "color");

            var spawnerInit = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var spawnerOutput = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();

            var delayValue = 1.2f;
            spawnerContext.SetSettingValue("delayBeforeLoop", VFXBasicSpawner.DelayMode.Constant);
            spawnerContext.GetInputSlot(0).value = delayValue;

            spawnerContext.AddChild(blockSpawnerBurst);
            spawnerContext.AddChild(setSpawnEventAttribute);
            spawnerInit.AddChild(inheritColor);

            graph.AddChild(spawnerContext);
            graph.AddChild(spawnerInit);
            graph.AddChild(spawnerOutput);
            spawnerInit.LinkFrom(spawnerContext);
            spawnerOutput.LinkFrom(spawnerInit);

            graph.SetCompilationMode(VFXCompilationMode.Edition);
            graph.RecompileIfNeeded(false, true);

            var gameObj = new GameObject("CreateSpawner_Set_Attribute_With_Delay");
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            var cameraObj = new GameObject("CreateSpawner_Set_Attribute_With_Delay_Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(vfxComponent.transform);

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            var spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
            //Catching sleeping state
            maxFrame = 512;
            while (--maxFrame > 0 && spawnerState.totalTime < delayValue / 10.0f)
            {
                spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            //Check SetAttribute State while delaying
            Assert.AreEqual(spawnerState.loopState, VFXSpawnerLoopState.DelayingBeforeLoop);
            Assert.IsTrue(spawnerState.vfxEventAttribute.HasVector3("color"));

            var actualColor = spawnerState.vfxEventAttribute.GetVector3("color");
            Assert.AreEqual((double)expectedColor.x, (double)actualColor.x, 0.001);
            Assert.AreEqual((double)expectedColor.y, (double)actualColor.y, 0.001);
            Assert.AreEqual((double)expectedColor.z, (double)actualColor.z, 0.001);
        }

        [UnityTest]
        public IEnumerator CreateSpawner_Single_Burst_With_Delay()
        {
            //This test cover a regression : 1154292
            
            var graph = VFXTestCommon.MakeTemporaryGraph();

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

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

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

            var spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);

            //Sleeping state
            maxFrame = 512;
            while (--maxFrame > 0)
            {
                spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
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
                spawnerState = VFXTestCommon.GetSpawnerState(vfxComponent, 0);
                if (spawnerState.spawnCount == spawnCountValue)
                    break;
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
        }

        string expectedLogFolder = "Packages/com.unity.testing.visualeffectgraph/Tests/Runtime/VFXSpawnerTests_";
        bool CompareWithExpectedLog(StringBuilder actualContent, string identifier, out string log)
        {
            var pathExpected = expectedLogFolder + identifier + ".expected.txt";
            var pathActual = expectedLogFolder + identifier + ".actual.txt";
            bool success = true;
            var sb = new StringBuilder();

            IEnumerable<string> expectedContent = Enumerable.Empty<string>();
            try
            {
                expectedContent = System.IO.File.ReadLines(pathExpected);
            }
            catch (System.Exception)
            {
                success = false;
                sb.AppendLine($"Can't locate file : {pathExpected}");
            }

            //Compare line by line to avoid carriage return differences
            var frameCount = 0;
            var reader = new System.IO.StringReader(actualContent.ToString());
            foreach (var expectedContentLine in expectedContent)
            {
                var line = reader.ReadLine();

                sb.AppendLine($"[{frameCount:D3}] Actual   : {line}");
                if (line == null || string.Compare(line, expectedContentLine, StringComparison.InvariantCulture) != 0)
                {
                    success = false;
                    sb.AppendLine($"[{frameCount:D3}] Expected : {expectedContentLine}");
                }

                frameCount++;
            }

            if (!success)
            {
                System.IO.File.WriteAllText(pathActual, actualContent.ToString());
            }

            log = success ? string.Empty : sb.ToString();
            return success;
        }

        public static IEnumerable<IEnumerable<T>> PermutationHelper<T>(IEnumerable<T> set, IEnumerable<T> subset = null)
        {
            if (subset == null)
                subset = Enumerable.Empty<T>();

            if (!set.Any())
                yield return subset;

            for (var i = 0; i < set.Count(); i++)
            {
                var newSubset = set.Take(i).Concat(set.Skip(i + 1));
                foreach (var permutation in PermutationHelper(newSubset, subset.Concat(set.Skip(i).Take(1))))
                {
                    yield return permutation;
                }
            }
        }

        public static string[] k_CreateSpawner_Chaining_And_Check_Expected_Ordering = PermutationHelper(new[] { "A", "B", "C", "D" }).Select(o => o.Aggregate((a, b) => a + b)).ToArray();
        public static bool[] k_CreateSpawner_Chaining_And_Check_Expected_Plug_C_First = { true, false };
        [UnityTest]
        public IEnumerator CreateSpawner_Chaining_And_Check_Expected_Ordering([ValueSource("k_CreateSpawner_Chaining_And_Check_Expected_Ordering")] string ordering, [ValueSource("k_CreateSpawner_Chaining_And_Check_Expected_Plug_C_First")] bool plugCFirst)
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            // A -> B -> C  -> Init
            //  \-> D      /
            var correctSequences = new string[] { "ABCD", "ABDC" };

            foreach (var c in ordering)
            {
                var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
                var blockSpawnerConstant = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();
                blockSpawnerConstant.GetInputSlot(0).value = 0.1f;
                spawnerContext.label = c.ToString();
                spawnerContext.AddChild(blockSpawnerConstant);
                graph.AddChild(spawnerContext);
            }

            var initialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var setPosition = ScriptableObject.CreateInstance<SetAttribute>();
            setPosition.SetSettingValue("attribute", "position");
            initialize.AddChild(setPosition);
            var output = ScriptableObject.CreateInstance<VFXPointOutput>();
            graph.AddChild(initialize);
            graph.AddChild(output);
            output.LinkFrom(initialize);

            var spawn_a = graph.children.OfType<VFXBasicSpawner>().First(o => o.label == "A");
            var spawn_b = graph.children.OfType<VFXBasicSpawner>().First(o => o.label == "B");
            var spawn_c = graph.children.OfType<VFXBasicSpawner>().First(o => o.label == "C");
            var spawn_d = graph.children.OfType<VFXBasicSpawner>().First(o => o.label == "D");

            if (plugCFirst)
            {
                initialize.LinkFrom(spawn_c);
                initialize.LinkFrom(spawn_d);
            }
            else
            {
                initialize.LinkFrom(spawn_d);
                initialize.LinkFrom(spawn_c);
            }

            spawn_d.LinkFrom(spawn_a);
            spawn_c.LinkFrom(spawn_b);
            spawn_b.LinkFrom(spawn_a);

            Assert.AreEqual(2, spawn_a.outputFlowSlot[0].link.Count);
            Assert.AreEqual(1, spawn_b.outputFlowSlot[0].link.Count);
            Assert.AreEqual(1, spawn_c.outputFlowSlot[0].link.Count);
            Assert.AreEqual(1, spawn_d.outputFlowSlot[0].link.Count);
            graph.SetCompilationMode(VFXCompilationMode.Runtime);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            var gameObj = new GameObject("CreateSpawner_Chaining_And_Check_Expected_Ordering");
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            var particleSystem = new List<string>();
            vfxComponent.GetParticleSystemNames(particleSystem);
            Assert.AreEqual(1, particleSystem.Count);

            var spawnSystem = new List<string>();
            vfxComponent.GetSpawnSystemNames(spawnSystem);
            Assert.AreEqual(4, spawnSystem.Count);
            Assert.Contains("A", spawnSystem);
            Assert.Contains("B", spawnSystem);
            Assert.Contains("C", spawnSystem);
            Assert.Contains("D", spawnSystem);

            var actualSequence = spawnSystem.Aggregate((a, b) => a + b);
            Assert.Contains(actualSequence, correctSequences);
            yield return null;

            GameObject.DestroyImmediate(gameObj);
        }

        static readonly System.Reflection.MethodInfo[] k_SpawnerStateGetter = typeof(VFXSpawnerState).GetMethods().Where(o => o.Name.StartsWith("get_") && o.Name != "get_vfxEventAttribute").ToArray();
        static string DebugSpawnerStateAggregate(IEnumerable<string> all)
        {
            return all.Select(o => new string(o.Take(12).ToArray()).PadRight(12)).Aggregate((a, b) => a + " | " + b);
        }

        static string DebugSpawnerStateHeader()
        {
            var allStateName = k_SpawnerStateGetter.Select(o => o.Name.Replace("get_", ""));
            return DebugSpawnerStateAggregate(allStateName);
        }

        static string DebugSpawnerState(VFXSpawnerState state)
        {
            var allState = k_SpawnerStateGetter.Select(o =>
            {
                var value = o.Invoke(state, null);
                if (value is float)
                {
                    return ((float)value).ToString("0.00", CultureInfo.InvariantCulture);
                }
                return value.ToString();
            });
            return DebugSpawnerStateAggregate(allState);
        }

        [UnityTest]
        public IEnumerator CreateSpawner_Chaining()
        {
            Assert.AreEqual(UnityEngine.VFX.VFXManager.fixedTimeStep, 0.1f); //Early guard

            var graph = VFXTestCommon.MakeTemporaryGraph();

            var spawnerContext_A = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockSpawnerConstant = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();
            blockSpawnerConstant.GetInputSlot(0).value = 0.6f; //spawn count constant

            var spawnerContext_B = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockSpawnerBurst = ScriptableObject.CreateInstance<VFXSpawnerBurst>();
            blockSpawnerBurst.GetInputSlot(0).value = 11.0f; //spawn count burst
            blockSpawnerBurst.GetInputSlot(1).value = 0.2f; //delay burst

            var spawnerInit = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var spawnerOutput = ScriptableObject.CreateInstance<VFXPointOutput>();

            spawnerContext_A.AddChild(blockSpawnerConstant);
            spawnerContext_B.AddChild(blockSpawnerBurst);
            graph.AddChild(spawnerContext_A);
            graph.AddChild(spawnerContext_B);
            graph.AddChild(spawnerInit);
            graph.AddChild(spawnerOutput);

            //Add Position to have minimal data, and thus, valid system
            var setPosition = ScriptableObject.CreateInstance<SetAttribute>();
            setPosition.SetSettingValue("attribute", "position");
            spawnerInit.AddChild(setPosition);

            spawnerContext_B.LinkFrom(spawnerContext_A, 0, 0 /* OnPlay */);
            spawnerInit.LinkFrom(spawnerContext_B);
            spawnerOutput.LinkFrom(spawnerInit);

            spawnerInit.SetSettingValue("capacity", 512u);

            graph.SetCompilationMode(VFXCompilationMode.Runtime);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            var gameObj = new GameObject("CreateSpawner_Chaining");
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            var cameraObj = new GameObject("CreateSpawner_Chaining_Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(vfxComponent.transform);

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            vfxComponent.Reinit();

            var log = new StringBuilder();
            log.AppendLine(DebugSpawnerStateHeader() + " & " + DebugSpawnerStateHeader());
            var state_A = VFXTestCommon.GetSpawnerState(vfxComponent, 0u);
            var state_B = VFXTestCommon.GetSpawnerState(vfxComponent, 1u);
            var aliveParticleCount = vfxComponent.aliveParticleCount;
            for (int i = 0; i < 115; ++i)
            {
                log.AppendFormat("{0} & {1} => {2}", DebugSpawnerState(state_A), DebugSpawnerState(state_B), aliveParticleCount.ToString("00.00", CultureInfo.InvariantCulture));
                log.AppendLine();

                if (i == 100)
                {
                    log.AppendLine("Stop");
                    vfxComponent.Stop();
                }

                if (i == 105)
                {
                    log.AppendLine("Play");
                    vfxComponent.Play();
                }

                var lastTime = state_A.totalTime;
                maxFrame = 512;
                while (state_A.totalTime == lastTime && --maxFrame > 0) //Ignore frame without vfxUpdate
                {
                    state_A = VFXTestCommon.GetSpawnerState(vfxComponent, 0u);
                    state_B = VFXTestCommon.GetSpawnerState(vfxComponent, 1u);
                    aliveParticleCount = vfxComponent.aliveParticleCount;
                    yield return null;
                }
            }

            string error;
            var compare = CompareWithExpectedLog(log, "Chaining", out error);
            Assert.IsTrue(compare, error);
            yield return null;
            
        }

        public struct CreateSpawner_ChangeLoopMode_TestCase
        {
            internal VFXBasicSpawner.LoopMode LoopDuration;
            internal VFXBasicSpawner.LoopMode LoopCount;
            internal VFXBasicSpawner.DelayMode DelayBeforeLoop;
            internal VFXBasicSpawner.DelayMode DelayAfterLoop;

            public override string ToString()
            {
                return string.Format("{0}{1}{2}{3}",
                    VFXCodeGeneratorHelper.GeneratePrefix((uint)LoopDuration),
                    VFXCodeGeneratorHelper.GeneratePrefix((uint)LoopCount),
                    VFXCodeGeneratorHelper.GeneratePrefix((uint)DelayBeforeLoop),
                    VFXCodeGeneratorHelper.GeneratePrefix((uint)DelayAfterLoop)).ToUpper();
            }
        }

        //Only testing a few cases, not all combination
        public static readonly CreateSpawner_ChangeLoopMode_TestCase[] k_CreateSpawner_ChangeLoopModeTestCases =
        {
            //Default : infinite loop, infinite loop duration
            new CreateSpawner_ChangeLoopMode_TestCase() {
                LoopDuration    = VFXBasicSpawner.LoopMode.Infinite,
                LoopCount       = VFXBasicSpawner.LoopMode.Infinite,
                DelayBeforeLoop = VFXBasicSpawner.DelayMode.None,
                DelayAfterLoop  = VFXBasicSpawner.DelayMode.None
            },
            //Simply random loop
            new CreateSpawner_ChangeLoopMode_TestCase() {
                LoopDuration    = VFXBasicSpawner.LoopMode.Random,
                LoopCount       = VFXBasicSpawner.LoopMode.Infinite,
                DelayBeforeLoop = VFXBasicSpawner.DelayMode.None,
                DelayAfterLoop  = VFXBasicSpawner.DelayMode.None
            },

            //Random loop, adding random before delay
            new CreateSpawner_ChangeLoopMode_TestCase() {
                LoopDuration    = VFXBasicSpawner.LoopMode.Random,
                LoopCount       = VFXBasicSpawner.LoopMode.Infinite,
                DelayBeforeLoop = VFXBasicSpawner.DelayMode.Random,
                DelayAfterLoop  = VFXBasicSpawner.DelayMode.None
            },

            //Random loop count, constant loop duration
            new CreateSpawner_ChangeLoopMode_TestCase() {
                LoopDuration    = VFXBasicSpawner.LoopMode.Constant,
                LoopCount       = VFXBasicSpawner.LoopMode.Random,
                DelayBeforeLoop = VFXBasicSpawner.DelayMode.None,
                DelayAfterLoop  = VFXBasicSpawner.DelayMode.None
            },

            //Everything random
            new CreateSpawner_ChangeLoopMode_TestCase() {
                LoopDuration    = VFXBasicSpawner.LoopMode.Random,
                LoopCount       = VFXBasicSpawner.LoopMode.Random,
                DelayBeforeLoop = VFXBasicSpawner.DelayMode.Random,
                DelayAfterLoop  = VFXBasicSpawner.DelayMode.Random
            },
        };

        [UnityTest, Timeout(300 * 1000)]
        public IEnumerator CreateSpawner_ChangeLoopMode([ValueSource("k_CreateSpawner_ChangeLoopModeTestCases")] CreateSpawner_ChangeLoopMode_TestCase testCase)
        {
            

            Assert.AreEqual(0.1f, UnityEngine.VFX.VFXManager.fixedTimeStep); //Early test
            Assert.AreEqual(10, Time.captureFramerate);

            var graph = VFXTestCommon.MakeTemporaryGraph();
            graph.visualEffectResource.updateMode = VFXUpdateMode.DeltaTime;

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockSpawnerConstant = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();
            var blockSpawnerBurst = ScriptableObject.CreateInstance<VFXSpawnerBurst>();

            blockSpawnerConstant.GetInputSlot(0).value = 3.0f;  //spawn count constant
            blockSpawnerBurst.GetInputSlot(0).value = 10.0f;    //spawn count burst
            blockSpawnerBurst.GetInputSlot(1).value = 0.5f;     //delay burst

            //Apply test case settings
            spawnerContext.SetSettingValue("loopDuration", testCase.LoopDuration);
            spawnerContext.SetSettingValue("loopCount", testCase.LoopCount);
            spawnerContext.SetSettingValue("delayBeforeLoop", testCase.DelayBeforeLoop);
            spawnerContext.SetSettingValue("delayAfterLoop", testCase.DelayAfterLoop);

            if (testCase.LoopDuration != VFXBasicSpawner.LoopMode.Infinite)
            {
                var slot = spawnerContext.inputSlots.FirstOrDefault(o => o.name == "LoopDuration");
                if (testCase.LoopDuration == VFXBasicSpawner.LoopMode.Random)
                    slot.value = new Vector2(0.4f, 0.7f);
                else
                    slot.value = 0.6f;
            }

            if (testCase.LoopCount != VFXBasicSpawner.LoopMode.Infinite)
            {
                var slot = spawnerContext.inputSlots.FirstOrDefault(o => o.name == "LoopCount");
                if (testCase.LoopCount == VFXBasicSpawner.LoopMode.Random)
                    slot.value = new Vector2(3, 8);
                else
                    slot.value = 4;
            }

            if (testCase.DelayBeforeLoop != VFXBasicSpawner.DelayMode.None)
            {
                var slot = spawnerContext.inputSlots.FirstOrDefault(o => o.name == "DelayBeforeLoop");
                if (testCase.DelayBeforeLoop == VFXBasicSpawner.DelayMode.Random)
                    slot.value = new Vector2(0.1f, 0.2f);
                else
                    slot.value = 0.1f;
            }

            if (testCase.DelayAfterLoop != VFXBasicSpawner.DelayMode.None)
            {
                var slot = spawnerContext.inputSlots.FirstOrDefault(o => o.name == "DelayAfterLoop");
                if (testCase.DelayAfterLoop == VFXBasicSpawner.DelayMode.Random)
                    slot.value = new Vector2(0.2f, 0.3f);
                else
                    slot.value = 0.2f;
            }

            var spawnerInit = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var spawnerOutput = ScriptableObject.CreateInstance<VFXPointOutput>();

            spawnerContext.AddChild(blockSpawnerBurst);
            spawnerContext.AddChild(blockSpawnerConstant);
            graph.AddChild(spawnerContext);
            graph.AddChild(spawnerInit);
            graph.AddChild(spawnerOutput);

            //Add Position to have minimal data, and thus, valid system
            var setPosition = ScriptableObject.CreateInstance<SetAttribute>();
            setPosition.SetSettingValue("attribute", "position");
            spawnerInit.AddChild(setPosition);

            spawnerInit.LinkFrom(spawnerContext);
            spawnerOutput.LinkFrom(spawnerInit);

            spawnerInit.SetSettingValue("capacity", 512u);

            graph.SetCompilationMode(VFXCompilationMode.Runtime);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            var gameObj = new GameObject("CreateSpawner_ChangeLoopMode_" + testCase.ToString());
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;
            vfxComponent.startSeed = 1986;
            vfxComponent.resetSeedOnPlay = false;

            var cameraObj = new GameObject("CreateSpawner_ChangeLoopMode_Camera" + testCase.ToString());
            var camera = cameraObj.AddComponent<Camera>();
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(vfxComponent.transform);

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            vfxComponent.Reinit();
            var log = new StringBuilder();
            log.AppendLine(DebugSpawnerStateHeader());
            var state = VFXTestCommon.GetSpawnerState(vfxComponent, 0u);
            var aliveParticleCount = vfxComponent.aliveParticleCount;
            for (int i = 0; i < 150; ++i)
            {
                log.AppendFormat("{0} ==> {1}", DebugSpawnerState(state), aliveParticleCount.ToString("0.00", CultureInfo.InvariantCulture));
                log.AppendLine();

                if (i == 100)
                {
                    log.AppendLine("Stop");
                    vfxComponent.Stop();
                }

                if (i == 110)
                {
                    log.AppendLine("Play");
                    vfxComponent.Play();
                }

                var lastTime = state.totalTime;
                maxFrame = 512;
                while (state.totalTime == lastTime && --maxFrame > 0) //Ignore frame without vfxUpdate
                {
                    state = VFXTestCommon.GetSpawnerState(vfxComponent, 0u);
                    aliveParticleCount = vfxComponent.aliveParticleCount;
                    yield return null;
                }
            }

            var compare = CompareWithExpectedLog(log, testCase.ToString(), out var error);
            Assert.IsTrue(compare, error);

            
        }

        [UnityTest]
        public IEnumerator CreateSpawner_With_All_Zero_Duration() //Cover possible infinite loop
        {
            Assert.AreEqual(VFXManager.fixedTimeStep, 0.1f);

            var graph = VFXTestCommon.MakeTemporaryGraph();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();

            var spawnerInit = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var spawnerOutput = ScriptableObject.CreateInstance<VFXPointOutput>();

            graph.AddChild(spawnerContext);
            graph.AddChild(spawnerInit);
            graph.AddChild(spawnerOutput);

            //Add Position to have minimal data, and thus, valid system
            var setPosition = ScriptableObject.CreateInstance<SetAttribute>();
            setPosition.SetSettingValue("attribute", "position");
            spawnerInit.AddChild(setPosition);

            spawnerInit.LinkFrom(spawnerContext);
            spawnerOutput.LinkFrom(spawnerInit);

            spawnerContext.SetSettingValue("loopDuration", VFXBasicSpawner.LoopMode.Constant);
            spawnerContext.SetSettingValue("loopCount", VFXBasicSpawner.LoopMode.Infinite);
            spawnerContext.SetSettingValue("delayBeforeLoop", VFXBasicSpawner.DelayMode.Constant);
            spawnerContext.SetSettingValue("delayAfterLoop", VFXBasicSpawner.DelayMode.Constant);

            Assert.AreEqual(3, spawnerContext.inputSlots.Count, "Something change in VFXBasicSpawner");
            foreach (var slot in spawnerContext.inputSlots)
            {
                Assert.AreEqual(slot.valueType, VFXValueType.Float, "Something change in VFXBasicSpawner");
                slot.value = 0.0f;
            }

            graph.SetCompilationMode(VFXCompilationMode.Runtime);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            var gameObj = new GameObject("CreateSpawner_All_Zero_Duration");
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;
            vfxComponent.startSeed = 1986;
            vfxComponent.resetSeedOnPlay = false;

            var cameraObj = new GameObject("CreateSpawner_All_Zero_Duration_Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(vfxComponent.transform);

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            maxFrame = 0;
            while (VFXTestCommon.GetSpawnerState(vfxComponent, 0u).loopIndex < 3 /* arbitrary loop count (should not be an infinite loop) */)
            {
                maxFrame++;
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
        }

        private void SetupVisualEffectGraph(VFXGraph graph, string[] attributes)
        {
            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();

            var init = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var output = ScriptableObject.CreateInstance<VFXPointOutput>();

            graph.AddChild(spawnerContext);
            graph.AddChild(init);
            graph.AddChild(output);

            foreach (var attribute in attributes)
            {
                var setAttribute = ScriptableObject.CreateInstance<SetAttribute>();
                setAttribute.SetSettingValue("attribute", attribute);
                setAttribute.SetSettingValue("Source", SetAttribute.ValueSource.Source);
                init.AddChild(setAttribute);
            }

            init.LinkFrom(spawnerContext);
            output.LinkFrom(init);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
        }

        public static readonly string[] s_Layouts = new[] { "position", "position,color", "color,position,direction", "velocity,color,position,direction" };
        private static readonly Dictionary<string, Vector3> s_TestValues = new Dictionary<string, Vector3>
        {
            { "position", new Vector3(48, 59, 26) },
            { "color", new Vector3(3, 2, 4) },
            { "direction", new Vector3(78, 54, 65) },
            { "velocity", new Vector3(7, 8, 9) },
        };


        [UnityTest]
        public IEnumerator Create_Two_Event_Attribute_With_Different_Layout_And_Try_Copy_One_Into_The_Other([ValueSource(nameof(s_Layouts))] string layout_A, [ValueSource(nameof(s_Layouts))] string layout_B)
        {
            var attributes_A = layout_A.Split(',');
            var attributes_B = layout_B.Split(',');

            var graph_A = VFXTestCommon.MakeTemporaryGraph();
            var graph_B = VFXTestCommon.MakeTemporaryGraph();

            SetupVisualEffectGraph(graph_A, attributes_A);
            SetupVisualEffectGraph(graph_B, attributes_B);

            var gameObj_A = new GameObject("Create_Two_Event_Attribute_With_Different_Layout_And_Try_Copy_One_Into_The_Other_A");
            var vfxComponent_A = gameObj_A.AddComponent<VisualEffect>();
            vfxComponent_A.visualEffectAsset = graph_A.visualEffectResource.asset;

            var gameObj_B = new GameObject("Create_Two_Event_Attribute_With_Different_Layout_And_Try_Copy_One_Into_The_Other_B");
            var vfxComponent_B = gameObj_B.AddComponent<VisualEffect>();
            vfxComponent_B.visualEffectAsset = graph_B.visualEffectResource.asset;

            yield return null;

            var event_A = vfxComponent_A.CreateVFXEventAttribute();
            var event_B = vfxComponent_B.CreateVFXEventAttribute();

            foreach (var attribute in attributes_A)
                Assert.IsTrue(event_A.HasVector3(attribute), "(A) Expecting :" + attribute);

            foreach (var attribute in attributes_B)
                Assert.IsTrue(event_B.HasVector3(attribute), "(B) Expecting :" + attribute);

            Assert.IsTrue(event_A.HasFloat("spawnCount"));
            Assert.IsTrue(event_B.HasFloat("spawnCount"));

            var spawnCountRef = 123.0f;
            foreach (var attribute in attributes_B)
                event_B.SetVector3(attribute, s_TestValues[attribute]);
            event_B.SetFloat("spawnCount", spawnCountRef);

            //Check content of event_A before
            foreach (var attribute in attributes_A)
            {
                var refValue = s_TestValues[attribute];
                var readValue = event_A.GetVector3(attribute);

                Assert.AreNotEqual(refValue.x, readValue.x);
                Assert.AreNotEqual(refValue.y, readValue.y);
                Assert.AreNotEqual(refValue.z, readValue.z);
            }
            Assert.AreNotEqual(spawnCountRef, event_A.GetFloat("spawnCount"));

            event_A.CopyValuesFrom(event_B);

            //Check content of event_A after copy
            var matchingAttribute = attributes_A.Where(o => attributes_B.Contains(o));
            foreach (var attribute in matchingAttribute)
            {
                var refValue = s_TestValues[attribute];
                var readValue = event_A.GetVector3(attribute);

                Assert.AreEqual(refValue.x, readValue.x);
                Assert.AreEqual(refValue.y, readValue.y);
                Assert.AreEqual(refValue.z, readValue.z);
            }
            Assert.AreEqual(spawnCountRef, event_A.GetFloat("spawnCount"));

            yield return null;

            GameObject.DestroyImmediate(gameObj_A);
            GameObject.DestroyImmediate(gameObj_B);
        }

        [UnityTest, Description("Regression test UUM-51509")]
        public IEnumerator Create_SpawnerCallback_Instanced()
        {
            const string customAttributeName = "gameObjectPosition";
            VFXGraph graph = VFXTestCommon.MakeTemporaryGraph();

            var eventStart = ScriptableObject.CreateInstance<VFXBasicEvent>();
            eventStart.eventName = "OnPlay";

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockConstantRate = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();
            var spawnerInit = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var spawnerOutput = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();

            var slotCount = blockConstantRate.GetInputSlot(0);
            slotCount.value = 10.0f;

            graph.TryAddCustomAttribute(customAttributeName, VFXValueType.Float3, "", false, out _);

            var customSpawnerBlock = ScriptableObject.CreateInstance<VFXSpawnerCustomWrapper>();
            customSpawnerBlock.SetSettingValue("m_customType", new SerializableType(typeof(VFXCustomSpawnerGameObjectPosition)));
            spawnerContext.AddChild(blockConstantRate);
            spawnerContext.AddChild(customSpawnerBlock);

            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.modelType == typeof(Block.SetAttribute));
            var blockAttribute = blockAttributeDesc.CreateInstance();
            blockAttribute.SetSettingValue("attribute", customAttributeName);
            blockAttribute.SetSettingValue("Source", SetAttribute.ValueSource.Source);
            spawnerInit.AddChild(blockAttribute);

            graph.AddChild(eventStart);
            graph.AddChild(spawnerContext);
            graph.AddChild(spawnerInit);
            graph.AddChild(spawnerOutput);

            spawnerContext.LinkFrom(eventStart, 0, 0);
            spawnerInit.LinkFrom(spawnerContext);
            spawnerOutput.LinkFrom(spawnerInit);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));


            GameObject gameObjectAtZero = new GameObject("GameObjectAtZero");
            gameObjectAtZero.transform.position = Vector3.zero;
            VisualEffect vfxComponent0 = gameObjectAtZero.AddComponent<VisualEffect>();
            vfxComponent0.visualEffectAsset = graph.visualEffectResource.asset;

            GameObject gameObjectAtOne = new GameObject("GameObjectAtOne");
            gameObjectAtOne.transform.position = Vector3.one;
            VisualEffect vfxComponent1 = gameObjectAtOne.AddComponent<VisualEffect>();
            vfxComponent1.visualEffectAsset = graph.visualEffectResource.asset;

            int maxFrame = 512;
            while (vfxComponent0.culled && vfxComponent1.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            yield return null;

            var state0 = VFXTestCommon.GetSpawnerState(vfxComponent0, 0);
            var state1 = VFXTestCommon.GetSpawnerState(vfxComponent1, 0);

            Vector3 readAttribute0 = state0.vfxEventAttribute.GetVector3(customAttributeName);
            Vector3 readAttribute1 = state1.vfxEventAttribute.GetVector3(customAttributeName);

            Assert.AreEqual( Vector3.zero, readAttribute0);
            Assert.AreEqual( Vector3.one, readAttribute1);
        }
    }
}
#endif
