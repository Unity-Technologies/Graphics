using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.Serialization
{
    [TestFixture]
    public class VFXParameterSerializationTests
    {
        private bool m_CreatedTestAssetDirectory;

        [OneTimeSetUp]
        public void SetUp()
        {
            VFXSerializationTestHelper.CreateDirectory(out m_CreatedTestAssetDirectory);
        }

        private void InnerSaveAndReloadTest(string suffixname, Action<VisualEffectAsset> write, Action<VisualEffectAsset> read)
        {
            VFXSerializationRoundTripTestHelper.SaveAndReload(VFXSerializationTestHelper.kTestAssetDir, suffixname, write, read);
        }

        [Test]
        public void SerializeParameter()
        {
            var name = "unity";
            Action<VisualEffectAsset> write = delegate(VisualEffectAsset asset)
            {
                var parameter = VFXLibrary.GetParameters().First(o => o.modelType == typeof(Vector2)).CreateInstance();
                parameter.SetSettingValue("m_Exposed", true);
                parameter.SetSettingValue("m_ExposedName", name);
                asset.GetResource().GetGraph().AddChild(parameter);
                Assert.AreEqual(VFXValueType.Float2, parameter.outputSlots[0].GetExpression().valueType);
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var parameter = asset.GetResource().GetGraph()[0] as VFXParameter;
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
                var graph = asset.GetResource().GetGraph();
                var add = ScriptableObject.CreateInstance<Operator.Add>();
                var parameter = VFXLibrary.GetParameters().First(o => o.modelType == typeof(Vector2)).CreateInstance();
                add.SetOperandType(0, typeof(Vector2));
                graph.AddChild(add);
                graph.AddChild(parameter);
                add.inputSlots[0].Link(parameter.outputSlots[0]);

                Assert.AreEqual(VFXValueType.Float2, add.outputSlots[0].GetExpression().valueType);
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var graph = asset.GetResource().GetGraph();
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
                var builtIn = VFXLibrary.GetOperators().First(o => o.variant.name.StartsWith("Total Time (VFX)")).CreateInstance();
                asset.GetResource().GetGraph().AddChild(builtIn);
                Assert.AreEqual(VFXExpressionOperation.TotalTime, builtIn.outputSlots[0].GetExpression().operation);
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var builtIn = asset.GetResource().GetGraph()[0] as VFXDynamicBuiltInParameter;
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
                var graph = asset.GetResource().GetGraph();
                var add = ScriptableObject.CreateInstance<Operator.Add>();
                var builtIn = VFXLibrary.GetOperators().First(o => o.variant.name.StartsWith("Total Time (VFX)")).CreateInstance();
                graph.AddChild(builtIn);
                graph.AddChild(add);
                add.inputSlots[0].Link(builtIn.outputSlots[0]);

                Assert.AreEqual(VFXExpressionOperation.TotalTime, builtIn.outputSlots[0].GetExpression().operation);
                Assert.IsTrue(add.inputSlots[0].HasLink());
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var graph = asset.GetResource().GetGraph();
                var builtIn = graph[0] as VFXDynamicBuiltInParameter;
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
                var sizeCurrent = VFXLibrary.GetOperators().First(o => o.variant.name.Contains(testAttribute, StringComparison.OrdinalIgnoreCase) && o.variant.modelType == typeof(VFXAttributeParameter)).CreateInstance();
                var sizeSource = VFXLibrary.GetOperators().First(o => o.variant.name.Contains(testAttribute, StringComparison.OrdinalIgnoreCase) && o.variant.modelType == typeof(VFXAttributeParameter)).CreateInstance();
                (sizeSource as VFXAttributeParameter).SetSettingValue("location", VFXAttributeLocation.Source);
                asset.GetResource().GetGraph().AddChild(sizeCurrent);
                asset.GetResource().GetGraph().AddChild(sizeSource);
                test(sizeCurrent as VFXAttributeParameter, VFXAttributeLocation.Current);
                test(sizeSource as VFXAttributeParameter, VFXAttributeLocation.Source);
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var sizeCurrent = asset.GetResource().GetGraph()[0] as VFXAttributeParameter;
                var sizeSource = asset.GetResource().GetGraph()[1] as VFXAttributeParameter;
                Assert.AreNotEqual(null, sizeCurrent);
                Assert.AreNotEqual(null, sizeSource);
                test(sizeCurrent, VFXAttributeLocation.Current);
                test(sizeSource, VFXAttributeLocation.Source);
            };
            InnerSaveAndReloadTest("AttributeParameter", write, read);
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            try
            {
                VFXTestCommon.DeleteAllTemporaryGraph();
            }
            finally
            {
                VFXSerializationTestHelper.CleanupDirectory(m_CreatedTestAssetDirectory);
            }
        }
    }

}
