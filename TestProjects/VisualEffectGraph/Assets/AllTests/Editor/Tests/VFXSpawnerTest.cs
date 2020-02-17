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
using UnityEditor.VFX.Block;
using System.Text;
using System.Globalization;

namespace UnityEditor.VFX.Test
{
    public class VFXSpawnerTest
    {
        int m_previousCaptureFrameRate;
        float m_previousFixedTimeStep;
        float m_previousMaxDeltaTime;

        [OneTimeSetUp]
        public void Init()
        {
            m_previousCaptureFrameRate = Time.captureFramerate;
            m_previousFixedTimeStep = UnityEngine.VFX.VFXManager.fixedTimeStep;
            m_previousMaxDeltaTime = UnityEngine.VFX.VFXManager.maxDeltaTime;
            Time.captureFramerate = 10;
            UnityEngine.VFX.VFXManager.fixedTimeStep = 0.1f;
            UnityEngine.VFX.VFXManager.maxDeltaTime = 0.1f;
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            Time.captureFramerate = m_previousCaptureFrameRate;
            UnityEngine.VFX.VFXManager.fixedTimeStep = m_previousFixedTimeStep;
            UnityEngine.VFX.VFXManager.maxDeltaTime = m_previousMaxDeltaTime;
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        private void CreateAssetAndComponent(float spawnCountValue, string playEventName, out VFXGraph graph, out VisualEffect vfxComponent, out GameObject gameObj, out GameObject cameraObj)
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");

            graph = VFXTestCommon.MakeTemporaryGraph();

            var eventStart = ScriptableObject.CreateInstance<VFXBasicEvent>();
            eventStart.eventName = playEventName;

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockConstantRate = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();
            var slotCount = blockConstantRate.GetInputSlot(0);

            var spawnerInit = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var spawnerOutput = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();

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

        [UnityTest]
        public IEnumerator Create_Asset_And_Component_Spawner_Check_Initial_Event()
        {
            var propertyInitialEventName = typeof(VisualEffect).GetProperty("initialEventName");
            if (propertyInitialEventName != null)
            {
                var setPropertyInitialEventName = propertyInitialEventName.GetSetMethod();
                var spawnCountValue = 666.0f;
                VisualEffect vfxComponent;
                GameObject cameraObj, gameObj;
                VFXGraph graph;

                var initialEventName = "CustomInitialEvent";
                CreateAssetAndComponent(spawnCountValue, initialEventName, out graph, out vfxComponent, out gameObj, out cameraObj);

                int maxFrame = 512;
                while (vfxComponent.culled && --maxFrame > 0)
                {
                    yield return null;
                }
                Assert.IsTrue(maxFrame > 0);
                yield return null; //wait for exactly one more update if visible

                //Default event state is supposed to be "OnPlay"
                var spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);
                Assert.AreEqual(0.0, spawnerState.spawnCount);

                var editor = Editor.CreateEditor(graph.GetResource().asset);
                editor.serializedObject.Update();
                var initialEventProperty = editor.serializedObject.FindProperty("m_Infos.m_InitialEventName");
                initialEventProperty.stringValue = initialEventName;
                editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                GameObject.DestroyImmediate(editor);
                yield return null;
                spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);
                var spawnCountRead = spawnerState.spawnCount / spawnerState.deltaTime;
                Assert.LessOrEqual(Mathf.Abs(spawnCountRead - spawnCountValue), 0.01f);

                //Now, do it on vfxComponent (override automatically taken into account)
                setPropertyInitialEventName.Invoke(vfxComponent, new object[] { "OnPlay" });
                vfxComponent.Reinit(); //Automatic while changing it through serialized property, here, it's a runtime behavior
                yield return null;
                spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);
                Assert.AreEqual(0.0, spawnerState.spawnCount);

                //Try setting the correct value
                setPropertyInitialEventName.Invoke(vfxComponent, new object[] { initialEventName });
                vfxComponent.Reinit();
                yield return null;
                spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);
                spawnCountRead = spawnerState.spawnCount / spawnerState.deltaTime;
                Assert.LessOrEqual(Mathf.Abs(spawnCountRead - spawnCountValue), 0.01f);

