### Added

- `IChangesetManager`, an interface for changeset managers.
- `SimpleChangeset<>`, a class using a `HashSet` to track changes.
- `StateComponent.ChangesetManager`, the changeset manager of a state component, if any changesets are recorded.

### Removed

- `DeclarationHighlighterStateComponent.Changeset`: replaced by `SimpleChangeset<IDeclarationModel>`.
- `SelectionStateComponent.Changeset`: replaced by `SimpleChangeset<IGraphElementModel>`.
- `BlackboardViewStateComponent.Changeset`: replaced by `SimpleChangeset<string>`.
- `ModelInspectorStateComponent.Changeset`: replaced by `SimpleChangeset<IInspectorSectionModel>`.
- `StateComponent.UpdateType`: no replacement.
