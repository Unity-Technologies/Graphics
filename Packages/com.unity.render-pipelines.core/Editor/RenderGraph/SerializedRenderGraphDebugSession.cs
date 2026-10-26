using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Serialized implementation of IRenderGraphDebugSession that can be initialized from
    /// a live session or loaded from JSON. Used for saving/loading debug sessions and testing.
    /// </summary>
    internal class SerializedRenderGraphDebugSession : IRenderGraphDebugSession
    {
        DebugDataContainer m_Container;

        // These events are never invoked since this is a read-only serialized session
        public event Action onRegisteredGraphsChanged { add { } remove { } }
        public event Action<string, EntityId> onDebugDataUpdated { add { } remove { } }
        public event Action<bool> onPausedStateChanged { add { } remove { } }

        /// <summary>
        /// Create a serialized session by loading from JSON.
        /// </summary>
        public SerializedRenderGraphDebugSession(string json)
        {
            m_Container = JsonUtility.FromJson<DebugDataContainer>(json);
            if (m_Container == null)
                throw new InvalidOperationException("Failed to load valid debug data from JSON");
        }

        /// <summary>
        /// Load a serialized session from a JSON file.
        /// </summary>
        /// <param name="path">Path to the JSON file.</param>
        /// <returns>A new SerializedRenderGraphDebugSession loaded from the file.</returns>
        public static SerializedRenderGraphDebugSession LoadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            if (!System.IO.File.Exists(path))
                throw new System.IO.FileNotFoundException($"File not found: {path}", path);

            string json = System.IO.File.ReadAllText(path);
            return new SerializedRenderGraphDebugSession(json);
        }

        bool IRenderGraphDebugSession.isActive => true;

        private bool m_IsPaused = true;
        bool IRenderGraphDebugSession.isPaused
        {
            get => m_IsPaused;
            set => m_IsPaused = value; // Tests needs to be able to set this, but it won't affect the serialized data
        }

        string IRenderGraphDebugSession.connectionName { get => string.Empty; set { } }

        public List<string> GetRegisteredGraphs()
        {
            return m_Container?.GetRenderGraphs() ?? new List<string>();
        }

        public List<RenderGraph.DebugExecutionItem> GetExecutions(string graphName)
        {
            return m_Container?.GetExecutions(graphName) ?? new List<RenderGraph.DebugExecutionItem>();
        }

        public RenderGraph.DebugData GetDebugData(string graphName, EntityId executionId)
        {
            return m_Container?.GetDebugData(graphName, executionId);
        }

        public DebugDataContainer debugDataContainer => m_Container;

        /// <summary>
        /// Export a debug session to a JSON file.
        /// </summary>
        /// <param name="session">The session to export.</param>
        /// <param name="path">File path to write the JSON to.</param>
        public static void Export(IRenderGraphDebugSession session, string path)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            var container = session.debugDataContainer;
            if (container == null)
                throw new InvalidOperationException("Session has no debug data to export");

            string json = JsonUtility.ToJson(container, prettyPrint: true);
            System.IO.File.WriteAllText(path, json);
        }

        void IRenderGraphDebugSession.SetDebugData(string renderGraph, EntityId executionId, RenderGraph.DebugData data)
        {
            // Read-only session
            throw new NotSupportedException("Cannot set debug data on a serialized session");
        }

        void IRenderGraphDebugSession.DeleteExecutionIds(string renderGraph, List<EntityId> executionIds)
        {
            // Read-only session
            throw new NotSupportedException("Cannot delete execution IDs on a serialized session");
        }

        void IDisposable.Dispose()
        {
            // Nothing to dispose
        }
    }
}
