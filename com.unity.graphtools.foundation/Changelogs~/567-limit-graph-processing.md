### Added
- `GraphModelStateComponent.StateUpdater.Changeset.AddNewModels()` method to add new models to the changeset.
- `GraphModelStateComponent.StateUpdater.Changeset.AddChangedModels()` method to add changed models to the changeset.
- `GraphModelStateComponent.StateUpdater.Changeset.AddDeletedModels()` method to add deleted models to the changeset.
- `GraphModelStateComponent.StateUpdater.Changeset.AddModelToAutoAlign()` method to add models to align to the changeset.
- `ProcessOnIdleAgent`, an agent that tracks the mouse and elapsed time to trigger automatic graph processing, along with an instance in `GraphView`, `GraphView.ProcessOnIdleAgent`.
- `GraphChangeDescription`, a class used to describe changes on the graph model to the graph processors.
- `ChangeHint`, an extensible enumeration to describe how a model changed.

### Changed
- `GraphModelStateComponent.StateUpdater.MarkChanged()` now has an additional optional parameter of type `ChangeHint` or `List<ChangeHint>`, to specify how the model changed. 
- `GraphModelStateComponent.StateUpdater.Changeset.NewModels` changed from `HashSet<IGraphElementModel>` to `IEnumerable<IGraphElementModel>`. To add elements, use the new `AddNewModels()` method.
- `GraphModelStateComponent.StateUpdater.Changeset.ChangedModels` changed from `HashSet<IGraphElementModel>` to `IEnumerable<KeyValuePair<IGraphElementModel, IReadOnlyList<ChangeHint>>>`. To add elements, use the new `AddChangedModels()` method.
- `GraphModelStateComponent.StateUpdater.Changeset.DeletedModels` changed from `HashSet<IGraphElementModel>` to `IEnumerable<IGraphElementModel>`. To add elements, use the new `AddDeletedModels()` method.
- `GraphModelStateComponent.StateUpdater.Changeset.ModelsToAutoAlign` changed from `HashSet<IGraphElementModel>` to `IEnumerable<IGraphElementModel>`. To add elements, use the new `AddModelToAutoAlign()` method.
- `BoolPref.AutoProcess` renamed to `BoolPref.OnlyProcessWhenIdle`.
- `IGraphProcessor.ProcessGraph()` now takes an additional parameter of type `GraphChangeDescription`.
- `GraphProcessingHelper.ProcessGraph()` now takes an additional parameter of type `GraphChangeDescription`.
- `AutomaticGraphProcessingObserver` now needs a `ProcessOnIdleStateComponent` in its constructor.

### Removed
- `GraphViewEditorWindow.ResetGraphProcessorTimer()`: replaced by `ProcessOnIdleAgent.OnMouseMove()`.
