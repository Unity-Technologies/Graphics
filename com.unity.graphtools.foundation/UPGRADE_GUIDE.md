# Upgrade Guide

This guide provides help on how to migrate your code from one release of GTF to the next.

## [Unreleased]

### Command State Observer refactor

The Command State Observer pattern was refactored to better modularize the `State`
and improve its extensibility. This impacts how tools are structured.

#### Tool Creation

Before the refactor, graph tools usually derived a class from `GraphViewEditorWindow`,
where the tool initialization was done.
In particular, the derived window needed to override `CreateInitialState()` to instantiate its own state
class, with all the state components hardcoded into the derived state.

In order to make graph tool more decoupled from the `EditorWindow`, we introduce a `BaseGraphTool`. Each
graph tool should derive a class from this and override the `Name` property and possibly the `InitState`
method.

The derived window class should override `GraphViewEditorWindow.CreateGraphTool()`
to instantiate the derived graph tool class. The correct way to create a tool
is to use `CsoTool.Create()`:

```c#
protected override BaseGraphTool CreateGraphTool()
{
    return CsoTool.Create<MyGraphTool>();
}
```

A simple graph tool implementation would look like this:

```c#
class MyGraphTool : BaseGraphTool
{
    static readonly string toolName = "My Special Graph Editor";

    public override string Name => toolName;

    /// <inheritdoc />
    protected override void InitState()
    {
        base.InitState();

        // Add additional tool-related state components here.
    }
}
```

Before the refactor, the `GraphToolState` was the class that defined the
`StateComponent`s that were part of the `State`. It had
properties for each of the `StateComponent`. Thus, if a tool wanted to
add more `StateComponents` to the `State`, it had to derive from `GraphToolState`.

Now, `StateComponent`s are added dynamically to the `State` by any object that needs to keep data in a
`StateComponent`. For example, when a `GraphView` is instantiated, it adds a
`GraphViewStateComponent` and a `SelectionStateComponent` to the `State`. The
`BaseGraphTool` adds `StateComponents` that are needed by all graph tools.
You do not need to derive a state for your tool anymore, and so `GraphToolState` has been removed.

Since the `State` does not have properties to quickly access `StateComponents` (as `GraphToolState` had),
objects creating `StateComponent` should probably keep a reference on them and provide a property to access them.


#### Command handler definition

Before the refactor, command handlers methods received a `State` as their first parameter.

```c#
public static void DefaultCommandHandler(GraphToolState graphToolState, SomeCommand command)
{
    graphToolState.PushUndo(command);

    using (var graphUpdater = graphToolState.SomeStateComponent.UpdateScope)
    {
        // Do things
    }
}
```

Now, they receive specific `StateComponent`s:

```c#
public static void DefaultCommandHandler(UndoStateComponent undoState,
        SomeStateComponent someStateComponent, /* possibly more state components, */ CreateEdgeCommand command)
{
    // This replaces PushUndo()
    using (var undoStateUpdater = undoState.UpdateScope)
    {
        // Only save the state components that will be modified by the handler.
        undoStateUpdater.SaveSingleState(someStateComponent, command);
    }

    using (var graphUpdater = someStateComponent.UpdateScope)
    {
        // Do something
    }
}
```

You can notice that `State.PushUndo()` is not called anymore. Now, instead of saving the whole state on
the undo stack, the command handler is responsible for choosing the state components to save.

#### Command handlers registration and binding

Before the refactor, you needed to derive a class from `GraphToolState` to register
custom commands and command handlers to the dispatcher:

```c#
public class MyState : GraphToolState
{
    public override void RegisterCommandHandlers(Dispatcher dispatcher)
    {
        base.RegisterCommandHandlers(dispatcher);

        if (!(dispatcher is CommandDispatcher commandDispatcher))
            return;

        commandDispatcher.RegisterCommandHandler<AddPortCommand>(AddPortCommand.DefaultHandler);
        commandDispatcher.RegisterCommandHandler<RemovePortCommand>(RemovePortCommand.DefaultHandler);

        commandDispatcher.RegisterCommandHandler<SetTemperatureCommand>(SetTemperatureCommand.DefaultHandler);
        commandDispatcher.RegisterCommandHandler<SetDurationCommand>(SetDurationCommand.DefaultHandler);
    }
}
```

Now, command and command handler registration should done by the classes that add state components to the state,
like the `GraphView` and the `BaseGraphTool`. For example, if your graph view needs some custom commands
or command handlers you would do:

```c#
public class MyGraphView : GraphView
{
    public MyGraphView(GraphViewEditorWindow window, BaseGraphTool graphTool, string graphViewName)
        : base(window, graphTool, graphViewName)
    {
        if (Dispatcher == null)
            return;

        // Using GraphView helper functions to register command handlers to the view's Dispatcher.
        this.RegisterCommandHandler<AddPortCommand>(AddPortCommand.DefaultHandler);
        this.RegisterCommandHandler<RemovePortCommand>(RemovePortCommand.DefaultHandler);

        // Alternatively, using Dispatcher to register command handlers.
        Dispatcher.RegisterCommandHandler<SetTemperatureCommand>(SetTemperatureCommand.DefaultHandler, self.GraphTool.UndoStateComponent, self.GraphViewState);
        Dispatcher.RegisterCommandHandler<SetDurationCommand>(SetDurationCommand.DefaultHandler, self.GraphTool.UndoStateComponent, self.GraphViewState);
    }
}
```

