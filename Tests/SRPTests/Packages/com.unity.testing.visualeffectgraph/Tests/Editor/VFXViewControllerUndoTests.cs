using System.Linq;
using NUnit.Framework;
using UnityEditor.VFX.Operator;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.Test
{
    public class VFXViewControllerUndoTests
    {
        const float k_InitialMinValue = 0.0f;

        VFXGraph m_Graph;
        VFXViewWindow m_Window;

        [SetUp]
        public void SetUp()
        {
            m_Graph = VFXTestCommon.MakeTemporaryGraph();
            var operatorModel = ScriptableObject.CreateInstance<Operator.Random>();
            m_Graph.AddChild(operatorModel);
            VFXTestCommon.ReimportVFXGraph(m_Graph);
        }

        [TearDown]
        public void TearDown()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
            VFXTestCommon.CloseAllVFXWindow();
            VFXTestCommon.DeleteAllTemporaryGraph();
            Undo.ClearAll();
        }

        void OpenGraph()
        {
            m_Window = VFXTestCommon.GetWindow(m_Graph, createIfNeeded: true);
            m_Window.LoadResource(m_Graph.GetResource());
        }

        VFXSlot GetMinSlot()
        {
            return m_Graph.children.OfType<Operator.Random>().Single().inputSlots[0];
        }

        void SetMinValueFromUI(float newValue, bool newUndoGroup = true)
        {
            if (newUndoGroup)
                Undo.IncrementCurrentGroup();

            var minSlot = GetMinSlot();
            var minProperty = m_Window.rootVisualElement
                .Query<FloatPropertyRM>()
                .Where(p => p.provider is VFXDataAnchorController anchor && anchor.model == minSlot)
                .First();
            var minField = minProperty?.Q<FloatField>();
            Assert.That(minField, Is.Not.Null, "Could not find the UI field for the Random operator Min slot");

            minField.value = newValue;
            Assert.That(minSlot.value, Is.EqualTo(newValue), "Setting the UI field did not change the slot value");
        }

        [Test]
        public void GraphWithOperator_EditAndUndo_GraphIsNotDirty()
        {
            OpenGraph();
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.False);

            SetMinValueFromUI(0.25f);
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.True);

            Undo.PerformUndo();

            Assert.That(GetMinSlot().value, Is.EqualTo(k_InitialMinValue));
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.False);
        }

        [Test]
        public void GraphWithOperator_EditAndUndoAndRedo_GraphIsDirty()
        {
            OpenGraph();
            SetMinValueFromUI(0.25f);
            Undo.PerformUndo();
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.False);

            Undo.PerformRedo();

            Assert.That(GetMinSlot().value, Is.EqualTo(0.25f));
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.True);
        }

        [Test]
        public void GraphWithOperator_EditAndSaveAndUndo_GraphIsDirty()
        {
            OpenGraph();
            SetMinValueFromUI(0.25f);
            m_Window.SaveChanges();
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.False);

            Undo.PerformUndo();

            Assert.That(GetMinSlot().value, Is.EqualTo(k_InitialMinValue));
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.True);
        }

        [Test]
        public void GraphWithOperator_EditAndSaveAndUndoAndRedo_GraphIsNotDirty()
        {
            OpenGraph();
            SetMinValueFromUI(0.25f);
            m_Window.SaveChanges();
            Undo.PerformUndo();
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.True);

            Undo.PerformRedo();

            Assert.That(GetMinSlot().value, Is.EqualTo(0.25f));
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.False);
        }

        [Test]
        public void GraphWithOperator_SaveAndUndoAndNewEditAndUndoAndRedo_GraphIsDirty()
        {
            OpenGraph();
            SetMinValueFromUI(0.25f);
            SetMinValueFromUI(0.5f);
            m_Window.SaveChanges();
            Undo.PerformUndo();

            // Editing here invalidates any redos already on the stack, one of which
            // represented our save point.
            SetMinValueFromUI(0.75f);
            Undo.PerformUndo();
            Undo.PerformRedo();

            Assert.That(GetMinSlot().value, Is.EqualTo(0.75f));
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.True);
        }

        [Test]
        public void GraphWithOperator_EditAndSaveAssetsAndUndo_GraphIsDirty()
        {
            OpenGraph();
            SetMinValueFromUI(0.25f);

            // Save As and Rebuild And Save All write the graph through OnWillSaveAssets, not the window
            AssetDatabase.SaveAssets();
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.False);

            Undo.PerformUndo();

            Assert.That(GetMinSlot().value, Is.EqualTo(k_InitialMinValue));
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.True);
        }

        [Test]
        public void GraphWithOperator_EditAndSaveAsAndUndo_GraphIsDirty()
        {
            OpenGraph();
            SetMinValueFromUI(0.25f);

            var copyPath = VFXTestCommon.tempBasePath + "vfx_" + System.Guid.NewGuid() + ".vfx";
            m_Window.graphView.SaveAs(copyPath);
            Assert.That(AssetDatabase.LoadAssetAtPath<UnityEngine.VFX.VisualEffectAsset>(copyPath), Is.Not.Null);
            Assert.That(m_Window.graphView.controller.graph, Is.EqualTo(m_Graph));
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.False);
            var originalContent = System.IO.File.ReadAllText(AssetDatabase.GetAssetPath(m_Graph));
            Assert.That(originalContent, Does.Contain("m_SerializableObject: 0.25"));

            Undo.PerformUndo();

            Assert.That(GetMinSlot().value, Is.EqualTo(k_InitialMinValue));
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.True);
        }

        [Test]
        public void GraphWithOperator_SaveAndEditAndChangeCategoryPathAndUndo_GraphIsDirtyUntilPathIsRestored()
        {
            OpenGraph();
            m_Graph.categoryPath = "QA/Saved";
            m_Window.SaveChanges();
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.False);

            SetMinValueFromUI(0.25f);
            Undo.IncrementCurrentGroup();
            m_Graph.categoryPath = "QA/Unsaved";
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.True);

            Undo.PerformUndo();
            Assert.That(m_Graph.categoryPath, Is.EqualTo("QA/Saved"));
            Assert.That(GetMinSlot().value, Is.EqualTo(0.25f));
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.True);

            Undo.PerformUndo();
            Assert.That(GetMinSlot().value, Is.EqualTo(k_InitialMinValue));
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.False);
        }

        [Test]
        public void GraphWithOperator_EditInSameUndoGroupAfterSaveAndUndo_GraphIsNotDirty()
        {
            OpenGraph();
            SetMinValueFromUI(0.25f);
            m_Window.SaveChanges();
            SetMinValueFromUI(0.5f, newUndoGroup: false);
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.True);

            Undo.PerformUndo();
            Assert.That(GetMinSlot().value, Is.EqualTo(0.25f));
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.False);

            Undo.PerformRedo();
            Assert.That(GetMinSlot().value, Is.EqualTo(0.5f));
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.True);
        }

        [Test]
        public void GraphWithOperator_SaveAndChangeWithoutUndoStepAndEditAndUndo_GraphIsDirty()
        {
            OpenGraph();
            m_Window.SaveChanges();
            m_Graph.children.OfType<Operator.Random>().Single().ExpandPath("min");
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.True);

            SetMinValueFromUI(0.25f);
            Undo.PerformUndo();

            Assert.That(EditorUtility.IsDirty(m_Graph), Is.True);
        }

        [Test]
        public void GraphDirtyWhenOpened_EditAndUndo_GraphIsDirty()
        {
            EditorUtility.SetDirty(m_Graph);
            OpenGraph();
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.True,
                "Opening the graph unexpectedly cleared its dirty flag");

            SetMinValueFromUI(0.25f);
            Undo.PerformUndo();

            Assert.That(EditorUtility.IsDirty(m_Graph), Is.True);
        }

        [Test]
        public void GraphWithOperator_AddOperatorAndUndo_GraphIsNotDirty()
        {
            OpenGraph();

            Undo.IncrementCurrentGroup();
            var controller = m_Window.graphView.controller;
            controller.AddVFXModel(Vector2.zero, ScriptableObject.CreateInstance<Absolute>());
            controller.ApplyChanges();
            Assert.That(m_Graph.children.Count(), Is.EqualTo(2));
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.True);

            Undo.PerformUndo();

            Assert.That(m_Graph.children.Count(), Is.EqualTo(1));
            Assert.That(EditorUtility.IsDirty(m_Graph), Is.False);
        }
    }
}
