using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Moq;
using UnityEditor.Search;
using UnityEditor.SearchService;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VFX.UI;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;
using RangeAttribute = UnityEngine.RangeAttribute;

namespace UnityEditor.VFX.Test.UI.GraphView
{
    [TestFixture]
    public class VFXPropertyFieldTests
    {
        [OneTimeTearDown]
        public void DestroyTestAssets()
        {
            VFXTestCommon.CloseAllVFXWindow();
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [SetUp]
        public void SetUp()
        {
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
            VFXTestCommon.CloseAllUnecessaryWindows();
            EventHelpers.TestSetUp();
        }

        [TearDown]
        public void TearDown()
        {
            EventHelpers.TestTearDown();
            if (EditorWindow.HasOpenInstances<GraphViewTemplateWindow>())
            {
                EditorWindow.GetWindow<GraphViewTemplateWindow>().Close();
            }
        }

        public class TestEditor : EditorWindow
        {

        }

        [Test]
        public void Check_NumericPropertyRM_Float_MinAttribute()
        {
            CheckNumericPropertyRM(x => new FloatPropertyRM(x, 60f), new MinAttribute(0f), 0f, new List<(float, float)> { (-5f, 0f), (5f, 5f )});
        }

        [Test]
        public void Check_NumericPropertyRM_Int_MinAttribute()
        {
            CheckNumericPropertyRM(x => new IntPropertyRM(x, 60f), new MinAttribute(0f), 0, new List<(int, int)> { (-1, 0), (1, 1 )});
        }

        [Test]
        public void Check_NumericPropertyRM_UInt_MinAttribute()
        {
            CheckNumericPropertyRM<uint, long>(x => new UintPropertyRM(x, 60f), new MinAttribute(1f), 1U, new List<(uint, uint)> { (0, 1), (5, 5 )});
        }

        [Test]
        public void Check_NumericPropertyRM_Float_RangeAttribute()
        {
            CheckNumericPropertyRM(x => new FloatPropertyRM(x, 60f), new RangeAttribute(1f, 100f), 1f, new List<(float, float)> { (-5f, 1f), (105f, 100f )});
            CheckNumericPropertyRM(x => new FloatPropertyRM(x, 60f), new MinMaxAttribute(1f, 100f), 1f, new List<(float, float)> { (-5f, 1f), (105f, 100f )});
        }

        [Test]
        public void Check_NumericPropertyRM_Int_RangeAttribute()
        {
            CheckNumericPropertyRM(x => new IntPropertyRM(x, 60f), new RangeAttribute(1f, 100f), 1, new List<(int, int)> { (-5, 1), (105, 100 )});
            CheckNumericPropertyRM(x => new IntPropertyRM(x, 60f), new MinMaxAttribute(1f, 100f), 1, new List<(int, int)> { (-5, 1), (105, 100 )});
        }

        [Test]
        public void Check_NumericPropertyRM_UInt_RangeAttribute()
        {
            CheckNumericPropertyRM(x => new UintPropertyRM(x, 60f), new RangeAttribute(1f, 100f), 1U, new List<(uint, uint)> { (0, 1), (105, 100 )});
            CheckNumericPropertyRM(x => new UintPropertyRM(x, 60f), new MinMaxAttribute(1f, 100f), 1U, new List<(uint, uint)> { (0, 1), (105, 100 )});
        }

        [UnityTest]
        public IEnumerator Check_ObjectPropertyRMTextureSearch()
        {
            var hasEngineOverride = ObjectSelectorSearch.HasEngineOverride();
            try
            {
                var viewController = VFXTestCommon.StartEditTestAsset();
                var texture2DDesc = VFXLibrary.GetOperators().Single(x => x.name == "Texture2D");
                viewController.AddVFXOperator(Vector2.zero, texture2DDesc.variant);
                viewController.ApplyChanges();
                yield return null;

                var window = VFXTestCommon.GetWindow(viewController.graph.GetResource(), true, true);
                var texture2DNodeUI = window.graphView.Q<VFXOperatorUI>();
                var button = texture2DNodeUI.Q<VisualElement>(null, "unity-object-field__selector");

                VFXGUITestHelper.SendDoubleClick(button, 1);

                Assert.IsTrue(EditorWindow.HasOpenInstances<ObjectSelector>());

                if (hasEngineOverride)
                {
                    // Work around a bug in ObjectSelector which property searchFilter do not return correct value when the search mode is advanced
                    var searchWindow = EditorWindow.GetWindowDontShow<SearchPickerWindow>();
                    Assert.AreEqual("t:Texture2D or t:RenderTexture", searchWindow.context.searchText);
                }
                else
                {
                    Assert.AreEqual("t:Texture2D t:RenderTexture", ObjectSelector.get.searchFilter);
                }
            }
            finally
            {
                if (hasEngineOverride)
                {
                    EditorWindow.GetWindow<SearchPickerWindow>().Close();
                }
                else
                {
                    EditorWindow.GetWindow<ObjectSelector>().Close();
                }
            }
        }

        private void CheckNumericPropertyRM<T,U>(Func<IPropertyRMProvider, NumericPropertyRM<T, U>> creator, UnityEngine.PropertyAttribute attribute, T initialValue, List<(T, T)> testCases)
        {
            // Arrange
            var editor = EditorWindow.GetWindow<TestEditor>();
            try
            {
                var propertyRMProviderMock = new Mock<IPropertyRMProvider>();
                propertyRMProviderMock.SetupProperty(x => x.value, initialValue);
                propertyRMProviderMock.SetupGet(x => x.name).Returns("Mocked property");
                propertyRMProviderMock.SetupGet(x => x.attributes).Returns(new VFXPropertyAttributes(attribute));
                var numericPropertyRM = creator(propertyRMProviderMock.Object);
                editor.rootVisualElement.Add(numericPropertyRM);
                numericPropertyRM.SetValue(initialValue);

                foreach (var testCase in testCases)
                {
                    // Act
                    numericPropertyRM.Q<TextValueField<U>>().value = (U)Convert.ChangeType(testCase.Item1, typeof(U));
                    // Assert
                    propertyRMProviderMock.Object.value = testCase.Item2;
                }
            }
            finally
            {
                editor.Close();
            }
        }
    }
}
