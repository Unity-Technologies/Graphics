### Changed
-  Part of the `ConvertEdgesToPortalsCommand` handler's code was moved to the `GraphModel`.
-  Calling `GraphModel.CreateEntryPortalFromEdge` now creates a portal declaration, deletes the old edge and connects the new portal.
-  Calling `GraphModel.CreateExitPortalFromEdge` now deletes the old edge and connects the new portal.

### Added
- `GraphModel.CreatePortalsFromEdge` is used to create a pair of portals from an edge.
