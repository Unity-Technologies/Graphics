### Added
- enum `EdgeSide` to designate one end of an edge or the other.
- `IEdgeModel` `GetPort` and `GetOtherPort` extension methods get a port based on an `EdgeSide`.
- `CreateNodeCommand.OnEdgeSide` to create nodes on one side of existing edges.
- `CreateNodeCommand.WithNodeOnEdges` extension method to create one or multiple nodes using `CreateNodeCommand.OnEdgeSide`.
- `MoveEdgeCommand` allows moving an existing edge to another port. The side to plug the edge can most of the times be inferred from the port to move to.

### Changed
- `Stencil.CreateNodesFromPort` renamed to `CreateNodesFromEdges` and now takes existing edges as parameters.

### Fixed 
- Moving edges to a new port uses these edges instead of creating new ones.
- Moving edges to the graph to create a node uses these edges instead of creating new ones.
- Dragging multiple edges from one port to a new port connects all edges instead of just one.

### Removed
- `EdgeConnectorListener` merged into `EdgeConnector`
- `EdgeConnector.SetDropOutsideDelegate` and `SetDropDelegate` replaced by virtual methods in `EdgeDragHelper`.
