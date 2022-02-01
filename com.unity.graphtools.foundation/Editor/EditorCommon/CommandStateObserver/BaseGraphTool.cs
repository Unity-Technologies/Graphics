using System;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A base tool for graph tools.
    /// </summary>
    public class BaseGraphTool : CsoTool
    {
#if DEBUG
        string m_InstantiationStackTrace;
#endif

        /// <summary>
        /// The name of the tool.
        /// </summary>
        public string Name { get; set; } = "UnnamedTool";

        internal bool WantsTransientPrefs { get; set; }

        /// <summary>
        /// The tool configuration.
        /// </summary>
        public Preferences Preferences { get; private set; }

        /// <summary>
        /// The state component that holds the tool state.
        /// </summary>
        public ToolStateComponent ToolState { get; private set; }

        /// <summary>
        /// The state component that holds the graph processing state.
        /// </summary>
        public GraphProcessingStateComponent GraphProcessingState { get; private set; }

        /// <summary>
        /// The state component that holds the undo state.
        /// </summary>
        public UndoStateComponent UndoStateComponent { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseGraphTool"/> class.
        /// </summary>
        public BaseGraphTool()
        {
#if DEBUG
            m_InstantiationStackTrace = Environment.StackTrace;
#endif
        }

#if DEBUG
        ~BaseGraphTool()
        {
            Debug.Assert(
                m_InstantiationStackTrace == null ||
                Undo.undoRedoPerformed.GetInvocationList().Count(d => ReferenceEquals(d.Target, this)) == 0,
                $"Unbalanced Initialize() and Dispose() calls for tool {GetType()} instantiated at {m_InstantiationStackTrace}");
        }
#endif

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Undo.undoRedoPerformed -= UndoRedoPerformed;
            }
            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override void InitDispatcher()
        {
            Dispatcher = new CommandDispatcher();
        }

        /// <inheritdoc />
        protected override void InitState()
        {
            base.InitState();

            if (WantsTransientPrefs)
            {
                Preferences = Preferences.CreateTransient(Name);
            }
            else
            {
                Preferences = Preferences.CreatePreferences(Name);
            }

            ToolState = new ToolStateComponent();
            State.AddStateComponent(ToolState);

            GraphProcessingState = new GraphProcessingStateComponent();
            State.AddStateComponent(GraphProcessingState);

            UndoStateComponent = new UndoStateComponent(State);
            State.AddStateComponent(UndoStateComponent);

            Dispatcher.RegisterCommandHandler<ToolStateComponent, GraphProcessingStateComponent, LoadGraphAssetCommand>(
                LoadGraphAssetCommand.DefaultCommandHandler, ToolState, GraphProcessingState);

            Dispatcher.RegisterCommandHandler<ToolStateComponent, GraphProcessingStateComponent, UnloadGraphAssetCommand>(
                UnloadGraphAssetCommand.DefaultCommandHandler, ToolState, GraphProcessingState);

            Dispatcher.RegisterCommandHandler<UndoStateComponent, UndoRedoCommand>(UndoRedoCommand.DefaultCommandHandler, UndoStateComponent);

            Dispatcher.RegisterCommandHandler<BuildAllEditorCommand>(BuildAllEditorCommand.DefaultCommandHandler);
        }

        /// <summary>
        /// Updates the state components by running the observers.
        /// </summary>
        public void Update()
        {
            if (Dispatcher is CommandDispatcher commandDispatcher)
            {
                var logAllCommands = Preferences?.GetBool(BoolPref.LogAllDispatchedCommands) ?? false;
                var errorRecursive = Preferences?.GetBool(BoolPref.ErrorOnRecursiveDispatch) ?? false;

                Diagnostics diagnosticFlags = Diagnostics.None;

                if (logAllCommands)
                    diagnosticFlags |= Diagnostics.LogAllCommands;
                if (errorRecursive)
                    diagnosticFlags |= Diagnostics.CheckRecursiveDispatch;

                commandDispatcher.DiagnosticFlags = diagnosticFlags;
            }

            ObserverManager.NotifyObservers(State);
        }

        void UndoRedoPerformed()
        {
            Dispatcher.Dispatch(new UndoRedoCommand());
        }
    }
}