                UnityEngine.Object.DestroyImmediate(gameObj);
                UnityEngine.Object.DestroyImmediate(cameraObj);
            }
            //else initial event feature isn't available yet
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

            var spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);

            var spawnCountRead = spawnerState.spawnCount / spawnerState.deltaTime;
            Assert.LessOrEqual(Mathf.Abs(spawnCountRead - spawnCountValue), 0.01f);
            UnityEngine.Object.DestroyImmediate(gameObj);
            UnityEngine.Object.DestroyImmediate(cameraObj);
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

            var spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);
            Assert.AreEqual(VFXSpawnerLoopState.Finished, spawnerState.loopState);

            //Now send event Stop, we expect to wake up
            vfxComponent.Stop();
            maxFrame = 512;
            while (--maxFrame > 0)
            {
                yield return null;
                spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);
                if (spawnerState.loopState != VFXSpawnerLoopState.Finished)
                    break;
            }
            Assert.AreNotEqual(VFXSpawnerLoopState.Finished, spawnerState.loopState);
            Assert.IsTrue(maxFrame > 0);

            UnityEngine.Object.DestroyImmediate(gameObj);
            UnityEngine.Object.DestroyImmediate(cameraObj);
        }

        [UnityTest]
        public IEnumerator CreateEventStartAndStop()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
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
        public IEnumerator Create_CustomSpawner_And_Component()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
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
            var spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);
            vfxComponent.Simulate(0.5f);
            while ((spawnerState.playing == false || spawnerState.deltaTime == 0.0f) && --maxFrame > 0)
            {
                yield return null;
                spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);
            }
            Assert.IsTrue(maxFrame > 0);

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
        public IEnumerator CreateSpawner_Set_Attribute_With_ContextDelay()
        {
            //This test cover an issue : 1205329
            EditorApplication.ExecuteMenuItem("Window/General/Game");

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
            graph.RecompileIfNeeded();

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

            var spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);
            //Catching sleeping state
            maxFrame = 512;
            while (--maxFrame > 0 && spawnerState.totalTime < delayValue / 10.0f)
            {
                spawnerState = VisualEffectUtility.GetSpawnerState(vfxComponent, 0);
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

            UnityEngine.Object.DestroyImmediate(gameObj);
            UnityEngine.Object.DestroyImmediate(cameraObj);
        }

        [UnityTest]
        public IEnumerator CreateSpawner_Single_Burst_With_Delay()
        {
            //This test cover a regression : 1154292
            EditorApplication.ExecuteMenuItem("Window/General/Game");

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

        string expectedLogFolder = "Assets/AllTests/Editor/Tests/VFXSpawnerTest_";
        bool CompareWithExpectedLog(StringBuilder actualContent, string identifier)
        {
            var pathExpected = expectedLogFolder + identifier + ".expected.txt";
            var pathActual = expectedLogFolder + identifier + ".actual.txt";
            bool success = true;

            IEnumerable<string> expectedContent = Enumerable.Empty<string>();
            try
            {
                expectedContent = System.IO.File.ReadLines(pathExpected);
            }
            catch(System.Exception)
            {
                success = false;
                Debug.LogErrorFormat("Can't locate file : {0}", pathExpected);
            }

            //Compare line by line to avoid carriage return differences
            var reader = new System.IO.StringReader(actualContent.ToString());
            foreach (var expectedContentLine in expectedContent)
            {
                var line = reader.ReadLine();
                if (line == null || string.Compare(line, expectedContentLine, StringComparison.InvariantCulture) != 0)
                {
                    success = false;
                    Debug.LogError("Expected Line : " + expectedContentLine);
                    Debug.LogError("Actual Line   : " + line);
                    break;
                }
            }

            if (!success)
            {
                System.IO.File.WriteAllText(pathActual, actualContent.ToString());
            }
            return success;
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
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            Assert.AreEqual(UnityEngine.VFX.VFXManager.fixedTimeStep, 0.1f);

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
            var state_A = VisualEffectUtility.GetSpawnerState(vfxComponent, 0u);
            var state_B = VisualEffectUtility.GetSpawnerState(vfxComponent, 1u);
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
                    state_A = VisualEffectUtility.GetSpawnerState(vfxComponent, 0u);
                    state_B = VisualEffectUtility.GetSpawnerState(vfxComponent, 1u);
                    aliveParticleCount = vfxComponent.aliveParticleCount;
                    yield return null;
                }
            }

            var compare = CompareWithExpectedLog(log, "Chaining");
            Assert.IsTrue(compare);
            yield return null;
            UnityEngine.Object.DestroyImmediate(gameObj);
            UnityEngine.Object.DestroyImmediate(cameraObj);
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
            new CreateSpawner_ChangeLoopMode_TestCase() {   LoopDuration    = VFXBasicSpawner.LoopMode.Infinite,
                                                            LoopCount       = VFXBasicSpawner.LoopMode.Infinite,
                                                            DelayBeforeLoop = VFXBasicSpawner.DelayMode.None,
                                                            DelayAfterLoop  = VFXBasicSpawner.DelayMode.None },
            //Simply random loop
            new CreateSpawner_ChangeLoopMode_TestCase() {   LoopDuration    = VFXBasicSpawner.LoopMode.Random,
                                                            LoopCount       = VFXBasicSpawner.LoopMode.Infinite,
                                                            DelayBeforeLoop = VFXBasicSpawner.DelayMode.None,
                                                            DelayAfterLoop  = VFXBasicSpawner.DelayMode.None },

            //Random loop, adding random before delay
            new CreateSpawner_ChangeLoopMode_TestCase() {   LoopDuration    = VFXBasicSpawner.LoopMode.Random,
                                                            LoopCount       = VFXBasicSpawner.LoopMode.Infinite,
                                                            DelayBeforeLoop = VFXBasicSpawner.DelayMode.Random,
                                                            DelayAfterLoop  = VFXBasicSpawner.DelayMode.None },

            //Random loop count, constant loop duration
            new CreateSpawner_ChangeLoopMode_TestCase() {   LoopDuration    = VFXBasicSpawner.LoopMode.Constant,
                                                            LoopCount       = VFXBasicSpawner.LoopMode.Random,
                                                            DelayBeforeLoop = VFXBasicSpawner.DelayMode.None,
                                                            DelayAfterLoop  = VFXBasicSpawner.DelayMode.None },

            //Everything random
            new CreateSpawner_ChangeLoopMode_TestCase() {   LoopDuration    = VFXBasicSpawner.LoopMode.Random,
                                                            LoopCount       = VFXBasicSpawner.LoopMode.Random,
                                                            DelayBeforeLoop = VFXBasicSpawner.DelayMode.Random,
                                                            DelayAfterLoop  = VFXBasicSpawner.DelayMode.Random },

        };

        [UnityTest]
        public IEnumerator CreateSpawner_ChangeLoopMode([ValueSource("k_CreateSpawner_ChangeLoopModeTestCases")] CreateSpawner_ChangeLoopMode_TestCase testCase)
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            Assert.AreEqual(UnityEngine.VFX.VFXManager.fixedTimeStep, 0.1f);

            var graph = VFXTestCommon.MakeTemporaryGraph();

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
            var state = VisualEffectUtility.GetSpawnerState(vfxComponent, 0u);
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
                    state = VisualEffectUtility.GetSpawnerState(vfxComponent, 0u);
                    aliveParticleCount = vfxComponent.aliveParticleCount;
                    yield return null;
                }
            }

            var compare = CompareWithExpectedLog(log, testCase.ToString());
            Assert.IsTrue(compare);

            yield return null;
            UnityEngine.Object.DestroyImmediate(gameObj);
            UnityEngine.Object.DestroyImmediate(cameraObj);
        }


        [UnityTest]
        public IEnumerator CreateSpawner_With_All_Zero_Duration() //Cover possible infinite loop
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            Assert.AreEqual(UnityEngine.VFX.VFXManager.fixedTimeStep, 0.1f);

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

            while (VisualEffectUtility.GetSpawnerState(vfxComponent, 0u).loopIndex < 3 /* arbitrary loop count */)
            {
                yield return null;
            }

            UnityEngine.Object.DestroyImmediate(gameObj);
            UnityEngine.Object.DestroyImmediate(cameraObj);
        }
    }
}
#endif
