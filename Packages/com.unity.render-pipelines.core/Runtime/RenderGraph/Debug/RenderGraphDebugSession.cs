using System;
using System.Collections.Generic;
using static UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace UnityEngine.Rendering.RenderGraphModule
{
    internal abstract class RenderGraphDebugSession : IDisposable
    {
        protected class DebugDataContainer
        {
            readonly Dictionary<string, Dictionary<EntityId, DebugData>> m_Container = new();

            public bool AddGraph(string graphName)
            {
                if (m_Container.ContainsKey(graphName))
                    return false;

                m_Container.Add(graphName, new Dictionary<EntityId, DebugData>());

                return true;
            }

            public bool RemoveGraph(string graphName)
            {
                return m_Container.Remove(graphName);
            }

            public bool AddExecution(string graphName, EntityId executionId, string executionName)
            {
                Debug.Assert(m_Container.ContainsKey(graphName));
                if (m_Container[graphName].ContainsKey(executionId))
                    return false;

                m_Container[graphName][executionId] = new DebugData(executionName);
                return true;
            }

            public List<string> GetRenderGraphs() => new(m_Container.Keys);

            public List<DebugExecutionItem> GetExecutions(string graphName)
            {
                var executions = new List<DebugExecutionItem>();
                if (!string.IsNullOrEmpty(graphName) && m_Container.TryGetValue(graphName, out var executionsDict))
                {
                    foreach (var (executionId, debugData) in executionsDict)
                    {
                        var item = new DebugExecutionItem(executionId, debugData.executionName);
                        executions.Add(item);
                    }
                }

                return executions;
            }

            public DebugData GetDebugData(string renderGraph, EntityId executionId)
            {
                if (!m_Container.TryGetValue(renderGraph, out var debugDataForGraph))
                    throw new InvalidOperationException();
                return debugDataForGraph[executionId];
            }

            public void SetDebugData(string renderGraph, EntityId executionId, DebugData data)
            {
                if (m_Container.TryGetValue(renderGraph, out var debugDataForGraph))
                    debugDataForGraph[executionId] = data;
            }

            public void DeleteExecutionIds(string renderGraph, List<EntityId> executionIds)
            {
                if (m_Container.TryGetValue(renderGraph, out var debugDataForGraph))
                {
                    foreach (var executionId in executionIds)
                        debugDataForGraph.Remove(executionId);
                }
            }

            public void Clear()
            {
                m_Container.Clear();
            }

            public void Invalidate()
            {
                foreach (var (graph, dict) in m_Container)
                {
                    foreach (var (cam, debugData) in dict)
                        debugData.Clear();
                }
            }
        }

        // Session is considered active when it is collecting debug data
        public abstract bool isActive { get; }

        DebugDataContainer debugDataContainer { get; }

        protected RenderGraphDebugSession()
        {
            debugDataContainer = new DebugDataContainer();

            onGraphRegistered += RegisterGraph;
            onGraphUnregistered += UnregisterGraph;
            onExecutionRegistered += RegisterExecution;
        }

        protected void RegisterGraph(string graphName)
        {
            if (debugDataContainer.AddGraph(graphName))
                onRegisteredGraphsChanged?.Invoke();
        }

        protected void UnregisterGraph(string graphName)
        {
            if (debugDataContainer.RemoveGraph(graphName))
                onRegisteredGraphsChanged?.Invoke();
        }

        protected void RegisterExecution(string graphName, EntityId executionId, string executionName)
        {
            if (debugDataContainer.AddExecution(graphName, executionId, executionName))
                onRegisteredGraphsChanged?.Invoke();
        }

        public virtual void Dispose()
        {
            onGraphRegistered -= RegisterGraph;
            onGraphUnregistered -= UnregisterGraph;
            onExecutionRegistered -= RegisterExecution;
            debugDataContainer.Clear();
        }

        protected void InvalidateData()
        {
            debugDataContainer.Invalidate();
        }

        public static event Action onRegisteredGraphsChanged;
        public static event Action<string, EntityId> onDebugDataUpdated;

        static RenderGraphDebugSession s_CurrentDebugSession;

        public static bool hasActiveDebugSession => s_CurrentDebugSession?.isActive ?? false;

        public static RenderGraphDebugSession currentDebugSession => s_CurrentDebugSession;

        public static void Create<TSession>() where TSession : RenderGraphDebugSession, new()
        {
            EndSession();
            s_CurrentDebugSession = new TSession();
        }

        public static void EndSession()
        {
            if (s_CurrentDebugSession != null)
            {
                s_CurrentDebugSession.Dispose();
                s_CurrentDebugSession = null;
            }
        }

        public static List<string> GetRegisteredGraphs() => s_CurrentDebugSession.debugDataContainer.GetRenderGraphs();

        public static List<DebugExecutionItem> GetExecutions(string graphName) => s_CurrentDebugSession.debugDataContainer.GetExecutions(graphName);

        public static DebugData GetDebugData(string renderGraph, EntityId executionId)
        {
            return s_CurrentDebugSession.debugDataContainer.GetDebugData(renderGraph, executionId);
        }

        public static void SetDebugData(string renderGraph, EntityId executionId, DebugData data)
        {
            s_CurrentDebugSession.debugDataContainer.SetDebugData(renderGraph, executionId, data);
            onDebugDataUpdated?.Invoke(renderGraph, executionId);
        }

        public static void DeleteExecutionIds(string renderGraph, List<EntityId> executionIds)
        {
            s_CurrentDebugSession.debugDataContainer.DeleteExecutionIds(renderGraph, executionIds);
            onRegisteredGraphsChanged?.Invoke();
        }

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
