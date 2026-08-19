using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.RenderGraphModule
{
    static partial class RenderGraphDebugSession
    {
        internal class GraphQuery
        {
            private readonly QueryScope m_Query;
            private readonly string m_GraphName;
            private readonly ExecutionInfo[] m_CapturedExecutions;
            private readonly int m_AllGraphsCount;

            internal GraphQuery(QueryScope query, string graphName, IRenderGraphDebugSession capturedSession)
            {
                m_Query = query;
                m_GraphName = graphName;

                // Capture executions at construction time using the captured session
                var allGraphs = capturedSession.GetRegisteredGraphs();
                m_AllGraphsCount = allGraphs.Count;

                using (UnityEngine.Pool.ListPool<ExecutionInfo>.Get(out var executionList))
                {
                    if (string.IsNullOrEmpty(graphName))
                    {
                        CollectExecutionInfo(capturedSession, allGraphs, executionList);
                    }
                    else
                    {
                        using (UnityEngine.Pool.ListPool<string>.Get(out var graphs))
                        {
                            graphs.Add(graphName);
                            CollectExecutionInfo(capturedSession, graphs, executionList);
                        }
                    }

                    m_CapturedExecutions = executionList.ToArray();
                }
            }

            public QueryResult<ExecutionListOutput> GetExecutions()
            {
                if (!m_Query.TryValidateSession(out var error))
                    return QueryResult<ExecutionListOutput>.Fail(error);

                if (m_CapturedExecutions.Length == 0)
                {
                    return QueryResult<ExecutionListOutput>.Fail(
                        "No executions found",
                        "Render a frame with a camera (e.g., select the Game view or Scene view)");
                }

                string note = null;
                if (string.IsNullOrEmpty(m_GraphName) && m_AllGraphsCount > 1)
                {
                    note = $"Multiple graphs available ({m_AllGraphsCount}). Provide a graph name to filter results. Call GetGraphs() to see all available graphs.";
                }

                return QueryResult<ExecutionListOutput>.Ok(new ExecutionListOutput
                {
                    executions = m_CapturedExecutions,
                    note = note
                });
            }

            // Helper to collect execution info for specified graphs using the captured session
            private static void CollectExecutionInfo(IRenderGraphDebugSession session, List<string> graphs, List<ExecutionInfo> executionList)
            {
                foreach (var graph in graphs)
                {
                    var executions = session.GetExecutions(graph);
                    foreach (var exec in executions)
                    {
                        // Get capture info from debug data
                        try
                        {
                            var debugData = session.GetDebugData(graph, exec.id);
                            if (debugData != null)
                            {
                                executionList.Add(new ExecutionInfo
                                {
                                    graphName = graph,
                                    executionItem = exec,
                                    captureSourceString = debugData.captureSourceString,
                                    captureTimestamp = debugData.captureTimestamp,
                                });
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // Skip executions without valid debug data
                        }
                    }
                }
            }

            public ExecutionQuery Execution(RenderGraph.DebugExecutionItem executionItem)
            {
                // Validate the execution item exists in our captured list
                bool found = false;
                string graphName = m_GraphName;
                foreach (var exec in m_CapturedExecutions)
                {
                    if (exec.executionItem.id == executionItem.id)
                    {
                        found = true;
                        graphName = exec.graphName;
                        break;
                    }
                }

                if (!found)
                {
                    throw new ArgumentException(
                        $"Execution '{executionItem.name}' (id: {executionItem.id}) was not captured by this GraphQuery. " +
                        "Use GetExecutions() to get valid execution items.", nameof(executionItem));
                }

                return new ExecutionQuery(m_Query, graphName, executionItem);
            }
        }
    }
}
