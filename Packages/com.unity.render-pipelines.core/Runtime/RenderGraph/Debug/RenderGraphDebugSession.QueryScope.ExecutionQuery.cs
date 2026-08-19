using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.RenderGraphModule
{
    static partial class RenderGraphDebugSession
    {
        internal class ExecutionQuery
        {
            private readonly QueryScope m_Query;
            private readonly string m_GraphName;
            private readonly RenderGraph.DebugExecutionItem m_ExecutionItem;
            private readonly int? m_ExpectedExecutionHash;

            internal ExecutionQuery(QueryScope query, string graphName, RenderGraph.DebugExecutionItem executionItem)
            {
                m_Query = query;
                m_GraphName = graphName;
                m_ExecutionItem = executionItem ?? throw new ArgumentNullException(nameof(executionItem));

                // Capture the execution hash at creation time to validate it hasn't changed
                m_ExpectedExecutionHash = ComputeExecutionHash(graphName, executionItem.id);
            }

            private static int? ComputeExecutionHash(string graphName, EntityId executionId)
            {
                try
                {
                    var debugData = RenderGraphDebugSession.GetDebugData(graphName, executionId);
                    if (debugData != null && debugData.valid)
                    {
                        var hash = HashFNV1A32.Create();
                        hash.Append(debugData.passList.Count);
                        hash.Append(debugData.graphHash);
                        hash.Append(debugData.captureTimestamp);
                        return hash.value;
                    }
                }
                catch (System.Exception)
                {
                }
                return null;
            }

            private bool TryValidateExecution(out string error)
            {
                error = null;

                // First check session is still valid
                if (!m_Query.TryValidateSession(out error))
                    return false;

                // If we couldn't compute a hash at construction, the execution was invalid from the start
                if (!m_ExpectedExecutionHash.HasValue)
                {
                    error = "Execution data was not valid when query was created. Ensure the execution exists and has valid debug data.";
                    return false;
                }

                // Check this specific execution hasn't changed
                int? currentHash = ComputeExecutionHash(m_GraphName, m_ExecutionItem.id);
                if (!currentHash.HasValue)
                {
                    error = "Execution data is no longer available. The debug session may have been reset.";
                    return false;
                }

                if (currentHash.Value != m_ExpectedExecutionHash.Value)
                {
                    error = "Execution data has changed since query was created. Create a new ExecutionQuery to get updated data.";
                    return false;
                }

                return true;
            }

            private bool TryGetResourceInfo(ResourceSummary summary, out ResourceInfo info)
            {
                var result = GetResource(summary.type, summary.index);
                if (result.TryGetValue(out var output))
                {
                    info = output.resource;
                    return true;
                }

                info = default;
                return false;
            }

            private bool TryGetPassInfo(PassSummary summary, out PassInfo info)
            {
                var result = GetPass(summary.index);
                if (result.TryGetValue(out var output))
                {
                    info = output.pass;
                    return true;
                }

                info = default;
                return false;
            }

            public QueryResult<PassListOutput> GetPasses()
            {
                if (!TryValidateExecution(out var error))
                    return QueryResult<PassListOutput>.Fail(error);

                if (!TryGetDebugDataOrError(m_GraphName, m_ExecutionItem.id, out var debugData, out var dataError))
                    return QueryResult<PassListOutput>.Fail(dataError);
                return QueryResult<PassListOutput>.Ok(GetPassesFromDebugData(debugData));
            }

            public QueryResult<PassInfoOutput> GetPass(int passIndex)
            {
                if (!TryValidateExecution(out var error))
                    return QueryResult<PassInfoOutput>.Fail(error);

                if (!TryGetDebugDataOrError(m_GraphName, m_ExecutionItem.id, out var debugData, out var dataError))
                    return QueryResult<PassInfoOutput>.Fail(dataError);

                if (passIndex < 0 || passIndex >= debugData.passList.Count)
                    return QueryResult<PassInfoOutput>.Fail($"Pass index {passIndex} out of range (0 to {debugData.passList.Count - 1})");

                return QueryResult<PassInfoOutput>.Ok(new PassInfoOutput
                {
                    pass = ConvertPassData(debugData, passIndex)
                });
            }

            public QueryResult<PassInfoOutput> GetPassByName(string passName)
            {
                if (!TryValidateExecution(out var error))
                    return QueryResult<PassInfoOutput>.Fail(error);

                if (!TryGetDebugDataOrError(m_GraphName, m_ExecutionItem.id, out var debugData, out var dataError))
                    return QueryResult<PassInfoOutput>.Fail(dataError);

                for (int i = 0; i < debugData.passList.Count; i++)
                {
                    if (string.Equals(debugData.passList[i].name, passName, StringComparison.OrdinalIgnoreCase))
                    {
                        return QueryResult<PassInfoOutput>.Ok(new PassInfoOutput
                        {
                            pass = ConvertPassData(debugData, i)
                        });
                    }
                }

                return QueryResult<PassInfoOutput>.Fail($"Pass '{passName}' not found");
            }

            public QueryResult<CulledPassListOutput> GetCulledPasses()
            {
                if (!TryValidateExecution(out var error))
                    return QueryResult<CulledPassListOutput>.Fail(error);

                if (!TryGetDebugDataOrError(m_GraphName, m_ExecutionItem.id, out var debugData, out var dataError))
                    return QueryResult<CulledPassListOutput>.Fail(dataError);
                return QueryResult<CulledPassListOutput>.Ok(GetCulledPassesFromDebugData(debugData));
            }

            public QueryResult<UnmergedPassListOutput> GetUnmergedPasses()
            {
                if (!TryValidateExecution(out var error))
                    return QueryResult<UnmergedPassListOutput>.Fail(error);

                if (!TryGetDebugDataOrError(m_GraphName, m_ExecutionItem.id, out var debugData, out var dataError))
                    return QueryResult<UnmergedPassListOutput>.Fail(dataError);
                return QueryResult<UnmergedPassListOutput>.Ok(GetUnmergedPassesFromDebugData(debugData));
            }

            public QueryResult<ResourceListOutput> GetResources(string resourceType = null)
            {
                if (!TryValidateExecution(out var error))
                    return QueryResult<ResourceListOutput>.Fail(error);

                if (!TryGetDebugDataOrError(m_GraphName, m_ExecutionItem.id, out var debugData, out var dataError))
                    return QueryResult<ResourceListOutput>.Fail(dataError);

                return GetResources(debugData, resourceType);
            }

            public QueryResult<ResourceInfoOutput> GetResource(string resourceType, int resourceIndex)
            {
                if (!TryValidateExecution(out var error))
                    return QueryResult<ResourceInfoOutput>.Fail(error);

                if (!TryGetDebugDataOrError(m_GraphName, m_ExecutionItem.id, out var debugData, out var dataError))
                    return QueryResult<ResourceInfoOutput>.Fail(dataError);

                if (!Enum.TryParse<RenderGraphResourceType>(resourceType, true, out var type) || type == RenderGraphResourceType.Count ||
                    !Enum.IsDefined(typeof(RenderGraphResourceType), type))
                {
                    return QueryResult<ResourceInfoOutput>.Fail($"Invalid resource type '{resourceType}'. Use: Texture, Buffer, or AccelerationStructure");
                }

                var resources = debugData.resourceLists[(int)type];
                if (resourceIndex < 0 || resourceIndex >= resources.Count)
                {
                    return QueryResult<ResourceInfoOutput>.Fail($"Resource index {resourceIndex} out of range for type {resourceType} (0 to {resources.Count - 1})");
                }

                return QueryResult<ResourceInfoOutput>.Ok(new ResourceInfoOutput
                {
                    resource = ConvertResourceData(debugData, type, resourceIndex)
                });
            }

            public QueryResult<ResourceInfoOutput> GetResourceByName(string resourceName)
            {
                if (!TryValidateExecution(out var error))
                    return QueryResult<ResourceInfoOutput>.Fail(error);

                if (!TryGetDebugDataOrError(m_GraphName, m_ExecutionItem.id, out var debugData, out var dataError))
                    return QueryResult<ResourceInfoOutput>.Fail(dataError);

                for (int type = 0; type < (int)RenderGraphResourceType.Count; type++)
                {
                    var resources = debugData.resourceLists[type];
                    for (int i = 0; i < resources.Count; i++)
                    {
                        if (string.Equals(resources[i].name, resourceName, StringComparison.OrdinalIgnoreCase))
                        {
                            return QueryResult<ResourceInfoOutput>.Ok(new ResourceInfoOutput
                            {
                                resource = ConvertResourceData(debugData, (RenderGraphResourceType)type, i)
                            });
                        }
                    }
                }

                return QueryResult<ResourceInfoOutput>.Fail($"Resource '{resourceName}' not found");
            }

            public QueryResult<ExecutionCaptureInfoOutput> GetCaptureInfo()
            {
                if (!TryValidateExecution(out var error))
                    return QueryResult<ExecutionCaptureInfoOutput>.Fail(error);
                return GetCaptureInfoInternal();
            }

            // === Convenience Methods ===
            public QueryResult<PassListOutput> GetPassesByType(string passType)
            {
                var passesResult = GetPasses();
                if (!passesResult.TryGetValue(out var passListOutput))
                    return QueryResult<PassListOutput>.Fail(passesResult.error);

                using (UnityEngine.Pool.ListPool<PassSummary>.Get(out var filteredPasses))
                {
                    foreach (var pass in passListOutput.passes)
                    {
                        if (string.Equals(pass.type, passType, StringComparison.OrdinalIgnoreCase))
                        {
                            filteredPasses.Add(pass);
                        }
                    }

                    int culledCount = 0;
                    for (int i = 0; i < filteredPasses.Count; i++)
                    {
                        if (filteredPasses[i].culled)
                            culledCount++;
                    }

                    return QueryResult<PassListOutput>.Ok(new PassListOutput
                    {
                        executionName = passListOutput.executionName,
                        passCount = filteredPasses.Count,
                        culledCount = culledCount,
                        passes = filteredPasses.ToArray()
                    });
                }
            }

            public QueryResult<PassDependencyOutput> GetPassesUsingResource(string resourceName)
            {
                var passesResult = GetPasses();
                if (!passesResult.TryGetValue(out var passListOutput))
                    return QueryResult<PassDependencyOutput>.Fail(passesResult.error);

                using (UnityEngine.Pool.ListPool<PassInfo>.Get(out var matchingPasses))
                {
                    foreach (var passSummary in passListOutput.passes)
                    {
                        if (TryGetPassInfo(passSummary, out var passInfo))
                        {
                            bool usesResource = false;

                            foreach (var read in passInfo.resourceReads ?? Array.Empty<string>())
                            {
                                if (string.Equals(read, resourceName, StringComparison.OrdinalIgnoreCase))
                                {
                                    usesResource = true;
                                    break;
                                }
                            }

                            if (!usesResource)
                            {
                                foreach (var write in passInfo.resourceWrites ?? Array.Empty<string>())
                                {
                                    if (string.Equals(write, resourceName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        usesResource = true;
                                        break;
                                    }
                                }
                            }

                            if (usesResource)
                                matchingPasses.Add(passInfo);
                        }
                    }

                    return QueryResult<PassDependencyOutput>.Ok(new PassDependencyOutput
                    {
                        resourceName = resourceName,
                        passCount = matchingPasses.Count,
                        passes = matchingPasses.ToArray()
                    });
                }
            }

            public QueryResult<PassInfoOutput> GetResourceProducerPass(string resourceName)
            {
                var resourceResult = GetResourceByName(resourceName);
                if (!resourceResult.TryGetValue(out var resourceOutput))
                    return QueryResult<PassInfoOutput>.Fail(resourceResult.error);

                var resourceInfo = resourceOutput.resource;

                if (resourceInfo.imported)
                {
                    return QueryResult<PassInfoOutput>.Fail(
                        $"Resource '{resourceName}' has no creation pass (may be imported)",
                        "This resource is likely imported from outside the render graph");
                }

                if (resourceInfo.creationPassIndex >= 0)
                {
                    return GetPass(resourceInfo.creationPassIndex);
                }

                return QueryResult<PassInfoOutput>.Fail(
                    $"Resource '{resourceName}' has no creation pass",
                    "This resource was not created by any pass in the render graph");
            }

            public QueryResult<GraphMetricsOutput> GetGraphMetrics()
            {
                if (!TryValidateExecution(out var error))
                    return QueryResult<GraphMetricsOutput>.Fail(error);

                if (!TryGetDebugDataOrError(m_GraphName, m_ExecutionItem.id, out var debugData, out var dataError))
                    return QueryResult<GraphMetricsOutput>.Fail(dataError);

                var passes = GetPassesFromDebugData(debugData);
                var culled = GetCulledPassesFromDebugData(debugData);
                var unmerged = GetUnmergedPassesFromDebugData(debugData);
                var resources = GetResourcesFromDebugData(debugData, null);

                int textures = 0, buffers = 0, importedCount = 0;
                foreach (var resource in resources.resources)
                {
                    if (resource.type == nameof(RenderGraphResourceType.Texture)) textures++;
                    if (resource.type == nameof(RenderGraphResourceType.Buffer)) buffers++;
                    if (resource.imported) importedCount++;
                }

                using (UnityEngine.Pool.ListPool<GraphInsight>.Get(out var allInsights))
                {
                    foreach (var insight in AnalyzeCulling(passes, culled))
                        allInsights.Add(insight);

                    foreach (var insight in AnalyzePassDistribution(passes))
                        allInsights.Add(insight);

                    foreach (var insight in AnalyzeAsyncCompute(passes))
                        allInsights.Add(insight);

                    foreach (var insight in AnalyzeMerging(passes, unmerged))
                        allInsights.Add(insight);

                    foreach (var insight in AnalyzeResources(resources, textures, buffers, importedCount))
                        allInsights.Add(insight);

                    return QueryResult<GraphMetricsOutput>.Ok(new GraphMetricsOutput
                    {
                        executionName = passes.executionName,
                        totalPasses = passes.passCount,
                        culledPasses = culled.culledPasses.Length,
                        unmergedPasses = unmerged.unmergedPasses.Length,
                        totalResources = resources.resourceCount,
                        textureCount = textures,
                        bufferCount = buffers,
                        importedResourceCount = importedCount,
                        insights = allInsights.ToArray()
                    });
                }
            }

            private static IEnumerable<GraphInsight> AnalyzeCulling(PassListOutput passes, CulledPassListOutput culled)
            {
                int activePasses = passes.passCount - culled.culledPasses.Length;
                float cullPercentage = (passes.passCount > 0) ? (culled.culledPasses.Length * 100f / passes.passCount) : 0f;

                yield return new GraphInsight
                {
                    severity = InsightSeverity.Info,
                    category = InsightCategory.Culling,
                    message = $"Culling: {culled.culledPasses.Length}/{passes.passCount} passes culled ({cullPercentage:F1}%), {activePasses} active"
                };
            }

            private IEnumerable<GraphInsight> AnalyzePassDistribution(PassListOutput passes)
            {
                int rasterPasses = 0, computePasses = 0, unsupportedPasses = 0, activePasses = 0;

                foreach (var pass in passes.passes)
                {
                    if (!pass.culled)
                    {
                        activePasses++;
                        if (pass.type == nameof(RenderGraphPassType.Raster)) rasterPasses++;
                        else if (pass.type == nameof(RenderGraphPassType.Compute)) computePasses++;
                        else if (pass.type == nameof(RenderGraphPassType.Unsafe)) unsupportedPasses++;
                    }
                }

                if (activePasses > 0)
                {
                    yield return new GraphInsight
                    {
                        severity = InsightSeverity.Info,
                        category = InsightCategory.PassDistribution,
                        message = $"Active passes: {activePasses} total ({rasterPasses} raster, {computePasses} compute{(unsupportedPasses > 0 ? $", {unsupportedPasses} unsupported" : "")})"
                    };
                }
            }

            private IEnumerable<GraphInsight> AnalyzeAsyncCompute(PassListOutput passes)
            {
                int asyncPasses = 0, syncPoints = 0;

                foreach (var pass in passes.passes)
                {
                    if (pass.culled) continue;
                    if (TryGetPassInfo(pass, out var passInfo))
                    {
                        if (passInfo.async) asyncPasses++;
                        if (passInfo.syncToPassIndex >= 0 || passInfo.syncFromPassIndex >= 0) syncPoints++;
                    }
                }

                yield return new GraphInsight
                {
                    severity = InsightSeverity.Info,
                    category = InsightCategory.AsyncCompute,
                    message = $"Async compute: {asyncPasses} async passes, {syncPoints} sync points"
                };
            }

            private static IEnumerable<GraphInsight> AnalyzeMerging(PassListOutput passes, UnmergedPassListOutput unmerged)
            {
                int activePasses = passes.passCount - passes.culledCount;
                float mergeEfficiency = activePasses > 0 ? ((activePasses - unmerged.unmergedPasses.Length) * 100f / activePasses) : 0f;

                yield return new GraphInsight
                {
                    severity = InsightSeverity.Info,
                    category = InsightCategory.Merging,
                    message = $"Native pass merging: {unmerged.unmergedPasses.Length} merge breaks out of {activePasses} active passes ({mergeEfficiency:F1}% efficiency)"
                };

                if (unmerged.unmergedPasses.Length > 0)
                {
                    using (UnityEngine.Pool.DictionaryPool<string, int>.Get(out var reasonCounts))
                    {
                        foreach (var pass in unmerged.unmergedPasses)
                        {
                            if (!string.IsNullOrEmpty(pass.breakReason))
                            {
                                if (!reasonCounts.ContainsKey(pass.breakReason))
                                    reasonCounts[pass.breakReason] = 0;
                                reasonCounts[pass.breakReason]++;
                            }
                        }

                        if (reasonCounts.Count > 0)
                        {
                            using (UnityEngine.Pool.ListPool<KeyValuePair<string, int>>.Get(out var sortedReasons))
                            {
                                sortedReasons.AddRange(reasonCounts);
                                sortedReasons.Sort((a, b) => b.Value.CompareTo(a.Value));

                                var topReason = sortedReasons[0];
                                yield return new GraphInsight
                                {
                                    severity = InsightSeverity.Info,
                                    category = InsightCategory.Merging,
                                    message = $"Most common merge break reason: '{topReason.Key}' ({topReason.Value} occurrences)"
                                };
                            }
                        }
                    }
                }
            }

            private static long ComputeTextureMemory(int width, int height, int depth, int samples, string formatString)
            {
                // Try to parse the format and use Unity's proper memory calculation
                if (string.IsNullOrEmpty(formatString))
                    return (long)width * height * Math.Max(1, depth) * Math.Max(1, samples) * 4; // Default fallback

                try
                {
                    if (Enum.TryParse<UnityEngine.Experimental.Rendering.GraphicsFormat>(formatString, out var format))
                    {
                        // ComputeMipmapSize properly handles compressed formats (BC, ASTC, etc.)
                        // For 3D textures, compute per-slice and multiply by depth
                        int depthSlices = Math.Max(1, depth);
                        long memoryPerSample = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.ComputeMipmapSize(width, height, format) * depthSlices;
                        return memoryPerSample * Math.Max(1, samples);
                    }
                }
                catch (System.Exception)
                {
                    // If parsing or utility fails, fall back to simple estimation
                }

                return (long)width * height * Math.Max(1, depth) * Math.Max(1, samples) * 4; // Default fallback
            }

            private IEnumerable<GraphInsight> AnalyzeResources(ResourceListOutput resources, int textureCount, int bufferCount, int importedCount)
            {
                int memorylessCount = 0, shortLivedResources = 0;
                long totalTextureMemory = 0;

                foreach (var resource in resources.resources)
                {
                    if (resource.type == nameof(RenderGraphResourceType.Texture))
                    {
                        if (TryGetResourceInfo(resource, out var res))
                        {
                            if (res.memoryless) memorylessCount++;

                            long memory = ComputeTextureMemory(res.width, res.height, res.depth, res.samples, res.format);
                            totalTextureMemory += memory;

                            if (!res.imported)
                            {
                                int lifetime = res.releasePassIndex - res.creationPassIndex;
                                if (lifetime <= 1 && (res.consumerPasses?.Length ?? 0) > 0)
                                    shortLivedResources++;
                            }
                        }
                    }
                }

                yield return new GraphInsight
                {
                    severity = InsightSeverity.Info,
                    category = InsightCategory.Resources,
                    message = $"Resources: {textureCount} textures, {bufferCount} buffers, {importedCount} imported, {memorylessCount} memoryless"
                };

                float memoryMB = totalTextureMemory / (1024f * 1024f);
                yield return new GraphInsight
                {
                    severity = InsightSeverity.Info,
                    category = InsightCategory.Resources,
                    message = $"Estimated texture memory (base mip only): {memoryMB:F1} MB"
                };

                if (shortLivedResources > 0)
                {
                    yield return new GraphInsight
                    {
                        severity = InsightSeverity.Info,
                        category = InsightCategory.Resources,
                        message = $"Short-lived resources (lifetime ≤ 1 pass): {shortLivedResources}"
                    };
                }
            }

            internal static PassListOutput GetPassesFromDebugData(RenderGraph.DebugData debugData)
            {
                var passes = new PassSummary[debugData.passList.Count];
                int culledCount = 0;
                for (int i = 0; i < debugData.passList.Count; i++)
                {
                    var pass = debugData.passList[i];
                    passes[i] = new PassSummary
                    {
                        index = i,
                        name = pass.name,
                        type = pass.type.ToString(),
                        culled = pass.culled
                    };
                    if (pass.culled) culledCount++;
                }

                return new PassListOutput
                {
                    executionName = debugData.executionName,
                    passCount = passes.Length,
                    culledCount = culledCount,
                    passes = passes
                };
            }

            internal static CulledPassListOutput GetCulledPassesFromDebugData(RenderGraph.DebugData debugData)
            {
                using (UnityEngine.Pool.ListPool<CulledPassInfo>.Get(out var culledList))
                {
                    for (int i = 0; i < debugData.passList.Count; i++)
                    {
                        var pass = debugData.passList[i];
                        if (pass.culled)
                        {
                            culledList.Add(new CulledPassInfo
                            {
                                index = i,
                                name = pass.name,
                                type = pass.type.ToString()
                            });
                        }
                    }

                    return new CulledPassListOutput
                    {
                        executionName = debugData.executionName,
                        culledPasses = culledList.ToArray()
                    };
                }
            }

            internal static UnmergedPassListOutput GetUnmergedPassesFromDebugData(RenderGraph.DebugData debugData)
            {
                using (UnityEngine.Pool.ListPool<UnmergedPassInfo>.Get(out var unmergedList))
                {
                    for (int i = 0; i < debugData.passList.Count; i++)
                    {
                        var pass = debugData.passList[i];
                        if (pass.nrpInfo?.nativePassInfo != null && !string.IsNullOrEmpty(pass.nrpInfo.nativePassInfo.passBreakReasoning))
                        {
                            string nextPassName = null;
                            if (i + 1 < debugData.passList.Count)
                            {
                                nextPassName = debugData.passList[i + 1].name;
                            }

                            unmergedList.Add(new UnmergedPassInfo
                            {
                                index = i,
                                name = pass.name,
                                breakReason = pass.nrpInfo.nativePassInfo.passBreakReasoning,
                                nextPassName = nextPassName
                            });
                        }
                    }

                    return new UnmergedPassListOutput
                    {
                        executionName = debugData.executionName,
                        unmergedPasses = unmergedList.ToArray()
                    };
                }
            }

            internal static ResourceListOutput GetResourcesFromDebugData(RenderGraph.DebugData debugData, string resourceType = null)
            {
                using (UnityEngine.Pool.ListPool<ResourceSummary>.Get(out var resourceList))
                {
                    int startType = 0;
                    int endType = (int)RenderGraphResourceType.Count;

                    if (!string.IsNullOrEmpty(resourceType))
                    {
                        if (Enum.TryParse<RenderGraphResourceType>(resourceType, true, out var parsed) &&
                            parsed != RenderGraphResourceType.Count &&
                            Enum.IsDefined(typeof(RenderGraphResourceType), parsed))
                        {
                            startType = (int)parsed;
                            endType = startType + 1;
                        }
                        else
                        {
                            return null;
                        }
                    }

                    for (int type = startType; type < endType; type++)
                    {
                        var resources = debugData.resourceLists[type];
                        for (int i = 0; i < resources.Count; i++)
                        {
                            var resource = resources[i];
                            resourceList.Add(new ResourceSummary
                            {
                                index = i,
                                name = resource.name,
                                type = ((RenderGraphResourceType)type).ToString(),
                                imported = resource.imported
                            });
                        }
                    }

                    return new ResourceListOutput
                    {
                        executionName = debugData.executionName,
                        resourceCount = resourceList.Count,
                        resources = resourceList.ToArray()
                    };
                }
            }

            internal QueryResult<ResourceListOutput> GetResources(RenderGraph.DebugData debugData, string resourceType = null)
            {
                if (!string.IsNullOrEmpty(resourceType))
                {
                    if (!Enum.TryParse<RenderGraphResourceType>(resourceType, true, out var parsed) ||
                        parsed == RenderGraphResourceType.Count ||
                        !Enum.IsDefined(typeof(RenderGraphResourceType), parsed))
                    {
                        return QueryResult<ResourceListOutput>.Fail($"Invalid resource type '{resourceType}'. Use: Texture, Buffer, or AccelerationStructure");
                    }
                }

                return QueryResult<ResourceListOutput>.Ok(GetResourcesFromDebugData(debugData, resourceType));
            }

            private QueryResult<ExecutionCaptureInfoOutput> GetCaptureInfoInternal()
            {
                if (!TryGetDebugDataOrError(m_GraphName, m_ExecutionItem.id, out var debugData, out var dataError))
                    return QueryResult<ExecutionCaptureInfoOutput>.Fail(dataError);

                return QueryResult<ExecutionCaptureInfoOutput>.Ok(new ExecutionCaptureInfoOutput
                {
                    executionName = debugData.executionName,
                    captureSourceString = debugData.captureSourceString,
                    captureTimestamp = debugData.captureTimestamp,
                });
            }

            internal static bool TryGetDebugDataOrError(string graphName, EntityId executionId, out RenderGraph.DebugData debugData, out string errorOutput)
            {
                debugData = null;
                errorOutput = null;
                try
                {
                    debugData = RenderGraphDebugSession.currentDebugSession.GetDebugData(graphName, executionId);
                    if (debugData == null || !debugData.valid)
                    {
                        errorOutput = "Debug data is not valid. Ensure the render graph viewer is open and a frame has been rendered";
                        return false;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    errorOutput = $"{ex.Message}. Check that the graph name and execution ID are correct";
                    return false;
                }
            }

            private static PassInfo ConvertPassData(RenderGraph.DebugData debugData, int passIndex)
            {
                var pass = debugData.passList[passIndex];

                string passBreakReasoning = null;
                int[] mergedPassIds = null;

                if (pass.nrpInfo?.nativePassInfo != null)
                {
                    passBreakReasoning = pass.nrpInfo.nativePassInfo.passBreakReasoning;
                    if (pass.nrpInfo.nativePassInfo.mergedPassIds != null)
                    {
                        mergedPassIds = pass.nrpInfo.nativePassInfo.mergedPassIds.ToArray();
                    }
                }

                using (UnityEngine.Pool.ListPool<string>.Get(out var resourceReads))
                using (UnityEngine.Pool.ListPool<string>.Get(out var resourceWrites))
                {
                    if (pass.resourceReadLists != null)
                    {
                        for (int type = 0; type < (int)RenderGraphResourceType.Count; type++)
                        {
                            var readList = pass.resourceReadLists[type];
                            if (readList != null)
                            {
                                foreach (var idx in readList)
                                {
                                    var resources = debugData.resourceLists[type];
                                    if (idx >= 0 && idx < resources.Count)
                                        resourceReads.Add(resources[idx].name);
                                }
                            }
                        }
                    }

                    if (pass.resourceWriteLists != null)
                    {
                        for (int type = 0; type < (int)RenderGraphResourceType.Count; type++)
                        {
                            var writeList = pass.resourceWriteLists[type];
                            if (writeList != null)
                            {
                                foreach (var idx in writeList)
                                {
                                    var resources = debugData.resourceLists[type];
                                    if (idx >= 0 && idx < resources.Count)
                                        resourceWrites.Add(resources[idx].name);
                                }
                            }
                        }
                    }

                    return new PassInfo
                    {
                        index = passIndex,
                        name = pass.name,
                        type = pass.type.ToString(),
                        culled = pass.culled,
                        async = pass.async,
                        nativeSubPassIndex = pass.nativeSubPassIndex,
                        syncToPassIndex = pass.syncToPassIndex,
                        syncFromPassIndex = pass.syncFromPassIndex,
                        passBreakReasoning = passBreakReasoning,
                        mergedPassIds = mergedPassIds,
                        resourceReads = resourceReads.ToArray(),
                        resourceWrites = resourceWrites.ToArray()
                    };
                }
            }

            private static ResourceInfo ConvertResourceData(RenderGraph.DebugData debugData, RenderGraphResourceType type, int resourceIndex)
            {
                var resource = debugData.resourceLists[(int)type][resourceIndex];

                int width = 0, height = 0, depth = 0, samples = 1;
                string format = null;

                if (resource.textureData != null)
                {
                    width = resource.textureData.width;
                    height = resource.textureData.height;
                    depth = resource.textureData.depth;
                    samples = resource.textureData.samples;
                    format = resource.textureData.format.ToString();
                }
                else if (resource.bufferData != null)
                {
                    width = resource.bufferData.count;
                    height = resource.bufferData.stride;
                    format = resource.bufferData.target.ToString();
                }

                return new ResourceInfo
                {
                    index = resourceIndex,
                    name = resource.name,
                    type = type.ToString(),
                    imported = resource.imported,
                    memoryless = resource.memoryless,
                    creationPassIndex = resource.creationPassIndex,
                    releasePassIndex = resource.releasePassIndex,
                    width = width,
                    height = height,
                    depth = depth,
                    format = format,
                    samples = samples,
                    consumerPasses = resource.consumerList?.ToArray(),
                    producerPasses = resource.producerList?.ToArray()
                };
            }
        }
    }
}
