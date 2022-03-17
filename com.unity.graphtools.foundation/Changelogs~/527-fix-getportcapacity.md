### Added
- `IPortModel.Capacity` now has a setter.

### Removed
- `IPortModel.GetDefaultCapacity`: the functionality has been folded in the default `PortModel.Capacity.get` implementation.
- `IPortNodeModel.GetPortCapacity(IPortModel portModel)`: interrogate `portModel` directly instead.
- `IStencil.GetPortCapacity(IPortModel portModel, out PortCapacity capacity)`: interrogate `portModel` directly instead.
