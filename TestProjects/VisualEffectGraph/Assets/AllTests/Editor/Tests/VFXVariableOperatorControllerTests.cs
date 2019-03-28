#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.UI;
using System.IO;
using UnityEngine.TestTools;
using UnityEditor.VFX.Block.Test;
using UnityEditor.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXVariableOperatorControllersTests
    {
        VFXViewController m_ViewController;

        const string testAssetName = "Assets/TmpTests/VFXGraph1.vfx";

        private int m_StartUndoGroupId;

        [SetUp]
        public void CreateTestAsset()
        {
            var directoryPath = Path.GetDirectoryName(testAssetName);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            if (File.Exists(testAssetName))
            {
                AssetDatabase.DeleteAsset(testAssetName);
            }

            var asset = VisualEffectResource.CreateNewAsset(testAssetName);
            var resource = asset.GetResource(); // force resource creation

            m_ViewController = VFXViewController.GetController(resource);
            m_ViewController.useCount++;

            m_StartUndoGroupId = Undo.GetCurrentGroup();


            experimental = EditorPrefs.GetBool(VFXViewPreference.experimentalOperatorKey, false);
            if (!experimental)
                EditorPrefs.SetBool(VFXViewPreference.experimentalOperatorKey, true);
        }

        bool experimental;

        [TearDown]
        public void DestroyTestAsset()
        {
            if (!experimental)
            {
                EditorPrefs.SetBool(VFXViewPreference.experimentalOperatorKey, false);
            }
            m_ViewController.useCount--;
            Undo.RevertAllDownToGroup(m_StartUndoGroupId);
            AssetDatabase.DeleteAsset(testAssetName);
        }

        VFXNodeController CreateNew(string name, Vector2 position, Type nodeType = null)
        {
            var desc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name.Contains(name) && (nodeType == null || o.modelType == nodeType));
            var op = m_ViewController.AddVFXOperator(position, desc);
            m_ViewController.LightApplyChanges();

            return m_ViewController.GetRootNodeController(op, 0);
        }

        #pragma warning disable 0414
        static private string[] variableOperators = { "Add", "Dot Product", "Clamp" };

        #pragma warning restore 0414

        [Test]
        public void LinkingValidOutputSlotToVariableOperatorChangesType([ValueSource("variableOperators")] string operatorName)
        {
            var variableOperator = CreateNew(operatorName, new Vector2(1, 2));

            var vector2inline = CreateNew(typeof(Vector2).UserFriendlyName(), new Vector2(2, 2), typeof(VFXInlineOperator));

            var output = vector2inline.outputPorts[0];

            var input = variableOperator.inputPorts[0];

            Assert.AreNotEqual(output.portType, input.portType);// this test require that the inline type is different from the default type of the variable operator

            m_ViewController.CreateLink(input, output);

            variableOperator.ApplyChanges();
            input = variableOperator.inputPorts[0];
            Assert.AreEqual(output.portType, input.portType);
        }

        [Test]
        public void LinkingValidOutputSlotToUniformOperatorChangesTypeIfNoLinkOrMandatory()
        {
            var variableOperator = CreateNew("Distance", new Vector2(1, 2));
            var operatorModel = variableOperator.model as VFXOperatorNumeric;

            var vector2inline = CreateNew(typeof(Vector2).UserFriendlyName(), new Vector2(2, 2), typeof(VFXInlineOperator));
            var vector3inline = CreateNew(typeof(Vector3).UserFriendlyName(), new Vector2(2, 2), typeof(VFXInlineOperator));

            var output = vector3inline.outputPorts[0];
            var input = variableOperator.inputPorts[0];

            m_ViewController.CreateLink(input, output); // this should change the type to Vector3
            variableOperator.ApplyChanges();

            input = variableOperator.inputPorts[0];
            Assert.AreEqual(typeof(Vector3), variableOperator.inputPorts[0].portType);

            input = variableOperator.inputPorts.First(t => t.model == operatorModel.inputSlots[1]); // Warning inputPorts contains subslots so dont use  variableOperator.inputPorts[1]
            output = vector2inline.outputPorts[0];

            m_ViewController.CreateLink(input, output); // this should not change type because link is not possible without change
            variableOperator.ApplyChanges();

            Assert.AreEqual(typeof(Vector2), variableOperator.inputPorts[0].portType);
        }

        [Test]
        public void CascadedOperatorTests()
        {
            var variableOperator = CreateNew("Add", new Vector2(1, 2)) as VFXCascadedOperatorController;
            var operatorModel = variableOperator.model as VFXOperatorNumeric;

            var vector2inline = CreateNew(typeof(Vector2).UserFriendlyName(), new Vector2(2, 2), typeof(VFXInlineOperator));
            var vector3inline = CreateNew(typeof(Vector3).UserFriendlyName(), new Vector2(2, 2), typeof(VFXInlineOperator));
            var vector4inline = CreateNew(typeof(Vector4).UserFriendlyName(), new Vector2(2, 2), typeof(VFXInlineOperator));

            var output = vector3inline.outputPorts[0];
            var input = variableOperator.inputPorts[0];

            m_ViewController.CreateLink(input, output); // this should change the type to Vector3
            variableOperator.ApplyChanges();

            input = variableOperator.inputPorts[0];
            Assert.AreEqual(typeof(Vector3), variableOperator.inputPorts[0].portType);

            input = variableOperator.inputPorts.First(t => t.model == operatorModel.inputSlots[1]); // Warning inputPorts contains subslots so dont use  variableOperator.inputPorts[1]
            output = vector2inline.outputPorts[0];

            m_ViewController.CreateLink(input, output);
            variableOperator.ApplyChanges();

            Assert.AreEqual(typeof(Vector2), variableOperator.inputPorts.First(t => t.model == operatorModel.inputSlots[1]).portType);

            input = variableOperator.inputPorts.Last(); //upcommingdataanchor

            Assert.IsTrue(input is VFXUpcommingDataAnchorController);

            output = vector4inline.outputPorts[0];

            m_ViewController.CreateLink(input, output); // this should not change type because link is not possible without change
            variableOperator.ApplyChanges();

            Assert.AreEqual(typeof(Vector4), variableOperator.inputPorts.First(t => t.model == operatorModel.inputSlots[2]).portType);

            Assert.IsTrue(variableOperator.inputPorts.Last() is VFXUpcommingDataAnchorController);

            m_ViewController.LightApplyChanges();

            variableOperator.inputPorts.First(t => t.model == operatorModel.inputSlots[1]);

            input = variableOperator.inputPorts.First(t => t.model == operatorModel.inputSlots[1]); // Warning inputPorts contains subslots so dont use  variableOperator.inputPorts[1]
            output = vector2inline.outputPorts[0];

            Assert.AreEqual(3, operatorModel.inputSlots.Count);

            var secondLink = m_ViewController.dataEdges.First(t => t.input == input && t.output == output);

            m_ViewController.RemoveElement(secondLink,true);

            Assert.AreEqual(2, operatorModel.inputSlots.Count);

            variableOperator.ApplyChanges();
            m_ViewController.LightApplyChanges();

            //The Vector4 slot should now be in second position and should still have its link.
            input = variableOperator.inputPorts.First(t => t.model == operatorModel.inputSlots[1]);
            output = vector4inline.outputPorts[0];

            Assert.AreEqual(typeof(Vector4), input.portType);
            Assert.IsNotNull(m_ViewController.dataEdges.FirstOrDefault(t => t.input == input && t.output == output));

            variableOperator.RemoveOperand(0);
            Assert.AreEqual(2, operatorModel.inputSlots.Count);
            variableOperator.RemoveOperand(1);
            Assert.AreEqual(2, operatorModel.inputSlots.Count);

            variableOperator.model.SetOperandName(0, "Miaou");
            variableOperator.model.SetOperandName(1, "Meuh");

            variableOperator.ApplyChanges();

            Assert.AreEqual("Miaou", variableOperator.inputPorts[0].name);
            Assert.AreEqual("Meuh", variableOperator.inputPorts.First(t => t.model == variableOperator.model.inputSlots[1]).name);

            //Check that move preserves name, type and links.
            variableOperator.model.OperandMoved(0, 1);
            variableOperator.ApplyChanges();
            m_ViewController.LightApplyChanges();

            Assert.AreEqual("Meuh", variableOperator.inputPorts[0].name);
            Assert.AreEqual(typeof(Vector4), variableOperator.inputPorts[0].portType);
            Assert.IsNotNull(m_ViewController.dataEdges.FirstOrDefault(t => t.input == variableOperator.inputPorts[0] && t.output == vector4inline.outputPorts[0]));

            VFXDataAnchorController miaou = variableOperator.inputPorts.First(t => t.model == variableOperator.model.inputSlots[1]);

            Assert.AreEqual("Miaou", miaou.name);
            Assert.AreEqual(typeof(Vector3), miaou.portType);
            Assert.IsNotNull(m_ViewController.dataEdges.FirstOrDefault(t => t.input == miaou && t.output == vector3inline.outputPorts[0]));
        }
    }
}
#endif
