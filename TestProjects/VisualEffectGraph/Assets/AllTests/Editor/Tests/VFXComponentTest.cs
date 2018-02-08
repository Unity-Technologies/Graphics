using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using System.Linq;
using System.Collections;
using UnityEditor.VFX.Block.Test;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VisualEffectTest
    {
        GameObject m_cubeEmpty;
        GameObject m_sphereEmpty;
        GameObject m_mainObject;
        GameObject m_mainCamera;
        string m_pathTexture2D_A;
        string m_pathTexture2D_B;
        Texture2D m_texture2D_A;
        Texture2D m_texture2D_B;
        string m_pathTexture2DArray_A;
        string m_pathTexture2DArray_B;
        Texture2DArray m_texture2DArray_A;
        Texture2DArray m_texture2DArray_B;
        string m_pathTexture3D_A;
        string m_pathTexture3D_B;
        Texture3D m_texture3D_A;
        Texture3D m_texture3D_B;
        string m_pathTextureCube_A;
        string m_pathTextureCube_B;
        Cubemap m_textureCube_A;
        Cubemap m_textureCube_B;
        string m_pathTextureCubeArray_A;
        string m_pathTextureCubeArray_B;
        CubemapArray m_textureCubeArray_A;
        CubemapArray m_textureCubeArray_B;

        [OneTimeSetUp]
        public void Init()
        {
            m_cubeEmpty = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_sphereEmpty = GameObject.CreatePrimitive(PrimitiveType.Sphere);

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

            m_mainObject = new GameObject("TestObject");

            m_mainCamera = new GameObject();
            var camera = m_mainCamera.AddComponent<Camera>();
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(m_mainCamera.transform);
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            UnityEngine.Object.DestroyImmediate(m_mainObject);
            UnityEngine.Object.DestroyImmediate(m_cubeEmpty);
            UnityEngine.Object.DestroyImmediate(m_sphereEmpty);
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
        }

        [UnityTest]
        [Timeout(1000 * 10)]
        public IEnumerator CreateComponentAndCheckDimensionConstraint()
        {
            EditorApplication.ExecuteMenuItem("Window/Game");
            var graph = ScriptableObject.CreateInstance<VFXGraph>();

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
            var type = VFXValueType.kTexture2D;

            var targetTextureName = "exposed_test_tex2D";

            if (type != VFXValueType.kNone)
            {
                parameter.SetSettingValue("m_exposedName", targetTextureName);
                parameter.SetSettingValue("m_exposed", true);
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

            graph.vfxAsset = new VisualEffectAsset();
            graph.RecompileIfNeeded();

            while (m_mainObject.GetComponent<VisualEffect>() != null)
            {
                UnityEngine.Object.DestroyImmediate(m_mainObject.GetComponent<VisualEffect>());
            }
            var vfxComponent = m_mainObject.AddComponent<VisualEffect>();
            vfxComponent.vfxAsset = graph.vfxAsset;

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
        }

        private static bool[] linkModes = { true, false };
        private static bool[] bindingModes = { true, false };

        [UnityTest]
        [Timeout(1000 * 10)]
        public IEnumerator CreateComponentWithAllBasicTypeExposed([ValueSource("linkModes")] bool linkMode, [ValueSource("bindingModes")] bool bindingModes)
        {
            var commonBaseName = "abcd_";
            Func<VFXValueType, object> GetValue_A = delegate(VFXValueType type)
                {
                    switch (type)
                    {
                        case VFXValueType.kFloat: return 2.0f;
                        case VFXValueType.kFloat2: return new Vector2(3.0f, 4.0f);
                        case VFXValueType.kFloat3: return new Vector3(8.0f, 9.0f, 10.0f);
                        case VFXValueType.kFloat4: return new Vector4(11.0f, 12.0f, 13.0f, 14.0f);
                        case VFXValueType.kInt: return 15;
                        case VFXValueType.kUint: return 16u;
                        case VFXValueType.kCurve: return new AnimationCurve(new Keyframe(0, 13), new Keyframe(1, 14));
                        case VFXValueType.kColorGradient: return new Gradient() { colorKeys = new GradientColorKey[] { new GradientColorKey(Color.white, 0.2f) } };
                        case VFXValueType.kMesh: return m_cubeEmpty.GetComponent<MeshFilter>().sharedMesh;
                        case VFXValueType.kTexture2D: return m_texture2D_A;
                        case VFXValueType.kTexture2DArray: return m_texture2DArray_A;
                        case VFXValueType.kTexture3D: return m_texture3D_A;
                        case VFXValueType.kTextureCube: return m_textureCube_A;
                        case VFXValueType.kTextureCubeArray: return m_textureCubeArray_A;
                        case VFXValueType.kBool: return true;
                        case VFXValueType.kMatrix4x4: return Matrix4x4.identity;
                    }
                    Assert.Fail();
                    return null;
                };

            Func<VFXValueType, object> GetValue_B = delegate(VFXValueType type)
                {
                    switch (type)
                    {
                        case VFXValueType.kFloat: return 50.0f;
                        case VFXValueType.kFloat2: return new Vector2(53.0f, 54.0f);
                        case VFXValueType.kFloat3: return new Vector3(58.0f, 59.0f, 510.0f);
                        case VFXValueType.kFloat4: return new Vector4(511.0f, 512.0f, 513.0f, 514.0f);
                        case VFXValueType.kInt: return 515;
                        case VFXValueType.kUint: return 516u;
                        case VFXValueType.kCurve: return new AnimationCurve(new Keyframe(0, 47), new Keyframe(0.5f, 23), new Keyframe(1.0f, 17));
                        case VFXValueType.kColorGradient: return new Gradient() { colorKeys = new GradientColorKey[] { new GradientColorKey(Color.white, 0.2f), new GradientColorKey(Color.black, 0.6f) } };
                        case VFXValueType.kMesh: return m_sphereEmpty.GetComponent<MeshFilter>().sharedMesh;
                        case VFXValueType.kTexture2D: return m_texture2D_B;
                        case VFXValueType.kTexture2DArray: return m_texture2DArray_B;
                        case VFXValueType.kTexture3D: return m_texture3D_B;
                        case VFXValueType.kTextureCube: return m_textureCube_B;
                        case VFXValueType.kTextureCubeArray: return m_textureCubeArray_B;
                        case VFXValueType.kBool: return false;
                        case VFXValueType.kMatrix4x4: return Matrix4x4.LookAt(new Vector3(1, 2, 3), new Vector3(4, 5, 6), Vector3.up);
                    }
                    Assert.Fail();
                    return null;
                };

            Func<VFXValueType, VisualEffect, string, bool> fnHas_UsingBindings = delegate(VFXValueType type, VisualEffect vfx, string name)
                {
                    switch (type)
                    {
                        case VFXValueType.kFloat: return vfx.HasFloat(name);
                        case VFXValueType.kFloat2: return vfx.HasVector2(name);
                        case VFXValueType.kFloat3: return vfx.HasVector3(name);
                        case VFXValueType.kFloat4: return vfx.HasVector4(name);
                        case VFXValueType.kInt: return vfx.HasInt(name);
                        case VFXValueType.kUint: return vfx.HasUInt(name);
                        case VFXValueType.kCurve: return vfx.HasAnimationCurve(name);
                        case VFXValueType.kColorGradient: return vfx.HasGradient(name);
                        case VFXValueType.kMesh: return vfx.HasMesh(name);
                        case VFXValueType.kTexture2D: return vfx.HasTexture(name) && vfx.GetTextureDimension(name) == TextureDimension.Tex2D;
                        case VFXValueType.kTexture2DArray: return vfx.HasTexture(name) && vfx.GetTextureDimension(name) == TextureDimension.Tex2DArray;
                        case VFXValueType.kTexture3D: return vfx.HasTexture(name) && vfx.GetTextureDimension(name) == TextureDimension.Tex3D;
                        case VFXValueType.kTextureCube: return vfx.HasTexture(name) && vfx.GetTextureDimension(name) == TextureDimension.Cube;
                        case VFXValueType.kTextureCubeArray: return vfx.HasTexture(name) && vfx.GetTextureDimension(name) == TextureDimension.CubeArray;
                        case VFXValueType.kBool: return vfx.HasBool(name);
                        case VFXValueType.kMatrix4x4: return vfx.HasMatrix4x4(name);
                    }
                    Assert.Fail();
                    return false;
                };

            Func<VFXValueType, VisualEffect, string, object> fnGet_UsingBindings = delegate(VFXValueType type, VisualEffect vfx, string name)
                {
                    switch (type)
                    {
                        case VFXValueType.kFloat: return vfx.GetFloat(name);
                        case VFXValueType.kFloat2: return vfx.GetVector2(name);
                        case VFXValueType.kFloat3: return vfx.GetVector3(name);
                        case VFXValueType.kFloat4: return vfx.GetVector4(name);
                        case VFXValueType.kInt: return vfx.GetInt(name);
                        case VFXValueType.kUint: return vfx.GetUInt(name);
                        case VFXValueType.kCurve: return vfx.GetAnimationCurve(name);
                        case VFXValueType.kColorGradient: return vfx.GetGradient(name);
                        case VFXValueType.kMesh: return vfx.GetMesh(name);
                        case VFXValueType.kTexture2D:
                        case VFXValueType.kTexture2DArray:
                        case VFXValueType.kTexture3D:
                        case VFXValueType.kTextureCube:
                        case VFXValueType.kTextureCubeArray: return vfx.GetTexture(name);
                        case VFXValueType.kBool: return vfx.GetBool(name);
                        case VFXValueType.kMatrix4x4: return vfx.GetMatrix4x4(name);
                    }
                    Assert.Fail();
                    return null;
                };

            Action<VFXValueType, VisualEffect, string, object> fnSet_UsingBindings = delegate(VFXValueType type, VisualEffect vfx, string name, object value)
                {
                    switch (type)
                    {
                        case VFXValueType.kFloat: vfx.SetFloat(name, (float)value); break;
                        case VFXValueType.kFloat2: vfx.SetVector2(name, (Vector2)value); break;
                        case VFXValueType.kFloat3: vfx.SetVector3(name, (Vector3)value); break;
                        case VFXValueType.kFloat4: vfx.SetVector4(name, (Vector4)value); break;
                        case VFXValueType.kInt: vfx.SetInt(name, (int)value); break;
                        case VFXValueType.kUint: vfx.SetUInt(name, (uint)value); break;
                        case VFXValueType.kCurve: vfx.SetAnimationCurve(name, (AnimationCurve)value); break;
                        case VFXValueType.kColorGradient: vfx.SetGradient(name, (Gradient)value); break;
                        case VFXValueType.kMesh: vfx.SetMesh(name, (Mesh)value); break;
                        case VFXValueType.kTexture2D:
                        case VFXValueType.kTexture2DArray:
                        case VFXValueType.kTexture3D:
                        case VFXValueType.kTextureCube:
                        case VFXValueType.kTextureCubeArray: vfx.SetTexture(name, (Texture)value); break;
                        case VFXValueType.kBool: vfx.SetBool(name, (bool)value); break;
                        case VFXValueType.kMatrix4x4: vfx.SetMatrix4x4(name, (Matrix4x4)value); break;
                    }
                };


            Func<VFXValueType, VisualEffect, string, bool> fnHas_UsingSerializedProperty = delegate(VFXValueType type, VisualEffect vfx, string name)
                {
                    var editor = Editor.CreateEditor(vfx);
                    var propertySheet = editor.serializedObject.FindProperty("m_PropertySheet");
                    var fieldName = VisualEffectUtility.GetTypeField(VFXExpression.TypeToType(type)) + ".m_Array";
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
                    return false;
                };

            Func<SerializedProperty, Matrix4x4> fnMatrixFromSerializedProperty = delegate(SerializedProperty property)
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
                };

            Action<SerializedProperty, Matrix4x4> fnMatrixToSerializedProperty = delegate(SerializedProperty property, Matrix4x4 mat)
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
                };

            Func<VFXValueType, VisualEffect, string, object> fnGet_UsingSerializedProperty = delegate(VFXValueType type, VisualEffect vfx, string name)
                {
                    var editor = Editor.CreateEditor(vfx);
                    var propertySheet = editor.serializedObject.FindProperty("m_PropertySheet");
                    editor.serializedObject.Update();

                    var fieldName = VisualEffectUtility.GetTypeField(VFXExpression.TypeToType(type)) + ".m_Array";
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
                                    case VFXValueType.kFloat: return property.floatValue;
                                    case VFXValueType.kFloat2: return property.vector2Value;
                                    case VFXValueType.kFloat3: return property.vector3Value;
                                    case VFXValueType.kFloat4: return property.vector4Value;
                                    case VFXValueType.kInt: return property.intValue;
                                    case VFXValueType.kUint: return property.intValue; // there isn't uintValue
                                    case VFXValueType.kCurve: return property.animationCurveValue;
                                    case VFXValueType.kColorGradient: return property.gradientValue;
                                    case VFXValueType.kMesh: return property.objectReferenceValue;
                                    case VFXValueType.kTexture2D:
                                    case VFXValueType.kTexture2DArray:
                                    case VFXValueType.kTexture3D:
                                    case VFXValueType.kTextureCube:
                                    case VFXValueType.kTextureCubeArray: return property.objectReferenceValue;
                                    case VFXValueType.kBool: return property.boolValue;
                                    case VFXValueType.kMatrix4x4: return fnMatrixFromSerializedProperty(property);
                                }
                                Assert.Fail();
                            }
                        }
                    }
                    return null;
                };

            Action<VFXValueType, VisualEffect, string, object> fnSet_UsingSerializedProperty = delegate(VFXValueType type, VisualEffect vfx, string name, object value)
                {
                    var editor = Editor.CreateEditor(vfx);
                    editor.serializedObject.Update();

                    var propertySheet = editor.serializedObject.FindProperty("m_PropertySheet");
                    var fieldName = VisualEffectUtility.GetTypeField(VFXExpression.TypeToType(type)) + ".m_Array";
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
                                    case VFXValueType.kFloat: propertyValue.floatValue = (float)value; break;
                                    case VFXValueType.kFloat2: propertyValue.vector2Value = (Vector2)value; break;
                                    case VFXValueType.kFloat3: propertyValue.vector3Value = (Vector3)value; break;
                                    case VFXValueType.kFloat4: propertyValue.vector4Value = (Vector4)value; break;
                                    case VFXValueType.kInt: propertyValue.intValue = (int)value; break;
                                    case VFXValueType.kUint: propertyValue.intValue = (int)((uint)value); break; // there isn't uintValue
                                    case VFXValueType.kCurve: propertyValue.animationCurveValue = (AnimationCurve)value; break;
                                    case VFXValueType.kColorGradient: propertyValue.gradientValue = (Gradient)value; break;
                                    case VFXValueType.kMesh: propertyValue.objectReferenceValue = (UnityEngine.Object)value; break;
                                    case VFXValueType.kTexture2D:
                                    case VFXValueType.kTexture2DArray:
                                    case VFXValueType.kTexture3D:
                                    case VFXValueType.kTextureCube:
                                    case VFXValueType.kTextureCubeArray: propertyValue.objectReferenceValue = (UnityEngine.Object)value;   break;
                                    case VFXValueType.kBool: propertyValue.boolValue = (bool)value; break;
                                    case VFXValueType.kMatrix4x4: fnMatrixToSerializedProperty(propertyValue, (Matrix4x4)value); break;
                                }
                                propertyOverriden.boolValue = true;
                            }
                        }
                    }
                    editor.serializedObject.ApplyModifiedProperties();
                };

            Func<VFXValueType, VisualEffect, string, bool> fnHas = bindingModes ? fnHas_UsingBindings : fnHas_UsingSerializedProperty;
            Func<VFXValueType, VisualEffect, string, object> fnGet = bindingModes ? fnGet_UsingBindings : fnGet_UsingSerializedProperty;
            Action<VFXValueType, VisualEffect, string, object> fnSet = bindingModes ? fnSet_UsingBindings : fnSet_UsingSerializedProperty;

            EditorApplication.ExecuteMenuItem("Window/Game");
            var graph = ScriptableObject.CreateInstance<VFXGraph>();

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
                .Where(e => e != VFXValueType.kSpline
                    &&  e != VFXValueType.kNone).ToArray();
            foreach (var parameter in VFXLibrary.GetParameters())
            {
                var newInstance = parameter.CreateInstance();

                VFXValueType type = types.FirstOrDefault(e => newInstance.type == VFXExpression.TypeToType(e));
                if (type != VFXValueType.kNone)
                {
                    newInstance.SetSettingValue("m_exposedName", commonBaseName + type.ToString());
                    newInstance.SetSettingValue("m_exposed", true);
                    var value = GetValue_A(type);
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
                    Assert.IsNotNull(parameter);
                    slot.Link(parameter.GetOutputSlot(0));
                }
            }

            graph.vfxAsset = new VisualEffectAsset();
            graph.RecompileIfNeeded();

            while (m_mainObject.GetComponent<VisualEffect>() != null)
            {
                UnityEngine.Object.DestroyImmediate(m_mainObject.GetComponent<VisualEffect>());
            }
            var vfxComponent = m_mainObject.AddComponent<VisualEffect>();
            vfxComponent.vfxAsset = graph.vfxAsset;

            yield return null;

            Func<AnimationCurve, AnimationCurve, bool> fnCompareCurve = delegate(AnimationCurve left, AnimationCurve right)
                {
                    return left.keys.Length == right.keys.Length;
                };

            Func<Gradient, Gradient, bool> fnCompareGradient = delegate(Gradient left, Gradient right)
                {
                    return left.colorKeys.Length == right.colorKeys.Length;
                };

            //Check default Value_A & change to Value_B
            foreach (var type in types)
            {
                var currentName = commonBaseName + type.ToString();
                var baseValue = GetValue_A(type);
                var newValue = GetValue_B(type);

                Assert.IsTrue(fnHas(type, vfxComponent, currentName));
                var currentValue = fnGet(type, vfxComponent, currentName);
                if (type == VFXValueType.kColorGradient)
                {
                    Assert.IsTrue(fnCompareGradient((Gradient)baseValue, (Gradient)currentValue));
                }
                else if (type == VFXValueType.kCurve)
                {
                    Assert.IsTrue(fnCompareCurve((AnimationCurve)baseValue, (AnimationCurve)currentValue));
                }
                else
                {
                    Assert.AreEqual(baseValue, currentValue);
                }
                fnSet(type, vfxComponent, currentName, newValue);

                yield return null;
            }

            //Compare new setted values
            foreach (var type in types)
            {
                var currentName = commonBaseName + type.ToString();
                var baseValue = GetValue_B(type);
                Assert.IsTrue(fnHas(type, vfxComponent, currentName));

                var currentValue = fnGet(type, vfxComponent, currentName);
                if (type == VFXValueType.kColorGradient)
                {
                    Assert.IsTrue(fnCompareGradient((Gradient)baseValue, (Gradient)currentValue));
                }
                else if (type == VFXValueType.kCurve)
                {
                    Assert.IsTrue(fnCompareCurve((AnimationCurve)baseValue, (AnimationCurve)currentValue));
                }
                else
                {
                    Assert.AreEqual(baseValue, currentValue);
                }
                yield return null;
            }
        }
    }
}
