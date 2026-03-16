#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif

#if ENABLE_RENDERING_DEBUGGER_UI
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Tests;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Tests
{
    using static DebugPanelDisplaySettings.DebugPanelDisplaySettingsData;

    partial class DebugPanelRenderingDebuggerTests
    {
        private DebugDisplaySettingsUI m_DebugDisplaySettingsUI;
        static DebugPanelDisplaySettings.DebugPanelDisplaySettingsData data = new DebugPanelDisplaySettings.DebugPanelDisplaySettingsData();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Register debug settings
            m_DebugDisplaySettingsUI = new DebugDisplaySettingsUI();
            m_DebugDisplaySettingsUI.RegisterDebug(DebugPanelDisplaySettings.Instance);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_DebugDisplaySettingsUI?.UnregisterDebug();
        }

        [Test]
        public void DebugPanel_ShouldBeRegistered()
        {
            var panelIndex = DebugManager.instance.PanelIndex("Debug Panel");

            // Since order = int.MinValue, should be first panel
            Assert.AreEqual(0, panelIndex, "Panel with int.MinValue order should be first");
        }

        static IEnumerable<TestCaseData> GetTestWidgets()
        {
            var contexts = new[] { DebugUI.Context.Editor, DebugUI.Context.Runtime };
            foreach (DebugUI.Context ctx in contexts)
            {
                yield return new TestCaseData(WidgetFactory.CreateIntField(data), ctx).SetName($"{ctx}_IntField");
                yield return new TestCaseData(WidgetFactory.CreateIntMinMaxField(data), ctx).SetName($"{ctx}_IntMinMaxField");
                yield return new TestCaseData(WidgetFactory.CreateUIntField(data), ctx).SetName($"{ctx}_CreateUIntField");
                yield return new TestCaseData(WidgetFactory.CreateUIntMinMaxField(data), ctx).SetName($"{ctx}_CreateUIntMinMaxField");
                yield return new TestCaseData(WidgetFactory.CreateFloatField(data), ctx).SetName($"{ctx}_CreateFloatField");
                yield return new TestCaseData(WidgetFactory.CreateFloatMinMaxField(data), ctx).SetName($"{ctx}_CreateFloatMinMaxField");
                yield return new TestCaseData(WidgetFactory.CreateBoolField(data), ctx).SetName($"{ctx}_CreateBoolField");
                yield return new TestCaseData(WidgetFactory.CreateHistoryBoolField(data), ctx).SetName($"{ctx}_CreateHistoryBoolField");
                yield return new TestCaseData(WidgetFactory.CreateEnumField(data), ctx).SetName($"{ctx}_CreateEnumField");
                yield return new TestCaseData(WidgetFactory.CreateHistoryEnumField(data), ctx).SetName($"{ctx}_CreateHistoryEnumField");
                yield return new TestCaseData(WidgetFactory.CreateBitField(data), ctx).SetName($"{ctx}_CreateBitField");
                yield return new TestCaseData(WidgetFactory.CreateColorField(data), ctx).SetName($"{ctx}_CreateColorField");
                yield return new TestCaseData(WidgetFactory.CreateVector2Field(data), ctx).SetName($"{ctx}_CreateVector2Field");
                yield return new TestCaseData(WidgetFactory.CreateVector3Field(data), ctx).SetName($"{ctx}_CreateVector3Field");
                yield return new TestCaseData(WidgetFactory.CreateVector4Field(data), ctx).SetName($"{ctx}_CreateVector4Field");
                yield return new TestCaseData(WidgetFactory.CreateObjectField(data), ctx).SetName($"{ctx}_CreateObjectField");
                yield return new TestCaseData(WidgetFactory.CreateValue(data), ctx).SetName($"{ctx}_CreateValue");
                yield return new TestCaseData(WidgetFactory.CreateValueTuple(data), ctx).SetName($"{ctx}_CreateValueTuple");
                yield return new TestCaseData(WidgetFactory.CreateObjectField(data), ctx).SetName($"{ctx}_CreateObjectField_Duplicate");
                yield return new TestCaseData(WidgetFactory.CreateObjectPopupField(data), ctx).SetName($"{ctx}_CreateObjectPopupField");
                yield return new TestCaseData(WidgetFactory.CreateCamerasField(data), ctx).SetName($"{ctx}_CreateCamerasField");
                yield return new TestCaseData(WidgetFactory.CreateRenderingLayersField(data), ctx).SetName($"{ctx}_CreateRenderingLayersField");
                yield return new TestCaseData(WidgetFactory.CreateProgressBar(data), ctx).SetName($"{ctx}_CreateTable");
            }
        }

        [Test, TestCaseSource(nameof(GetTestWidgets))]
        public void AllIndividualWidgets_ShouldCreateVisualElements(DebugUI.Widget widget, DebugUI.Context ctx)
        {
            CheckWidgetCreatesVisualElement(widget, ctx);
        }

        static IEnumerable<TestCaseData> GetMessageBoxWidgets()
        {
            var contexts = new[] { DebugUI.Context.Editor, DebugUI.Context.Runtime };
            foreach (DebugUI.Context ctx in contexts)
            {
                yield return new TestCaseData(DebugUI.MessageBox.Style.None, ctx).SetName($"{ctx}_Style_None");
                yield return new TestCaseData(DebugUI.MessageBox.Style.Info, ctx).SetName($"{ctx}_Style_Info");
                yield return new TestCaseData(DebugUI.MessageBox.Style.Warning, ctx).SetName($"{ctx}_Style_Warning");
                yield return new TestCaseData(DebugUI.MessageBox.Style.Error, ctx).SetName($"{ctx}_Style_Error");
            }
        }

        [Test, TestCaseSource(nameof(GetMessageBoxWidgets))]
        public void AllMessageBoxes_ShouldCreateVisualElements(DebugUI.MessageBox.Style style, DebugUI.Context ctx)
        {
            var messageBox = WidgetFactory.CreateMessageBox(data, style);
            CheckWidgetCreatesVisualElement(messageBox, ctx);
        }

        [Test]
        public void CompletePanel_AllWidgets_ShouldCreateVisualElements(
            [Values(DebugUI.Context.Runtime, DebugUI.Context.Editor)] DebugUI.Context ctx)
        {
            using var panel = data.CreatePanel() as SettingsPanel;

            Assert.IsNotNull(panel, "Panel should not be null");
            Assert.Greater(panel.Widgets.Length, 0, "Panel should have widgets");

            foreach (var widget in panel.Widgets)
            {
                CheckWidgetCreatesVisualElement(widget, ctx);
            }
        }

        [Test]
        public void AllTableCells_ShouldCreateVisualElements(
            [Values(DebugUI.Context.Runtime, DebugUI.Context.Editor)] DebugUI.Context ctx)
        {
            var table = WidgetFactory.CreateTable(data);

            Assert.IsNotNull(table, "Table should not be null");
            Assert.Greater(table.children.Count, 0, "Table should have rows");

            // Check table itself creates VisualElement
            CheckWidgetCreatesVisualElement(table, ctx);

            // Check each row and cell
            for (int i = 0; i < table.children.Count; i++)
            {
                var row = table.children[i] as DebugUI.Table.Row;
                CheckWidgetCreatesVisualElement(row, ctx);

                // Check each cell in the row
                for (int j = 0; j < row.children.Count; j++)
                {
                    var cell = row.children[j];
                    CheckWidgetCreatesVisualElement(cell, ctx, $"Row {i}, Cell {j}");
                }
            }
        }

        [Test]
        public void FoldoutChildren_ShouldCreateVisualElements(
            [Values(DebugUI.Context.Runtime, DebugUI.Context.Editor)] DebugUI.Context ctx)
        {
            using var panel = data.CreatePanel() as SettingsPanel;
            Assume.That(panel, Is.Not.Null);

            var foldouts = panel.Widgets.ToArray();
            Assert.Greater(foldouts.Length, 0, "Panel should have foldouts");

            foreach (var foldout in foldouts)
            {
                // Check foldout itself
                CheckWidgetCreatesVisualElement(foldout, ctx, $"Foldout '{foldout.displayName}'");

                // Check all children recursively
                CheckAllChildrenCreateVisualElements(foldout, ctx);
            }
        }

        /// <summary>
        /// Helper method to recursively check that a widget and all its children create VisualElements
        /// </summary>
        private void CheckAllChildrenCreateVisualElements(DebugUI.Widget widget, DebugUI.Context context)
        {
            if (widget is DebugUI.Container container)
            {
                foreach (var child in container.children)
                {
                    CheckWidgetCreatesVisualElement(child, context, $"Child of '{widget.displayName}'");
                    CheckAllChildrenCreateVisualElements(child, context); // Recursive check
                }
            }
        }

        /// <summary>
        /// Helper method to check that a specific widget creates a VisualElement
        /// </summary>
        private void CheckWidgetCreatesVisualElement(DebugUI.Widget widget, DebugUI.Context context, string description = null)
        {
            var widgetName = description ?? widget?.displayName ?? widget?.GetType().Name ?? "null widget";
            Assume.That(widget, Is.Not.Null, $"{widgetName} should not be null");

            VisualElement visualElement = null;
            Assert.DoesNotThrow(() => visualElement = widget.ToVisualElement(context),
                $"{widgetName} should create VisualElement without throwing");

            Assert.IsNotNull(visualElement, $"{widgetName} should create non-null VisualElement");
        }

        static TestCaseData[] GetSetFieldTestCases()
        {
            var testData = new DebugPanelDisplaySettings.DebugPanelDisplaySettingsData();

            return new TestCaseData[]
            {
                // Number fields
                new TestCaseData(WidgetFactory.CreateIntField(testData), 42, 13)
                    .SetName("IntField_SetAndGet"),
                new TestCaseData(WidgetFactory.CreateIntMinMaxField(testData), 50, 13)
                    .SetName("IntMinMaxField_SetAndGet"),
                new TestCaseData(WidgetFactory.CreateUIntField(testData), 100u, 23u)
                    .SetName("UIntField_SetAndGet"),
                new TestCaseData(WidgetFactory.CreateUIntMinMaxField(testData), 50u, 23u)
                    .SetName("UIntMinMaxField_SetAndGet"),
                new TestCaseData(WidgetFactory.CreateFloatField(testData), 3.14f, 7.7f)
                    .SetName("FloatField_SetAndGet"),
                new TestCaseData(WidgetFactory.CreateFloatMinMaxField(testData), 50.5f, 7.7f)
                    .SetName("FloatMinMaxField_SetAndGet"),

                // Bool fields
                new TestCaseData(WidgetFactory.CreateBoolField(testData), false, true)
                    .SetName("BoolField_SetAndGet"),

                // Enum fields
                new TestCaseData(WidgetFactory.CreateEnumField(testData),
                    (int)EnumValues.TypeB,
                    (int)EnumValues.None)
                    .SetName("EnumField_SetAndGet"),

                // Color fields
                new TestCaseData(WidgetFactory.CreateColorField(testData), Color.red, Color.darkMagenta)
                    .SetName("ColorField_SetAndGet"),

                // Vector fields
                new TestCaseData(WidgetFactory.CreateVector2Field(testData),
                    new Vector2(1.5f, 2.5f), Vector2.zero)
                    .SetName("Vector2Field_SetAndGet"),
                new TestCaseData(WidgetFactory.CreateVector3Field(testData),
                    new Vector3(1.0f, 2.0f, 3.0f), Vector3.zero)
                    .SetName("Vector3Field_SetAndGet"),
                new TestCaseData(WidgetFactory.CreateVector4Field(testData),
                    new Vector4(1.0f, 2.0f, 3.0f, 4.0f), Vector4.zero)
                    .SetName("Vector4Field_SetAndGet"),
            };
        }

        [Test, TestCaseSource(nameof(GetSetFieldTestCases))]
        public void Field_SetAndGet_ShouldReturnCorrectValue<T>(DebugUI.Widget widget, T newValue, T initialValue)
        {
            var field = widget as DebugUI.Field<T>;
            Assume.That(field, Is.Not.Null, $"Widget is not a Field<{typeof(T).Name}>");

            // Test initial value
            var currentValue = field.GetValue();
            Assert.AreEqual(initialValue, currentValue, $"Initial value should be {initialValue}");

            // Test setting new value
            field.SetValue(newValue);
            var updatedValue = field.GetValue();
            Assert.AreEqual(newValue, updatedValue, $"Value after SetValue should be {newValue}");
        }

        static TestCaseData[] GetSetFieldMinMaxTestCases()
        {
            var testData = new DebugPanelDisplaySettings.DebugPanelDisplaySettingsData();

            return new TestCaseData[]
            {
                // Number fields
                new TestCaseData(WidgetFactory.CreateIntMinMaxField(testData), -100, 100, 42, -200, 200)
                    .SetName("IntMinMaxField_SetAndGet"),
                new TestCaseData(WidgetFactory.CreateUIntMinMaxField(testData), 1u, 100u, 50u, 0u, 200u)
                    .SetName("UIntMinMaxField_SetAndGet"),
                new TestCaseData(WidgetFactory.CreateFloatMinMaxField(testData), -100.0f, 100.0f, 3.14f, -200.0f, 200.0f)
                    .SetName("FloatMinMaxField_SetAndGet"),
            };
        }

        [Test, TestCaseSource(nameof(GetSetFieldMinMaxTestCases))]
        public void Field_SetAndGet_MinMax_ShouldReturnCorrectValue<T>(DebugUI.Widget widget, T minValue, T maxValue, T validValue, T smallerValue, T biggerValue)
        {
            var field = widget as DebugUI.Field<T>;
            Assume.That(field, Is.Not.Null, $"Widget is not a Field<{typeof(T).Name}>");

            field.SetValue(validValue);
            var updatedValue = field.GetValue();
            Assert.AreEqual(updatedValue, validValue, $"Value after SetValue should be {validValue}");

            // Test set an smaller value than min
            field.SetValue(smallerValue);
            updatedValue = field.GetValue();
            Assert.AreNotEqual(smallerValue, updatedValue, $"Value after SetValue smaller than min should not be {biggerValue}");
            Assert.AreEqual(minValue, updatedValue, $"Value after SetValue smaller than min should be {minValue}");

            // Test setting new value
            field.SetValue(biggerValue);
            updatedValue = field.GetValue();
            Assert.AreNotEqual(biggerValue, updatedValue, $"Value after SetValue bigger than max should not be {biggerValue}");
            Assert.AreEqual(maxValue, updatedValue, $"Value after SetValue bigger than max should be {maxValue}");
        }

        static TestCaseData[] OnIncrementOnDecrementTestCases()
        {
            var testData = new DebugPanelDisplaySettings.DebugPanelDisplaySettingsData();

            return new TestCaseData[]
            {
                // Number fields
                new TestCaseData(WidgetFactory.CreateIntField(testData), 13, 14, 23, -1)
                    .SetName("IntField_OnIncrement_OnDecrement"),
                new TestCaseData(WidgetFactory.CreateUIntField(testData), 23u, 24u, 33u, -1)
                    .SetName("UIntField_OnIncrement_OnDecrement"),
                new TestCaseData(WidgetFactory.CreateFloatField(testData), 7.7f, 8.7f, 17.7f, -1)
                    .SetName("FloatField_OnIncrement_OnDecrement"),
                new TestCaseData(WidgetFactory.CreateVector2Field(testData), Vector2.zero, new Vector2(0.025f, 0.0f), new Vector2(10.0f, 0.0f), 0)
                    .SetName("Vector2Field0_OnIncrement_OnDecrement"),
                new TestCaseData(WidgetFactory.CreateVector2Field(testData), Vector2.zero, new Vector2(0.0f, 0.025f), new Vector2(0.0f, 10.0f), 1)
                    .SetName("Vector2Field1_OnIncrement_OnDecrement"),
                new TestCaseData(WidgetFactory.CreateVector3Field(testData), Vector3.zero, new Vector3(0.025f, 0.0f, 0.0f), new Vector3(10.0f, 0.0f, 0.0f), 0)
                    .SetName("Vector3Field0_OnIncrement_OnDecrement"),
                new TestCaseData(WidgetFactory.CreateVector3Field(testData), Vector3.zero, new Vector3(0.0f, 0.025f, 0.0f), new Vector3(0.0f, 10.0f, 0.0f), 1)
                    .SetName("Vector3Field1_OnIncrement_OnDecrement"),
                new TestCaseData(WidgetFactory.CreateVector3Field(testData), Vector3.zero, new Vector3(0.0f, 0.0f, 0.025f), new Vector3(0.0f, 0.0f, 10.0f), 2)
                    .SetName("Vector3Field2_OnIncrement_OnDecrement"),
                new TestCaseData(WidgetFactory.CreateVector4Field(testData), Vector4.zero, new Vector4(0.025f, 0.0f, 0.0f, 0.0f), new Vector4(10.0f, 0.0f, 0.0f, 0.0f), 0)
                    .SetName("Vector4Field0_OnIncrement_OnDecrement"),
                new TestCaseData(WidgetFactory.CreateVector4Field(testData), Vector4.zero, new Vector4(0.0f, 0.025f, 0.0f, 0.0f), new Vector4(0.0f, 10.0f, 0.0f, 0.0f), 1)
                    .SetName("Vector4Field1_OnIncrement_OnDecrement"),
                new TestCaseData(WidgetFactory.CreateVector4Field(testData), Vector4.zero, new Vector4(0.0f, 0.0f, 0.025f, 0.0f), new Vector4(0.0f, 0.0f, 10.0f, 0.0f), 2)
                    .SetName("Vector4Field2_OnIncrement_OnDecrement"),
                new TestCaseData(WidgetFactory.CreateVector4Field(testData), Vector4.zero, new Vector4(0.0f, 0.0f, 0.0f, 0.025f), new Vector4(0.0f, 0.0f, 0.0f, 10.0f), 3)
                    .SetName("Vector4Field3_OnIncrement_OnDecrement"),
            };
        }

        [Test, TestCaseSource(nameof(OnIncrementOnDecrementTestCases))]
        public void Field_OnIncrementOnDecrement_ShouldReturnCorrectValue<T>(DebugUI.Widget widget, T initialStep, T smallStep, T bigStep, int index)
        {
            var field = widget as DebugUI.Field<T>;
            Assume.That(field, Is.Not.Null, $"Widget is not a Field<{typeof(T).Name}>");

            if (index != -1)
            {
                switch (widget)
                {
                    case DebugUI.Vector2Field vector2:
                        vector2.selectedComponent = index;
                        break;
                    case DebugUI.Vector3Field vector3:
                        vector3.selectedComponent = index;
                        break;
                    case DebugUI.Vector4Field vector4:
                        vector4.selectedComponent = index;
                        break;
                }
            }

            var initalValue = field.GetValue();
            AssertEqual(initalValue, initialStep, $"Initial value should be {initialStep}");

            field.OnIncrement(false);
            var updatedValue = field.GetValue();
            AssertEqual(updatedValue, smallStep, $"Value after OnIncrement(false) should be {smallStep}");

            field.OnDecrement(false);
            updatedValue = field.GetValue();
            AssertEqual(updatedValue, initalValue, $"Value after OnDecrement(false) should be {initalValue}");

            field.OnIncrement(true);
            updatedValue = field.GetValue();
            AssertEqual(updatedValue, bigStep, $"Value after OnIncrement(true) should be {bigStep}");

            field.OnDecrement(true);
            updatedValue = field.GetValue();
            AssertEqual(updatedValue, initalValue, $"Value after OnDecrement(true) should be {initalValue}");
        }

        private void AssertEqual<T>(T expected, T actual, string message)
        {
            const float tolerance = 0.0001f;

            if (typeof(T) == typeof(float))
            {
                Assert.AreEqual((float)(object)actual, (float)(object)expected, tolerance, message);
            }
            else if (typeof(T) == typeof(double))
            {
                Assert.AreEqual((double)(object)actual, (double)(object)expected, tolerance, message);
            }
            else
            {
                Assert.AreEqual(actual, expected, message);
            }
        }

        [Test]
        public void NumberHiddenFields_ShouldBeHidden_WhenBothConditionsAreMet()
        {
            var widget = WidgetFactory.CreateNumberHiddenFields(data) as DebugUI.Container;

            Assert.IsNotNull(widget);
            Assert.IsNotNull(widget.isHiddenCallback);

            // Set conditions to show the container (one or both >= 0)
            data.intMinMaxField = 10;
            data.floatMinMaxField = 5.0f;
            Assert.IsFalse(widget.isHiddenCallback(), "Container should be visible when both fields are >= 0");

            // Set one negative
            data.intMinMaxField = -10;
            data.floatMinMaxField = 5.0f;
            Assert.IsFalse(widget.isHiddenCallback(), "Container should be visible when only one field is negative");

            // Set both negative to hide
            data.intMinMaxField = -10;
            data.floatMinMaxField = -5.0f;
            Assert.IsTrue(widget.isHiddenCallback(), "Container should be hidden when both fields are negative");
        }

        [Test]
        public void BoolHiddenFields_ShouldBeHidden_WhenBoolFieldIsTrue()
        {
            var widget = WidgetFactory.CreateBoolHiddenFields(data) as DebugUI.Container;

            Assert.IsNotNull(widget);
            Assert.IsNotNull(widget.isHiddenCallback);

            // Should be hidden when boolField is true
            data.boolField = true;
            Assert.IsTrue(widget.isHiddenCallback(), "Container should be hidden when boolField is true");

            // Should be visible when boolField is false
            data.boolField = false;
            Assert.IsFalse(widget.isHiddenCallback(), "Container should be visible when boolField is false");
        }

        [Test]
        public void EnumHiddenFields_ShouldBeHidden_WhenEnumIsNone()
        {
            var widget = WidgetFactory.CreateEnumHiddenFields(data) as DebugUI.Container;

            Assert.IsNotNull(widget);
            Assert.IsNotNull(widget.isHiddenCallback);

            // Should be hidden when enum is None
            data.enumField = EnumValues.None;
            Assert.IsTrue(widget.isHiddenCallback(), "Container should be hidden when enumField is None");

            // Should be visible for other enum values
            data.enumField = EnumValues.TypeA;
            Assert.IsFalse(widget.isHiddenCallback(), "Container should be visible when enumField is TypeA");

            data.enumField = EnumValues.TypeB;
            Assert.IsFalse(widget.isHiddenCallback(), "Container should be visible when enumField is TypeB");

            data.enumField = EnumValues.TypeC;
            Assert.IsFalse(widget.isHiddenCallback(), "Container should be visible when enumField is TypeC");
        }

        [Test]
        public void SearchFilter_MatchesBasicText()
        {
            var widget = WidgetFactory.CreateIntField(data);
            widget.displayName = "Test Widget";

            var visualElement = widget.ToVisualElement(DebugUI.Context.Editor);
            var textElements = visualElement.Query<TextElement>(className: "debug-window-search-filter-target").ToList();

            var cache = new Dictionary<DebugUI.Widget, WidgetSearchData>
            {
                [widget] = new WidgetSearchData(textElements, string.Empty)
            };

            DebugWindow.PerformSearch(cache, "test", hideRootElementIfNoMatch: true);

            Assert.IsFalse(widget.m_IsHiddenBySearchFilter);
            Assert.IsTrue(textElements[0].text.Contains("<mark="));
        }

        [Test]
        public void SearchFilter_HidesNonMatchingWidget()
        {
            var widget = WidgetFactory.CreateIntField(data);
            widget.displayName = "Alpha";

            var visualElement = widget.ToVisualElement(DebugUI.Context.Editor);
            var textElements = visualElement.Query<TextElement>(className: "debug-window-search-filter-target").ToList();

            var cache = new Dictionary<DebugUI.Widget, WidgetSearchData>
            {
                [widget] = new WidgetSearchData(textElements, string.Empty)
            };

            DebugWindow.PerformSearch(cache, "beta", hideRootElementIfNoMatch: true);

            Assert.IsTrue(widget.m_IsHiddenBySearchFilter);
            Assert.IsFalse(textElements[0].text.Contains("<mark="));
        }

        [Test]
        public void SearchFilter_MatchesAdditionalSearchText()
        {
            var widget = WidgetFactory.CreateEnumField(data);
            widget.displayName = "Mode";

            var visualElement = widget.ToVisualElement(DebugUI.Context.Editor);
            var textElements = visualElement.Query<TextElement>(className: "debug-window-search-filter-target").ToList();
            string aggregatedText = DebugWindow.CollectAggregatedAdditionalSearchText(widget);

            var cache = new Dictionary<DebugUI.Widget, WidgetSearchData>
            {
                [widget] = new WidgetSearchData(textElements, aggregatedText)
            };

            DebugWindow.PerformSearch(cache, "Type A", hideRootElementIfNoMatch: true);

            Assert.IsFalse(widget.m_IsHiddenBySearchFilter);
        }

        [Test]
        public void SearchFilter_ContainerVisibilityWithMatchingChild()
        {
            var container = new DebugUI.Foldout { displayName = "Container" };
            var child = WidgetFactory.CreateIntField(data);
            child.displayName = "Matching Child";
            container.children.Add(child);

            var containerVisualElement = container.ToVisualElement(DebugUI.Context.Editor);
            var childVisualElement = child.ToVisualElement(DebugUI.Context.Editor);

            var containerTextElements = containerVisualElement.Query<TextElement>(className: "debug-window-search-filter-target").ToList();
            var childTextElements = childVisualElement.Query<TextElement>(className: "debug-window-search-filter-target").ToList();

            var cache = new Dictionary<DebugUI.Widget, WidgetSearchData>
            {
                [container] = new WidgetSearchData(containerTextElements, DebugWindow.CollectAggregatedAdditionalSearchText(container)),
                [child] = new WidgetSearchData(childTextElements, DebugWindow.CollectAggregatedAdditionalSearchText(child))
            };

            DebugWindow.PerformSearch(cache, "Matching", hideRootElementIfNoMatch: true);

            Assert.IsFalse(container.m_IsHiddenBySearchFilter);
            Assert.IsFalse(child.m_IsHiddenBySearchFilter);
        }

        [Test]
        public void SearchFilter_ChildVisibleWhenParentDoesntMatch()
        {
            var container = new DebugUI.Foldout { displayName = "Container" };
            var child = WidgetFactory.CreateIntField(data);
            child.displayName = "MatchingChild";
            container.children.Add(child);

            var containerVisualElement = container.ToVisualElement(DebugUI.Context.Editor);
            var childVisualElement = child.ToVisualElement(DebugUI.Context.Editor);

            var containerTextElements = containerVisualElement.Query<TextElement>(className: "debug-window-search-filter-target").ToList();
            var childTextElements = childVisualElement.Query<TextElement>(className: "debug-window-search-filter-target").ToList();

            var cache = new Dictionary<DebugUI.Widget, WidgetSearchData>
            {
                [container] = new WidgetSearchData(containerTextElements, DebugWindow.CollectAggregatedAdditionalSearchText(container)),
                [child] = new WidgetSearchData(childTextElements, DebugWindow.CollectAggregatedAdditionalSearchText(child))
            };

            DebugWindow.PerformSearch(cache, "MatchingChild", hideRootElementIfNoMatch: true);

            Assert.IsFalse(child.m_IsHiddenBySearchFilter);
            Assert.IsFalse(container.m_IsHiddenBySearchFilter);
        }

        [Test]
        public void SearchFilter_ChildVisibleViaAdditionalTextWhenParentDoesntMatch()
        {
            var container = new DebugUI.Foldout { displayName = "Container" };
            var child = WidgetFactory.CreateEnumField(data);
            child.displayName = "EnumMode";
            container.children.Add(child);

            var containerVisualElement = container.ToVisualElement(DebugUI.Context.Editor);
            var childVisualElement = child.ToVisualElement(DebugUI.Context.Editor);

            var containerTextElements = containerVisualElement.Query<TextElement>(className: "debug-window-search-filter-target").ToList();
            var childTextElements = childVisualElement.Query<TextElement>(className: "debug-window-search-filter-target").ToList();

            var cache = new Dictionary<DebugUI.Widget, WidgetSearchData>
            {
                [container] = new WidgetSearchData(containerTextElements, DebugWindow.CollectAggregatedAdditionalSearchText(container)),
                [child] = new WidgetSearchData(childTextElements, DebugWindow.CollectAggregatedAdditionalSearchText(child))
            };

            DebugWindow.PerformSearch(cache, "Type B", hideRootElementIfNoMatch: true);

            Assert.IsFalse(child.m_IsHiddenBySearchFilter);
            Assert.IsFalse(container.m_IsHiddenBySearchFilter);
        }

        [Test]
        public void SearchFilter_CaseInsensitiveMatching()
        {
            var widget = WidgetFactory.CreateIntField(data);
            widget.displayName = "TestWidget";

            var visualElement = widget.ToVisualElement(DebugUI.Context.Editor);
            var textElements = visualElement.Query<TextElement>(className: "debug-window-search-filter-target").ToList();

            var cache = new Dictionary<DebugUI.Widget, WidgetSearchData>
            {
                [widget] = new WidgetSearchData(textElements, string.Empty)
            };

            string[] searchStrings = { "testwidget", "TESTWIDGET", "TeStWiDgEt" };

            foreach (var searchString in searchStrings)
            {
                widget.m_IsHiddenBySearchFilter = false;
                DebugWindow.PerformSearch(cache, searchString, hideRootElementIfNoMatch: true);
                Assert.IsFalse(widget.m_IsHiddenBySearchFilter);
            }
        }

        [Test]
        public void SearchFilter_EmptySearchShowsAll()
        {
            var widget1 = WidgetFactory.CreateIntField(data);
            widget1.displayName = "Alpha";
            var widget2 = WidgetFactory.CreateIntField(data);
            widget2.displayName = "Beta";

            var visualElement1 = widget1.ToVisualElement(DebugUI.Context.Editor);
            var visualElement2 = widget2.ToVisualElement(DebugUI.Context.Editor);

            var textElements1 = visualElement1.Query<TextElement>(className: "debug-window-search-filter-target").ToList();
            var textElements2 = visualElement2.Query<TextElement>(className: "debug-window-search-filter-target").ToList();

            var cache = new Dictionary<DebugUI.Widget, WidgetSearchData>
            {
                [widget1] = new WidgetSearchData(textElements1, string.Empty),
                [widget2] = new WidgetSearchData(textElements2, string.Empty)
            };

            DebugWindow.PerformSearch(cache, "Alpha", hideRootElementIfNoMatch: true);

            Assert.IsFalse(widget1.m_IsHiddenBySearchFilter);
            Assert.IsTrue(widget2.m_IsHiddenBySearchFilter);

            DebugWindow.PerformSearch(cache, "", hideRootElementIfNoMatch: true);

            Assert.IsFalse(widget1.m_IsHiddenBySearchFilter);
            Assert.IsFalse(widget2.m_IsHiddenBySearchFilter);
        }
    }
}
#endif
