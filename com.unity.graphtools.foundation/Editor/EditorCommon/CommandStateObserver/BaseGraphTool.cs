using System;
using System.Collections.Generic;
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

        protected IToolbarProvider m_MainToolbarProvider;
#if UNITY_2022_2_OR_NEWER
        protected Dictionary<string, IOverlayToolbarProvider> m_ToolbarProviders;
#endif

        /// <summary>
        /// The name of the tool.
        /// </summary>
        public string Name { get; set; } = "UnnamedTool";

        /// <summary>
        /// The icon of the tool.
        /// </summary>
        public Texture2D Icon { get; set; } = EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_ScriptableObject Icon" : "ScriptableObject Icon").image as Texture2D;

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
        /// The state component holding information about which variable declarations should be highlighted.
        /// </summary>
        public DeclarationHighlighterStateComponent HighlighterState { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseGraphTool"/> class.
        /// </summary>
        public BaseGraphTool()
        {
            m_MainToolbarProvider = null;
#if UNITY_2022_2_OR_NEWER
            m_ToolbarProviders = new Dictionary<string, IOverlayToolbarProvider>();
#endif
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
                ToolState.Dispose();
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

            ToolState = PersistedState.GetOrCreateAssetViewStateComponent<ToolStateComponent>(default, WindowID, Name);
            State.AddStateComponent(ToolState);

            GraphProcessingState = new GraphProcessingStateComponent();
            State.AddStateComponent(GraphProcessingState);

            UndoStateComponent = new UndoStateComponent(State, ToolState);
            State.AddStateComponent(UndoStateComponent);

            HighlighterState = new DeclarationHighlighterStateComponent();
            State.AddStateComponent(HighlighterState);

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

        /// <summary>
        /// Gets the toolbar provider for the main toolbar.
        /// </summary>
        /// <remarks>Use this method to get the provider for the legacy <see cref="MainToolbar"/>.</remarks>
        /// <returns>The toolbar provider for the main toolbar.</returns>
        public virtual IToolbarProvider GetToolbarProvider()
        {
            return m_MainToolbarProvider ?? new MainToolbarProvider();
        }

#if UNITY_2022_2_OR_NEWER
        protected virtual IOverlayToolbarProvider CreateToolbarProvider(string toolbarId)
        {
            switch (toolbarId)
            {
                case MainOverlayToolbar.toolbarId:
                    return new MainToolbarProvider();
                case PanelsToolbar.toolbarId:
                    return new PanelsToolbarProvider();
                case ErrorOverlayToolbar.toolbarId:
                    return new ErrorToolbarProvider();
                case OptionsMenuToolbar.toolbarId:
                    return new OptionsToolbarProvider();
                case BreadcrumbsToolbar.toolbarId:
                    return new BreadcrumbsToolbarProvider();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the toolbar provider for a toolbar.
        /// </summary>
        /// <param name="toolbar">The toolbar for which to get the provider.</param>
        /// <returns>The toolbar provider for the toolbar.</returns>
        public IOverlayToolbarProvider GetToolbarProvider(OverlayToolbar toolbar)
        {
            if (!m_ToolbarProviders.TryGetValue(toolbar.id, out var toolbarProvider))
            {
                toolbarProvider = CreateToolbarProvider(toolbar.id);
                m_ToolbarProviders[toolbar.id] = toolbarProvider;
            }

            return toolbarProvider;
        }
#endif
    }
}
