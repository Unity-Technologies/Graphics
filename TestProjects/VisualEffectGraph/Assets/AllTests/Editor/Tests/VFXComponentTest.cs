#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using System.Linq;
using System.Collections;
using UnityEditor.VFX.Block.Test;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    public class VisualEffectTest : ScriptableObject
    {
        [SerializeField] bool m_Init = false;

        [SerializeField] GameObject m_cubeEmpty;
        [SerializeField] GameObject m_sphereEmpty;
        [SerializeField] GameObject m_mainObject;
        [SerializeField] GameObject m_mainCamera;

        [SerializeField] string m_pathTexture2D_A;
        [SerializeField] string m_pathTexture2D_B;
        [SerializeField] Texture2D m_texture2D_A;
        [SerializeField] Texture2D m_texture2D_B;
        [SerializeField] string m_pathTexture2DArray_A;
        [SerializeField] string m_pathTexture2DArray_B;
        [SerializeField] Texture2DArray m_texture2DArray_A;
        [SerializeField] Texture2DArray m_texture2DArray_B;
        [SerializeField] string m_pathTexture3D_A;
        [SerializeField] string m_pathTexture3D_B;
        [SerializeField] Texture3D m_texture3D_A;
        [SerializeField] Texture3D m_texture3D_B;
        [SerializeField] string m_pathTextureCube_A;
        [SerializeField] string m_pathTextureCube_B;
        [SerializeField] Cubemap m_textureCube_A;
        [SerializeField] Cubemap m_textureCube_B;
        [SerializeField] string m_pathTextureCubeArray_A;
        [SerializeField] string m_pathTextureCubeArray_B;
        [SerializeField] CubemapArray m_textureCubeArray_A;
        [SerializeField] CubemapArray m_textureCubeArray_B;

        [OneTimeSetUp]
        public void Init()
        {
            if (m_Init)
                return;
            m_Init = true;

            System.IO.Directory.CreateDirectory("Assets/Temp");
            var cubeEmptyName = "VFX_Test_Cube_Empty_Name";
            var sphereEmptyName = "VFX_Test_Sphere_Empty_Name";

            m_cubeEmpty = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_cubeEmpty.name = cubeEmptyName;
            m_sphereEmpty = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_sphereEmpty.name = sphereEmptyName;

            m_pathTexture2D_A = "Assets/texture2D_A.asset";
            m_pathTexture2D_B = "Assets/texture2D_B.asset";
            m_texture2D_A = new Texture2D(16, 16);
            m_texture2D_B = new Texture2D(32, 32);
            AssetDatabase.CreateAsset(m_texture2D_A, m_pathTexture2D_A);
            AssetDatabase.CreateAsset(m_texture2D_B, m_pathTexture2D_B);
            m_texture2D_A = AssetDatabase.LoadAssetAtPath<Texture2D>(m_pathTexture2D_A);
            m_texture2D_B = AssetDatabase.LoadAssetAtPath<Texture2D>(m_pathTexture2D_B);

            m_pathTexture2DArray_A = "Assets/texture2DArray_A.asset";
            m_pathTexture2DArray_B = "Assets/texture2DArray_B.asset";
            m_texture2DArray_A = new Texture2DArray(16, 16, 4, TextureFormat.ARGB32, false);
            m_texture2DArray_B = new Texture2DArray(32, 32, 4, TextureFormat.ARGB32, false);
            AssetDatabase.CreateAsset(m_texture2DArray_A, m_pathTexture2DArray_A);
            AssetDatabase.CreateAsset(m_texture2DArray_B, m_pathTexture2DArray_B);
            m_texture2DArray_A = AssetDatabase.LoadAssetAtPath<Texture2DArray>(m_pathTexture2DArray_A);
            m_texture2DArray_B = AssetDatabase.LoadAssetAtPath<Texture2DArray>(m_pathTexture2DArray_B);

            m_pathTexture3D_A = "Assets/texture3D_A.asset";
            m_pathTexture3D_B = "Assets/texture3D_B.asset";
            m_texture3D_A = new Texture3D(16, 16, 16, TextureFormat.ARGB32, false);
            m_texture3D_B = new Texture3D(8, 8, 8, TextureFormat.ARGB32, false);
            AssetDatabase.CreateAsset(m_texture3D_A, m_pathTexture3D_A);
            AssetDatabase.CreateAsset(m_texture3D_B, m_pathTexture3D_B);
            m_texture3D_A = AssetDatabase.LoadAssetAtPath<Texture3D>(m_pathTexture3D_A);
            m_texture3D_B = AssetDatabase.LoadAssetAtPath<Texture3D>(m_pathTexture3D_B);

            m_pathTextureCube_A = "Assets/textureCube_A.asset";
            m_pathTextureCube_B = "Assets/textureCube_B.asset";
            m_textureCube_A = new Cubemap(16, TextureFormat.ARGB32, false);
            m_textureCube_B = new Cubemap(32, TextureFormat.ARGB32, false);
            AssetDatabase.CreateAsset(m_textureCube_A, m_pathTextureCube_A);
            AssetDatabase.CreateAsset(m_textureCube_B, m_pathTextureCube_B);
            m_textureCube_A = AssetDatabase.LoadAssetAtPath<Cubemap>(m_pathTextureCube_A);
            m_textureCube_B = AssetDatabase.LoadAssetAtPath<Cubemap>(m_pathTextureCube_B);

            m_pathTextureCubeArray_A = "Assets/textureCubeArray_A.asset";
            m_pathTextureCubeArray_B = "Assets/textureCubeArray_B.asset";
            m_textureCubeArray_A = new CubemapArray(16, 4, TextureFormat.ARGB32, false);
            m_textureCubeArray_B = new CubemapArray(32, 4, TextureFormat.ARGB32, false);
            AssetDatabase.CreateAsset(m_textureCubeArray_A, m_pathTextureCubeArray_A);
            AssetDatabase.CreateAsset(m_textureCubeArray_B, m_pathTextureCubeArray_B);
            m_textureCubeArray_A = AssetDatabase.LoadAssetAtPath<CubemapArray>(m_pathTextureCubeArray_A);
            m_textureCubeArray_B = AssetDatabase.LoadAssetAtPath<CubemapArray>(m_pathTextureCubeArray_B);

            var mainObjectName = "VFX_Test_Main_Object";
            m_mainObject = new GameObject(mainObjectName);

            var mainCameraName = "VFX_Test_Main_Camera";
            m_mainCamera = new GameObject(mainCameraName);
            var camera = this.m_mainCamera.AddComponent<Camera>();
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(this.m_mainCamera.transform);

            Time.captureFramerate = 10;
            UnityEngine.VFX.VFXManager.fixedTimeStep = 0.1f;
            UnityEngine.VFX.VFXManager.maxDeltaTime = 0.1f;
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            m_Init = false;
            Debug.unityLogger.logEnabled = true;
            Time.captureFramerate = 0;
            UnityEngine.VFX.VFXManager.fixedTimeStep = 1.0f / 60.0f;
            UnityEngine.VFX.VFXManager.maxDeltaTime = 1.0f / 20.0f;

            UnityEngine.Object.DestroyImmediate(m_mainObject);
            UnityEngine.Object.DestroyImmediate(m_cubeEmpty);
            UnityEngine.Object.DestroyImmediate(m_sphereEmpty);
            UnityEngine.Object.DestroyImmediate(m_mainCamera);
            AssetDatabase.DeleteAsset(m_pathTexture2D_A);
            AssetDatabase.DeleteAsset(m_pathTexture2D_B);
            AssetDatabase.DeleteAsset(m_pathTexture2DArray_A);
            AssetDatabase.DeleteAsset(m_pathTexture2DArray_B);
            AssetDatabase.DeleteAsset(m_pathTexture3D_A);
            AssetDatabase.DeleteAsset(m_pathTexture3D_B);
            AssetDatabase.DeleteAsset(m_pathTextureCube_A);
            AssetDatabase.DeleteAsset(m_pathTextureCube_B);
            AssetDatabase.DeleteAsset(m_pathTextureCubeArray_A);
            AssetDatabase.DeleteAsset(m_pathTextureCubeArray_B);
            VFXTestCommon.DeleteAllTemporaryGraph();
        }


        VFXGraph CreateGraph_And_System()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();

            var output = ScriptableObject.CreateInstance<VFXPointOutput>();
            output.SetSettingValue("castShadows", true);
            graph.AddChild(output);

            var contextInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();

            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.modelType == typeof(Block.SetAttribute));
            var blockAttribute = blockAttributeDesc.CreateInstance();
            blockAttribute.SetSettingValue("attribute", "position");
            contextInitialize.AddChild(blockAttribute);

            contextInitialize.LinkTo(output);
            graph.AddChild(contextInitialize);

            var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            spawner.LinkTo(contextInitialize);
            graph.AddChild(spawner);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph)); ;

            return graph;
        }

        [UnityTest]
        public IEnumerator CreateComponent_And_Graph_Restart_Component_Expected()
        {
            yield return new EnterPlayMode();

            var graph = CreateGraph_And_System();

            while (m_mainObject.GetComponent<VisualEffect>() != null)
                UnityEngine.Object.DestroyImmediate(m_mainObject.GetComponent<VisualEffect>());
            var vfxComponent = m_mainObject.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            //Assert.DoesNotThrow(() => VisualEffectUtility.GetSpawnerState(vfxComponent, 0)); //N.B. : This cannot be tested after EnterPlayMode due to the closure
            int maxFrame = 512;
            while (VisualEffectUtility.GetSpawnerState(vfxComponent, 0).totalTime < 1.0f && maxFrame-->0)
                yield return null;

            Assert.GreaterOrEqual(VisualEffectUtility.GetSpawnerState(vfxComponent, 0).totalTime, 1.0f);

            vfxComponent.enabled = false;
            vfxComponent.enabled = true;
            yield return null;

            maxFrame = 64;
            while (VisualEffectUtility.GetSpawnerState(vfxComponent, 0).totalTime > 1.0f && maxFrame-- > 0)
                yield return null;

            Assert.Less(VisualEffectUtility.GetSpawnerState(vfxComponent, 0).totalTime, 1.0f);

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator CreateComponent_And_Graph_Modify_It_To_Generate_Expected_Exception()
        {
            var graph = CreateGraph_And_System();
            yield return null;
        
            while (m_mainObject.GetComponent<VisualEffect>() != null)
                UnityEngine.Object.DestroyImmediate(m_mainObject.GetComponent<VisualEffect>());
            var vfxComponent = m_mainObject.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;
            Assert.DoesNotThrow(() => VisualEffectUtility.GetSpawnerState(vfxComponent, 0));
            yield return null;
        
            //Plug a GPU instruction on bounds, excepting an exception while recompiling
            var getPositionDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.modelType == typeof(VFXAttributeParameter) && o.name.Contains(VFXAttribute.Position.name));
            var getPosition = getPositionDesc.CreateInstance();
            graph.AddChild(getPosition);
            var initializeContext = graph.children.OfType<VFXBasicInitialize>().FirstOrDefault();
            Assert.AreEqual(VFXValueType.Float3, initializeContext.inputSlots[0][0].valueType);
        
            getPosition.outputSlots[0].Link(initializeContext.inputSlots[0][0]);

            //LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Exception while compiling expression graph:*")); < Incorrect with our katana configuration
            Debug.unityLogger.logEnabled = false;
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
            Debug.unityLogger.logEnabled = true;
            Assert.Throws(typeof(IndexOutOfRangeException), () => VisualEffectUtility.GetSpawnerState(vfxComponent, 0)); //This is the exception which matters for this test
        }

        [UnityTest]
        public IEnumerator CreateComponent_And_VerifyRendererState()
        {
            yield return new EnterPlayMode();
            var graph = CreateGraph_And_System();

            //< Same Behavior as Drag & Drop
            GameObject currentObject = new GameObject("TemporaryGameObject_RenderState", /*typeof(Transform),*/ typeof(VisualEffect));
            var vfx = currentObject.GetComponent<VisualEffect>();
            var asset = graph.visualEffectResource.asset;
            Assert.IsNotNull(asset);

            vfx.visualEffectAsset = asset;

            int maxFrame = 512;
            while (vfx.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            yield return null;

            Assert.IsNotNull(currentObject.GetComponent<VFXRenderer>());
            var actualShadowCastingMode = currentObject.GetComponent<VFXRenderer>().shadowCastingMode;
            Assert.AreEqual(actualShadowCastingMode, ShadowCastingMode.On);

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator CreateComponent_And_VerifyRenderBounds()
        {
            yield return new EnterPlayMode();
            var graph = CreateGraph_And_System();
            var initializeContext = graph.children.OfType<VFXBasicInitialize>().FirstOrDefault();

            var center = new Vector3(1.0f, 2.0f, 3.0f);
            var size = new Vector3(111.0f, 222.0f, 333.0f);

            initializeContext.inputSlots[0][0].value = center;
            initializeContext.inputSlots[0][1].value = size;
            graph.SetExpressionGraphDirty();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            //< Same Behavior as Drag & Drop
            GameObject currentObject = new GameObject("TemporaryGameObject_RenderBounds", /*typeof(Transform),*/ typeof(VisualEffect));
            var vfx = currentObject.GetComponent<VisualEffect>();
            var asset = graph.visualEffectResource.asset;
            Assert.IsNotNull(asset);

            vfx.visualEffectAsset = asset;

            int maxFrame = 512;
            while ((    vfx.culled
                    ||  currentObject.GetComponent<VFXRenderer>().bounds.extents.x == 0.0f)
                    &&  --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            yield return null;

            var vfxRenderer = currentObject.GetComponent<VFXRenderer>();
            var bounds = vfxRenderer.bounds;

            Assert.AreEqual(center.x, bounds.center.x, 10e-5);
            Assert.AreEqual(center.y, bounds.center.y, 10e-5);
            Assert.AreEqual(center.z, bounds.center.z, 10e-5);
            Assert.AreEqual(size.x / 2.0f, bounds.extents.x, 10e-5);
            Assert.AreEqual(size.y / 2.0f, bounds.extents.y, 10e-5);
            Assert.AreEqual(size.z / 2.0f, bounds.extents.z, 10e-5);

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator CreateComponent_And_Check_NoneTexture_Constraint_Doesnt_Generate_Any_Error()
        {
            yield return new EnterPlayMode();
            var graph = CreateGraph_And_System();

            var burst = ScriptableObject.CreateInstance<VFXSpawnerBurst>();
            burst.inputSlots.First(o => o.name.ToLowerInvariant().Contains("count")).value = 147.0f;
            graph.children.OfType<VFXBasicSpawner>().First().AddChild(burst);

            var operatorSample3D = ScriptableObject.CreateInstance<Operator.SampleTexture3D>();
            operatorSample3D.inputSlots.First(o => o.valueType == VFXValueType.Texture3D).value = null;
            graph.AddChild(operatorSample3D);

            var initialize = graph.children.First(o => o is VFXBasicInitialize);
            bool r = operatorSample3D.outputSlots.First().Link(initialize.children.OfType<VFXBlock>().First().inputSlots.First());
            Assert.IsTrue(r);
            graph.SetExpressionGraphDirty();
            graph.RecompileIfNeeded();

            GameObject currentObject = new GameObject("TemporaryGameObject_NoneTexture", typeof(VisualEffect));
            var vfx = currentObject.GetComponent<VisualEffect>();
            var asset = graph.visualEffectResource.asset;
            vfx.visualEffectAsset = asset;

            int maxFrame = 512;
            while ((vfx.culled || vfx.aliveParticleCount == 0) && --maxFrame > 0)
                yield return null;

            //Wait for a few frame to be sure the rendering has been triggered
            for (int i = 0; i < 3; ++i)
                yield return null;

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator CreateComponent_And_CheckDimension_Constraint()
        {
            yield return new EnterPlayMode();
            var graph = VFXTestCommon.MakeTemporaryGraph();

            var contextInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var allType = ScriptableObject.CreateInstance<AllType>();

            contextInitialize.AddChild(allType);
            graph.AddChild(contextInitialize);

            // Needs a spawner and output for the system to be valid (TODOPAUL : Should not be needed here)
            {
                var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
                spawner.LinkTo(contextInitialize);
                graph.AddChild(spawner);

                var output = ScriptableObject.CreateInstance<VFXPointOutput>();
                output.LinkFrom(contextInitialize);
                graph.AddChild(output);
            }

            var parameter = VFXLibrary.GetParameters().First(o => o.model.type == typeof(Texture2D)).CreateInstance();
            var type = VFXValueType.Texture2D;

            var targetTextureName = "exposed_test_tex2D";

            if (type != VFXValueType.None)
            {
                parameter.SetSettingValue("m_ExposedName", targetTextureName);
                parameter.SetSettingValue("m_Exposed", true);
                graph.AddChild(parameter);
            }

            for (int i = 0; i < allType.GetNbInputSlots(); ++i)
            {
                var currentSlot = allType.GetInputSlot(i);
                var expression = currentSlot.GetExpression();
                if (expression != null && expression.valueType == type)
                {
                    currentSlot.Link(parameter.GetOutputSlot(0));
                    break;
                }
            }

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            while (m_mainObject.GetComponent<VisualEffect>() != null)
            {
                UnityEngine.Object.DestroyImmediate(m_mainObject.GetComponent<VisualEffect>());
            }
            var vfxComponent = m_mainObject.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            yield return null;

            Assert.IsTrue(vfxComponent.HasTexture(targetTextureName));
            Assert.AreEqual(TextureDimension.Tex2D, vfxComponent.GetTextureDimension(targetTextureName));

            var renderTartget3D = new RenderTexture(4, 4, 4, RenderTextureFormat.ARGB32);
            renderTartget3D.dimension = TextureDimension.Tex3D;

            vfxComponent.SetTexture(targetTextureName, renderTartget3D);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("3D"));
            Assert.AreNotEqual(renderTartget3D, vfxComponent.GetTexture(targetTextureName));

            var renderTartget2D = new RenderTexture(4, 4, 4, RenderTextureFormat.ARGB32);
            renderTartget2D.dimension = TextureDimension.Tex2D;
            vfxComponent.SetTexture(targetTextureName, renderTartget2D);
            Assert.AreEqual(renderTartget2D, vfxComponent.GetTexture(targetTextureName));
            yield return null;

            /*
             * Actually, this error is only caught in debug mode, ignored in release for performance reason
            renderTartget2D.dimension = TextureDimension.Tex3D; //try to hack dimension
            Assert.AreEqual(renderTartget2D, vfxComponent.GetTexture(targetTextureName));
            yield return null;
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("3D"));
            */
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator CreateComponent_Switch_Asset_Keep_Override()
        {
            yield return new EnterPlayMode();

            var graph_A = VFXTestCommon.MakeTemporaryGraph();
            var graph_B = VFXTestCommon.MakeTemporaryGraph();
            var parametersVector3Desc = VFXLibrary.GetParameters().Where(o => o.model.type == typeof(Vector3)).First();

            var commonExposedName = "vorfji";
            var parameter_A = parametersVector3Desc.CreateInstance();
            parameter_A.SetSettingValue("m_ExposedName", commonExposedName);
            parameter_A.SetSettingValue("m_Exposed", true);
            parameter_A.value = new Vector3(0, 0, 0);
            graph_A.AddChild(parameter_A);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph_A));

            var parameter_B = parametersVector3Desc.CreateInstance();
            parameter_B.SetSettingValue("m_ExposedName", commonExposedName);
            parameter_B.SetSettingValue("m_Exposed", true);
            parameter_B.value = new Vector3(0, 0, 0);
            graph_B.AddChild(parameter_B);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph_B));

            while (m_mainObject.GetComponent<VisualEffect>() != null)
                UnityEngine.Object.DestroyImmediate(m_mainObject.GetComponent<VisualEffect>());
            var vfx = m_mainObject.AddComponent<VisualEffect>();
            vfx.visualEffectAsset = graph_A.visualEffectResource.asset;
            Assert.IsTrue(vfx.HasVector3(commonExposedName));
            var expectedOverriden = new Vector3(1, 2, 3);
            vfx.SetVector3(commonExposedName, expectedOverriden);
            yield return null;

            var actualOverriden = vfx.GetVector3(commonExposedName);
            Assert.AreEqual(actualOverriden.x, expectedOverriden.x); Assert.AreEqual(actualOverriden.y, expectedOverriden.y); Assert.AreEqual(actualOverriden.z, expectedOverriden.z);

            vfx.visualEffectAsset = graph_B.visualEffectResource.asset;
            yield return null;

            actualOverriden = vfx.GetVector3(commonExposedName);
            Assert.AreEqual(actualOverriden.x, expectedOverriden.x); Assert.AreEqual(actualOverriden.y, expectedOverriden.y); Assert.AreEqual(actualOverriden.z, expectedOverriden.z);

            yield return new ExitPlayMode();
        }

#pragma warning disable 0414
        private static bool[] trueOrFalse = { true, false };
#pragma warning restore 0414

        [UnityTest]
        public IEnumerator CreateComponent_Modify_Value_Doesnt_Reset([ValueSource("trueOrFalse")] bool modifyValue, [ValueSource("trueOrFalse")] bool modifyAssetValue)
        {
            yield return new EnterPlayMode();

            var graph = VFXTestCommon.MakeTemporaryGraph();
            var parametersVector2Desc = VFXLibrary.GetParameters().Where(o => o.model.type == typeof(Vector2)).First();

            Vector2 expectedValue = new Vector2(1.0f, 2.0f);

            var exposedName = "bvcxw";
            var parameter = parametersVector2Desc.CreateInstance();
            parameter.SetSettingValue("m_ExposedName", exposedName);
            parameter.SetSettingValue("m_Exposed", true);
            parameter.value = expectedValue;
            graph.AddChild(parameter);

            var contextInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            graph.AddChild(contextInitialize);

            var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var constantRate = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();
            spawner.AddChild(constantRate);

            graph.AddChild(spawner);
            spawner.LinkTo(contextInitialize);

            var output = ScriptableObject.CreateInstance<VFXPointOutput>();
            graph.AddChild(output);
            output.LinkFrom(contextInitialize);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            while (m_mainObject.GetComponent<VisualEffect>() != null)
                UnityEngine.Object.DestroyImmediate(m_mainObject.GetComponent<VisualEffect>());
            var vfx = m_mainObject.AddComponent<VisualEffect>();
            vfx.visualEffectAsset = graph.visualEffectResource.asset;
            Assert.IsTrue(vfx.HasVector2(exposedName));
            if (modifyValue)
            {
                expectedValue = new Vector2(3.0f, 4.0f);
                vfx.SetVector2(exposedName, expectedValue);
            }
            Assert.AreEqual(expectedValue.x, vfx.GetVector2(exposedName).x); Assert.AreEqual(expectedValue.y, vfx.GetVector2(exposedName).y);

            float spawnerLimit = 1.8f; //Arbitrary enough large time
            int maxFrameCount = 1024;
            while (maxFrameCount-- > 0)
            {
                var spawnerState = VisualEffectUtility.GetSpawnerState(vfx, 0u);
                if (spawnerState.totalTime > spawnerLimit)
                    break;
                yield return null;
            }
            Assert.IsTrue(maxFrameCount > 0);

            if (modifyAssetValue)
            {
                expectedValue = new Vector2(5.0f, 6.0f);
                parameter.value = expectedValue;
                graph.RecompileIfNeeded();
            }

            if (modifyValue)
            {
                var editor = Editor.CreateEditor(vfx);
                editor.serializedObject.Update();

                var propertySheet = editor.serializedObject.FindProperty("m_PropertySheet");
                var fieldName = VisualEffectSerializationUtility.GetTypeField(VFXExpression.TypeToType(VFXValueType.Float2)) + ".m_Array";
                var vfxField = propertySheet.FindPropertyRelative(fieldName);

                Assert.AreEqual(1, vfxField.arraySize);

                var property = vfxField.GetArrayElementAtIndex(0);
                property = property.FindPropertyRelative("m_Value");
                expectedValue = new Vector2(7.0f, 8.0f);
                property.vector2Value = expectedValue;
                editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();

                GameObject.DestroyImmediate(editor);
            }
            yield return null;

            var spawnerStateFinal = VisualEffectUtility.GetSpawnerState(vfx, 0u);
            Assert.IsTrue(spawnerStateFinal.totalTime > spawnerLimit); //Check there isn't any reset time
            Assert.IsTrue(vfx.HasVector2(exposedName));
            Assert.AreEqual(expectedValue.x, vfx.GetVector2(exposedName).x); Assert.AreEqual(expectedValue.y, vfx.GetVector2(exposedName).y);

            //Last step, if trying to modify component value, verify reset override restore value in asset without reinit
            if (modifyValue)
            {
                var editor = Editor.CreateEditor(vfx);
                editor.serializedObject.Update();

                var propertySheet = editor.serializedObject.FindProperty("m_PropertySheet");
                var fieldName = VisualEffectSerializationUtility.GetTypeField(VFXExpression.TypeToType(VFXValueType.Float2)) + ".m_Array";
                var vfxField = propertySheet.FindPropertyRelative(fieldName);

                Assert.AreEqual(1, vfxField.arraySize);

                var property = vfxField.GetArrayElementAtIndex(0);
                property = property.FindPropertyRelative("m_Overridden");
                expectedValue = (Vector2)parameter.value;
                property.boolValue = false;
                editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();

                GameObject.DestroyImmediate(editor);

                yield return null;
                spawnerStateFinal = VisualEffectUtility.GetSpawnerState(vfx, 0u);

                Assert.IsTrue(spawnerStateFinal.totalTime > spawnerLimit); //Check there isn't any reset time
                Assert.IsTrue(vfx.HasVector2(exposedName));
                Assert.AreEqual(expectedValue.x, vfx.GetVector2(exposedName).x); Assert.AreEqual(expectedValue.y, vfx.GetVector2(exposedName).y);
            }

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator CreateComponent_Modify_Asset_Keep_Override()
        {
            yield return new EnterPlayMode();
            var graph = VFXTestCommon.MakeTemporaryGraph();

            var parametersVector3Desc = VFXLibrary.GetParameters().Where(o => o.model.type == typeof(Vector3)).First();

            var exposedName = "poiuyt";
            var parameter = parametersVector3Desc.CreateInstance();
            parameter.SetSettingValue("m_ExposedName", exposedName);
            parameter.SetSettingValue("m_Exposed", true);
            parameter.value = new Vector3(0, 0, 0);
            graph.AddChild(parameter);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            while (m_mainObject.GetComponent<VisualEffect>() != null)
                UnityEngine.Object.DestroyImmediate(m_mainObject.GetComponent<VisualEffect>());
            var vfx = m_mainObject.AddComponent<VisualEffect>();
            vfx.visualEffectAsset = graph.visualEffectResource.asset;
            Assert.IsTrue(vfx.HasVector3(exposedName));
            var expectedOverriden = new Vector3(1, 2, 3);
            vfx.SetVector3(exposedName, expectedOverriden);

            yield return null;

            var actualOverriden = vfx.GetVector3(exposedName);
            Assert.AreEqual(actualOverriden.x, expectedOverriden.x); Assert.AreEqual(actualOverriden.y, expectedOverriden.y); Assert.AreEqual(actualOverriden.z, expectedOverriden.z);

            /* Add system & another exposed */
            var contextInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var allType = ScriptableObject.CreateInstance<AllType>();

            contextInitialize.AddChild(allType);
            graph.AddChild(contextInitialize);

            var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            spawner.LinkTo(contextInitialize);
            graph.AddChild(spawner);

            var output = ScriptableObject.CreateInstance<VFXPointOutput>();
            output.LinkFrom(contextInitialize);
            graph.AddChild(output);

            var parameter_Other = parametersVector3Desc.CreateInstance();
            var exposedName_Other = "tyuiop";
            parameter_Other.SetSettingValue("m_ExposedName", exposedName_Other);
            parameter_Other.SetSettingValue("m_Exposed", true);
            parameter_Other.value = new Vector3(6, 6, 6);
            graph.AddChild(parameter_Other);
            parameter.value = new Vector3(5, 5, 5);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            yield return null;

            Assert.IsTrue(vfx.HasVector3(exposedName));
            Assert.IsTrue(vfx.HasVector3(exposedName_Other));
            actualOverriden = vfx.GetVector3(exposedName);

            Assert.AreEqual(actualOverriden.x, expectedOverriden.x);
            Assert.AreEqual(actualOverriden.y, expectedOverriden.y);
            Assert.AreEqual(actualOverriden.z, expectedOverriden.z);

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator Create_Component_With_All_Basic_Type_Exposed_Check_Exposed_API()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var types = Enum.GetValues(typeof(VFXValueType)).Cast<VFXValueType>()
                        .Where(e => e != VFXValueType.Spline
                                && e != VFXValueType.Buffer //TODO : Remove this when Buffer as exposed property is possible
                                && e != VFXValueType.None).ToArray();

            foreach (var type in types)
            {
                var parameterDesc = VFXLibrary.GetParameters().First(o => VFXExpression.GetVFXValueTypeFromType(o.model.type) == type);
                var newInstance = parameterDesc.CreateInstance();

                newInstance.SetSettingValue("m_ExposedName", "abcd_" + type.ToString());
                newInstance.SetSettingValue("m_Exposed", true);
                graph.AddChild(newInstance);
            }


            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
            yield return null;

            var vfxAsset = graph.visualEffectResource.asset;

            var exposedProperties = new List<VFXExposedProperty>();
            vfxAsset.GetExposedProperties(exposedProperties);
            foreach (var type in types)
            {
                var expectedType = VFXExpression.TypeToType(type);
                var whereExpectedType = exposedProperties.Where(o => o.type == expectedType);
                Assert.IsTrue(whereExpectedType.Any());
                var expectedName = "abcd_" + type.ToString();
                var whereExpectedName = whereExpectedType.Where(o => o.name == expectedName);
                Assert.AreEqual(1, whereExpectedName.Count());

                var entry = whereExpectedName.First();
                if (entry.type == typeof(Texture))
                {
                    var dimension = vfxAsset.GetTextureDimension(entry.name);
                    switch (dimension)
                    {
                        case TextureDimension.Tex2D:        Assert.AreEqual(type, VFXValueType.Texture2D);        break;
                        case TextureDimension.Tex3D:        Assert.AreEqual(type, VFXValueType.Texture3D);        break;
                        case TextureDimension.Cube:         Assert.AreEqual(type, VFXValueType.TextureCube);      break;
                        case TextureDimension.Tex2DArray:   Assert.AreEqual(type, VFXValueType.Texture2DArray);   break;
                        case TextureDimension.CubeArray:    Assert.AreEqual(type, VFXValueType.TextureCubeArray); break;
                        default: Assert.Fail("Unknown expected type"); break;
                    }
                }
                else
                {
                    Assert.IsFalse(VFXExpression.IsTexture(type));
                }
            }
            Assert.AreEqual(types.Length, exposedProperties.Count);
        }

        private object GetValue_A_Type(Type type)
        {
            if (typeof(float) == type)
                return 2.0f;
            else if (typeof(Vector2) == type)
                return new Vector2(3.0f, 4.0f);
            else if (typeof(Vector3) == type)
                return new Vector3(8.0f, 9.0f, 10.0f);
            else if (typeof(Vector4) == type)
                return new Vector4(11.0f, 12.0f, 13.0f, 14.0f);
            else if (typeof(Color) == type)
                return new Color(0.1f, 0.2f, 0.3f, 0.4f);
            else if (typeof(int) == type)
                return 15;
            else if (typeof(uint) == type)
                return 16u;
            else if (typeof(AnimationCurve) == type)
                return new AnimationCurve(new Keyframe(0, 13), new Keyframe(1, 14));
            else if (typeof(Gradient) == type)
                return new Gradient() { colorKeys = new GradientColorKey[] { new GradientColorKey(Color.white, 0.2f) } };
            else if (typeof(Mesh) == type)
                return m_cubeEmpty.GetComponent<MeshFilter>().sharedMesh;
            else if (typeof(Texture2D) == type)
                return m_texture2D_A;
            else if (typeof(Texture2DArray) == type)
                return m_texture2DArray_A;
            else if (typeof(Texture3D) == type)
                return m_texture3D_A;
            else if (typeof(Cubemap) == type)
                return m_textureCube_A;
            else if (typeof(CubemapArray) == type)
                return m_textureCubeArray_A;
            else if (typeof(bool) == type)
                return true;
            else if (typeof(Matrix4x4) == type)
                return Matrix4x4.identity;
            return null;
        }

        private object GetValue_B_Type(Type type)
        {
            if (typeof(float) == type)
                return 50.0f;
            else if (typeof(Vector2) == type)
                return new Vector2(53.0f, 54.0f);
            else if (typeof(Vector3) == type)
                return new Vector3(58.0f, 59.0f, 510.0f);
            else if (typeof(Vector4) == type || typeof(Color) == type)// ValueB_Type is used to set a component value, so return a Vector4 with color values
                return new Vector4(511.0f, 512.0f, 513.0f, 514.0f);
            else if (typeof(int) == type)
                return 515;
            else if (typeof(uint) == type)
                return 516u;
            else if (typeof(AnimationCurve) == type)
                return new AnimationCurve(new Keyframe(0, 47), new Keyframe(0.5f, 23), new Keyframe(1.0f, 17));
            else if (typeof(Gradient) == type)
                return new Gradient() { colorKeys = new GradientColorKey[] { new GradientColorKey(Color.white, 0.2f), new GradientColorKey(Color.black, 0.6f) } };
            else if (typeof(Mesh) == type)
                return m_sphereEmpty.GetComponent<MeshFilter>().sharedMesh;
            else if (typeof(Texture2D) == type)
                return m_texture2D_B;
            else if (typeof(Texture2DArray) == type)
                return m_texture2DArray_B;
            else if (typeof(Texture3D) == type)
                return m_texture3D_B;
            else if (typeof(Cubemap) == type)
                return m_textureCube_B;
            else if (typeof(CubemapArray) == type)
                return m_textureCubeArray_B;
            else if (typeof(bool) == type)
                return true;
            else if (typeof(Matrix4x4) == type)
                return Matrix4x4.identity;
            return null;
        }

        bool fnHas_UsingBindings(VFXValueType type, VisualEffect vfx, string name)
        {
            switch (type)
            {
                case VFXValueType.Float: return vfx.HasFloat(name);
                case VFXValueType.Float2: return vfx.HasVector2(name);
                case VFXValueType.Float3: return vfx.HasVector3(name);
                case VFXValueType.Float4: return vfx.HasVector4(name);
                case VFXValueType.Int32: return vfx.HasInt(name);
                case VFXValueType.Uint32: return vfx.HasUInt(name);
                case VFXValueType.Curve: return vfx.HasAnimationCurve(name);
                case VFXValueType.ColorGradient: return vfx.HasGradient(name);
                case VFXValueType.Mesh: return vfx.HasMesh(name);
                case VFXValueType.Texture2D: return vfx.HasTexture(name) && vfx.GetTextureDimension(name) == TextureDimension.Tex2D;
                case VFXValueType.Texture2DArray: return vfx.HasTexture(name) && vfx.GetTextureDimension(name) == TextureDimension.Tex2DArray;
                case VFXValueType.Texture3D: return vfx.HasTexture(name) && vfx.GetTextureDimension(name) == TextureDimension.Tex3D;
                case VFXValueType.TextureCube: return vfx.HasTexture(name) && vfx.GetTextureDimension(name) == TextureDimension.Cube;
                case VFXValueType.TextureCubeArray: return vfx.HasTexture(name) && vfx.GetTextureDimension(name) == TextureDimension.CubeArray;
                case VFXValueType.Boolean: return vfx.HasBool(name);
                case VFXValueType.Matrix4x4: return vfx.HasMatrix4x4(name);
            }
            return false;
        }

        object fnGet_UsingBindings(VFXValueType type, VisualEffect vfx, string name)
        {
            switch (type)
            {
                case VFXValueType.Float: return vfx.GetFloat(name);
                case VFXValueType.Float2: return vfx.GetVector2(name);
                case VFXValueType.Float3: return vfx.GetVector3(name);
                case VFXValueType.Float4: return vfx.GetVector4(name);
                case VFXValueType.Int32: return vfx.GetInt(name);
                case VFXValueType.Uint32: return vfx.GetUInt(name);
                case VFXValueType.Curve: return vfx.GetAnimationCurve(name);
                case VFXValueType.ColorGradient: return vfx.GetGradient(name);
                case VFXValueType.Mesh: return vfx.GetMesh(name);
                case VFXValueType.Texture2D:
                case VFXValueType.Texture2DArray:
                case VFXValueType.Texture3D:
                case VFXValueType.TextureCube:
                case VFXValueType.TextureCubeArray: return vfx.GetTexture(name);
                case VFXValueType.Boolean: return vfx.GetBool(name);
                case VFXValueType.Matrix4x4: return vfx.GetMatrix4x4(name);
            }
            return null;
        }

        void fnSet_UsingBindings(VFXValueType type, VisualEffect vfx, string name, object value)
        {
            switch (type)
            {
                case VFXValueType.Float: vfx.SetFloat(name, (float)value); break;
                case VFXValueType.Float2: vfx.SetVector2(name, (Vector2)value); break;
                case VFXValueType.Float3: vfx.SetVector3(name, (Vector3)value); break;
                case VFXValueType.Float4: vfx.SetVector4(name, (Vector4)value); break;
                case VFXValueType.Int32: vfx.SetInt(name, (int)value); break;
                case VFXValueType.Uint32: vfx.SetUInt(name, (uint)value); break;
                case VFXValueType.Curve: vfx.SetAnimationCurve(name, (AnimationCurve)value); break;
                case VFXValueType.ColorGradient: vfx.SetGradient(name, (Gradient)value); break;
                case VFXValueType.Mesh: vfx.SetMesh(name, (Mesh)value); break;
                case VFXValueType.Texture2D:
                case VFXValueType.Texture2DArray:
                case VFXValueType.Texture3D:
                case VFXValueType.TextureCube:
                case VFXValueType.TextureCubeArray: vfx.SetTexture(name, (Texture)value); break;
                case VFXValueType.Boolean: vfx.SetBool(name, (bool)value); break;
                case VFXValueType.Matrix4x4: vfx.SetMatrix4x4(name, (Matrix4x4)value); break;
            }
        }

        bool fnHas_UsingSerializedProperty(VFXValueType type, VisualEffect vfx, string name)
        {
            var editor = Editor.CreateEditor(vfx);
            try
            {
                var propertySheet = editor.serializedObject.FindProperty("m_PropertySheet");
                var fieldName = VisualEffectSerializationUtility.GetTypeField(VFXExpression.TypeToType(type)) + ".m_Array";
                var vfxField = propertySheet.FindPropertyRelative(fieldName);
                if (vfxField != null)
                {
                    for (int i = 0; i < vfxField.arraySize; ++i)
                    {
                        var property = vfxField.GetArrayElementAtIndex(i);
                        var nameProperty = property.FindPropertyRelative("m_Name").stringValue;
                        if (nameProperty == name)
                        {
                            return true;
                        }
                    }
                }
            }
            finally
            {
                GameObject.DestroyImmediate(editor);
            }
            return false;
        }

        Matrix4x4 fnMatrixFromSerializedProperty(SerializedProperty property)
        {
            var mat = new Matrix4x4();

            mat.m00 = property.FindPropertyRelative("e00").floatValue;
            mat.m01 = property.FindPropertyRelative("e01").floatValue;
            mat.m02 = property.FindPropertyRelative("e02").floatValue;
            mat.m03 = property.FindPropertyRelative("e03").floatValue;

            mat.m10 = property.FindPropertyRelative("e10").floatValue;
            mat.m11 = property.FindPropertyRelative("e11").floatValue;
            mat.m12 = property.FindPropertyRelative("e12").floatValue;
            mat.m13 = property.FindPropertyRelative("e13").floatValue;

            mat.m20 = property.FindPropertyRelative("e20").floatValue;
            mat.m21 = property.FindPropertyRelative("e21").floatValue;
            mat.m22 = property.FindPropertyRelative("e22").floatValue;
            mat.m23 = property.FindPropertyRelative("e23").floatValue;

            mat.m30 = property.FindPropertyRelative("e30").floatValue;
            mat.m31 = property.FindPropertyRelative("e31").floatValue;
            mat.m32 = property.FindPropertyRelative("e32").floatValue;
            mat.m33 = property.FindPropertyRelative("e33").floatValue;

            return mat;
        }

        void fnMatrixToSerializedProperty(SerializedProperty property, Matrix4x4 mat)
        {
            property.FindPropertyRelative("e00").floatValue = mat.m00;
            property.FindPropertyRelative("e01").floatValue = mat.m01;
            property.FindPropertyRelative("e02").floatValue = mat.m02;
            property.FindPropertyRelative("e03").floatValue = mat.m03;

            property.FindPropertyRelative("e10").floatValue = mat.m10;
            property.FindPropertyRelative("e11").floatValue = mat.m11;
            property.FindPropertyRelative("e12").floatValue = mat.m12;
            property.FindPropertyRelative("e13").floatValue = mat.m13;

            property.FindPropertyRelative("e20").floatValue = mat.m20;
            property.FindPropertyRelative("e21").floatValue = mat.m21;
            property.FindPropertyRelative("e22").floatValue = mat.m22;
            property.FindPropertyRelative("e23").floatValue = mat.m23;

            property.FindPropertyRelative("e30").floatValue = mat.m30;
            property.FindPropertyRelative("e31").floatValue = mat.m31;
            property.FindPropertyRelative("e32").floatValue = mat.m32;
            property.FindPropertyRelative("e33").floatValue = mat.m33;
        }

        object fnGet_UsingSerializedProperty(VFXValueType type, VisualEffect vfx, string name)
        {
            var editor = Editor.CreateEditor(vfx);
            try
            {
                var propertySheet = editor.serializedObject.FindProperty("m_PropertySheet");
                editor.serializedObject.Update();

                var fieldName = VisualEffectSerializationUtility.GetTypeField(VFXExpression.TypeToType(type)) + ".m_Array";
                var vfxField = propertySheet.FindPropertyRelative(fieldName);
                if (vfxField != null)
                {
                    for (int i = 0; i < vfxField.arraySize; ++i)
                    {
                        var property = vfxField.GetArrayElementAtIndex(i);
                        var nameProperty = property.FindPropertyRelative("m_Name").stringValue;
                        if (nameProperty == name)
                        {
                            property = property.FindPropertyRelative("m_Value");

                            switch (type)
                            {
                                case VFXValueType.Float: return property.floatValue;
                                case VFXValueType.Float2: return property.vector2Value;
                                case VFXValueType.Float3: return property.vector3Value;
                                case VFXValueType.Float4: return property.vector4Value;
                                case VFXValueType.Int32: return property.intValue;
                                case VFXValueType.Uint32: return property.intValue;     // there isn't uintValue
                                case VFXValueType.Curve: return property.animationCurveValue;
                                case VFXValueType.ColorGradient: return property.gradientValue;
                                case VFXValueType.Mesh: return property.objectReferenceValue;
                                case VFXValueType.Texture2D:
                                case VFXValueType.Texture2DArray:
                                case VFXValueType.Texture3D:
                                case VFXValueType.TextureCube:
                                case VFXValueType.TextureCubeArray: return property.objectReferenceValue;
                                case VFXValueType.Boolean: return property.boolValue;
                                case VFXValueType.Matrix4x4: return fnMatrixFromSerializedProperty(property);
                            }
                            Assert.Fail();
                        }
                    }
                }
            }
            finally
            {
                GameObject.DestroyImmediate(editor);
            }
            return null;
        }

        void fnSet_UsingSerializedProperty(VFXValueType type, VisualEffect vfx, string name, object value)
        {
            var editor = Editor.CreateEditor(vfx);
            try
            {
                editor.serializedObject.Update();

                var propertySheet = editor.serializedObject.FindProperty("m_PropertySheet");
                var fieldName = VisualEffectSerializationUtility.GetTypeField(VFXExpression.TypeToType(type)) + ".m_Array";
                var vfxField = propertySheet.FindPropertyRelative(fieldName);
                if (vfxField != null)
                {
                    for (int i = 0; i < vfxField.arraySize; ++i)
                    {
                        var property = vfxField.GetArrayElementAtIndex(i);
                        var propertyName = property.FindPropertyRelative("m_Name").stringValue;
                        if (propertyName == name)
                        {
                            var propertyValue = property.FindPropertyRelative("m_Value");
                            var propertyOverriden = property.FindPropertyRelative("m_Overridden");

                            switch (type)
                            {
                                case VFXValueType.Float: propertyValue.floatValue = (float)value; break;
                                case VFXValueType.Float2: propertyValue.vector2Value = (Vector2)value; break;
                                case VFXValueType.Float3: propertyValue.vector3Value = (Vector3)value; break;
                                case VFXValueType.Float4: propertyValue.vector4Value = (Vector4)value; break;
                                case VFXValueType.Int32: propertyValue.intValue = (int)value; break;
                                case VFXValueType.Uint32: propertyValue.intValue = (int)((uint)value); break;     // there isn't uintValue
                                case VFXValueType.Curve: propertyValue.animationCurveValue = (AnimationCurve)value; break;
                                case VFXValueType.ColorGradient: propertyValue.gradientValue = (Gradient)value; break;
                                case VFXValueType.Mesh: propertyValue.objectReferenceValue = (UnityEngine.Object)value; break;
                                case VFXValueType.Texture2D:
                                case VFXValueType.Texture2DArray:
                                case VFXValueType.Texture3D:
                                case VFXValueType.TextureCube:
                                case VFXValueType.TextureCubeArray: propertyValue.objectReferenceValue = (UnityEngine.Object)value; break;
                                case VFXValueType.Boolean: propertyValue.boolValue = (bool)value; break;
                                case VFXValueType.Matrix4x4: fnMatrixToSerializedProperty(propertyValue, (Matrix4x4)value); break;
                            }
                            propertyOverriden.boolValue = true;
                        }
                    }
                }
                editor.serializedObject.ApplyModifiedProperties();
            }
            finally
            {
                GameObject.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator CreateComponentWithAllBasicTypeExposed([ValueSource("trueOrFalse")] bool linkMode, [ValueSource("trueOrFalse")] bool bindingModes)
        {
            yield return new EnterPlayMode();
            var commonBaseName = "abcd_";

            var graph = VFXTestCommon.MakeTemporaryGraph();

            var contextInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var allType = ScriptableObject.CreateInstance<AllType>();

            contextInitialize.AddChild(allType);
            graph.AddChild(contextInitialize);

            // Needs a spawner and output for the system to be valid
            {
                var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
                spawner.LinkTo(contextInitialize);
                graph.AddChild(spawner);

                var output = ScriptableObject.CreateInstance<VFXPointOutput>();
                output.LinkFrom(contextInitialize);
                graph.AddChild(output);
            }

            var types = Enum.GetValues(typeof(VFXValueType)).Cast<VFXValueType>()
                .Where(e => e != VFXValueType.Spline
                        &&  e != VFXValueType.Buffer //TODO : Remove this when Buffer as exposed property is possible
                        &&  e != VFXValueType.None).ToArray();
            foreach (var parameter in VFXLibrary.GetParameters())
            {
                var newInstance = parameter.CreateInstance();

                VFXValueType type = types.FirstOrDefault(e => VFXExpression.GetVFXValueTypeFromType(newInstance.type) == e);
                if (type != VFXValueType.None)
                {
                    newInstance.SetSettingValue("m_ExposedName", commonBaseName + newInstance.type.UserFriendlyName());
                    newInstance.SetSettingValue("m_Exposed", true);
                    var value = GetValue_A_Type(newInstance.type);
                    Assert.IsNotNull(value);
                    newInstance.value = value;
                    graph.AddChild(newInstance);
                }
            }

            if (linkMode)
            {
                foreach (var type in types)
                {
                    VFXSlot slot = null;
                    for (int i = 0; i < allType.GetNbInputSlots(); ++i)
                    {
                        var currentSlot = allType.GetInputSlot(i);
                        var expression = currentSlot.GetExpression();
                        if (expression != null && expression.valueType == type)
                        {
                            slot = currentSlot;
                            break;
                        }
                    }
                    Assert.IsNotNull(slot, type.ToString());

                    var parameter = graph.children.OfType<VFXParameter>().FirstOrDefault(o =>
                    {
                        if (o.GetNbOutputSlots() > 0)
                        {
                            var expression = o.outputSlots[0].GetExpression();
                            if (expression != null && expression.valueType == type)
                            {
                                return true;
                            }
                        }
                        return false;
                    });
                    Assert.IsNotNull(parameter, "parameter with type : " + type.ToString());
                    slot.Link(parameter.GetOutputSlot(0));
                }
            }

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            while (m_mainObject.GetComponent<VisualEffect>() != null)
            {
                UnityEngine.Object.DestroyImmediate(m_mainObject.GetComponent<VisualEffect>());
            }
            var vfxComponent = m_mainObject.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            yield return null;

            Func<AnimationCurve, AnimationCurve, bool> fnCompareCurve = delegate(AnimationCurve left, AnimationCurve right)
            {
                return left.keys.Length == right.keys.Length;
            };

            Func<Gradient, Gradient, bool> fnCompareGradient = delegate(Gradient left, Gradient right)
            {
                return left.colorKeys.Length == right.colorKeys.Length;
            };

            //Check default Value_A & change to Value_B (At this stage, it's useless to access with SerializedProperty)
            foreach (var parameter in VFXLibrary.GetParameters())
            {
                VFXValueType type = types.FirstOrDefault(e => VFXExpression.GetVFXValueTypeFromType(parameter.model.type) == e);
                if (type == VFXValueType.None)
                    continue;
                var currentName = commonBaseName + parameter.model.type.UserFriendlyName();
                var baseValue = GetValue_A_Type(parameter.model.type);
                var newValue = GetValue_B_Type(parameter.model.type);

                Assert.IsTrue(fnHas_UsingBindings(type, vfxComponent, currentName));
                var currentValue = fnGet_UsingBindings(type, vfxComponent, currentName);
                if (type == VFXValueType.ColorGradient)
                {
                    Assert.IsTrue(fnCompareGradient((Gradient)baseValue, (Gradient)currentValue));
                }
                else if (type == VFXValueType.Curve)
                {
                    Assert.IsTrue(fnCompareCurve((AnimationCurve)baseValue, (AnimationCurve)currentValue));
                }
                else if (parameter.model.type == typeof(Color))
                {
                    Color col = (Color)baseValue;
                    Assert.AreEqual(new Vector4(col.r, col.g, col.b, col.a), currentValue);
                }
                else
                {
                    Assert.AreEqual(baseValue, currentValue);
                }
                fnSet_UsingBindings(type, vfxComponent, currentName, newValue);

                yield return null;
            }

            //Compare new setted values
            foreach (var parameter in VFXLibrary.GetParameters())
            {
                VFXValueType type = types.FirstOrDefault(e => VFXExpression.GetVFXValueTypeFromType(parameter.model.type) == e);
                if (type == VFXValueType.None)
                    continue;
                var currentName = commonBaseName + parameter.model.type.UserFriendlyName();
                var baseValue = GetValue_B_Type(parameter.model.type);
                if (bindingModes)
                    Assert.IsTrue(fnHas_UsingBindings(type, vfxComponent, currentName));
                else
                    Assert.IsTrue(fnHas_UsingSerializedProperty(type, vfxComponent, currentName));

                object currentValue = null;
                if (bindingModes)
                    currentValue = fnGet_UsingBindings(type, vfxComponent, currentName);
                else
                    currentValue = fnGet_UsingSerializedProperty(type, vfxComponent, currentName);

                //current = fnGet(type, vfxComponent, currentName);
                if (type == VFXValueType.ColorGradient)
                {
                    Assert.IsTrue(fnCompareGradient((Gradient)baseValue, (Gradient)currentValue));
                }
                else if (type == VFXValueType.Curve)
                {
                    Assert.IsTrue(fnCompareCurve((AnimationCurve)baseValue, (AnimationCurve)currentValue));
                }
                else
                {
                    Assert.AreEqual(baseValue, currentValue);
                }
                yield return null;
            }

            //Test ResetOverride function
            foreach (var parameter in VFXLibrary.GetParameters())
            {
                VFXValueType type = types.FirstOrDefault(e => VFXExpression.GetVFXValueTypeFromType(parameter.model.type) == e);
                if (type == VFXValueType.None)
                    continue;
                var currentName = commonBaseName + parameter.model.type.UserFriendlyName();
                vfxComponent.ResetOverride(currentName);

                {
                    //If we use bindings, internal value is restored but it doesn't change serialized property (strange at first but intended behavior)
                    var baseValue = bindingModes ? GetValue_A_Type(parameter.model.type) : GetValue_B_Type(parameter.model.type);

                    object currentValue = null;
                    if (bindingModes)
                        currentValue = fnGet_UsingBindings(type, vfxComponent, currentName);
                    else
                        currentValue = fnGet_UsingSerializedProperty(type, vfxComponent, currentName);
                    if (type == VFXValueType.ColorGradient)
                    {
                        Assert.IsTrue(fnCompareGradient((Gradient)baseValue, (Gradient)currentValue));
                    }
                    else if (type == VFXValueType.Curve)
                    {
                        Assert.IsTrue(fnCompareCurve((AnimationCurve)baseValue, (AnimationCurve)currentValue));
                    }
                    else if (bindingModes && parameter.model.type == typeof(Color))
                    {
                        Color col = (Color)baseValue;
                        Assert.AreEqual(new Vector4(col.r, col.g, col.b, col.a), currentValue);
                    }
                    else
                    {
                        Assert.AreEqual(baseValue, currentValue);
                    }
                }

                if (!bindingModes)
                {
                    var internalValue = fnGet_UsingBindings(type, vfxComponent, currentName);
                    var originalAssetValue = GetValue_A_Type(parameter.model.type);

                    if (type == VFXValueType.ColorGradient)
                    {
                        Assert.IsTrue(fnCompareGradient((Gradient)originalAssetValue, (Gradient)internalValue));
                    }
                    else if (type == VFXValueType.Curve)
                    {
                        Assert.IsTrue(fnCompareCurve((AnimationCurve)originalAssetValue, (AnimationCurve)internalValue));
                    }
                    else if (parameter.model.type == typeof(Color))
                    {
                        Color col = (Color)originalAssetValue;
                        Assert.AreEqual(new Vector4(col.r, col.g, col.b, col.a), internalValue);
                    }
                    else
                    {
                        Assert.AreEqual(originalAssetValue, internalValue);
                    }
                }
                yield return null;
            }
            yield return new ExitPlayMode();
        }
    }
}
#endif
