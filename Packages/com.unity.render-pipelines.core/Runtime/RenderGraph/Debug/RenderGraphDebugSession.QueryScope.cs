using System;

namespace UnityEngine.Rendering.RenderGraphModule
{
    internal readonly struct QueryResult<T> where T : class
    {
        readonly T m_Value;
        readonly string m_Error;

        QueryResult(T value, string error)
        {
            m_Value = value;
            m_Error = error;
        }

        public static QueryResult<T> Ok(T value) => new(value, null);

        public static QueryResult<T> Fail(string error, string suggestion = null)
        {
            if (string.IsNullOrEmpty(suggestion))
                return new(null, error);
            return new(null, $"{error}. {suggestion}");
        }

        public bool TryGetValue(out T value)
        {
            value = m_Value;
            return m_Error == null;
        }

        public string error => m_Error;
    }

    [Serializable]
    internal struct GraphInfo
    {
        public string name;
    }

    [Serializable]
    internal class GraphListOutput
    {
        public GraphInfo[] graphs;
    }

    [Serializable]
    internal struct ExecutionInfo
    {
        public string graphName;
        public RenderGraph.DebugExecutionItem executionItem;
        public string captureSourceString;
        public string captureTimestamp;
    }

    [Serializable]
    internal class ExecutionListOutput
    {
        public ExecutionInfo[] executions;
        public string note;
    }

    [Serializable]
    internal class SessionInfo
    {
        public string capturedAt;
        public bool isPaused;
        public bool wasPausedOnCapture;
        public bool isEditor;
        public bool isPlaying;
    }

    [Serializable]
    internal class ExecutionCaptureInfoOutput
    {
        public string executionName;
        public string captureSourceString;
        public string captureTimestamp;
    }

    [Serializable]
    internal struct PassSummary
    {
        public int index;
        public string name;
        public string type;
        public bool culled;
    }

    [Serializable]
    internal class PassListOutput
    {
        public string executionName;
        public int passCount;
        public int culledCount;
        public PassSummary[] passes;
    }

    [Serializable]
    internal struct PassInfo
    {
        public int index;
        public string name;
        public string type;
        public bool culled;
        public bool async;
        public int nativeSubPassIndex;
        public int syncToPassIndex;
        public int syncFromPassIndex;
        public string passBreakReasoning;
        public int[] mergedPassIds;
        public string[] resourceReads;
        public string[] resourceWrites;
    }

    [Serializable]
    internal class PassInfoOutput
    {
        public PassInfo pass;
    }

    [Serializable]
    internal struct CulledPassInfo
    {
        public int index;
        public string name;
        public string type;
    }

    [Serializable]
    internal class CulledPassListOutput
    {
        public string executionName;
        public CulledPassInfo[] culledPasses;
    }

    [Serializable]
    internal struct UnmergedPassInfo
    {
        public int index;
        public string name;
        public string breakReason;
        public string nextPassName;
    }

    [Serializable]
    internal class UnmergedPassListOutput
    {
        public string executionName;
        public UnmergedPassInfo[] unmergedPasses;
    }

    [Serializable]
    internal struct ResourceSummary
    {
        public int index;
        public string name;
        public string type;
        public bool imported;
    }

    [Serializable]
    internal class ResourceListOutput
    {
        public string executionName;
        public int resourceCount;
        public ResourceSummary[] resources;
    }

    [Serializable]
    internal struct ResourceInfo
    {
        public int index;
        public string name;
        public string type;
        public bool imported;
        public bool memoryless;
        public int creationPassIndex;
        public int releasePassIndex;
        public int width;
        public int height;
        public int depth;
        public string format;
        public int samples; // MSAA sample count (1 = no MSAA)
        public int[] consumerPasses;
        public int[] producerPasses;
    }

    [Serializable]
    internal class ResourceInfoOutput
    {
        public ResourceInfo resource;
    }

    [Serializable]
    internal class PassDependencyOutput
    {
        public string resourceName;
        public int passCount;
        public PassInfo[] passes;
    }

    [Serializable]
    internal enum InsightSeverity
    {
        Info,
        Warning,
        Critical
    }

    [Serializable]
    internal enum InsightCategory
    {
        Culling,
        Merging,
        Resources,
        AsyncCompute,
        PassDistribution
    }

    [Serializable]
    internal struct GraphInsight
    {
        public InsightSeverity severity;
        public InsightCategory category;
        public string message;
    }

    [Serializable]
    internal class GraphMetricsOutput
    {
        public string executionName;
        public int totalPasses;
        public int culledPasses;
        public int unmergedPasses;
        public int totalResources;
        public int textureCount;
        public int bufferCount;
        public int importedResourceCount;
        public GraphInsight[] insights;
    }

