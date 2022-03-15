### Fixed
- Fixed a bug where it was possible to connect several data outputs to a single data input.

### Removed

- `EdgesToDelete` from `CreateEdgeCommand`, `CreateVariableNodesCommand` and `CreateNodeFromPortCommand`. Edges to delete are computed by command handlers.
- `edgesToDelete` parameter from `CreateNodesFromPort` for the same reasons.
- `EdgeConnectorListener.GetDropEdgeModelsToDelete`, this should be done on the model side, also Command Handlers should compute those.
