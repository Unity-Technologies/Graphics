### Added
- `IModel`, a base interface for models.
- Model for the inspector: `IInspectorModel`, `IInspectorSectionModel`, `InspectorModel`, `InspectorSectionModel`.
- `IStencil.CreateInspectorModel()` to create an inspector model.
- `ModelInspectorStateComponent` now keeps track of changes.
- `ModelInspectorStateComponent.InspectedModel`, the inspected model.
- `ModelInspectorStateComponent.GetInspectorModel()`, to get the inspector model.
- `ModelInspectorStateComponent.StateUpdater.UpdateTitle` to update the inspector title when the inspected model changes.
- `ModelInspectorStateComponent.StateUpdater.SetSectionCollapsed` to collapse and expand inspector sections.
- `NodePortsInspector`, an inspector to edit all the port default values of a node.
- `CollapseInspectorSectionCommand`, `InspectorSection` and `CollapsibleSection` and `InspectorSectionContext` for inspector section UI.
- `CollapsibleSectionHeader`, the header of a `CollapsibleSection`.
- `PortField`, a field used by the inspector to edit port constant values.
- `IConstant.Initialize()` to initialize a constant to the default value of its `TypeHandle`.
- `IConstant.Clone()` to clone a constant.
- `IConstant.GetTypeHandle()` to get the type handle of a constant.
- `MissingFieldEditor`, a `BaseModelPropertyField` to use when no proper field is found.
- A section in the node UI named `Node.nodeSettingsContainerPartName` to show the node basic settings.
- `ModelSettingAttribute` to mark a serialized field as being a graph or node setting.
- `InspectorUsePropertyAttribute`, to tell the local inspector to use a property to get or set a field.

### Removed
- `IPortModel.DisableEmbeddedValueEditor`. No replacement provided.
- `UpdatePortConstantCommand`: use `UpdateConstantValueCommand` instead.
- `ConstantEditorExtensions.UpdateConnectedStatus`: there is no need to call this anymore as `ConstantField` update its look automatically.
- `ConstantEditorExtensions.BuildValueEditor<T>`: use `new ConstantField()` or `InlineValueEditor.CreateEditorForConstant()` instead.
- `CloneHelpers.CloneConstant()`: use `IConstant.Clone()` instead.
- `IBaseModelPropertyField`: use abstract class `BaseModelPropertyField` instead.

### Changed
- The signature for constant editor extension methods has changed. They should now return a `BaseModelPropertyField` instead of a `VisualElement`.
- `IConstantEditorBuilder.OnValueChanged` is now obsolete. Register a custom command handler for `UpdateConstantValueCommand` instead.
- `IConstantEditorBuilder.PortModel` changed to the more general `IConstantEditorBuilder.ConstantOwner`.
- `IConstantNodeModel.PredefineSetup()` renamed to `Initialize()`, now takes a `TypeHandle` parameter to define the type of the underlying `IConstant`.
- `BaseModelPropertyField` simplified, with most of its content moved to the new `CustomizableModelPropertyField`.
- Renamed `SetModelFieldCommand` to `SetInspectedObjectFieldCommand`.
- `ICustomPropertyField.Build()` now have a `string tooltip` parameter.
