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
    [TestFixture]
    public class VFXComponentTest
    {
        GameObject m_cubeEmpty;
        GameObject m_sphereEmpty;
        GameObject m_mainObject;
        string m_pathTexture2D_A;
        string m_pathTexture2D_B;
        Texture2D m_texture2D_A;
        Texture2D m_texture2D_B;
        string m_pathTexture3D_A;
        string m_pathTexture3D_B;
        Texture3D m_texture3D_A;
        Texture3D m_texture3D_B;

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

            m_pathTexture3D_A = "Assets/texture3D_A.asset";
            m_pathTexture3D_B = "Assets/texture3D_B.asset";
            m_texture3D_A = new Texture3D(16, 16, 16, TextureFormat.ARGB32, false);
            m_texture3D_B = new Texture3D(8, 8, 8, TextureFormat.ARGB32, false);
            AssetDatabase.CreateAsset(m_texture3D_A, m_pathTexture3D_A);
            AssetDatabase.CreateAsset(m_texture3D_B, m_pathTexture3D_B);
            m_texture3D_A = AssetDatabase.LoadAssetAtPath<Texture3D>(m_pathTexture3D_A);
            m_texture3D_B = AssetDatabase.LoadAssetAtPath<Texture3D>(m_pathTexture3D_B);

            m_mainObject = new GameObject("TestObject");
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            UnityEngine.Object.DestroyImmediate(m_mainObject);
            UnityEngine.Object.DestroyImmediate(m_cubeEmpty);
            UnityEngine.Object.DestroyImmediate(m_sphereEmpty);
            AssetDatabase.DeleteAsset(m_pathTexture2D_A);
            AssetDatabase.DeleteAsset(m_pathTexture2D_B);
            AssetDatabase.DeleteAsset(m_pathTexture3D_A);
            AssetDatabase.DeleteAsset(m_pathTexture3D_B);
        }

        [UnityTest]
        [Timeout(1000 * 10)]
        public IEnumerator CreateComponentWithAllBasicTypeExposed()
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
                        case VFXValueType.kTexture3D: return m_texture3D_A;
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
                        case VFXValueType.kTexture3D: return m_texture3D_B;
                    }
                    Assert.Fail();
                    return null;
                };

            EditorApplication.ExecuteMenuItem("Window/Game");
            var graph = ScriptableObject.CreateInstance<VFXGraph>();

            var contextInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var allType = ScriptableObject.CreateInstance<VFXAllType>();

            contextInitialize.AddChild(allType);
            graph.AddChild(contextInitialize);

            var types = Enum.GetValues(typeof(VFXValueType)).Cast<VFXValueType>()
                .Where(e => e != VFXValueType.kSpline
                    &&  e != VFXValueType.kTransform
                    &&  e != VFXValueType.kNone).ToArray();
            foreach (var parameter in VFXLibrary.GetParameters())
            {
                var newInstance = parameter.CreateInstance();

                VFXValueType type = types.FirstOrDefault(e => newInstance.type == VFXExpression.TypeToType(e));
                if (type != VFXValueType.kNone)
                {
                    newInstance.exposedName = commonBaseName + type.ToString();
                    newInstance.exposed = true;
                    var value = GetValue_A(type);
                    Assert.IsNotNull(value);
                    newInstance.value = value;
                    graph.AddChild(newInstance);
                }
            }

            foreach (var type in types)
            {
                VFXSlot slot = null;
                for (int i = 0; i < allType.GetNbInputSlots(); ++i)
                {
                    var currentSlot = allType.GetInputSlot(i);
                    var expression = currentSlot.GetExpression();
                    if (expression != null && expression.ValueType == type)
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
                            if (expression != null && expression.ValueType == type)
                            {
                                return true;
                            }
                        }
                        return false;
                    });
                Assert.IsNotNull(parameter);
                slot.Link(parameter.GetOutputSlot(0));
            }

            graph.vfxAsset = new VFXAsset();
            graph.RecompileIfNeeded();
            graph.vfxAsset.bounds = new Bounds(Vector3.zero, Vector3.positiveInfinity);

            while (m_mainObject.GetComponent<VFXComponent>() != null)
            {
                UnityEngine.Object.DestroyImmediate(m_mainObject.GetComponent<VFXComponent>());
            }
            var vfxComponent = m_mainObject.AddComponent<VFXComponent>();
            vfxComponent.vfxAsset = graph.vfxAsset;

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

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

                if (type == VFXValueType.kFloat)
                {
                    Assert.IsTrue(vfxComponent.HasFloat(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetFloat(currentName));
                    vfxComponent.SetFloat(currentName, (float)newValue);
                }
                else if (type == VFXValueType.kFloat2)
                {
                    Assert.IsTrue(vfxComponent.HasVector2(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetVector2(currentName));
                    vfxComponent.SetVector2(currentName, (Vector2)newValue);
                }
                else if (type == VFXValueType.kFloat3)
                {
                    Assert.IsTrue(vfxComponent.HasVector3(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetVector3(currentName));
                    vfxComponent.SetVector3(currentName, (Vector3)newValue);
                }
                else if (type == VFXValueType.kFloat4)
                {
                    Assert.IsTrue(vfxComponent.HasVector4(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetVector4(currentName));
                    vfxComponent.SetVector4(currentName, (Vector4)newValue);
                }
                else if (type == VFXValueType.kInt)
                {
                    Assert.IsTrue(vfxComponent.HasInt(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetInt(currentName));
                    vfxComponent.SetInt(currentName, (int)newValue);
                }
                else if (type == VFXValueType.kUint)
                {
                    Assert.IsTrue(vfxComponent.HasUInt(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetUInt(currentName));
                    vfxComponent.SetUInt(currentName, (uint)newValue);
                }
                else if (type == VFXValueType.kCurve)
                {
                    Assert.IsTrue(vfxComponent.HasAnimationCurve(currentName));
                    Assert.IsTrue(fnCompareCurve(baseValue as AnimationCurve, vfxComponent.GetAnimationCurve(currentName)));
                    vfxComponent.SetAnimationCurve(currentName, newValue as AnimationCurve);
                }
                else if (type == VFXValueType.kColorGradient)
                {
                    Assert.IsTrue(vfxComponent.HasGradient(currentName));
                    Assert.IsTrue(fnCompareGradient(baseValue as Gradient, vfxComponent.GetGradient(currentName)));
                    vfxComponent.SetGradient(currentName, newValue as Gradient);
                }
                else if (type == VFXValueType.kMesh)
                {
                    Assert.IsTrue(vfxComponent.HasMesh(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetMesh(currentName));
                    vfxComponent.SetMesh(currentName, newValue as Mesh);
                }
                else if (type == VFXValueType.kTexture2D)
                {
                    Assert.IsTrue(vfxComponent.HasTexture2D(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetTexture2D(currentName));
                    vfxComponent.SetTexture2D(currentName, newValue as Texture2D);
                }
                else if (type == VFXValueType.kTexture3D)
                {
                    Assert.IsTrue(vfxComponent.HasTexture3D(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetTexture3D(currentName));
                    vfxComponent.SetTexture3D(currentName, newValue as Texture3D);
                }
                else
                {
                    Assert.Fail();
                }

                yield return null;
            }

            //Compare new setted values
            foreach (var type in types)
            {
                var currentName = commonBaseName + type.ToString();
                var baseValue = GetValue_B(type);

                if (type == VFXValueType.kFloat)
                {
                    Assert.IsTrue(vfxComponent.HasFloat(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetFloat(currentName));
                }
                else if (type == VFXValueType.kFloat2)
                {
                    Assert.IsTrue(vfxComponent.HasVector2(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetVector2(currentName));
                }
                else if (type == VFXValueType.kFloat3)
                {
                    Assert.IsTrue(vfxComponent.HasVector3(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetVector3(currentName));
                }
                else if (type == VFXValueType.kFloat4)
                {
                    Assert.IsTrue(vfxComponent.HasVector4(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetVector4(currentName));
                }
                else if (type == VFXValueType.kInt)
                {
                    Assert.IsTrue(vfxComponent.HasInt(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetInt(currentName));
                }
                else if (type == VFXValueType.kUint)
                {
                    Assert.IsTrue(vfxComponent.HasUInt(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetUInt(currentName));
                }
                else if (type == VFXValueType.kCurve)
                {
                    Assert.IsTrue(vfxComponent.HasAnimationCurve(currentName));
                    Assert.IsTrue(fnCompareCurve(baseValue as AnimationCurve, vfxComponent.GetAnimationCurve(currentName)));
                }
                else if (type == VFXValueType.kColorGradient)
                {
                    Assert.IsTrue(vfxComponent.HasGradient(currentName));
                    Assert.IsTrue(fnCompareGradient(baseValue as Gradient, vfxComponent.GetGradient(currentName)));
                }
                else if (type == VFXValueType.kMesh)
                {
                    Assert.IsTrue(vfxComponent.HasMesh(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetMesh(currentName));
                }
                else if (type == VFXValueType.kTexture2D)
                {
                    Assert.IsTrue(vfxComponent.HasTexture2D(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetTexture2D(currentName));
                }
                else if (type == VFXValueType.kTexture3D)
                {
                    Assert.IsTrue(vfxComponent.HasTexture3D(currentName));
                    Assert.AreEqual(baseValue, vfxComponent.GetTexture3D(currentName));
                }
                else
                {
                    Assert.Fail();
                }
                yield return null;
            }
        }
    }
}