    static partial class RenderGraphDebugSession
    {
        public static QueryScope CreateQueryScope()
        {
            return new QueryScope();
        }

        internal class QueryScope : IDisposable
        {
            private readonly IRenderGraphDebugSession m_CapturedSession;
            private readonly string m_CaptureTimestamp;
            private readonly bool m_DidPauseSession;
            private readonly string[] m_CapturedGraphs;
            private bool m_Disposed;

            public SessionInfo session { get; private set; }

            internal QueryScope()
            {
                if (!hasActiveDebugSession)
                {
                    throw new InvalidOperationException("No active debug session. Open the Render Graph Viewer window first.");
                }

                // Capture the session instance to detect if it changes
                m_CapturedSession = s_CurrentDebugSession;

                // Pause the session if not already paused
                m_DidPauseSession = !m_CapturedSession.isPaused;
                if (!m_CapturedSession.isPaused)
                    m_CapturedSession.isPaused = true;

                // Capture session state
                m_CaptureTimestamp = DateTime.Now.ToString("HH:mm:ss");

                // Capture registered graphs at construction time
                var graphList = m_CapturedSession.GetRegisteredGraphs();
                m_CapturedGraphs = new string[graphList.Count];
                for (int i = 0; i < graphList.Count; i++)
                {
                    m_CapturedGraphs[i] = graphList[i];
                }

                this.session = CreateSessionInfo();
            }

            private SessionInfo CreateSessionInfo()
            {
                if (!hasActiveDebugSession)
                {
                    throw new InvalidOperationException("No active debug session. Open the Render Graph Viewer window first.");
                }

                return new SessionInfo
                {
                    capturedAt = m_CaptureTimestamp,
                    isPaused = m_CapturedSession.isPaused,
                    wasPausedOnCapture = !m_DidPauseSession,
                    isEditor = Application.isEditor,
                    isPlaying = Application.isPlaying
                };
            }

            internal bool TryValidateSession(out string error)
            {
                error = null;

                if (m_Disposed)
                {
                    error = "Query session has been disposed. Create a new QueryScope to continue querying";
                    return false;
                }

                if (!hasActiveDebugSession)
                {
                    error = "Session no longer active. The debug session may have been ended";
                    return false;
                }

                // Validate that the session instance hasn't changed
                if (!ReferenceEquals(m_CapturedSession, s_CurrentDebugSession))
                {
                    error = "Debug session changed since capture. The active session was replaced. Re-capture to query the new session";
                    return false;
                }

                if (!m_CapturedSession.isPaused)
                {
                    error = "Session was unpaused. The query is no longer valid. Re-capture to continue querying";
                    return false;
                }

                return true;
            }

            public QueryResult<GraphListOutput> GetGraphs()
            {
                if (!TryValidateSession(out var error))
                    return QueryResult<GraphListOutput>.Fail(error);

                var graphs = new GraphInfo[m_CapturedGraphs.Length];
                for (int i = 0; i < m_CapturedGraphs.Length; i++)
                {
                    graphs[i] = new GraphInfo { name = m_CapturedGraphs[i] };
                }

                return QueryResult<GraphListOutput>.Ok(new GraphListOutput { graphs = graphs });
            }

            public GraphQuery Graph(string graphName = null)
            {
                if (string.IsNullOrEmpty(graphName))
                {
                    if (m_CapturedGraphs.Length == 0)
                    {
                        throw new InvalidOperationException("No render graphs registered. Render a frame with the Render Graph Viewer open.");
                    }
                    graphName = m_CapturedGraphs[0];
                }
                else
                {
                    // Validate the requested graph exists in our captured list
                    bool found = false;
                    foreach (var graph in m_CapturedGraphs)
                    {
                        if (string.Equals(graph, graphName, StringComparison.Ordinal))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        throw new ArgumentException(
                            $"Graph '{graphName}' was not captured by this QueryScope. " +
                            "Use GetGraphs() to see available graphs.", nameof(graphName));
                    }
                }

                return new GraphQuery(this, graphName, m_CapturedSession);
            }

            public void Dispose()
            {
                if (!m_Disposed)
                {
                    // Resume the session if it was paused by this query
                    if (m_DidPauseSession && hasActiveDebugSession && ReferenceEquals(m_CapturedSession, s_CurrentDebugSession))
                    {
                        s_CurrentDebugSession.isPaused = false;
                    }
                    m_Disposed = true;
                }
            }
        }
    }
}
