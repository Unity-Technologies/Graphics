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
    public class VisualEffectTest
    {

        GameObject m_mainObject;
        GameObject m_mainCamera;

        [OneTimeSetUp]
        public void Init()
        {
            var mainObjectName = "VFX_Test_Main_Object";
            m_mainObject = new GameObject(mainObjectName);

            var mainCameraName = "VFX_Test_Main_Camera";
            m_mainCamera = new GameObject(mainCameraName);
            var camera = this.m_mainCamera.AddComponent<Camera>();
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(m_mainObject.transform.position);

            Time.captureFramerate = 10;
            UnityEngine.VFX.VFXManager.fixedTimeStep = 0.1f;
            UnityEngine.VFX.VFXManager.maxDeltaTime = 0.1f;

            ShaderUtil.allowAsyncCompilation = false;

            VFXTestCommon.CloseAllUnecessaryWindows();
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            Time.captureFramerate = 0;
            UnityEngine.VFX.VFXManager.fixedTimeStep = 1.0f/60.0f;
            UnityEngine.VFX.VFXManager.maxDeltaTime = 1.0f/20.0f;
            ShaderUtil.allowAsyncCompilation = true;

            VFXTestCommon.DeleteAllTemporaryGraph();
            GameObject.DestroyImmediate(m_mainObject);
            GameObject.DestroyImmediate(m_mainCamera);
        }

        VFXGraph CreateTemporaryGraph_With_GraphicsBuffer(IEnumerable<string> names)
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();

            var graphicsBufferDesc = VFXLibrary.GetParameters().FirstOrDefault(o => o.name.ToLowerInvariant().Contains("graphics buffer"));
            Assert.IsNotNull(graphicsBufferDesc);
            foreach (var targetGraphicsBuffer in names)
            {
                var parameter = graphicsBufferDesc.variant.CreateInstance();
                parameter.SetSettingValue("m_ExposedName", targetGraphicsBuffer);
                parameter.SetSettingValue("m_Exposed", true);
                graph.AddChild(parameter);
            }

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
            return graph;
        }

        VFXGraph CreateTemporaryGraph_With_GraphicsBuffer(string name)
        {
            return CreateTemporaryGraph_With_GraphicsBuffer(new []{ name });
        }

        //Case 1406873, Vector4 & Color are bound to the same HLSL type
        [UnityTest]
        public IEnumerator CreateGraph_With_GraphicsBuffer_Using_SampleFloat4_And_Color()
        {
            var graphicsBuffers = new(string name, Type type)[] {("Buffer_A", typeof(Color)), ("Buffer_B", typeof(Vector4))};
            var graph = CreateTemporaryGraph_With_GraphicsBuffer(graphicsBuffers.Select(o => o.name));

            var contextInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            graph.AddChild(contextInitialize);

            var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            spawner.LinkTo(contextInitialize);
            graph.AddChild(spawner);

            var output = ScriptableObject.CreateInstance<VFXPointOutput>();
            output.LinkFrom(contextInitialize);
            graph.AddChild(output);

            foreach (var graphicsBuffer in graphicsBuffers)
            {
                var sampleBuffer = ScriptableObject.CreateInstance<UnityEditor.VFX.Operator.SampleBuffer>();
                sampleBuffer.SetOperandType(graphicsBuffer.type);
                graph.AddChild(sampleBuffer);

                var param = graph.children.OfType<VFXParameter>().FirstOrDefault(o => o.exposedName == graphicsBuffer.name);
                Assert.IsNotNull(param);
                Assert.IsTrue(sampleBuffer.inputSlots[0].Link(param.outputSlots[0]));

                var block = VFXLibrary.GetBlocks().FirstOrDefault(o =>
                     o.modelType == typeof(Block.SetAttribute)
                     && o.variant.settings.Any(x => x.Value.Equals(VFXAttribute.Color.name)));
                Assert.IsNotNull(block);

                var addAttribute = (VFXBlock)block.variant.CreateInstance();
                addAttribute.SetSettingValue("Composition", UnityEditor.VFX.Block.AttributeCompositionMode.Add);
                contextInitialize.AddChild(addAttribute);
                Assert.IsTrue(sampleBuffer.outputSlots[0].Link(addAttribute.inputSlots[0]));
            }

            var assetPath = AssetDatabase.GetAssetPath(graph);
            AssetDatabase.ImportAsset(assetPath);

            for (uint i = 0; i < 8; i++)
                yield return null;

            //Check compilation error
            var allSubAsset = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var computeInitialize = allSubAsset.OfType<ComputeShader>().FirstOrDefault(o => o.name.Contains("Initialize"));
            Assert.IsNotNull(computeInitialize);
            Assert.AreNotEqual(computeInitialize.FindKernel("CSMain"), -1);

            yield return null;
        }

        // Deactivated test by now as GetGraphicsBuffer is not public
        //[UnityTest]
        public IEnumerator CreateComponent_And_BindGraphicsBuffer()
        {
            var targetGraphicsBuffer = "my_exposed_graphics_buffer";
            var graph = CreateTemporaryGraph_With_GraphicsBuffer(targetGraphicsBuffer);

            while (m_mainObject.GetComponent<VisualEffect>() != null)
                UnityEngine.Object.DestroyImmediate(m_mainObject.GetComponent<VisualEffect>());
            var vfx = m_mainObject.AddComponent<VisualEffect>();
            vfx.visualEffectAsset = graph.visualEffectResource.asset;

            yield return null;

            Assert.IsTrue(vfx.HasGraphicsBuffer(targetGraphicsBuffer));
            Assert.IsNull(vfx.GetGraphicsBuffer(targetGraphicsBuffer));

            var newGraphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 4);
            vfx.SetGraphicsBuffer(targetGraphicsBuffer, newGraphicsBuffer);
            Assert.IsNotNull(vfx.GetGraphicsBuffer(targetGraphicsBuffer));

            var readGraphicBuffer = vfx.GetGraphicsBuffer(targetGraphicsBuffer);
            Assert.IsNotNull(readGraphicBuffer);
            Assert.AreEqual(newGraphicsBuffer.count, readGraphicBuffer.count);
            Assert.AreEqual(newGraphicsBuffer.stride, readGraphicBuffer.stride);
            Assert.AreEqual(newGraphicsBuffer.GetNativeBufferPtr(), readGraphicBuffer.GetNativeBufferPtr());

            newGraphicsBuffer.Release();
            yield return null;

        }

        [UnityTest]
        public IEnumerator CreateComponent_With_Spaceable_Properties()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();

            var coneParameter = VFXLibrary.GetParameters().FirstOrDefault(o => o.modelType == typeof(TCone));
            Assert.IsNotNull(coneParameter);

            //Basic spaceable parameter
            var baseName = "my_exposed_cone_";
            var availableSpace = Enum.GetValues(typeof(VFXSpace)).Cast<VFXSpace>().ToArray();
            var expectedSpaceCount = 3;
            Assert.AreEqual(expectedSpaceCount, availableSpace.Length);
            foreach (var space in availableSpace)
            {
                var parameter = (VFXParameter)coneParameter.variant.CreateInstance();
                parameter.SetSettingValue("m_ExposedName", baseName + space.ToString().ToLowerInvariant());
                parameter.SetSettingValue("m_Exposed", true);
                parameter.outputSlots[0].space = space;
                graph.AddChild(parameter);
            }

            //Other parameter (not spaceable)
            var intDesc = VFXLibrary.GetParameters().FirstOrDefault(o => o.modelType == typeof(int));
            Assert.IsNotNull(intDesc);
            var parameterInteger = intDesc.CreateInstance();
            parameterInteger.SetSettingValue("m_ExposedName", "my_exposed_integer");
            parameterInteger.SetSettingValue("m_Exposed", true);
            graph.AddChild(parameterInteger);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            var visualEffectAsset = graph.visualEffectResource.asset;

            var exposedProperties = new List<VFXExposedProperty>();
            visualEffectAsset.GetExposedProperties(exposedProperties);
            var valueCountInCone = 6;
            Assert.AreEqual(valueCountInCone * expectedSpaceCount + 1, exposedProperties.Count);

            foreach (var exposedProperty in exposedProperties)
            {
                var readSpace = visualEffectAsset.GetExposedSpace(exposedProperty.name);
                var expectedSpace = VFXSpace.None;
                foreach (var space in availableSpace)
                {
                    //There is a mix of exposed properties in this test component
                    //The name indicates the expected space if mentioned
                    //If not in name, then, it wasn't a spaceable property
                    if (exposedProperty.name.IndexOf(space.ToString(),
                            StringComparison.InvariantCultureIgnoreCase) > 0)
                    {
                        expectedSpace = space;
                        break;
                    }
                }
                Assert.AreEqual(readSpace, expectedSpace);
            }
            yield return null;
        }

        public enum GraphicsBufferResetCase
        {
            Reinit,
            DisableAndRenable,
            ChangeVisualEffectAsset,
            EditSerializedObject
        }

        static GraphicsBufferResetCase[] s_GraphicsBufferResetCase = Enum.GetValues(typeof(GraphicsBufferResetCase)).Cast<GraphicsBufferResetCase>().ToArray();

        // Deactivated test by now as GetGraphicsBuffer is not public
        //[UnityTest]
        public IEnumerator CreateComponent_And_BindGraphicsBuffer_And_([ValueSource("s_GraphicsBufferResetCase")] GraphicsBufferResetCase resetCase)
        {
            var targetGraphicsBuffer = "my_exposed_graphics_buffer";
            var graph = CreateTemporaryGraph_With_GraphicsBuffer(targetGraphicsBuffer);
            var targetInteger = "my_exposed_graphics_integer";

            if (resetCase == GraphicsBufferResetCase.EditSerializedObject)
            {
                //Other value used for vfx editor update
                var intDesc = VFXLibrary.GetParameters().Where(o => o.variant.name.ToLowerInvariant().Contains("int")).FirstOrDefault();
                Assert.IsNotNull(intDesc);
                var parameterInteger = intDesc.CreateInstance();
                parameterInteger.SetSettingValue("m_ExposedName", targetInteger);
                parameterInteger.SetSettingValue("m_Exposed", true);
                graph.AddChild(parameterInteger);

                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
            }

            while (m_mainObject.GetComponent<VisualEffect>() != null)
                UnityEngine.Object.DestroyImmediate(m_mainObject.GetComponent<VisualEffect>());
            var vfx = m_mainObject.AddComponent<VisualEffect>();
            vfx.visualEffectAsset = graph.visualEffectResource.asset;

            var newGraphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 4);
            vfx.SetGraphicsBuffer(targetGraphicsBuffer, newGraphicsBuffer);
            Assert.IsNotNull(vfx.GetGraphicsBuffer(targetGraphicsBuffer));

            switch (resetCase)
            {
                case GraphicsBufferResetCase.Reinit:
                    vfx.Reinit();
                    break;
                case GraphicsBufferResetCase.DisableAndRenable:
                    vfx.enabled = false;
                    vfx.enabled = true;
                    break;
                case GraphicsBufferResetCase.ChangeVisualEffectAsset:
                    vfx.visualEffectAsset = CreateTemporaryGraph_With_GraphicsBuffer(targetGraphicsBuffer).visualEffectResource.asset;
                    vfx.visualEffectAsset = graph.visualEffectResource.asset;
                    break;
                case GraphicsBufferResetCase.EditSerializedObject:
                    {
                        vfx.SetInt(targetInteger, 123);

                        var editor = Editor.CreateEditor(vfx);
                        editor.serializedObject.Update();

                        var propertySheet = editor.serializedObject.FindProperty("m_PropertySheet");
                        var fieldName = VisualEffectSerializationUtility.GetTypeField(VFXExpression.TypeToType(VFXValueType.Int32)) + ".m_Array";
                        var vfxField = propertySheet.FindPropertyRelative(fieldName);

                        Assert.AreEqual(1, vfxField.arraySize);

                        var property = vfxField.GetArrayElementAtIndex(0);
                        property = property.FindPropertyRelative("m_Value");
                        property.intValue = 666;
                        editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();

                        GameObject.DestroyImmediate(editor);
                    }
                    break;
            }

            Assert.IsNotNull(vfx.GetGraphicsBuffer(targetGraphicsBuffer));

            var readGraphicBuffer = vfx.GetGraphicsBuffer(targetGraphicsBuffer);
            Assert.AreEqual(newGraphicsBuffer.GetNativeBufferPtr(), readGraphicBuffer.GetNativeBufferPtr());
            newGraphicsBuffer.Release();

            yield return null;
        }

        [UnityTest]
        public IEnumerator Check_VFXRenderer_DefaultRenderingLayerNames()
        {
            var layerNames = RenderingLayerMask.GetDefinedRenderingLayerNames();
            Assert.IsNotNull(layerNames);
            Assert.IsTrue(layerNames.Length != 0);
            Assert.IsFalse(layerNames.Any(o => string.IsNullOrEmpty(o)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateComponent_And_Graph_Modify_It_To_Generate_Expected_Exception()
        {
            var graph = VFXTestCommon.CreateGraph_And_System();

            yield return null;

            while (m_mainObject.GetComponent<VisualEffect>() != null)
                UnityEngine.Object.DestroyImmediate(m_mainObject.GetComponent<VisualEffect>());
            var vfxComponent = m_mainObject.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;
            Assert.DoesNotThrow(() => VFXTestCommon.GetSpawnerState(vfxComponent, 0));

            yield return null;

            //Plug a GPU instruction on bounds, excepting an exception while recompiling
            var getPositionDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.variant.modelType == typeof(VFXAttributeParameter) && o.variant.name.Contains(VFXAttribute.Position.name, StringComparison.OrdinalIgnoreCase));
            var getPosition = getPositionDesc.CreateInstance();
            graph.AddChild(getPosition);
            var initializeContext = graph.children.OfType<VFXBasicInitialize>().FirstOrDefault();
            Assert.AreEqual(VFXValueType.Float3, initializeContext.inputSlots[0][0].valueType);

            getPosition.outputSlots[0].Link(initializeContext.inputSlots[0][0]);

            //LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Exception while compiling expression graph:*")); < Incorrect with our katana configuration
            Debug.unityLogger.logEnabled = false;
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
            Debug.unityLogger.logEnabled = true;
            Assert.Throws(typeof(IndexOutOfRangeException), () => VFXTestCommon.GetSpawnerState(vfxComponent, 0)); //This is the exception which matters for this test
        }


        [UnityTest]
        public IEnumerator Create_Component_With_All_Basic_Type_Exposed_Check_Exposed_API()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            foreach (var type in VFXTestCommon.s_supportedValueType)
            {
                var parameterDesc = VFXLibrary.GetParameters().First(o => VFXExpression.GetVFXValueTypeFromType(o.modelType) == type);
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
            foreach (var type in VFXTestCommon.s_supportedValueType)
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
            Assert.AreEqual(VFXTestCommon.s_supportedValueType.Length, exposedProperties.Count);
        }

        public struct VFXNullableTest
        {
            internal VFXValueType type;
            public override string ToString()
            {
                return type.ToString();
            }
        }

        private static bool IsTypeNullable(Type type)
        {
            if (!type.IsValueType)
                return true;
            if (Nullable.GetUnderlyingType(type) != null)
                return true;
            return false;
        }

#pragma warning disable 0414
        private static VFXNullableTest[] nullableTestCase = VFXTestCommon.s_supportedValueType.Where(o => IsTypeNullable(VFXValue.TypeToType(o))).Select(o => new VFXNullableTest() { type = o }).ToArray();
#pragma warning restore 0414

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
                case VFXValueType.CameraBuffer: vfx.SetTexture(name, (Texture)value); break;
                case VFXValueType.Boolean: vfx.SetBool(name, (bool)value); break;
                case VFXValueType.Matrix4x4: vfx.SetMatrix4x4(name, (Matrix4x4)value); break;
            }
        }

        [UnityTest]
        public IEnumerator Check_SetNullable_Throw_An_Exception_While_Using_Null([ValueSource("nullableTestCase")] VFXNullableTest valueType)
        {
            while (m_mainObject.GetComponent<VisualEffect>() != null)
                UnityEngine.Object.DestroyImmediate(m_mainObject.GetComponent<VisualEffect>());
            var vfx = m_mainObject.AddComponent<VisualEffect>();
            yield return null;
            Assert.Throws<System.ArgumentNullException>(() => fnSet_UsingBindings(valueType.type, vfx, "null", null));
        }
    }
}
#endif
