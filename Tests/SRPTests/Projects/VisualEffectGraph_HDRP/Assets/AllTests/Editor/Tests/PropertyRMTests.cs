using System.Collections;
using System.Linq;

using NUnit.Framework;

using UnityEditor.VFX.Block;
using UnityEditor.VFX.Operator;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class PropertyRMTests
    {
        [UnityTest, Description("Covers UUM-92186")]
        public IEnumerator Undo_SliderChange()
        {
            // Arrange
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var noiseOperator = (Noise)VFXLibrary.GetOperators().First(x => x.modelType == typeof(Noise)).CreateInstance();
            noiseOperator.GetInputSlot(3).value = 0f;
            graph.AddChild(noiseOperator);
            var path = AssetDatabase.GetAssetPath(graph);
            AssetDatabase.ImportAsset(path);

            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetEntityId(), 0));
            var window = VFXTestCommon.GetWindow(asset, true);
            window.LoadAsset(asset, null);
            window.Show();
            yield return null;

            // Find the Roughness slider UI
            var operatorUI = window.graphView.Q<VFXOperatorUI>();
            var roughnessSlider = operatorUI.Query<Slider>().Last();
            Assert.NotNull(roughnessSlider);
            var floatField = roughnessSlider.parent as FloatField;
            Assert.NotNull(floatField);

            // Act
            Undo.IncrementCurrentGroup();
            floatField.Focus();
            roughnessSlider.value = 1f;
            yield return null;

            Assert.AreEqual(1f, noiseOperator.GetInputSlot(3).value);
            Undo.PerformUndo();
            yield return null;

            // Assert
            var value = (float)noiseOperator.GetInputSlot(3).value;
            Assert.AreEqual(0f, value);
            Assert.AreEqual(0f, floatField.value);
        }

        [UnityTest, Description("Covers UUM-139398: a constant-fed color slot stays evaluable (not indeterminate) after reimport")]
        public IEnumerator ColorSlot_FedByConstant_StaysEvaluable_AfterReimport()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();

            var initContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            graph.AddChild(initContext);

            var setColor = ScriptableObject.CreateInstance<SetAttribute>();
            setColor.SetSettingValue("attribute", "color");
            initContext.AddChild(setColor);

            var inlineVector3 = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inlineVector3.SetSettingValue("m_Type", (SerializableType)typeof(Vector3));
            inlineVector3.inputSlots[0].value = new Vector3(0.25f, 0.5f, 0.75f);
            graph.AddChild(inlineVector3);

            var path = AssetDatabase.GetAssetPath(graph);
            AssetDatabase.ImportAsset(path);

            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetEntityId(), 0));
            var window = VFXTestCommon.GetWindow(asset, true);
            window.LoadAsset(asset, null);
            window.Show();
            yield return null;

            var controller = window.graphView.controller;
            Assert.IsNotNull(controller);

            // Link the constant operator to the color input slot.
            var colorSlot = setColor.inputSlots[0];
            Assert.IsTrue(inlineVector3.outputSlots[0].Link(colorSlot));
            yield return null;
            yield return null;

            controller.RecompileExpressionGraphIfNeeded();
            Assert.IsTrue(controller.CanGetEvaluatedContent(colorSlot),
                "Sanity: constant-fed color slot should be evaluable after linking.");

            // Reimport through the editor's save path; a stale expression context used to make this indeterminate.
            window.graphView.OnSave();
            yield return null;
            yield return null;

            controller.RecompileExpressionGraphIfNeeded();
            Assert.IsTrue(controller.CanGetEvaluatedContent(colorSlot),
                "Constant-fed color slot must stay evaluable (not indeterminate) after reimport.");

            window.Close();
        }
    }
}