The rationale behind this is: if a class adds state components to the `State`,
this class knows how user actions should affect those state components. Thus, it should register
the appropriate command handlers.

Note that you do not have to derive a specialized `GraphView` class for your tool. You have the option
of instantiating the base `GraphView` class and registering command handlers on the instance.

You can also see that `RegisterCommandHandler` is used to bind the command handlers to the state components
that will be passed as parameters to the handler.

There is not a single, tool-global, `Dispatcher` anymore. Each view instance, as well as the `BaseGraphTool`
instance, has its own `Dispatcher`. This means that when handlers are registered in the example
above, they are only registered to the view's dispatcher. This is important to keep in mind for command
dispatching.


#### Dispatching a command

Commands should now be dispatched to an `ICommandTarget`, to identify which object is
targeted by the command.
All views (`GraphView`, `ModelInspectorView`) and `BaseGraphTool` implement
`ICommandTarget`.

In GTF, each `ICommandTarget` has its own `Dispatcher`, which means it has a specific set of
commands it can handle (defined by the command handler registration process).

After handling a command (or if it cannot handle it), the `ICommandTarget` dispatches the command
to its `ICommandTarget.Parent`. In GTF, by default, `GraphView.Parent` is the `BaseGraphTool`.
and the tool is the last target in to receive a command.

The code to dispatch a command is very similar to what it was before:

Before:

```c#
m_GraphView.CommandDispatcher.Dispatch(...)
```

Now:

```c#
m_GraphView.Dispatch(...)
```


#### Changes to Observers dependency declaration

Before the refactor, observer dependencies (defining which components are observed
and which components are modified by an observer)
were declared using the state component property name in the `GraphToolState`.
They now need to be declared using references to `StateComponents`.

Before:

```c#
public class BlackboardUpdateObserver : StateObserver<GraphToolState>
{
    protected Blackboard m_Blackboard;

    public BlackboardUpdateObserver(Blackboard blackboard) :
        base(nameof(GraphToolState.GraphViewState),
            nameof(GraphToolState.SelectionState),
            nameof(GraphToolState.BlackboardViewState),
            nameof(GraphToolState.WindowState))
    {
        m_Blackboard = blackboard;
    }

    protected override void Observe(GraphToolState state)
    {
        if (m_Blackboard?.panel != null)
        {
            using (var winObservation = this.ObserveState(state.WindowState))
            using (var selObservation = this.ObserveState(state.SelectionState))
            using (var gvObservation = this.ObserveState(state.GraphViewState))
            using (var bbObservation = this.ObserveState(state.BlackboardViewState))
            {
                // ...
            }
        }
    }
}
```

Now:

```c#
public class BlackboardUpdateObserver : StateObserver
{
    protected Blackboard m_Blackboard;
    GraphViewStateComponent m_GraphViewStateComponent;
    SelectionStateComponent m_SelectionStateComponent;
    BlackboardViewStateComponent m_BlackboardViewStateComponent;
    ToolStateComponent m_ToolStateComponent;

    public BlackboardUpdateObserver(Blackboard blackboard, GraphViewStateComponent graphViewState,
        SelectionStateComponent selectionState, BlackboardViewStateComponent blackboardViewState, ToolStateComponent toolState) :
        base(graphViewState, selectionState, blackboardViewState, toolState)
    {
        m_Blackboard = blackboard;

        m_GraphViewStateComponent = graphViewState;
        m_SelectionStateComponent = selectionState;
        m_BlackboardViewStateComponent = blackboardViewState;
        m_ToolStateComponent = toolState;
    }

    public override void Observe()
    {
        if (m_Blackboard?.panel != null)
        {
            using (var winObservation = this.ObserveState(m_ToolStateComponent))
            using (var selObservation = this.ObserveState(m_SelectionStateComponent))
            using (var gvObservation = this.ObserveState(m_GraphViewStateComponent))
            using (var bbObservation = this.ObserveState(m_BlackboardViewStateComponent))
            {
                // ...
            }
        }
    }
}
```

#### Factory methods

Since views have there own dispatcher, it is not necessary anymore to pass a `Dispatcher` when calling `CreateUI<>`.
Thus, factory method signatures have changed from this:

```c#
public static IModelUI CreateContext(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IContextNodeModel nodeModel)
{
    IModelUI ui = new ContextNode();

    ui.SetupBuildAndUpdate(nodeModel, commandDispatcher, elementBuilder.View, elementBuilder.Context);
    return ui;
}
```

to this:

```c#
public static IModelUI CreateContext(this ElementBuilder elementBuilder, IContextNodeModel nodeModel)
{
    IModelUI ui = new ContextNode();

    ui.SetupBuildAndUpdate(nodeModel, elementBuilder.View, elementBuilder.Context);
    return ui;
}
```

If for some reason you need the dispatcher object in your factory method, use `elementBuilder.View.Dispatcher`.

It is important that you update your factory methods to the new signature, otherwise they will not be found by
the factory discovery process. To find factory methods, we suggest that you search for classes having
the `GraphElementsExtensionMethodsCache` attribute.
