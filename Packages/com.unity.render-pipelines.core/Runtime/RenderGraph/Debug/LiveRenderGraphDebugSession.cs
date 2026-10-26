using System;
using System.Collections.Generic;
using static UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace UnityEngine.Rendering.RenderGraphModule
{
    /// <summary>
    /// Base class for live debug sessions that actively collect data from render graph executions.
    /// Handles event subscriptions and container management.
    /// </summary>
    internal abstract class LiveRenderGraphDebugSession : IRenderGraphDebugSession
    {
        protected DebugDataContainer container { get; }

        protected LiveRenderGraphDebugSession()
        {
            container = new DebugDataContainer();

            // Subscribe to render graph events
            onGraphRegistered += RegisterGraph;
            onGraphUnregistered += UnregisterGraph;
            onExecutionRegistered += RegisterExecution;
        }

        protected void RegisterGraph(string graphName)
        {
            if (container.AddGraph(graphName))
                onRegisteredGraphsChanged?.Invoke();
        }

        protected void UnregisterGraph(string graphName)
        {
            if (container.RemoveGraph(graphName))
                onRegisteredGraphsChanged?.Invoke();
        }

        protected void RegisterExecution(string graphName, EntityId executionId, string executionName)
        {
            if (container.AddExecution(graphName, executionId, executionName))
                onRegisteredGraphsChanged?.Invoke();
        }

        public virtual void Dispose()
        {
            onGraphRegistered -= RegisterGraph;
            onGraphUnregistered -= UnregisterGraph;
            onExecutionRegistered -= RegisterExecution;
            container.Clear();
        }

        protected void InvalidateData()
        {
            container.Invalidate();
        }

        // IRenderGraphDebugSession implementation
        public abstract bool isActive { get; }
        public string connectionName { get; set; }

        private bool m_IsPaused;
        public virtual bool isPaused
        {
            get => m_IsPaused;
            set => ChangePausedState(value);
        }

        protected void ChangePausedState(bool paused)
        {
            if (m_IsPaused == paused)
                return;

            m_IsPaused = paused;
            onPausedStateChanged?.Invoke(paused);

        }

        public List<string> GetRegisteredGraphs()
        {
            return container.GetRenderGraphs();
        }

        public List<DebugExecutionItem> GetExecutions(string graphName)
        {
            return container.GetExecutions(graphName);
        }

        public DebugData GetDebugData(string graphName, EntityId executionId)
        {
            return container.GetDebugData(graphName, executionId);
        }

        public void SetDebugData(string renderGraph, EntityId executionId, DebugData data)
        {
            // Note: data can be null when there's a version mismatch - in that case we still want to register
            // the execution with null data so the UI can display an incompatible version message
            if (data != null)
            {
                // Set capture metadata
                data.captureSourceString = connectionName;
                data.captureTimestamp = $"{DateTime.Now:HH:mm:ss}";
            }


            container.SetDebugData(renderGraph, executionId, data);
            onDebugDataUpdated?.Invoke(renderGraph, executionId);
        }

        public void DeleteExecutionIds(string renderGraph, List<EntityId> executionIds)
        {
            container.DeleteExecutionIds(renderGraph, executionIds);
            onRegisteredGraphsChanged?.Invoke();
        }

        public DebugDataContainer debugDataContainer => container;

        // Events
        public event Action onRegisteredGraphsChanged;
        public event Action<string, EntityId> onDebugDataUpdated;
        public event Action<bool> onPausedStateChanged;

        /// <summary>
        /// Helper method for derived classes to register all locally known graphs and executions.
        /// Useful for initializing sessions with existing render graph state.
        /// </summary>
        protected void RegisterAllLocallyKnownGraphsAndExecutions()
        {
            var registeredExecutions = GetRegisteredExecutions();
            foreach (var (graph, executions) in registeredExecutions)
            {
                RegisterGraph(graph.name);
                foreach (var executionItem in executions)
                    RegisterExecution(graph.name, executionItem.id, executionItem.name);
            }
        }
    }
}
