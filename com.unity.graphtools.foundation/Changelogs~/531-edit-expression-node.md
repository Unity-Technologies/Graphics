### Added
- `NodeEdgeDiff`, to compute the edges that are added or removed to a node.
- `InspectorUseSetterMethodAttribute`, an attribute for fields, to tell the local inspector to use a setter method to change a field's value.

### Removed
- `IPortNodeModel.CreatePort`: still available on `NodeModel` as a protected method.
- `IPortNodeModel.DisconnectPort`: still available on `NodeModel` as a protected method.

### Changed
- `SetInspectedObjectFieldCommand.SetField` is now protected and has three `out` parameters to communicate side-effects on the model.
- Unused missing ports are automatically removed on all `NodeModel`, not just `SubgraphNodeModel`.
- `ISubgraphNodeModel.RemoveUnusedMissingPort()` moved to `IPortNodeModel`.
