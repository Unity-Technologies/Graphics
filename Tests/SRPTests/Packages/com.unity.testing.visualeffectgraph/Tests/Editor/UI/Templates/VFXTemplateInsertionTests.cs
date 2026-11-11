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

namespace UnityEditor.VFX.Test.UI.Templates
{
    [TestFixture]
    public class VFXTemplateInsertionTests
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

        [Test]
        public void CreateTemplate()
        {
            VFXViewWindow window = VFXTestCommon.GetViewWindow();

            VFXView view = window.graphView;
            view.controller = m_ViewController;

            view.CreateTemplateSystem(VFXTestCommon.simpleParticleSystemPath, Vector2.zero, null, false);
        }

        [UnityTest, Description("UUM-121263")]
        public IEnumerator InsertTemplate_Preserves_CustomAttributeType()
        {
            var templateGraph = VFXTestCommon.MakeTemporaryGraph();
            var customAttributeName = "initPos";
            templateGraph.TryAddCustomAttribute(customAttributeName, VFXValueType.Float3, "No description", false, out _);
            VFXTestCommon.ReimportVFXGraph(templateGraph);
            var templatePath = AssetDatabase.GetAssetPath(templateGraph);
            yield return null;

            VFXViewWindow window = VFXTestCommon.GetViewWindow();
            VFXView view = window.graphView;
            view.controller = m_ViewController;
            Assert.IsFalse(m_ViewController.graph.attributesManager.Exist(customAttributeName));
            yield return null;

            view.CreateTemplateSystem(templatePath, Vector2.zero, null, false);
            yield return null;

            Assert.IsTrue(m_ViewController.graph.TryFindCustomAttributeDescriptor(customAttributeName, out var descriptor));
            Assert.AreEqual(CustomAttributeUtility.Signature.Vector3, descriptor.type);
        }

        [UnityTest, Description("UUM-121263")]
        public IEnumerator InsertTemplate_With_CollidingCustomAttribute_Is_Rejected()
        {
            var attributeName = "initPos";

            var templateGraph = VFXTestCommon.MakeTemporaryGraph();
            templateGraph.TryAddCustomAttribute(attributeName, VFXValueType.Float3, "No description", false, out _);
            var templateContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            templateGraph.AddChild(templateContext);
            var templateBlock = VFXLibrary.GetBlocks().First(x => x.modelType == typeof(SetAttribute)).CreateInstance();
            templateBlock.SetSettingValue("attribute", attributeName);
            templateContext.AddChild(templateBlock);
            VFXTestCommon.ReimportVFXGraph(templateGraph);
            var templatePath = AssetDatabase.GetAssetPath(templateGraph);
            yield return null;

            m_ViewController.graph.TryAddCustomAttribute(attributeName, VFXValueType.Float, "No description", false, out _);
            var childCountBefore = m_ViewController.graph.children.Count();

            VFXViewWindow window = VFXTestCommon.GetViewWindow();
            VFXView view = window.graphView;
            view.controller = m_ViewController;
            yield return null;

            LogAssert.Expect(LogType.Error, new Regex($".*{attributeName}.*conflict.*"));
            view.CreateTemplateSystem(templatePath, Vector2.zero, null, false);
            yield return null;

            Assert.IsTrue(m_ViewController.graph.TryFindCustomAttributeDescriptor(attributeName, out var original));
            Assert.AreEqual(CustomAttributeUtility.Signature.Float, original.type);
            Assert.IsFalse(m_ViewController.graph.customAttributes.Any(x => x.type == CustomAttributeUtility.Signature.Vector3));
            Assert.AreEqual(childCountBefore, m_ViewController.graph.children.Count());
        }
    }
}
