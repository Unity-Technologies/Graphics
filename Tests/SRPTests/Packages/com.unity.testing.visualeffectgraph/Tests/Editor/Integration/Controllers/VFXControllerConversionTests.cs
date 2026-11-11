using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.VFX;

using UnityEditor.VFX.Block;
using UnityEditor.VFX.Block.Test;
using UnityEditor.VFX.UI;
using System.Reflection;

using static UnityEditor.VFX.Test.VFXControllerTestHelper;

namespace UnityEditor.VFX.Test.Integration.Controllers
{
    [TestFixture]
    public class VFXControllerConversionTests
    {
        private VFXViewController m_ViewController;
        private int m_StartUndoGroupId;
        private string testAssetRandomFileName;

        [SetUp]
        public void CreateTestAsset()
        {
            m_ViewController = VFXControllerTestHelper.CreateTestAsset();
            m_StartUndoGroupId = Undo.GetCurrentGroup();
        }

        [OneTimeSetUp]
        public void Init()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
        }

        [OneTimeTearDown]
        public void OnTimeCleanup()
        {
            VFXTestCommon.CloseAllVFXWindow();
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [TearDown]
        public void DestroyTestAsset()
        {
            VFXControllerTestHelper.DestroyTestAsset(m_ViewController, m_StartUndoGroupId, testAssetRandomFileName);
        }

        [Test]
        public void ConvertParameterToInline()
        {
            VFXParameter newParameter = m_ViewController.AddVFXParameter(Vector2.zero, VFXLibrary.GetParameters().First(t => t.modelType == typeof(AABox)).variant);

            m_ViewController.LightApplyChanges();

            VFXParameterController parameterController = m_ViewController.GetParameterController(newParameter);

            parameterController.model.AddNode(new Vector2(123, 456));

            AABox value = new AABox { center = new Vector3(1, 2, 3), size = new Vector3(4, 5, 6) };

            parameterController.value = value;

            m_ViewController.LightApplyChanges();

            VFXParameterNodeController parameterNode = parameterController.nodes.First();

            parameterNode.ConvertToInline();

            VFXInlineOperator op = m_ViewController.graph.children.OfType<VFXInlineOperator>().First();

            Assert.AreEqual(new Vector2(123, 456), op.position);
            Assert.AreEqual(typeof(AABox), op.type);
            Assert.AreEqual(value, op.inputSlots[0].value);
        }

        [Test]
        public void ConvertInlineToParameter()
        {
            var op = ScriptableObject.CreateInstance<VFXInlineOperator>();
            m_ViewController.graph.AddChild(op);
            op.SetSettingValue("m_Type", (SerializableType)typeof(AABox));
            op.position = new Vector2(123, 456);
            AABox value = new AABox { center = new Vector3(1, 2, 3), size = new Vector3(4, 5, 6) };

            op.inputSlots[0].value = value;

            m_ViewController.LightApplyChanges();

            var nodeController = m_ViewController.GetNodeController(op, 0) as VFXOperatorController;

            nodeController.ConvertToProperty();

            VFXParameter param = m_ViewController.graph.children.OfType<VFXParameter>().First();

            Assert.AreEqual(new Vector2(123, 456), param.nodes[0].position);
            Assert.AreEqual(typeof(AABox), param.type);
            Assert.AreEqual(value, param.value);
        }

        [Test]
        public void Convert_Output_Quad_To_ShaderGraph_Triangle()
        {
            testAssetRandomFileName = $"Assets/TmpTests/random_{Guid.NewGuid()}.vfx";
            var templateString = File.ReadAllText(VFXTestCommon.simpleParticleSystemPath);
            File.WriteAllText(testAssetRandomFileName, templateString);
            AssetDatabase.ImportAsset(testAssetRandomFileName);

            var window = VFXTestCommon.GetViewWindow();
            window.LoadAsset(AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(testAssetRandomFileName), null);

            var viewController = VFXViewController.GetController(window.displayedResource);
            var test = viewController.allChildren.ToArray();

            var outputContextController = viewController.allChildren.OfType<VFXContextController>().Single(o => o.model.contextType == VFXContextType.Output);
            var outputContextUI = window.graphView.Query().OfType<VFXContextUI>().Where(o => o.controller == outputContextController).ToList().Single();

            var originalSetting = outputContextController.model.GetSetting("primitiveType");
            Assert.IsTrue(originalSetting.valid);
            Assert.AreEqual(originalSetting.value, VFXPrimitiveType.Quad);

            Assert.IsNotNull(outputContextUI);

            var variantProvider = new VFXTopologySubVariantProvider();
            var triangleVariant = variantProvider.GetVariants().Single(o => o.name.Contains("Triangle"));

            var fnConvertContext = outputContextUI.GetType().GetMethod("ConvertContext", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(fnConvertContext);
            fnConvertContext.Invoke(outputContextUI, new object[] {triangleVariant, Vector2.zero});
            viewController.ApplyChanges();

            outputContextController = viewController.allChildren.OfType<VFXContextController>().Single(o => o.model.contextType == VFXContextType.Output);
            Assert.IsInstanceOf<VFXComposedParticleOutput>(outputContextController.model);
            var topology = outputContextController.model.GetSetting("m_Topology");
            Assert.IsTrue(topology.valid);
            Assert.IsInstanceOf<ParticleTopologyPlanarPrimitive>(topology.value);

            var planarTopology = topology.value as ParticleTopologyPlanarPrimitive;
            Assert.IsNotNull(planarTopology);
            var primitiveTypeFieldAccess = planarTopology.GetType().GetField("primitiveType", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(primitiveTypeFieldAccess);

            Assert.AreEqual(VFXPrimitiveType.Triangle, primitiveTypeFieldAccess.GetValue(planarTopology));
        }

        [UnityTest, Description("Repro UUM-131487")]
        public IEnumerator Convert_To_ShaderGraphOutput()
        {
            var vfxGraph = VFXTestCommon.MakeTemporaryGraph();
            var output = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();
            vfxGraph.AddChild(output);

            var window = VFXTestCommon.GetWindow(vfxGraph.visualEffectResource, true, true);
            Assert.IsNotNull(window);
            window.LoadResource(vfxGraph.visualEffectResource, null);
            yield return null;

            var planarPrimitiveController = window.graphView.controller.AllSlotContainerControllers
                .Single(x => x.model is VFXPlanarPrimitiveOutput);
            Assert.IsNotNull(planarPrimitiveController);

            var planarPrimitiveControllerUI = window.graphView.Query().OfType<VFXContextUI>().Where(o => o.controller == planarPrimitiveController).ToList().Single();
            Assert.IsNotNull(planarPrimitiveControllerUI);

            var modelSgDescriptor = VFXLibrary.GetContexts().FirstOrDefault(o => o.model is VFXComposedParticleOutput);
            Assert.IsNotNull(modelSgDescriptor);

            var fnConvertContext = planarPrimitiveControllerUI.GetType().GetMethod("ConvertContext", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fnConvertContext);
            fnConvertContext.Invoke(planarPrimitiveControllerUI, new object[] { modelSgDescriptor.variant, planarPrimitiveControllerUI.controller.position });
            yield return null;

            var planarPrimitiveControllerWithSg = window.graphView.controller.AllSlotContainerControllers
                .Single(x => x.model is VFXComposedParticleOutput);
            Assert.IsNotNull(planarPrimitiveControllerWithSg);

            var shaderGraphFromConversion = (planarPrimitiveControllerWithSg.model as IVFXShaderGraphOutput).GetShaderGraph();
            Assert.IsNotNull(shaderGraphFromConversion);

            var otherSgOutput = modelSgDescriptor.CreateInstance();
            vfxGraph.AddChild(otherSgOutput);
            var shaderGraphReference = (otherSgOutput as IVFXShaderGraphOutput).GetShaderGraph();
            Assert.IsNotNull(shaderGraphReference);

            Assert.AreEqual(AssetDatabase.GetAssetPath(shaderGraphReference), AssetDatabase.GetAssetPath(shaderGraphFromConversion));
        }
    }
}
