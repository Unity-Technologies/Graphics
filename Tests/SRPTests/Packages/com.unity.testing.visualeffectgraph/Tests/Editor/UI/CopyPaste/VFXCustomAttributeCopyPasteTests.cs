using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX.UI;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Experimental.VFX.Utility;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.Operator;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.Test.UI.CopyPaste
{
    [TestFixture]
    public class VFXCustomAttributeCopyPasteTests
    {
        VFXViewController m_ViewController;

        [SetUp]
        public void CreateTestAsset()
        {
            m_ViewController = VFXCopyPasteTestHelper.CreateTestAsset();
        }

        [TearDown]
        public void DestroyTestAsset()
        {
            VFXCopyPasteTestHelper.DestroyTestAsset();
            m_ViewController = null;
        }

        [UnityTest, Description("UUM-75894")]
        public IEnumerator CopyPasteContextWithCustomAttribute()
        {
            var viewController1 = m_ViewController;
            var window1 = VFXTestCommon.GetViewWindow();
            var view1 = window1.graphView;
            view1.controller = viewController1;

            // Create a BasicInitialize context
            var initContextDesc = VFXLibrary.GetContexts().First(x => x.modelType == typeof(VFXBasicInitialize));
            var newContext = viewController1.AddVFXContext(new Vector2(100, 100), initContextDesc.variant);
            viewController1.LightApplyChanges();
            var contextController = viewController1.allChildren.OfType<VFXContextController>().Single();
            yield return null;

            // Add a custom attribute
            var customAttributeName = "initPos";
            viewController1.graph.TryAddCustomAttribute(customAttributeName, VFXValueType.Float3, "No description", false, out var attribute);
            yield return null;

            // Add a block to that context
            var setInitPos = VFXLibrary.GetBlocks().First(x => x.modelType == typeof(SetAttribute)).CreateInstance();
            setInitPos.SetSettingValue("attribute", customAttributeName);
            contextController.AddBlock(0, setInitPos);
            viewController1.LightApplyChanges();
            yield return null;

            // Select the created context and copy
            view1.ClearSelection();
            view1.AddToSelection(view1.Query().OfType<VFXContextUI>().First());
            window1.graphView.CopySelectionCallback();
            yield return null;

            // Create a second asset and open window
            CreateTestAsset();
            var viewController2 = m_ViewController;
            var window2 = VFXTestCommon.GetViewWindow();
            var view2 = window2.graphView;
            view2.controller = viewController2;

            // Paste selection in the second window
            window2.graphView.PasteCallback();
            yield return null;

            // Check that the second window has a single custom attribute of type Vector3
            Assert.IsTrue(viewController2.graph.TryFindCustomAttributeDescriptor(customAttributeName, out var customAttributeDescriptor));
            Assert.IsNotNull(customAttributeDescriptor);
            Assert.AreEqual(CustomAttributeUtility.Signature.Vector3, customAttributeDescriptor.type);
        }

        [UnityTest, Description("UUM-75894")]
        public IEnumerator CopyPasteOperatorWithCustomAttribute()
        {
            var viewController1 = m_ViewController;
            var window1 = VFXTestCommon.GetViewWindow();
            var view1 = window1.graphView;
            view1.controller = viewController1;

            // Add a custom attribute
            var customAttributeName = "initPos";
            viewController1.graph.TryAddCustomAttribute(customAttributeName, VFXValueType.Float3, "No description", false, out var attribute);
            yield return null;

            // Add a block to that context
            var getInitPosDesc = VFXLibrary.GetOperators().First(x => x.modelType == typeof(VFXAttributeParameter));
            var getInitPos = viewController1.AddVFXOperator(Vector2.zero, getInitPosDesc.variant);
            getInitPos.SetSettingValue("attribute", customAttributeName);

            viewController1.LightApplyChanges();
            yield return null;

            // Select the created context and copy
            view1.ClearSelection();
            view1.AddToSelection(view1.Query().OfType<VFXOperatorUI>().First());
            window1.graphView.CopySelectionCallback();
            yield return null;

            // Create a second asset and open window
            CreateTestAsset();
            var viewController2 = m_ViewController;
            var window2 = VFXTestCommon.GetViewWindow();
            var view2 = window2.graphView;
            view2.controller = viewController2;

            // Paste selection in the second window
            window2.graphView.PasteCallback();
            yield return null;

            // Check that the second window has a single custom attribute of type Vector3
            Assert.IsTrue(viewController2.graph.TryFindCustomAttributeDescriptor(customAttributeName, out var customAttributeDescriptor));
            Assert.IsNotNull(customAttributeDescriptor);
            Assert.AreEqual(CustomAttributeUtility.Signature.Vector3, customAttributeDescriptor.type);
        }
    }
}
