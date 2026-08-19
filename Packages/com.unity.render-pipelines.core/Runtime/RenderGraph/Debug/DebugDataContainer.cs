using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace UnityEngine.Rendering.RenderGraphModule
{
    /// <summary>
    /// Container for RenderGraph debug data. Stores debug data for multiple render graphs and their executions.
    /// Uses dual representation: Dictionary for runtime, List for JSON serialization.
    /// </summary>
    [Serializable]
    internal class DebugDataContainer : ISerializationCallbackReceiver
    {
        // Serialized representation for JSON
        [SerializeField]
        private List<GraphData> m_SerializedGraphs = new();

        // Runtime representation (not serialized to JSON, rebuilt from list)
        [NonSerialized]
        private Dictionary<string, Dictionary<EntityId, DebugData>> m_Container = new();

        [Serializable]
        internal class GraphData
        {
            public string graphName;
            public List<ExecutionData> executions = new();
        }

        [Serializable]
        internal class ExecutionData
        {
            [SerializeField]
            private ulong m_ExecutionId;

            public EntityId executionId
            {
                get => EntityId.FromULong(m_ExecutionId);
                set => m_ExecutionId = EntityId.ToULong(value);
            }

            public string executionName;
            public DebugData debugData;
        }

        /// <summary>
        /// Adds a new render graph to the container.
        /// </summary>
        /// <param name="graphName">Name of the render graph.</param>
        /// <returns>True if added successfully, false if already exists.</returns>
        public bool AddGraph(string graphName)
        {
            if (m_Container.ContainsKey(graphName))
                return false;

            m_Container.Add(graphName, new Dictionary<EntityId, DebugData>());
            return true;
        }

        /// <summary>
        /// Removes a render graph from the container.
        /// </summary>
        /// <param name="graphName">Name of the render graph.</param>
        /// <returns>True if removed successfully, false if not found.</returns>
        public bool RemoveGraph(string graphName)
        {
            return m_Container.Remove(graphName);
        }

        /// <summary>
        /// Adds a new execution to a render graph.
        /// </summary>
        /// <param name="graphName">Name of the render graph.</param>
        /// <param name="executionId">ID of the execution (usually camera/viewport).</param>
        /// <param name="executionName">Name of the execution.</param>
        /// <returns>True if added successfully, false if already exists.</returns>
        public bool AddExecution(string graphName, EntityId executionId, string executionName)
        {
            Debug.Assert(m_Container.ContainsKey(graphName),
                $"Graph '{graphName}' must be added before adding executions.");

            if (m_Container[graphName].ContainsKey(executionId))
                return false;

            m_Container[graphName][executionId] = new DebugData(executionName);
            return true;
        }

        /// <summary>
        /// Gets the list of all registered render graph names.
        /// </summary>
        /// <returns>List of graph names.</returns>
        public List<string> GetRenderGraphs() => new(m_Container.Keys);

        /// <summary>
        /// Gets all executions for a specific render graph.
        /// </summary>
        /// <param name="graphName">Name of the render graph.</param>
        /// <returns>List of execution items.</returns>
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

        /// <summary>
        /// Gets debug data for a specific execution.
        /// </summary>
        /// <param name="renderGraph">Name of the render graph.</param>
        /// <param name="executionId">ID of the execution.</param>
        /// <returns>Debug data for the execution.</returns>
        /// <exception cref="InvalidOperationException">Thrown if graph not registered or execution not found.</exception>
        public DebugData GetDebugData(string renderGraph, EntityId executionId)
        {
            if (!m_Container.TryGetValue(renderGraph, out var debugDataForGraph))
                throw new InvalidOperationException($"RenderGraph '{renderGraph}' was never registered with the debug session.");

            if (!debugDataForGraph.TryGetValue(executionId, out var data))
                throw new InvalidOperationException($"Execution '{executionId}' not found in graph '{renderGraph}'.");

            return data;
        }

        /// <summary>
        /// Sets debug data for a specific execution.
        /// </summary>
        /// <param name="renderGraph">Name of the render graph.</param>
        /// <param name="executionId">ID of the execution.</param>
        /// <param name="data">Debug data to set.</param>
        public void SetDebugData(string renderGraph, EntityId executionId, DebugData data)
        {
            if (m_Container.TryGetValue(renderGraph, out var debugDataForGraph))
                debugDataForGraph[executionId] = data;
        }

        /// <summary>
        /// Deletes specific execution IDs from a render graph.
        /// </summary>
        /// <param name="renderGraph">Name of the render graph.</param>
        /// <param name="executionIds">List of execution IDs to delete.</param>
        public void DeleteExecutionIds(string renderGraph, List<EntityId> executionIds)
        {
            if (m_Container.TryGetValue(renderGraph, out var debugDataForGraph))
            {
                foreach (var executionId in executionIds)
                    debugDataForGraph.Remove(executionId);
            }
        }

        /// <summary>
        /// Gets the internal container with all debug data.
        /// </summary>
        /// <returns>Dictionary mapping graph names to their execution data.</returns>
        public Dictionary<string, Dictionary<EntityId, DebugData>> GetAllData()
        {
            return m_Container;
        }

        /// <summary>
        /// Clears all data from the container.
        /// </summary>
        public void Clear()
        {
            m_Container.Clear();
        }

        /// <summary>
        /// Invalidates all debug data (marks as stale but doesn't remove).
        /// </summary>
        public void Invalidate()
        {
            foreach (var (graph, dict) in m_Container)
            {
                foreach (var (cam, debugData) in dict)
                    debugData.Clear();
            }
        }

        // ISerializationCallbackReceiver implementation
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Convert dictionary → list before JSON serialization
            m_SerializedGraphs.Clear();
            foreach (var (graphName, executions) in m_Container)
            {
                var graphData = new GraphData { graphName = graphName };
                foreach (var (execId, debugData) in executions)
                {
                    // Only serialize valid debug data
                    if (debugData != null && debugData.valid)
                    {
                        graphData.executions.Add(new ExecutionData
                        {
                            executionId = execId,
                            executionName = debugData.executionName,
                            debugData = debugData
                        });
                    }
                }

                if (graphData.executions.Count > 0)
                    m_SerializedGraphs.Add(graphData);
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Rebuild dictionary from list after JSON deserialization
            m_Container = new Dictionary<string, Dictionary<EntityId, DebugData>>();

            foreach (var graph in m_SerializedGraphs)
            {
                var execDict = new Dictionary<EntityId, DebugData>();
                foreach (var exec in graph.executions)
                {
                    execDict[exec.executionId] = exec.debugData;
                }
                m_Container[graph.graphName] = execDict;
            }
        }
    }
}
