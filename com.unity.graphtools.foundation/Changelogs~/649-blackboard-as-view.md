### Added

- `BlackboardView`, the host view element for the blackboard.
- `GraphViewClickSelector`, a concrete `ClickSelector` for the `GraphView`.
- `SelectionDropper.GetDraggedElements()`, to get the elements being dragged.
- `BlackboardElement`, the base class for UI elements in the blackboard.
- `ViewSelection`, a class handling cut/copy/paste operations on the selection.
- `BlackboardViewSelection`, `GraphViewSelection`: implementation of `ViewSelection` for the `BlackboardView` and the `GraphView`, respectively.

### Changed

- `ClickSelector` is now an abstract class.
- `ClickSelector.HandleClick()` is now abstract.
- `Blackboard` is now a `BlackboardElement`, instead of `GraphElement`, `IDragSource`.
- `BlackboardField` is now a `BlackboardElement`.
- `BlackboardGroup` is now a `BlackboardElement`.
- `BlackboardRow` is now a `BlackboardElement`.
- `BlackboardVariablePropertyView` is now a `BlackboardElement`
- `GraphElement.Rename()` moved to parent class `ModelUI`.
- `GraphElement.IsRenameKey()` moved to parent class `ModelUI`.
- `GraphModel.RemoveVariableDeclaration()` now returns the removed item's parent.
- `IGraphModel.DeleteVariableDeclarations()` now returns a `GraphChangeDescription`.
- `IGraphModel.DeleteGroups()` now returns a `GraphChangeDescription`.
- `GraphModelExtensions.DeleteVariableDeclaration()` now returns a `GraphChangeDescription`.
- `GraphModelExtensions.DeleteElements()` now returns a `GraphChangeDescription`.

### Removed

- `Blackboard.persistenceKey`.
- `Blackboard.m_ScrollView` : use `ScrollView`.
- `Blackboard.Dragger`: no alternative provided.
- `Blackboard.Windowed`: replace by `true`.
- `Blackboard.OnValidateCommand`: moved to `ViewSelection`.
- `Blackboard.OnExecuteCommand`: moved to `ViewSelection`.
- `Blackboard.OnRenameKeyDown`: moved to `BlackboardView`.
- `Blackboard.ContinuousSelectionOn`: moved to `BlackboardView.ExtendSelection()`.
- `Blackboard.GetSelection()`: use `BlackboardView.ViewSelection.GetSelection()` instead.
- `BlackboardGroup.Section`: use `UQuery`.
- `GraphView.CanPasteSerializedDataDelegate`, `GraphView.CanPasteSerializedDataCallback`: derive a class from `ViewSelection` and use it in `GraphView`.
- `GraphView.UnserializeAndPasteDelegate`, `GraphView.UnserializeAndPasteCallback`: derive a class from `ViewSelection` and use it in `GraphView`.
- `GraphView.BlackboardViewState`: use `BlackboardView.BlackboardViewState` instead.
- `GraphView.GetBlackboard()`: use `UQuery`.
- `GraphView.SupportsWindowedBlackboard`: use `true` instead.
- `GraphView`: all copy/paste related methods were moved to `ViewSelection`.
- `GraphViewBlackboardWindow.Blackboard`: use `UQuery`.
