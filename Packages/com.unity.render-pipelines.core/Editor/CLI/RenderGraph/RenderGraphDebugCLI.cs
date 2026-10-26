using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Pipeline.Commands;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEditor.Rendering.CLI
{
    /// <summary>
    /// CLI adapter for RenderGraph debug queries.
    /// Provides stateful session management (capture/resume) over the scope-based API.
    /// </summary>
    internal static class RenderGraphDebugCLI
    {
        private static RenderGraphDebugSessionManager.QueryScope s_ActiveQueryScope;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsOnDomainReload()
        {
            // Dispose and clear scope on domain reload (script recompilation)
            s_ActiveQueryScope?.Dispose();
            s_ActiveQueryScope = null;
            s_CachedCommandList = null;
        }

        [Serializable]
        internal struct ParameterInfo
        {
            public string name;
            public string description;
        }

        [Serializable]
        internal struct CommandInfo
        {
            public string name;
            public string description;
            public ParameterInfo[] required;
            public ParameterInfo[] optional;
        }

        [Serializable]
        internal class CommandListOutput
        {
            public CommandInfo[] commands;
        }

        [Serializable]
        internal struct GraphWithExecutions
        {
            public string graphName;
            public ExecutionInfo[] executions;
        }

        [Serializable]
        internal class CaptureOutput
        {
            public SessionInfo session;
            public GraphWithExecutions[] graphs;
            public string message;
        }

        [Serializable]
        internal class ResumeOutput
        {
            public string message;
        }

        [Serializable]
        internal class RenderGraphResult
        {
            public bool ok;
            public string error;
            [SerializeReference]
            public object data;
        }

        /// <summary>
        /// Validates we have an active scope, returns error result if not.
        /// </summary>
        private static bool TryGetActiveScope(out RenderGraphDebugSessionManager.QueryScope scope, out RenderGraphResult errorResult)
        {
            if (s_ActiveQueryScope == null)
            {
                scope = null;
                errorResult = new RenderGraphResult
                {
                    ok = false,
                    error = "No active capture session. Run 'unity command render_graph.capture' first to lock the viewer and capture the current frame"
                };
                return false;
            }

            // Validate the scope is still valid
            if (!s_ActiveQueryScope.TryValidateSession(out var error))
            {
                // Scope is invalid, dispose it
                s_ActiveQueryScope.Dispose();
                s_ActiveQueryScope = null;

                scope = null;
                errorResult = new RenderGraphResult
                {
                    ok = false,
                    error = $"Capture session invalidated: {error}. Run 'unity command render_graph.capture' again to create a new capture session"
                };
                return false;
            }

            scope = s_ActiveQueryScope;
            errorResult = null;
            return true;
        }

        /// <summary>
        /// Gets a GraphQuery for the specified graph name (or defaults to first graph).
        /// </summary>
        private static bool TryGetGraphQuery(
            RenderGraphDebugSessionManager.QueryScope scope,
            string graphName,
            out RenderGraphDebugSessionManager.GraphQuery graphQuery,
            out RenderGraphResult errorResult)
        {
            try
            {
                // Graph() method already handles defaulting to first graph if null/empty
                graphQuery = scope.Graph(graphName);
                errorResult = null;
                return true;
            }
            catch (Exception ex)
            {
                graphQuery = null;
                errorResult = new RenderGraphResult
                {
                    ok = false,
                    error = ex.Message
                };
                return false;
            }
        }

        /// <summary>
        /// Finds an execution by name or ID within a GraphQuery.
        /// </summary>
        private static bool TryFindExecution(
            RenderGraphDebugSessionManager.GraphQuery graphQuery,
            string executionNameOrId,
            out ExecutionInfo execInfo,
            out RenderGraphResult errorResult)
        {
            var result = graphQuery.GetExecutions();
            if (!result.TryGetValue(out var output))
            {
                execInfo = default;
                errorResult = new RenderGraphResult
                {
                    ok = false,
                    error = result.error
                };
                return false;
            }

            if (output.executions.Length == 0)
            {
                execInfo = default;
                errorResult = new RenderGraphResult
                {
                    ok = false,
                    error = "No executions found. Render a frame with a camera (e.g., select the Game view or Scene view)"
                };
                return false;
            }

            // Try to find by name (case-insensitive)
            foreach (var exec in output.executions)
            {
                if (string.Equals(exec.executionName, executionNameOrId, StringComparison.OrdinalIgnoreCase))
                {
                    execInfo = exec;
                    errorResult = null;
                    return true;
                }
            }

            // Try to find by ID (as string or ulong)
            foreach (var exec in output.executions)
            {
                if (exec.executionId.ToString() == executionNameOrId)
                {
                    execInfo = exec;
                    errorResult = null;
                    return true;
                }
            }

            // Not found - provide helpful error with available executions
            var availableNames = new List<string>();
            foreach (var exec in output.executions)
            {
                availableNames.Add($"'{exec.executionName}'");
            }

            execInfo = default;
            errorResult = new RenderGraphResult
            {
                ok = false,
                error = $"Execution '{executionNameOrId}' not found. Available executions: {string.Join(", ", availableNames)}"
            };
            return false;
        }

        /// <summary>
        /// Validates CLI call and returns ExecutionQuery if successful.
        /// Combines scope, graph, and execution validation into one call.
        /// </summary>
        private static bool TryGetExecutionQuery(
            string execution,
            string graph,
            out RenderGraphDebugSessionManager.ExecutionQuery executionQuery,
            out RenderGraphResult errorResult)
        {
            executionQuery = null;

            if (!TryGetActiveScope(out var scope, out errorResult))
                return false;

            if (!TryGetGraphQuery(scope, graph, out var graphQuery, out errorResult))
                return false;

            if (!TryFindExecution(graphQuery, execution, out var execInfo, out errorResult))
                return false;

            executionQuery = graphQuery.Execution(execInfo.executionItem);
            errorResult = null;
            return true;
        }

        /// <summary>
        /// Executes a CLI query by validating the execution context and wrapping the result.
        /// </summary>
        private static RenderGraphResult ExecuteCLIQuery<T>(
            string execution,
            string graph,
            Func<RenderGraphDebugSessionManager.ExecutionQuery, QueryResult<T>> queryFunc) where T : class
        {
            if (!TryGetExecutionQuery(execution, graph, out var executionQuery, out var errorResult))
                return errorResult;

            return WrapQueryResult(queryFunc(executionQuery));
        }

        // Query method groups (zero-allocation delegates)
        private static QueryResult<PassListOutput> QueryGetPasses(RenderGraphDebugSessionManager.ExecutionQuery q) => q.GetPasses();
        private static QueryResult<PassInfoOutput> QueryGetPassByName(RenderGraphDebugSessionManager.ExecutionQuery q, string name) => q.GetPassByName(name);
        private static QueryResult<PassInfoOutput> QueryGetPassByIndex(RenderGraphDebugSessionManager.ExecutionQuery q, int index) => q.GetPass(index);
        private static QueryResult<CulledPassListOutput> QueryGetCulledPasses(RenderGraphDebugSessionManager.ExecutionQuery q) => q.GetCulledPasses();
        private static QueryResult<UnmergedPassListOutput> QueryGetUnmergedPasses(RenderGraphDebugSessionManager.ExecutionQuery q) => q.GetUnmergedPasses();
        private static QueryResult<PassListOutput> QueryGetPassesByType(RenderGraphDebugSessionManager.ExecutionQuery q, string type) => q.GetPassesByType(type);
        private static QueryResult<ExecutionCaptureInfoOutput> QueryGetCaptureInfo(RenderGraphDebugSessionManager.ExecutionQuery q) => q.GetCaptureInfo();
        private static QueryResult<GraphMetricsOutput> QueryGetGraphMetrics(RenderGraphDebugSessionManager.ExecutionQuery q) => q.GetGraphMetrics();
        private static QueryResult<ResourceListOutput> QueryGetResources(RenderGraphDebugSessionManager.ExecutionQuery q, string type) => q.GetResources(type);
        private static QueryResult<ResourceInfoOutput> QueryGetResourceByName(RenderGraphDebugSessionManager.ExecutionQuery q, string name) => q.GetResourceByName(name);
        private static QueryResult<PassDependencyOutput> QueryGetPassesUsingResource(RenderGraphDebugSessionManager.ExecutionQuery q, string name) => q.GetPassesUsingResource(name);
        private static QueryResult<PassInfoOutput> QueryGetResourceProducerPass(RenderGraphDebugSessionManager.ExecutionQuery q, string name) => q.GetResourceProducerPass(name);

        /// <summary>
        /// Wraps a QueryResult into a RenderGraphResult.
        /// </summary>
        private static RenderGraphResult WrapQueryResult<T>(QueryResult<T> queryResult) where T : class
        {
            if (queryResult.TryGetValue(out var value))
            {
                return new RenderGraphResult
                {
                    ok = true,
                    data = value
                };
            }

            return new RenderGraphResult
            {
                ok = false,
                error = queryResult.error
            };
        }

        /// <summary>
        /// Collects all graphs and their executions from the active scope.
        /// </summary>
        private static RenderGraphResult CollectGraphsAndExecutions(string messagePrefix)
        {
            // Get all graphs
            var graphsResult = s_ActiveQueryScope.GetGraphs();
            if (!graphsResult.TryGetValue(out var graphsOutput))
            {
                return WrapQueryResult(graphsResult);
            }

            // Collect graphs with their executions
            var graphsList = new List<GraphWithExecutions>();
            int totalExecutions = 0;

            foreach (var graphInfo in graphsOutput.graphs)
            {
                var graphQuery = s_ActiveQueryScope.Graph(graphInfo.name);
                var execResult = graphQuery.GetExecutions();

                ExecutionInfo[] executions = Array.Empty<ExecutionInfo>();
                if (execResult.TryGetValue(out var execOutput))
                {
                    executions = execOutput.executions;
                    totalExecutions += executions.Length;
                }

                graphsList.Add(new GraphWithExecutions
                {
                    graphName = graphInfo.name,
                    executions = executions
                });
            }

            return new RenderGraphResult
            {
                ok = true,
                data = new CaptureOutput
                {
                    session = s_ActiveQueryScope.session,
                    graphs = graphsList.ToArray(),
                    message = $"{messagePrefix}. Found {graphsList.Count} graph(s) with {totalExecutions} total execution(s)."
                }
            };
        }

        private static CommandListOutput s_CachedCommandList;

        /// <summary>
        /// Lists all available render_graph CLI commands.
        /// </summary>
        [CliCommand("render_graph", "List all available render_graph commands", MainThreadRequired = true)]
        public static RenderGraphResult Execute()
        {
            if (s_CachedCommandList == null)
            {
                s_CachedCommandList = BuildCommandList();
            }

            return new RenderGraphResult
            {
                ok = true,
                data = s_CachedCommandList
            };
        }

        private static CommandListOutput BuildCommandList()
        {
            var methods = TypeCache.GetMethodsWithAttribute<CliCommandAttribute>();
            var commandList = new List<CommandInfo>();

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<CliCommandAttribute>();
                if (attr != null && attr.Name.StartsWith("render_graph", StringComparison.Ordinal))
                {
                    // Parse parameters from method signature
                    var requiredParams = new List<ParameterInfo>();
                    var optionalParams = new List<ParameterInfo>();

                    var parameters = method.GetParameters();
                    foreach (var param in parameters)
                    {
                        var cliArgAttr = param.GetCustomAttribute<CliArgAttribute>();
                        if (cliArgAttr != null)
                        {
                            // Validate attribute has required properties
                            if (string.IsNullOrEmpty(cliArgAttr.Name))
                                continue;

                            var paramInfo = new ParameterInfo
                            {
                                name = cliArgAttr.Name,
                                description = cliArgAttr.Description ?? string.Empty
                            };

                            // Check if parameter is required
                            // Try to get Required property via reflection with error handling
                            bool isRequired = false;
                            try
                            {
                                var requiredProperty = cliArgAttr.GetType().GetProperty("Required");
                                if (requiredProperty != null && requiredProperty.PropertyType == typeof(bool))
                                {
                                    isRequired = (bool)requiredProperty.GetValue(cliArgAttr);
                                }
                            }
                            catch (Exception)
                            {
                                // Default to false if property doesn't exist or can't be read
                            }

                            if (isRequired)
                            {
                                requiredParams.Add(paramInfo);
                            }
                            else
                            {
                                optionalParams.Add(paramInfo);
                            }
                        }
                    }

                    commandList.Add(new CommandInfo
                    {
                        name = attr.Name,
                        description = attr.Description,
                        required = requiredParams.ToArray(),
                        optional = optionalParams.ToArray()
                    });
                }
            }

            commandList.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            return new CommandListOutput { commands = commandList.ToArray() };
        }

        /// <summary>
        /// Captures the current render graph state and pauses the viewer.
        /// Requires the Render Graph Viewer to be open with an active debug session.
        /// All subsequent data queries require running this command first.
        /// By default, reuses the existing scope if still valid (same session, paused, no data changes).
        /// Use --force to always create a fresh capture.
        /// </summary>
        [CliCommand("render_graph.capture", "Capture current frame and pause viewer (locks session for CLI)", MainThreadRequired = true)]
        public static RenderGraphResult Capture(
            [CliArg("force", "Force new capture even if existing scope is valid")] bool force = false)
        {
            try
            {
                // Check if we can reuse existing scope
                if (!force && s_ActiveQueryScope != null && s_ActiveQueryScope.TryValidateSession(out _))
                {
                    // Scope is still valid, reuse it
                    return CollectGraphsAndExecutions("Reused existing capture session");
                }

                // Check if there's an active debug session
                if (!RenderGraphDebugSessionManager.hasActiveDebugSession)
                {
                    return new RenderGraphResult
                    {
                        ok = false,
                        error = "No active debug session. Open the Render Graph Viewer window first (Window > Analysis > Render Graph Viewer)."
                    };
                }

                // Dispose any existing scope
                s_ActiveQueryScope?.Dispose();
                s_ActiveQueryScope = null;

                // Create new scope (this pauses the viewer if not already paused)
                s_ActiveQueryScope = RenderGraphDebugSessionManager.CreateQueryScope();

                // Collect and return graphs + executions
                return CollectGraphsAndExecutions("Session captured and paused");
            }
            catch (InvalidOperationException ex)
            {
                return new RenderGraphResult
                {
                    ok = false,
                    error = ex.Message
                };
            }
        }

        /// <summary>
        /// Resumes the render graph viewer and unlocks the CLI session.
        /// </summary>
        [CliCommand("render_graph.resume", "Resume viewer and unlock CLI session", MainThreadRequired = true)]
        public static RenderGraphResult Resume()
        {
            if (s_ActiveQueryScope == null)
            {
                return new RenderGraphResult
                {
                    ok = false,
                    error = "No active capture session to resume"
                };
            }

            s_ActiveQueryScope.Dispose();
            s_ActiveQueryScope = null;

            return new RenderGraphResult
            {
                ok = true,
                data = new ResumeOutput
                {
                    message = "Viewer resumed and session unlocked"
                }
            };
        }

        /// <summary>
        /// Lists all registered render graphs.
        /// Requires active capture.
        /// </summary>
        [CliCommand("render_graph.graphs", "List all registered render graphs. Requires active capture.", MainThreadRequired = true)]
        public static RenderGraphResult GetGraphs()
        {
            if (!TryGetActiveScope(out var scope, out var errorResult))
                return errorResult;

            return WrapQueryResult(scope.GetGraphs());
        }

        /// <summary>
        /// Lists all executions (cameras) for a render graph.
        /// Requires active capture.
        /// </summary>
        [CliCommand("render_graph.executions", "List all executions (cameras) for a graph. Requires active capture.", MainThreadRequired = true)]
        public static RenderGraphResult GetExecutions(
            [CliArg("graph", "Render graph name (optional, defaults to first)")] string graph = null)
        {
            if (!TryGetActiveScope(out var scope, out var errorResult))
                return errorResult;

            if (!TryGetGraphQuery(scope, graph, out var graphQuery, out errorResult))
                return errorResult;

            return WrapQueryResult(graphQuery.GetExecutions());
        }

        /// <summary>
        /// Lists all passes for an execution (lightweight summary).
        /// Requires active capture.
        /// </summary>
        [CliCommand("render_graph.passes", "List all passes (lightweight summary). Requires active capture.", MainThreadRequired = true)]
        public static RenderGraphResult GetPasses(
            [CliArg("execution", "Execution name or ID", Required = true)] string execution,
            [CliArg("graph", "Render graph name (optional, defaults to first)")] string graph = null)
        {
            return ExecuteCLIQuery(execution, graph, QueryGetPasses);
        }

        /// <summary>
        /// Gets detailed information about a specific pass by name or index.
        /// Requires active capture.
        /// </summary>
        [CliCommand("render_graph.pass", "Get full details for a specific pass. Requires active capture.", MainThreadRequired = true)]
        public static RenderGraphResult GetPass(
            [CliArg("execution", "Execution name or ID", Required = true)] string execution,
            [CliArg("name", "Pass name")] string name = null,
            [CliArg("index", "Pass index")] int index = -1,
            [CliArg("graph", "Render graph name (optional, defaults to first)")] string graph = null)
        {
            // Validate at least one identifier is provided
            if (string.IsNullOrEmpty(name) && index < 0)
            {
                return new RenderGraphResult
                {
                    ok = false,
                    error = "Specify either --name or --index"
                };
            }

            return ExecuteCLIQuery(execution, graph, q =>
                !string.IsNullOrEmpty(name) ? QueryGetPassByName(q, name) : QueryGetPassByIndex(q, index));
        }

        /// <summary>
        /// Lists only the passes that were culled by the render graph.
        /// Requires active capture.
        /// </summary>
        [CliCommand("render_graph.passes.culled", "List only culled passes. Requires active capture.", MainThreadRequired = true)]
        public static RenderGraphResult GetCulledPasses(
            [CliArg("execution", "Execution name or ID", Required = true)] string execution,
            [CliArg("graph", "Render graph name (optional, defaults to first)")] string graph = null)
        {
            return ExecuteCLIQuery(execution, graph, QueryGetCulledPasses);
        }

        /// <summary>
        /// Lists passes that couldn't merge with the next pass and the reasons why.
        /// Requires active capture.
        /// </summary>
        [CliCommand("render_graph.passes.unmerged", "List passes that couldn't merge with reasons. Requires active capture.", MainThreadRequired = true)]
        public static RenderGraphResult GetUnmergedPasses(
            [CliArg("execution", "Execution name or ID", Required = true)] string execution,
            [CliArg("graph", "Render graph name (optional, defaults to first)")] string graph = null)
        {
            return ExecuteCLIQuery(execution, graph, QueryGetUnmergedPasses);
        }

        /// <summary>
        /// Lists passes filtered by type (e.g., Raster, Compute, Unsafe).
        /// Requires active capture.
        /// </summary>
        [CliCommand("render_graph.passes.by_type", "List passes filtered by type. Requires active capture.", MainThreadRequired = true)]
        public static RenderGraphResult GetPassesByType(
            [CliArg("execution", "Execution name or ID", Required = true)] string execution,
            [CliArg("type", "Pass type (e.g., Raster, Compute, Unsafe)", Required = true)] string type,
            [CliArg("graph", "Render graph name (optional, defaults to first)")] string graph = null)
        {
            return ExecuteCLIQuery(execution, graph, q => QueryGetPassesByType(q, type));
        }

        /// <summary>
        /// Gets capture information for an execution (timestamp, source).
        /// Requires active capture.
        /// </summary>
        [CliCommand("render_graph.execution.info", "Get capture info for an execution. Requires active capture.", MainThreadRequired = true)]
        public static RenderGraphResult GetExecutionCaptureInfo(
            [CliArg("execution", "Execution name or ID", Required = true)] string execution,
            [CliArg("graph", "Render graph name (optional, defaults to first)")] string graph = null)
        {
            return ExecuteCLIQuery(execution, graph, QueryGetCaptureInfo);
        }

        /// <summary>
        /// Gets aggregated metrics and insights for an execution.
        /// Includes pass counts, resource counts, and performance insights.
        /// Requires active capture.
        /// </summary>
        [CliCommand("render_graph.metrics", "Get aggregated metrics and insights. Requires active capture.", MainThreadRequired = true)]
        public static RenderGraphResult GetMetrics(
            [CliArg("execution", "Execution name or ID", Required = true)] string execution,
            [CliArg("graph", "Render graph name (optional, defaults to first)")] string graph = null)
        {
            return ExecuteCLIQuery(execution, graph, QueryGetGraphMetrics);
        }

        /// <summary>
        /// Lists all resources for an execution, optionally filtered by type.
        /// Requires active capture.
        /// </summary>
        [CliCommand("render_graph.resources", "List all resources for an execution. Requires active capture.", MainThreadRequired = true)]
        public static RenderGraphResult GetResources(
            [CliArg("execution", "Execution name or ID", Required = true)] string execution,
            [CliArg("graph", "Render graph name (optional, defaults to first)")] string graph = null,
            [CliArg("type", "Resource type filter: Texture, Buffer, AccelerationStructure")] string type = null)
        {
            return ExecuteCLIQuery(execution, graph, q => QueryGetResources(q, type));
        }

        /// <summary>
        /// Gets detailed information about a specific resource by name.
        /// Requires active capture.
        /// </summary>
        [CliCommand("render_graph.resource", "Get details for a specific resource. Requires active capture.", MainThreadRequired = true)]
        public static RenderGraphResult GetResource(
            [CliArg("execution", "Execution name or ID", Required = true)] string execution,
            [CliArg("name", "Resource name", Required = true)] string name,
            [CliArg("graph", "Render graph name (optional, defaults to first)")] string graph = null)
        {
            return ExecuteCLIQuery(execution, graph, q => QueryGetResourceByName(q, name));
        }

        /// <summary>
        /// Finds all passes that read or write a specific resource.
        /// Requires active capture.
        /// </summary>
        [CliCommand("render_graph.resource.passes", "Find passes that use a specific resource. Requires active capture.", MainThreadRequired = true)]
        public static RenderGraphResult GetPassesUsingResource(
            [CliArg("execution", "Execution name or ID", Required = true)] string execution,
            [CliArg("name", "Resource name", Required = true)] string name,
            [CliArg("graph", "Render graph name (optional, defaults to first)")] string graph = null)
        {
            return ExecuteCLIQuery(execution, graph, q => QueryGetPassesUsingResource(q, name));
        }

        /// <summary>
        /// Finds the pass that created/produced a specific resource.
        /// Requires active capture.
        /// </summary>
        [CliCommand("render_graph.resource.producer", "Find the pass that created a resource. Requires active capture.", MainThreadRequired = true)]
        public static RenderGraphResult GetResourceProducerPass(
            [CliArg("execution", "Execution name or ID", Required = true)] string execution,
            [CliArg("name", "Resource name", Required = true)] string name,
            [CliArg("graph", "Render graph name (optional, defaults to first)")] string graph = null)
        {
            return ExecuteCLIQuery(execution, graph, q => QueryGetResourceProducerPass(q, name));
        }
    }
}
