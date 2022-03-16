### Fixed
- Dragging some nodes to compatible ports was sometimes incorrectly rejected.

### Removed
- `Port.GetPortToConnect` removed. Compatibility should be tested on the model using `PortModel.GetPortFitToConnectTo` instead.
- `Port.HasModelToDrop` removed. No replacement provided, internally replaced with `PortModel.GetPortFitToConnectTo(...) != null`.
