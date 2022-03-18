### Changed

- `InsertBlocksInContextCommand.ContextData.m_Context` renamed to `InsertBlocksInContextCommand.ContextData.Context`.
- `InsertBlocksInContextCommand.ContextData.m_Index` renamed to `InsertBlocksInContextCommand.ContextData.Index`.
- `InsertBlocksInContextCommand.ContextData.m_Blocks` renamed to `InsertBlocksInContextCommand.ContextData.Blocks`.
- `BlackboardHeaderPart.defaultTitle` renamed to `BlackboardHeaderPart.k_DefaultTitle`.
- `BlackboardHeaderPart.defaultSubTitle` renamed to `BlackboardHeaderPart.k_DefaultSubTitle`.
- `BlackboardGroup.TitlePartName` renamed to `BlackboardGroup.k_TitlePartName`.
- `BlackboardGroup.ItemsPartName` renamed to `BlackboardGroup.k_ItemsPartName`.
- `Placemat.defaultCollapsedSize` renamed to `BlackboardGroup.k_DefaultCollapsedSize`.
- `Placemat.bounds` renamed to `Placemat.k_Bounds`.
- `Placemat.boundTop` renamed to `Placemat.k_BoundTop`.
- `GraphElementModel.AssignNewGuid` is not virtual anymore.
- Field `VariableDeclarationModel.variableFlags` now exposed using property `VariableFlags`.
- `IGTFSearcherAdapter` renamed to `ISearcherAdapter`.
- `SearcherService.Usage.k_CreateNode` renamed to `SearcherService.Usage.CreateNode`.
- `SearcherService.Usage.k_Values` renamed to `SearcherService.Usage.Values`.
- `SearcherService.Usage.k_Types` renamed to `SearcherService.Usage.Types`.
- `DefaultSearcherDatabaseProvider.SupportedTypes` from public to protected.
- `TypeHandle.Identification` field is now a get-only property.

### Removed

- `GraphElement.GetPosition()`: use `VisualElement.layout` instead.
- `PlacematModel.m_ZOrder` serialized field.
