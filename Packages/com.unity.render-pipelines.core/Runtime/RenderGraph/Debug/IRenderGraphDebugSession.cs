using System;
using System.Collections.Generic;
using static UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace UnityEngine.Rendering.RenderGraphModule
{
    /// <summary>
    /// Interface for render graph debug sessions.
    /// Implementations can provide data from live sessions or serialized sessions loaded from files.
    /// </summary>
    internal interface IRenderGraphDebugSession : IDisposable
    {
        bool isActive { get; }
        bool isPaused { get; set; }
        string connectionName { get; set; }
        List<string> GetRegisteredGraphs();
        List<DebugExecutionItem> GetExecutions(string graphName);
        DebugData GetDebugData(string graphName, EntityId executionId);
        void SetDebugData(string renderGraph, EntityId executionId, DebugData data);
        void DeleteExecutionIds(string renderGraph, List<EntityId> executionIds);

        /// <summary>
        /// Gets the underlying debug data container for serialization.
        /// </summary>
        DebugDataContainer debugDataContainer { get; }

        event Action onRegisteredGraphsChanged;
        event Action<string, EntityId> onDebugDataUpdated;
        event Action<bool> onPausedStateChanged;
    }
}
