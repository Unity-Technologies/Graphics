using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    [Serializable]
    class FakeNodeWithSettings : Type0FakeNodeModel
    {
        public const string tooltip = "Test";

        [SerializeField]
        [ModelSetting]
        [Tooltip(tooltip)]
        internal int m_FakenessLevel;

        [SerializeField]
        [ModelSetting]
        [InspectorUseProperty(nameof(SpecialFakenessLevel))]
        internal double m_SpecialFakenessLevel;

        [SerializeField]
        [ModelSetting]
        [InspectorUseSetterMethod(nameof(SetSpecialFakenessSubLevel))]
        internal double m_SpecialFakenessSubLevel;

        [SerializeField]
        internal int m_AdvancedFakeness;

        [SerializeField, HideInInspector]
        internal int m_HiddenFakeness;

        public double SpecialFakenessLevel
        {
            get => m_SpecialFakenessLevel;
            set => m_SpecialFakenessLevel = value / 2;
        }

        public void SetSpecialFakenessSubLevel(double v,
            out IEnumerable<IGraphElementModel> newModels,
            out IEnumerable<IGraphElementModel> changedModels,
            out IEnumerable<IGraphElementModel> deletedModels)
        {
            m_SpecialFakenessSubLevel = v / 3;
            newModels = null;
            changedModels = Enumerable.Empty<IGraphElementModel>();
            deletedModels = new List<IGraphElementModel>();
        }
    }

    class InspectorTests : BaseUIFixture
    {
        /// <inheritdoc />
        protected override bool CreateGraphOnStartup => true;

        /// <inheritdoc />
        protected override bool WithSidePanel => true;

        public override void SetUp()
        {
            base.SetUp();
#if UNITY_2022_2_OR_NEWER
            if (Window.TryGetOverlay(ModelInspectorOverlay.idValue, out var inspectorOverlay))
            {
                inspectorOverlay.displayed = true;
            }
#endif
        }

        ModelInspectorView GetInspectorView()
        {
#if UNITY_2022_2_OR_NEWER
            Window.TryGetOverlay(ModelInspectorOverlay.idValue, out var inspectorOverlay);
            var overlayRoot = inspectorOverlay == null ? null : GraphViewStaticBridge.GetOverlayRoot(inspectorOverlay);
            return overlayRoot?.Q<ModelInspectorView>();
#else
            return  Window.ModelInspectorView;
#endif
        }

        object GetInspectedModel()
        {
            return GetInspectorView()?.ModelInspectorViewModel.ModelInspectorState.InspectedModels.FirstOrDefault();
        }

        [UnityTest]
        public IEnumerator GraphIsInspectedByDefault()
        {
            GraphModel.CreateNode<Type0FakeNodeModel>();
            MarkGraphModelStateDirty();
            yield return null;

            var inspectedModel = GetInspectedModel();
            Assert.AreSame(GraphModel, inspectedModel);
        }

        void SelectNodeToShowInspector(INodeModel nodeModel)
        {
            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, nodeModel));
        }

        [UnityTest]
        public IEnumerator SelectedNodeIsInspectedOtherwiseGraphIsInspected()
        {
            var nodeModel = GraphModel.CreateNode<Type0FakeNodeModel>();
            MarkGraphModelStateDirty();
            yield return null;

            SelectNodeToShowInspector(nodeModel);
            yield return null;

            var inspectedModel = GetInspectedModel();
            Assert.AreSame(nodeModel, inspectedModel);

            // Unselect node to show graph inspector.
            GraphView.Dispatch(new ClearSelectionCommand());
            yield return null;

            inspectedModel = GetInspectedModel();
            Assert.AreSame(GraphModel, inspectedModel);
        }

        [UnityTest]
        public IEnumerator BasicNodeInspectorHasThreeSections()
        {
            var nodeModel = GraphModel.CreateNode<FakeNodeWithSettings>();
            MarkGraphModelStateDirty();
            yield return null;

            SelectNodeToShowInspector(nodeModel);
            yield return null;

            var modelInspector = GetInspectorView();
            Assert.IsNotNull(modelInspector);
            var inspectors = modelInspector.Query<ModelInspector>().ToList();
            Assert.AreEqual(3, inspectors.Count);

            Assert.IsInstanceOf<SerializedFieldsInspector>(inspectors[0].PartList.GetPart(ModelInspector.fieldsPartName));
            Assert.IsInstanceOf<NodePortsInspector>(inspectors[1].PartList.GetPart(ModelInspector.fieldsPartName));
            Assert.IsInstanceOf<SerializedFieldsInspector>(inspectors[2].PartList.GetPart(ModelInspector.fieldsPartName));
        }

        [UnityTest]
        public IEnumerator ModelSettingsFieldIsInBasicSettingsSection()
        {
            var nodeModel = GraphModel.CreateNode<FakeNodeWithSettings>();
            MarkGraphModelStateDirty();
            yield return null;

            SelectNodeToShowInspector(nodeModel);
            yield return null;

            var modelInspector = GetInspectorView();
            var firstInspector = modelInspector.Query<ModelInspector>().First();
            var fieldInspector = firstInspector.PartList.Parts.FirstOrDefault(p => p.PartName == ModelInspector.fieldsPartName) as FieldsInspector;
            Assert.IsNotNull(fieldInspector);
            Assert.AreEqual(3, fieldInspector.FieldCount);

            var propertyFieldLabel = fieldInspector.Root.SafeQ<BaseModelPropertyField>().SafeQ<Label>();
            Assert.AreEqual(ObjectNames.NicifyVariableName(nameof(FakeNodeWithSettings.m_FakenessLevel)), propertyFieldLabel.text);
        }

        [UnityTest]
        public IEnumerator NonModelSettingsFieldIsInAdvancedSettingsSectionAndHiddenSettingIsNot()
        {
            var nodeModel = GraphModel.CreateNode<FakeNodeWithSettings>();
            MarkGraphModelStateDirty();
            yield return null;

            SelectNodeToShowInspector(nodeModel);
            yield return null;

            var modelInspector = GetInspectorView();
            var lastInspector = modelInspector.Query<ModelInspector>().Last();
            var fieldInspector = lastInspector.PartList.Parts.FirstOrDefault(p => p.PartName == ModelInspector.fieldsPartName) as FieldsInspector;
            Assert.IsNotNull(fieldInspector);
            Assert.AreEqual(1, fieldInspector.FieldCount, "Too many fields. Is HideInInspector field there? It should not.");

            var propertyFieldLabel = fieldInspector.Root.SafeQ<BaseModelPropertyField>().SafeQ<Label>();
            Assert.AreEqual(ObjectNames.NicifyVariableName(nameof(FakeNodeWithSettings.m_AdvancedFakeness)), propertyFieldLabel.text);
        }

        [UnityTest]
        public IEnumerator FieldTooltipIsUsed()
        {
            var nodeModel = GraphModel.CreateNode<FakeNodeWithSettings>();
            MarkGraphModelStateDirty();
            yield return null;

            SelectNodeToShowInspector(nodeModel);
            yield return null;

            var modelInspector = GetInspectorView();
            var firstInspector = modelInspector.Query<ModelInspector>().First();
            var fieldInspector = firstInspector.PartList.Parts.FirstOrDefault(p => p.PartName == ModelInspector.fieldsPartName) as FieldsInspector;
            Assert.IsNotNull(fieldInspector);

            var propertyField = fieldInspector.Root.SafeQ<BaseModelPropertyField>().SafeQ<BaseField<int>>();
            Assert.AreEqual(FakeNodeWithSettings.tooltip, propertyField.tooltip);
        }

        [UnityTest]
        public IEnumerator CollapsibleSectionCollapsesOnClickInHeader()
        {
            var nodeModel = GraphModel.CreateNode<FakeNodeWithSettings>();
            MarkGraphModelStateDirty();
            yield return null;

            SelectNodeToShowInspector(nodeModel);
            yield return null;

            var modelInspector = GetInspectorView();
            var collapsibleSection = modelInspector.Query<CollapsibleSection>().First();
            Assert.IsNotNull(collapsibleSection);
            var collapsibleSectionHeader = collapsibleSection.Query<CollapsibleSectionHeader>().First();
            Assert.IsNotNull(collapsibleSectionHeader);

            var model = collapsibleSection.Model as IInspectorSectionModel;
            Assert.IsNotNull(model);
            var initialState = model.Collapsed;

            var p = collapsibleSectionHeader.parent.LocalToWorld(collapsibleSectionHeader.layout.center);
            Helpers.Click(p);
            yield return null;

            Assert.AreNotEqual(initialState, model.Collapsed);
        }

        [UnityTest]
        public IEnumerator InspectorHasFieldForEveryPort()
        {
            var nodeModel = GraphModel.CreateNode<FakeNodeWithSettings>();
            (string name, TypeHandle type)[] ports = new  []
            {
                ("Blah", TypeHandle.Vector4),
                ("Bleh", TypeHandle.Float),
                ("Blih", TypeHandle.String),
                ("Bloh", TypeHandle.Bool),
                ("Bluh", TypeHandle.Int),
            };
            foreach (var port in ports)
            {
                nodeModel.AddInputPort(port.name, PortType.Data, port.type, options: PortModelOptions.Default);
            }
            MarkGraphModelStateDirty();
            yield return null;

            SelectNodeToShowInspector(nodeModel);
            yield return null;

            var modelInspector = GetInspectorView();
            var secondInspector = modelInspector.Query<ModelInspector>().ToList().ElementAt(1);
            var portInspector = secondInspector.PartList.Parts.FirstOrDefault(p => p.PartName == ModelInspector.fieldsPartName) as FieldsInspector;
            Assert.IsNotNull(portInspector);

            var fields = portInspector.Root.Query<BaseModelPropertyField>().ToList();
            foreach (var port in ports)
            {
                var field = fields.FirstOrDefault(f => f.SafeQ<Label>().text == port.name);
                Assert.IsNotNull(field, $"Field for port {port.name} not found.");
            }
        }

        [UnityTest]
        public IEnumerator UpdateFieldChangesNode()
        {
            var nodeModel = GraphModel.CreateNode<FakeNodeWithSettings>();
            nodeModel.m_FakenessLevel = 0;
            MarkGraphModelStateDirty();
            yield return null;

            SelectNodeToShowInspector(nodeModel);
            yield return null;

            var modelInspector = GetInspectorView();
            var inspector = modelInspector.Query<ModelInspector>().First();
            var settingsInspector = inspector.PartList.Parts.FirstOrDefault(p => p.PartName == ModelInspector.fieldsPartName) as FieldsInspector;
            Assert.IsNotNull(settingsInspector);

            // Get the field to edit m_FakenessLevel
            var field = settingsInspector.Root.Query<BaseField<int>>().First();
            Assert.IsNotNull(field);
            Assert.AreEqual(ObjectNames.NicifyVariableName(nameof(FakeNodeWithSettings.m_FakenessLevel)), field.label);

            field.value = 42;
            yield return null;

            Assert.AreEqual(42, nodeModel.m_FakenessLevel);
        }

        [UnityTest]
        public IEnumerator UpdateFieldChangesNodeUsingProperty()
        {
            var nodeModel = GraphModel.CreateNode<FakeNodeWithSettings>();
            nodeModel.m_FakenessLevel = 0;
            MarkGraphModelStateDirty();
            yield return null;

            SelectNodeToShowInspector(nodeModel);
            yield return null;

            var modelInspector = GetInspectorView();
            var inspector = modelInspector.Query<ModelInspector>().First();
            var settingsInspector = inspector.PartList.Parts.FirstOrDefault(p => p.PartName == ModelInspector.fieldsPartName) as FieldsInspector;
            Assert.IsNotNull(settingsInspector);

            // Get the field to edit m_SpecialFakenessLevel
            var field = settingsInspector.Root.Query<BaseField<double>>().First();
            Assert.IsNotNull(field);
            Assert.AreEqual(ObjectNames.NicifyVariableName(nameof(FakeNodeWithSettings.m_SpecialFakenessLevel)), field.label);

            field.value = 42.0;
            yield return null;

            Assert.AreEqual(42.0/2, nodeModel.m_SpecialFakenessLevel);
        }

        [UnityTest]
        public IEnumerator UpdateFieldChangesNodeUsingSetterMethod()
        {
            var nodeModel = GraphModel.CreateNode<FakeNodeWithSettings>();
            nodeModel.m_FakenessLevel = 0;
            MarkGraphModelStateDirty();
            yield return null;

            SelectNodeToShowInspector(nodeModel);
            yield return null;

            var modelInspector = GetInspectorView();
            var inspector = modelInspector.Query<ModelInspector>().First();
            var settingsInspector = inspector.PartList.Parts.FirstOrDefault(p => p.PartName == ModelInspector.fieldsPartName) as FieldsInspector;
            Assert.IsNotNull(settingsInspector);

            // Get the field to edit m_SpecialFakenessSubLevel
            var field = settingsInspector.Root.Query<BaseField<double>>().ToList()[1];
            Assert.IsNotNull(field);
            Assert.AreEqual(ObjectNames.NicifyVariableName(nameof(FakeNodeWithSettings.m_SpecialFakenessSubLevel)), field.label);

            field.value = 42.0;
            yield return null;

            Assert.AreEqual(42.0/3, nodeModel.m_SpecialFakenessSubLevel);
        }

        [UnityTest]
        public IEnumerator ChangingNodeUpdatesInspectorField()
        {
            var nodeModel = GraphModel.CreateNode<FakeNodeWithSettings>();
            nodeModel.m_FakenessLevel = 5;
            MarkGraphModelStateDirty();
            yield return null;

            SelectNodeToShowInspector(nodeModel);
            yield return null;

            var modelInspector = GetInspectorView();
            var inspector = modelInspector.Query<ModelInspector>().First();
            var settingsInspector = inspector.PartList.Parts.FirstOrDefault(p => p.PartName == ModelInspector.fieldsPartName) as FieldsInspector;
            Assert.IsNotNull(settingsInspector);

            // Get the field to edit m_FakenessLevel
            var field = settingsInspector.Root.Query<BaseField<int>>().First();
            Assert.IsNotNull(field);
            Assert.AreEqual(ObjectNames.NicifyVariableName(nameof(FakeNodeWithSettings.m_FakenessLevel)), field.label);
            Assert.AreEqual(5, field.value);

            var fieldInfo = typeof(FakeNodeWithSettings).GetField(nameof(FakeNodeWithSettings.m_FakenessLevel), BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fieldInfo);
            var command = new SetInspectedGraphElementModelFieldCommand(42, nodeModel, nodeModel, fieldInfo);
            modelInspector.Dispatch(command);
            yield return null;

            Assert.AreEqual(42, nodeModel.m_FakenessLevel, "Value not updated on node.");
            Assert.AreEqual(42, field.value, "Value not updated in UI.");
        }

        class TestableNodePortsInspector : NodePortsInspector
        {
            /// <inheritdoc />
            public TestableNodePortsInspector(string name, IModel model, IModelView ownerElement, string parentClassName)
                : base(name, model, ownerElement, parentClassName) { }

            public IEnumerable<IPortModel> TestableGetPortsToDisplay()
            {
                return GetPortsToDisplay();
            }
        }

        [Test]
        public void NodePortInspectorShowsInputPorts()
        {
            var nodeModel = GraphModel.CreateNode<Type0FakeNodeModel>();
            var inspector = new TestableNodePortsInspector("Test", nodeModel, null, "");
            var expectedPorts = nodeModel.GetInputPorts().Where(p => p.EmbeddedValue != null);

            Assert.AreEqual(expectedPorts, inspector.TestableGetPortsToDisplay());
        }
    }
}
