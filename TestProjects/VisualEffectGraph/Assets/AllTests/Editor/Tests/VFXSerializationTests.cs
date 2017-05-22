using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEditor.VFX;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSerializationTests
    {
        private readonly static string kTestAssetDir = "Assets/VFXEditor/Editor/Tests";
        private readonly static string kTestAssetName = "TestAsset";
        private readonly static string kTestAssetPath = kTestAssetDir + "/" + kTestAssetName + ".asset";

        [OneTimeSetUpAttribute]
        public void OneTimeSetUpAttribute()
        {
            string[] guids = AssetDatabase.FindAssets(kTestAssetName, new string[] { kTestAssetDir });

            // If the asset does not exist, create it
            if (guids.Length == 0)
            {
                VFXAsset asset = new VFXAsset();
                InitAsset(asset);
                AssetDatabase.CreateAsset(asset, kTestAssetPath);
                asset.UpdateSubAssets();
            }
        }

        [Test]
        public void SerializeModel()
        {
            VFXAsset assetSrc = new VFXAsset();
            VFXAsset assetDst = new VFXAsset();

            InitAsset(assetSrc);
            EditorUtility.CopySerialized(assetSrc, assetDst);
            CheckAsset(assetDst);

            Object.DestroyImmediate(assetSrc);
            Object.DestroyImmediate(assetDst);
        }

        [Test]
        public void LoadAssetFromPath()
        {
            VFXAsset asset = AssetDatabase.LoadAssetAtPath<VFXAsset>(kTestAssetPath);
            CheckAsset(asset);
        }

        private void InitAsset(VFXAsset asset)
        {
            var graph = asset.GetOrCreateGraph();
            graph.RemoveAllChildren();

            VFXSystem system0 = ScriptableObject.CreateInstance<VFXSystem>();
            system0.AddChild(ScriptableObject.CreateInstance<VFXBasicInitialize>());
            system0.AddChild(ScriptableObject.CreateInstance<VFXBasicUpdate>());
            system0.AddChild(ScriptableObject.CreateInstance<VFXBasicOutput>());

            VFXSystem system1 = ScriptableObject.CreateInstance<VFXSystem>();
            system1.AddChild(ScriptableObject.CreateInstance<VFXBasicInitialize>());
            system1.AddChild(ScriptableObject.CreateInstance<VFXBasicOutput>());

            // Add some block
            var block0 = ScriptableObject.CreateInstance<VFXInitBlockTest>();
            var block1 = ScriptableObject.CreateInstance<VFXUpdateBlockTest>();
            var block2 = ScriptableObject.CreateInstance<VFXOutputBlockTest>();

            // Add some operator
            VFXOperator add = ScriptableObject.CreateInstance<VFXOperatorAdd>();

            system0[0].AddChild(block0);
            system0[1].AddChild(block1);
            system0[2].AddChild(block2);

            graph.AddChild(system0);
            graph.AddChild(system1);
            graph.AddChild(add);
        }

        private void CheckAsset(VFXAsset asset)
        {
            VFXGraph graph = asset.GetOrCreateGraph();

            Assert.AreEqual(3, graph.GetNbChildren());

            Assert.AreEqual(3, graph[0].GetNbChildren());
            Assert.AreEqual(2, graph[1].GetNbChildren());
            Assert.AreEqual(0, graph[2].GetNbChildren());

            Assert.IsNotNull(((VFXSystem)(graph[0])).GetChild(0));
            Assert.IsNotNull(((VFXSystem)(graph[0])).GetChild(1));
            Assert.IsNotNull(((VFXSystem)(graph[0])).GetChild(2));
            Assert.IsNotNull(((VFXSystem)(graph[1])).GetChild(0));
            Assert.IsNotNull(((VFXSystem)(graph[1])).GetChild(1));

            Assert.AreEqual(VFXContextType.kInit, ((VFXSystem)(graph[0])).GetChild(0).contextType);
            Assert.AreEqual(VFXContextType.kUpdate, ((VFXSystem)(graph[0])).GetChild(1).contextType);
            Assert.AreEqual(VFXContextType.kOutput, ((VFXSystem)(graph[0])).GetChild(2).contextType);
            Assert.AreEqual(VFXContextType.kInit, ((VFXSystem)(graph[1])).GetChild(0).contextType);
            Assert.AreEqual(VFXContextType.kOutput, ((VFXSystem)(graph[1])).GetChild(1).contextType);

            Assert.IsNotNull(((VFXSystem)(graph[0])).GetChild(0).GetChild(0));
            Assert.IsNotNull(((VFXSystem)(graph[0])).GetChild(1).GetChild(0));
            Assert.IsNotNull(((VFXSystem)(graph[0])).GetChild(2).GetChild(0));
            Assert.IsNotNull((VFXOperatorAdd)graph[2]);
        }

        private void CheckIsolatedOperatorAdd(VFXOperatorAdd add)
        {
            Assert.AreEqual(1, add.outputSlots.Count);
            Assert.AreEqual(2, add.inputSlots.Count);
            Assert.AreEqual(typeof(FloatN), add.inputSlots[0].property.type);
            Assert.AreEqual(typeof(FloatN), add.inputSlots[1].property.type);
            Assert.AreEqual(typeof(float), add.outputSlots[0].property.type);
            Assert.IsNotNull(add.outputSlots[0].GetExpression());
            Assert.IsNotNull(add.outputSlots[0].GetExpression() as VFXExpressionAdd);
        }

        private void CheckIsolatedOperatorAbs(VFXOperatorAbs add)
        {
            Assert.AreEqual(1, add.outputSlots.Count);
            Assert.AreEqual(1, add.inputSlots.Count);
            Assert.AreEqual(typeof(FloatN), add.inputSlots[0].property.type);
            Assert.AreEqual(typeof(float), add.outputSlots[0].property.type);
            Assert.IsNotNull(add.outputSlots[0].GetExpression());
            Assert.IsNotNull(add.outputSlots[0].GetExpression() as VFXExpressionAbs);
        }

        private void CheckConnectedAbs(VFXOperatorAbs abs)
        {
            Assert.IsTrue(abs.inputSlots[0].HasLink());
            Assert.AreEqual(1, abs.inputSlots[0].LinkedSlots.Count);
            Assert.IsTrue(abs.inputSlots[0].GetExpression() is VFXExpressionAdd);
        }

        private void InnerSaveAndReloadTest(string suffixname, Action<VFXAsset> write, Action<VFXAsset> read)
        {
            var kTempAssetPathA = string.Format("{0}/Temp_{1}_A.asset", kTestAssetDir, suffixname);
            var kTempAssetPathB = string.Format("{0}/Temp_{1}_B.asset", kTestAssetDir, suffixname);
            AssetDatabase.DeleteAsset(kTempAssetPathA);
            AssetDatabase.DeleteAsset(kTempAssetPathB);

            int hashCodeAsset = 0; //check reference are different between load & reload
            {
                var asset = new VFXAsset();
                hashCodeAsset = asset.GetHashCode();

                write(asset);

                AssetDatabase.CreateAsset(asset, kTempAssetPathA);
                asset.UpdateSubAssets();

                AssetDatabase.SaveAssets();

                asset = null;
                EditorUtility.UnloadUnusedAssetsImmediate();
                AssetDatabase.CopyAsset(kTempAssetPathA, kTempAssetPathB);
                AssetDatabase.RemoveObject(asset);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            EditorUtility.UnloadUnusedAssetsImmediate();
            {
                VFXAsset asset = AssetDatabase.LoadAssetAtPath<VFXAsset>(kTempAssetPathB);
                Assert.AreNotEqual(hashCodeAsset, asset.GetHashCode());

                read(asset);
            }
            AssetDatabase.DeleteAsset(kTempAssetPathA);
            AssetDatabase.DeleteAsset(kTempAssetPathB);
        }

        private void WriteBasicOperators(VFXAsset asset, bool spawnAbs, bool linkAbs)
        {
            var add = ScriptableObject.CreateInstance<VFXOperatorAdd>();
            var graph = asset.GetOrCreateGraph();
            graph.AddChild(add);

            CheckIsolatedOperatorAdd(add);

            if (spawnAbs)
            {
                var abs = ScriptableObject.CreateInstance<VFXOperatorAbs>();
                abs.position = new Vector2(64.0f, 64.0f);
                graph.AddChild(abs);
                CheckIsolatedOperatorAbs(abs);
                if (linkAbs)
                {
                    abs.inputSlots[0].Link(add.outputSlots[0]);
                    CheckConnectedAbs(abs);
                }
            }
        }

        private void ReadBasicOperators(VFXAsset asset, bool spawnAbs, bool linkAbs)
        {
            var graph = asset.GetOrCreateGraph();
            Assert.AreEqual(spawnAbs ? 2 : 1, graph.GetNbChildren());
            Assert.IsNotNull((VFXOperatorAdd)graph[0]);
            var add = (VFXOperatorAdd)graph[0];
            CheckIsolatedOperatorAdd(add);

            if (spawnAbs)
            {
                Assert.IsNotNull((VFXOperatorAbs)graph[1]);
                var abs = (VFXOperatorAbs)graph[1];
                CheckIsolatedOperatorAbs(abs);
                Assert.AreEqual(abs.position.x, 64.0f);
                Assert.AreEqual(abs.position.y, 64.0f);
                if (linkAbs)
                {
                    CheckConnectedAbs(abs);
                }
            }
        }

        private void BasicOperatorTest(string suffix, bool spawnAbs, bool linkAbs)
        {
            InnerSaveAndReloadTest(suffix,
                (a) => WriteBasicOperators(a, spawnAbs, linkAbs),
                (a) => ReadBasicOperators(a, spawnAbs, linkAbs));
        }

        [Test]
        public void SerializeOneOperator()
        {
            BasicOperatorTest("One", false, false);
        }

        [Test]
        public void SerializeTwoOperators()
        {
            BasicOperatorTest("Two", true, false);
        }

        [Test]
        public void SerializeTwoOperatorsLink()
        {
            BasicOperatorTest("TwoLinked", true, true);
        }

        [Test]
        public void SerializeOperatorMaskWithState()
        {
            string expectedValue = "xyx";
            Action<VFXAsset> write = delegate(VFXAsset asset)
                {
                    var mask = ScriptableObject.CreateInstance<VFXOperatorComponentMask>();
                    mask.settings = new VFXOperatorComponentMask.Settings()
                    {
                        mask = expectedValue
                    };
                    asset.GetOrCreateGraph().AddChild(mask);
                    Assert.AreEqual(expectedValue, (mask.settings as VFXOperatorComponentMask.Settings).mask);
                };

            Action<VFXAsset> read = delegate(VFXAsset asset)
                {
                    var graph = asset.GetOrCreateGraph();
                    Assert.AreEqual(1, graph.GetNbChildren());
                    Assert.IsInstanceOf(typeof(VFXOperatorComponentMask), graph[0]);
                    var mask = graph[0] as VFXOperatorComponentMask;
                    Assert.IsInstanceOf(typeof(VFXOperatorComponentMask.Settings), mask.settings);
                    Assert.AreEqual(expectedValue, (mask.settings as VFXOperatorComponentMask.Settings).mask);
                };

            InnerSaveAndReloadTest("Mask", write, read);
        }

        [Test]
        public void SerializeParameter()
        {
            var name = "unity";
            Action<VFXAsset> write = delegate(VFXAsset asset)
                {
                    var parameter = VFXLibrary.GetParameters().First(o => o.name == "Vector2").CreateInstance();
                    parameter.exposed = true;
                    parameter.exposedName = name;
                    asset.GetOrCreateGraph().AddChild(parameter);
                    Assert.AreEqual(VFXValueType.kFloat2, parameter.outputSlots[0].GetExpression().ValueType);
                };

            Action<VFXAsset> read = delegate(VFXAsset asset)
                {
                    var parameter = asset.GetOrCreateGraph()[0] as VFXParameter;
                    Assert.AreNotEqual(null, parameter);
                    Assert.AreEqual(true, parameter.exposed);
                    Assert.AreEqual(parameter.exposedName, name);
                    Assert.AreEqual(VFXValueType.kFloat2, parameter.outputSlots[0].GetExpression().ValueType);
                };

            InnerSaveAndReloadTest("Parameter", write, read);
        }

        [Test]
        public void SerializeOperatorAndParameter()
        {
            Action<VFXAsset> write = delegate(VFXAsset asset)
                {
                    var graph = asset.GetOrCreateGraph();
                    var add = ScriptableObject.CreateInstance<VFXOperatorAdd>();
                    var parameter = VFXLibrary.GetParameters().First(o => o.name == "Vector2").CreateInstance();
                    graph.AddChild(add);
                    graph.AddChild(parameter);
                    add.inputSlots[0].Link(parameter.outputSlots[0]);

                    Assert.AreEqual(VFXValueType.kFloat2, add.outputSlots[0].GetExpression().ValueType);
                };

            Action<VFXAsset> read = delegate(VFXAsset asset)
                {
                    var graph = asset.GetOrCreateGraph();
                    var add = graph[0] as VFXOperatorAdd;
                    var parameter = graph[1] as VFXParameter;
                    Assert.AreNotEqual(null, parameter);
                    Assert.AreEqual(VFXValueType.kFloat2, add.outputSlots[0].GetExpression().ValueType);
                };

            InnerSaveAndReloadTest("ParameterAndOperator", write, read);
        }

        [Test]
        public void SerializeBuiltInParameter()
        {
            Action<VFXAsset> write = delegate(VFXAsset asset)
                {
                    var builtIn = VFXLibrary.GetBuiltInParameters().First(o => o.name == VFXExpressionOp.kVFXTotalTimeOp.ToString()).CreateInstance();
                    asset.GetOrCreateGraph().AddChild(builtIn);
                    Assert.AreEqual(VFXExpressionOp.kVFXTotalTimeOp, builtIn.outputSlots[0].GetExpression().Operation);
                };

            Action<VFXAsset> read = delegate(VFXAsset asset)
                {
                    var builtIn = asset.GetOrCreateGraph()[0] as VFXBuiltInParameter;
                    Assert.AreNotEqual(null, builtIn);
                    Assert.AreEqual(VFXExpressionOp.kVFXTotalTimeOp, builtIn.outputSlots[0].GetExpression().Operation);
                };
            InnerSaveAndReloadTest("BuiltInParameter", write, read);
        }

        [Test]
        public void SerializeOperatorAndBuiltInParameter()
        {
            Action<VFXAsset> write = delegate(VFXAsset asset)
                {
                    var graph = asset.GetOrCreateGraph();
                    var add = ScriptableObject.CreateInstance<VFXOperatorAdd>();
                    var builtIn = VFXLibrary.GetBuiltInParameters().First(o => o.name == VFXExpressionOp.kVFXTotalTimeOp.ToString()).CreateInstance();
                    graph.AddChild(builtIn);
                    graph.AddChild(add);
                    add.inputSlots[0].Link(builtIn.outputSlots[0]);

                    Assert.AreEqual(VFXExpressionOp.kVFXTotalTimeOp, builtIn.outputSlots[0].GetExpression().Operation);
                    Assert.IsTrue(add.inputSlots[0].HasLink());
                };

            Action<VFXAsset> read = delegate(VFXAsset asset)
                {
                    var graph = asset.GetOrCreateGraph();
                    var builtIn = graph[0] as VFXBuiltInParameter;
                    var add = graph[1] as VFXOperatorAdd;

                    Assert.AreNotEqual(null, builtIn);
                    Assert.AreNotEqual(null, add);
                    Assert.AreEqual(VFXExpressionOp.kVFXTotalTimeOp, builtIn.outputSlots[0].GetExpression().Operation);
                    Assert.IsTrue(add.inputSlots[0].HasLink());
                };
            InnerSaveAndReloadTest("BuiltInParameter", write, read);
        }

        [Test]
        public void SerializeAttributeParameter()
        {
            var testAttribute = "size";
            Action<VFXAttributeParameter> test = delegate(VFXAttributeParameter parameter)
                {
                    Assert.AreEqual(VFXExpressionOp.kVFXNoneOp, parameter.outputSlots[0].GetExpression().Operation);
                    Assert.AreEqual(VFXValueType.kFloat2, parameter.outputSlots[0].GetExpression().ValueType);
                    Assert.IsInstanceOf(typeof(VFXAttributeExpression), parameter.outputSlots[0].GetExpression());
                    Assert.AreEqual(testAttribute, (parameter.outputSlots[0].GetExpression() as VFXAttributeExpression).attributeName);
                };

            Action<VFXAsset> write = delegate(VFXAsset asset)
                {
                    var size = VFXLibrary.GetAttributeParameters().First(o => o.name == testAttribute).CreateInstance();
                    asset.GetOrCreateGraph().AddChild(size);
                    test(size);
                };

            Action<VFXAsset> read = delegate(VFXAsset asset)
                {
                    var size = asset.GetOrCreateGraph()[0] as VFXAttributeParameter;
                    Assert.AreNotEqual(null, size);
                    test(size);
                };
            InnerSaveAndReloadTest("AttributeParameter", write, read);
        }
    }
}
