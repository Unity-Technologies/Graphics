### Added

- `GlobalSelectionCommandHelper`, a class used to update all `SelectionStateComponent`s in the `State`.
- `RootViewModel`: a base class for the model backing a `RootView`.
- `RootView.RootViewModel`: the model for the view.
- `RootView.RegisterObservers()` and `RootView.UnregisterObservers()`, to be implemented by `RootView`s to register and unregister their observers.
- `RootView.OnEnterPanel()` and `RootView.OnLeavePanel()`, called when the `RootView` is added to the window or removed from the window.
- `IState.OnStateComponentListModified`, a delegate called when state components are added to the state or removed from the state.
- `IStateComponent.OnAddedToState()`, called when the state component has been added to the state.
- `IStateComponent.OnRemovedFromState()`, called when the state component has been removed from the state.
