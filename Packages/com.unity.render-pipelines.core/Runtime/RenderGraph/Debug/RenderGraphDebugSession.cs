using System;
using System.Collections.Generic;
using static UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace UnityEngine.Rendering.RenderGraphModule
{
    /// <summary>
    /// Static manager for render graph debug sessions.
    /// Manages the current active session and provides convenience methods for accessing it.
    /// </summary>
    internal static partial class RenderGraphDebugSession
    {
        // Cleared via ResetStaticsOnLoad() on domain reload
        static IRenderGraphDebugSession s_CurrentDebugSession;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void ResetStaticsOnLoad()
        {
            s_CurrentDebugSession = null;
        }
#endif

        /// <summary>
        /// Returns true if there is an active debug session.
        /// </summary>
        public static bool hasActiveDebugSession => s_CurrentDebugSession?.isActive ?? false;

        /// <summary>
        /// Gets the current debug session instance.
        /// </summary>
        public static IRenderGraphDebugSession currentDebugSession => s_CurrentDebugSession;

        /// <summary>
        /// Creates a new debug session of the specified type.
        /// Ends any existing session before creating the new one.
        /// </summary>
        /// <typeparam name="TSession">Type of session to create.</typeparam>
        public static void BeginSession<TSession>() where TSession : IRenderGraphDebugSession, new()
        {
            BeginSession(new TSession());
        }

        /// <summary>
        /// Creates a new debug session of the specified type.
        /// Ends any existing session before creating the new one.
        /// </summary>
        /// <param name="sessionType">Type of session to create (must implement IRenderGraphDebugSession).</param>
        internal static void BeginSession(Type sessionType)
        {
            EndSession();

            if (!typeof(IRenderGraphDebugSession).IsAssignableFrom(sessionType))
                throw new ArgumentException("Incorrect session type. Type should implement IRenderGraphDebugSession.");

            BeginSession(Activator.CreateInstance(sessionType) as IRenderGraphDebugSession);
        }

        /// <summary>
        /// Sets a pre-constructed debug session as the current session.
        /// Ends any existing session before setting the new one.
        /// Useful for tests and loading serialized sessions.
        /// </summary>
        /// <param name="session">The session instance to set as current.</param>
        public static void BeginSession(IRenderGraphDebugSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            EndSession();
            s_CurrentDebugSession = session;
        }

        /// <summary>
        /// Ends the current debug session and disposes of its resources.
        /// </summary>
        public static void EndSession()
        {
            s_CurrentDebugSession?.Dispose();
            s_CurrentDebugSession = null;
        }

        // Convenience delegates to current session
        public static readonly List<string> s_EmptyRegisteredGraphs = new();

        /// <summary>
        /// Gets the list of registered render graphs from the current session.
        /// </summary>
        /// <returns>List of graph names, or empty list if no session active.</returns>
        public static List<string> GetRegisteredGraphs()
        {
            return s_CurrentDebugSession == null ? s_EmptyRegisteredGraphs : s_CurrentDebugSession.GetRegisteredGraphs();
        }

        public static readonly List<DebugExecutionItem> s_EmptyExecutions = new();

        /// <summary>
        /// Gets the list of executions for a specific render graph from the current session.
        /// </summary>
        /// <param name="graphName">Name of the render graph.</param>
        /// <returns>List of executions, or empty list if no session active.</returns>
        public static List<DebugExecutionItem> GetExecutions(string graphName)
        {
            return string.IsNullOrEmpty(graphName) || s_CurrentDebugSession == null ? s_EmptyExecutions : s_CurrentDebugSession.GetExecutions(graphName);
        }

        /// <summary>
        /// Gets debug data for a specific execution from the current session.
        /// </summary>
        /// <param name="renderGraph">Name of the render graph.</param>
        /// <param name="executionId">ID of the execution.</param>
        /// <returns>Debug data for the execution.</returns>
        public static DebugData GetDebugData(string renderGraph, EntityId executionId)
        {
            return s_CurrentDebugSession.GetDebugData(renderGraph, executionId);
        }
    }
}
