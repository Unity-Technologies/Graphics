using System.Collections;
using System.Linq;

using NUnit.Framework;

using UnityEditor.VFX.Operator;
using UnityEditor.VFX.UI;
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
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetInstanceID(), 0));
            var window = VFXViewWindow.GetWindow(asset, true);
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
    }
}
