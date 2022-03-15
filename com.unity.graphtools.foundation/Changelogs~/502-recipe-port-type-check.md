### Added
`IStencil.CanAssignTo` has been added to determine if two types are compatible with each other. The default implementation in `Stencil` is simply:
```csharp
destination == TypeHandle.Unknown || source.IsAssignableFrom(destination, this);
```

### Changed
`IGraphModel.GetCompatiblePorts` now takes a `IReadOnlyList<IPortModel>` instead of a `List<IPortModel>`.
