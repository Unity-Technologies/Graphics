#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.VFX.Block.Test;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;


using Object = UnityEngine.Object;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSerializationTests
    {
        private readonly static string kTestAssetDir = "Assets/Tests";
        private readonly static string kTestAssetName = "TestAsset";
        private readonly static string kTestAssetPath = kTestAssetDir + "/" + kTestAssetName + ".vfx";

        private VisualEffectAsset CreateAssetAtPath(string path)
        {
            return VisualEffectAssetEditorUtility.CreateNewAsset(path);
        }

        [OneTimeSetUpAttribute]
        public void OneTimeSetUpAttribute()
        {
            string[] guids = AssetDatabase.FindAssets(kTestAssetName, new string[] { kTestAssetDir });

            // If the asset does not exist, create it
            if (guids.Length == 0)
            {
                VisualEffectAsset asset = CreateAssetAtPath(kTestAssetPath);
                InitAsset(asset);
            }
        }

        /*
        [Test]
        public void SerializeModel()
        {
            VisualEffectAsset assetSrc = new VisualEffectAsset();
            VisualEffectAsset assetDst = new VisualEffectAsset();

            InitAsset(assetSrc);
            EditorUtility.CopySerialized(assetSrc, assetDst);
            CheckAsset(assetDst);

            Object.DestroyImmediate(assetSrc);
            Object.DestroyImmediate(assetDst);
        }*/

        [Test]
        public void LoadAssetFromPath()
        {
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(kTestAssetPath);
            CheckAsset(asset);
        }

        private void InitAsset(VisualEffectAsset asset)
        {
            VisualEffectResource resource = asset.GetResource();
            var graph = resource.GetOrCreateGraph();
            graph.RemoveAllChildren();

            var init0 = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var update0 = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var output0 = ScriptableObject.CreateInstance<VFXPointOutput>();

            graph.AddChild(init0);
            graph.AddChild(update0);
            graph.AddChild(output0);

            init0.LinkTo(update0);
            update0.LinkTo(output0);

            var init1 = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var output1 = ScriptableObject.CreateInstance<VFXPointOutput>();

            init1.LinkTo(output1);

            graph.AddChild(init1);
            graph.AddChild(output1);

            // Add some block
            var block0 = ScriptableObject.CreateInstance<InitBlockTest>();
            var block1 = ScriptableObject.CreateInstance<UpdateBlockTest>();
            var block2 = ScriptableObject.CreateInstance<OutputBlockTest>();

            // Add some operator
            VFXOperator add = ScriptableObject.CreateInstance<Operator.Add>();

            init0.AddChild(block0);
            update0.AddChild(block1);
            output0.AddChild(block2);

            graph.AddChild(add);
        }

        private void CheckAsset(VisualEffectAsset asset)
        {
            VisualEffectResource resource = asset.GetResource();
            var graph = resource.GetOrCreateGraph();

            Assert.AreEqual(6, graph.GetNbChildren());

            Assert.AreEqual(1, graph[0].GetNbChildren());
            Assert.AreEqual(1, graph[1].GetNbChildren());
            Assert.AreEqual(1, graph[2].GetNbChildren());
            Assert.AreEqual(0, graph[3].GetNbChildren());
            Assert.AreEqual(0, graph[4].GetNbChildren());
            Assert.AreEqual(0, graph[5].GetNbChildren());

            Assert.IsNotNull((graph[0])[0]);
            Assert.IsNotNull((graph[1])[0]);
            Assert.IsNotNull((graph[2])[0]);

            Assert.AreEqual(VFXContextType.Init,   ((VFXContext)(graph[0])).contextType);
            Assert.AreEqual(VFXContextType.Update, ((VFXContext)(graph[1])).contextType);
            Assert.AreEqual(VFXContextType.Output, ((VFXContext)(graph[2])).contextType);
            Assert.AreEqual(VFXContextType.Init,   ((VFXContext)(graph[3])).contextType);
            Assert.AreEqual(VFXContextType.Output, ((VFXContext)(graph[4])).contextType);

            Assert.IsNotNull(graph[5] as Operator.Add);
        }

        private void CheckIsolatedOperatorAdd(Operator.Add add)
        {
            Assert.AreEqual(1, add.outputSlots.Count);
            Assert.AreEqual(2, add.inputSlots.Count);
            Assert.AreEqual(typeof(float), add.inputSlots[0].property.type);
            Assert.AreEqual(typeof(float), add.inputSlots[1].property.type);
            Assert.AreEqual(typeof(float), add.outputSlots[0].property.type);
            Assert.IsNotNull(add.outputSlots[0].GetExpression());
            Assert.IsNotNull(add.outputSlots[0].GetExpression() as VFXExpressionAdd);
        }

        private void CheckIsolatedOperatorAbs(Operator.Absolute add)
        {
            Assert.AreEqual(1, add.outputSlots.Count);
            Assert.AreEqual(1, add.inputSlots.Count);
            Assert.AreEqual(typeof(float), add.inputSlots[0].property.type);
            Assert.AreEqual(typeof(float), add.outputSlots[0].property.type);
            Assert.IsNotNull(add.outputSlots[0].GetExpression());
            Assert.IsNotNull(add.outputSlots[0].GetExpression() as VFXExpressionAbs);
        }

        private void CheckConnectedAbs(Operator.Absolute abs)
        {
            Assert.IsTrue(abs.inputSlots[0].HasLink());
            Assert.AreEqual(1, abs.inputSlots[0].LinkedSlots.Count());
            Assert.IsTrue(abs.inputSlots[0].GetExpression() is VFXExpressionAdd);
        }

        private void InnerSaveAndReloadTest(string suffixname, Action<VisualEffectAsset> write, Action<VisualEffectAsset> read)
        {
            var kTempAssetPathA = string.Format("{0}/Temp_{1}_A.vfx", kTestAssetDir, suffixname);
            var kTempAssetPathB = string.Format("{0}/Temp_{1}_B.vfx", kTestAssetDir, suffixname);
            AssetDatabase.DeleteAsset(kTempAssetPathA);
            AssetDatabase.DeleteAsset(kTempAssetPathB);

            int hashCodeAsset = 0; //check reference are different between load & reload
            {
                var asset = CreateAssetAtPath(kTempAssetPathA);

                hashCodeAsset = asset.GetHashCode();

                write(asset);
                asset.GetResource().UpdateSubAssets();

                AssetDatabase.SaveAssets();

                asset = null;
                EditorUtility.UnloadUnusedAssetsImmediate();
                AssetDatabase.CopyAsset(kTempAssetPathA, kTempAssetPathB);
                if (asset != null)
                    AssetDatabase.RemoveObjectFromAsset(asset);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            EditorUtility.UnloadUnusedAssetsImmediate();
            {
                VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(kTempAssetPathB);
                Assert.AreNotEqual(hashCodeAsset, asset.GetHashCode());

                read(asset);
            }
            AssetDatabase.DeleteAsset(kTempAssetPathA);
            AssetDatabase.DeleteAsset(kTempAssetPathB);
        }

        private void WriteBasicOperators(VisualEffectAsset asset, bool spawnAbs, bool linkAbs)
        {
            var add = ScriptableObject.CreateInstance<Operator.Add>();
            VisualEffectResource resource = asset.GetResource();
            var graph = resource.GetOrCreateGraph();
            graph.AddChild(add);

            CheckIsolatedOperatorAdd(add);

            if (spawnAbs)
            {
                var abs = ScriptableObject.CreateInstance<Operator.Absolute>();
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

        private void ReadBasicOperators(VisualEffectAsset asset, bool spawnAbs, bool linkAbs)
        {
            VisualEffectResource resource = asset.GetResource();
            var graph = resource.GetOrCreateGraph();
            Assert.AreEqual(spawnAbs ? 2 : 1, graph.GetNbChildren());
            Assert.IsNotNull((Operator.Add)graph[0]);
            var add = (Operator.Add)graph[0];
            CheckIsolatedOperatorAdd(add);

            if (spawnAbs)
            {
                Assert.IsNotNull((Operator.Absolute)graph[1]);
                var abs = (Operator.Absolute)graph[1];
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
            var expectedValue = "xyxy";
            Action<VisualEffectAsset> write = delegate(VisualEffectAsset asset)
            {
                var mask = ScriptableObject.CreateInstance<Operator.Swizzle>();
                mask.SetSettingValue("mask", expectedValue);
                asset.GetResource().GetOrCreateGraph().AddChild(mask);
                Assert.AreEqual(expectedValue, mask.mask);
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var graph = asset.GetResource().GetOrCreateGraph();
                Assert.AreEqual(1, graph.GetNbChildren());
                Assert.IsInstanceOf(typeof(Operator.Swizzle), graph[0]);
                var mask = graph[0] as Operator.Swizzle;

                Assert.AreEqual(expectedValue, mask.mask);
            };

            InnerSaveAndReloadTest("Mask", write, read);
        }

        [Test]
        public void SerializeParameter()
        {
            var name = "unity";
            Action<VisualEffectAsset> write = delegate(VisualEffectAsset asset)
            {
                var parameter = VFXLibrary.GetParameters().First(o => o.name == "Vector2").CreateInstance();
                parameter.SetSettingValue("m_Exposed", true);
                parameter.SetSettingValue("m_ExposedName", name);
                asset.GetResource().GetOrCreateGraph().AddChild(parameter);
                Assert.AreEqual(VFXValueType.Float2, parameter.outputSlots[0].GetExpression().valueType);
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var parameter = asset.GetResource().GetOrCreateGraph()[0] as VFXParameter;
                Assert.AreNotEqual(null, parameter);
                Assert.AreEqual(true, parameter.exposed);
                Assert.AreEqual(parameter.exposedName, name);
                Assert.AreEqual(VFXValueType.Float2, parameter.outputSlots[0].GetExpression().valueType);
            };

            InnerSaveAndReloadTest("Parameter", write, read);
        }

        [Test]
        public void SerializeOperatorAndParameter()
        {
            Action<VisualEffectAsset> write = delegate(VisualEffectAsset asset)
            {
                var graph = asset.GetResource().GetOrCreateGraph();
                var add = ScriptableObject.CreateInstance<Operator.Add>();
                var parameter = VFXLibrary.GetParameters().First(o => o.name == "Vector2").CreateInstance();
                add.SetOperandType(0, typeof(Vector2));
                graph.AddChild(add);
                graph.AddChild(parameter);
                add.inputSlots[0].Link(parameter.outputSlots[0]);

                Assert.AreEqual(VFXValueType.Float2, add.outputSlots[0].GetExpression().valueType);
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var graph = asset.GetResource().GetOrCreateGraph();
                var add = graph[0] as Operator.Add;
                var parameter = graph[1] as VFXParameter;
                Assert.AreNotEqual(null, parameter);
                Assert.AreEqual(VFXValueType.Float2, add.outputSlots[0].GetExpression().valueType);
            };

            InnerSaveAndReloadTest("ParameterAndOperator", write, read);
        }

        [Test]
        public void SerializeBuiltInParameter()
        {
            Action<VisualEffectAsset> write = delegate(VisualEffectAsset asset)
            {
                var builtIn = VFXLibrary.GetOperators().First(o => o.name == VFXExpressionOperation.TotalTime.ToString()).CreateInstance();
                asset.GetResource().GetOrCreateGraph().AddChild(builtIn);
                Assert.AreEqual(VFXExpressionOperation.TotalTime, builtIn.outputSlots[0].GetExpression().operation);
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var builtIn = asset.GetResource().GetOrCreateGraph()[0] as VFXBuiltInParameter;
                Assert.AreNotEqual(null, builtIn);
                Assert.AreEqual(VFXExpressionOperation.TotalTime, builtIn.outputSlots[0].GetExpression().operation);
            };
            InnerSaveAndReloadTest("BuiltInParameter", write, read);
        }

        [Test]
        public void SerializeOperatorAndBuiltInParameter()
        {
            Action<VisualEffectAsset> write = delegate(VisualEffectAsset asset)
            {
                var graph = asset.GetResource().GetOrCreateGraph();
                var add = ScriptableObject.CreateInstance<Operator.Add>();
                var builtIn = VFXLibrary.GetOperators().First(o => o.name == VFXExpressionOperation.TotalTime.ToString()).CreateInstance();
                graph.AddChild(builtIn);
                graph.AddChild(add);
                add.inputSlots[0].Link(builtIn.outputSlots[0]);

                Assert.AreEqual(VFXExpressionOperation.TotalTime, builtIn.outputSlots[0].GetExpression().operation);
                Assert.IsTrue(add.inputSlots[0].HasLink());
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var graph = asset.GetResource().GetOrCreateGraph();
                var builtIn = graph[0] as VFXBuiltInParameter;
                var add = graph[1] as Operator.Add;

                Assert.AreNotEqual(null, builtIn);
                Assert.AreNotEqual(null, add);
                Assert.AreEqual(VFXExpressionOperation.TotalTime, builtIn.outputSlots[0].GetExpression().operation);
                Assert.IsTrue(add.inputSlots[0].HasLink());
            };
            InnerSaveAndReloadTest("BuiltInParameter", write, read);
        }

        [Test]
        public void SerializeAttributeParameter()
        {
            var testAttribute = "lifetime";
            Action<VFXAttributeParameter, VFXAttributeLocation> test = delegate(VFXAttributeParameter parameter, VFXAttributeLocation location)
            {
                Assert.AreEqual(VFXExpressionOperation.None, parameter.outputSlots[0].GetExpression().operation);
                Assert.AreEqual(VFXValueType.Float, parameter.outputSlots[0].GetExpression().valueType);
                Assert.IsInstanceOf(typeof(VFXAttributeExpression), parameter.outputSlots[0].GetExpression());
                Assert.AreEqual(location, (parameter.outputSlots[0].GetExpression() as VFXAttributeExpression).attributeLocation);
                Assert.AreEqual(testAttribute, (parameter.outputSlots[0].GetExpression() as VFXAttributeExpression).attributeName);
            };

            Action<VisualEffectAsset> write = delegate(VisualEffectAsset asset)
            {
                var sizeCurrent = VFXLibrary.GetOperators().First(o => o.name.Contains(testAttribute) && o.modelType == typeof(VFXAttributeParameter)).CreateInstance();
                var sizeSource = VFXLibrary.GetOperators().First(o => o.name.Contains(testAttribute) && o.modelType == typeof(VFXAttributeParameter)).CreateInstance();
                (sizeSource as VFXAttributeParameter).SetSettingValue("location", VFXAttributeLocation.Source);
                asset.GetResource().GetOrCreateGraph().AddChild(sizeCurrent);
                asset.GetResource().GetOrCreateGraph().AddChild(sizeSource);
                test(sizeCurrent as VFXAttributeParameter, VFXAttributeLocation.Current);
                test(sizeSource as VFXAttributeParameter, VFXAttributeLocation.Source);
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var sizeCurrent = asset.GetResource().GetOrCreateGraph()[0] as VFXAttributeParameter;
                var sizeSource = asset.GetResource().GetOrCreateGraph()[1] as VFXAttributeParameter;
                Assert.AreNotEqual(null, sizeCurrent);
                Assert.AreNotEqual(null, sizeSource);
                test(sizeCurrent, VFXAttributeLocation.Current);
                test(sizeSource, VFXAttributeLocation.Source);
            };
            InnerSaveAndReloadTest("AttributeParameter", write, read);
        }
    }
}
#endif
