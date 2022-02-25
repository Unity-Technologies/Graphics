using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

// Typedef for the in-engine RendererList API (to avoid conflicts with the experimental version)
using CoreRendererListDesc = UnityEngine.Rendering.RendererUtils.RendererListDesc;
using System.Runtime.CompilerServices;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    class RenderGraphDebugParams
    {
        DebugUI.Widget[] m_DebugItems;
        DebugUI.Panel m_DebugPanel;

        public bool clearRenderTargetsAtCreation;
        public bool clearRenderTargetsAtRelease;
        public bool disablePassCulling;
        public bool immediateMode;
        public bool enableLogging;
        public bool logFrameInformation;
        public bool logGraphVis;
        public bool logResources;

        private static class Strings
        {
            public static readonly NameAndTooltip ClearRenderTargetsAtCreation = new() { name = "Clear Render Targets At Creation", tooltip = "Enable to clear all render textures before any rendergraph passes to check if some clears are missing." };
            public static readonly NameAndTooltip DisablePassCulling = new() { name = "Disable Pass Culling", tooltip = "Enable to temporarily disable culling to asses if a pass is culled." };
            public static readonly NameAndTooltip ImmediateMode = new() { name = "Immediate Mode", tooltip = "Enable to force render graph to execute all passes in the order you registered them." };
            public static readonly NameAndTooltip EnableLogging = new() { name = "Enable Logging", tooltip = "Enable to allow HDRP to capture information in the log." };
            public static readonly NameAndTooltip LogFrameInformation = new() { name = "Log Frame Information", tooltip = "Enable to log information output from each frame." };
            public static readonly NameAndTooltip LogResources = new() { name = "Log Resources", tooltip = "Enable to log the current render graph's global resource usage." };
            public static readonly NameAndTooltip LogGraphVis = new() { name = "GraphVis Output", tooltip = "Generate GraphVis output for the render graph/" };
        }

        public void RegisterDebug(string name, DebugUI.Panel debugPanel = null)
        {
            var list = new List<DebugUI.Widget>();
            list.Add(new DebugUI.Container
            {
                displayName = $"{name} Render Graph",
                children =
                {
                    new DebugUI.BoolField { nameAndTooltip = Strings.ClearRenderTargetsAtCreation, getter = () => clearRenderTargetsAtCreation, setter = value => clearRenderTargetsAtCreation = value },
                    // We cannot expose this option as it will change the active render target and the debug menu won't know where to render itself anymore.
                    //    list.Add(new DebugUI.BoolField { displayName = "Clear Render Targets at release", getter = () => clearRenderTargetsAtRelease, setter = value => clearRenderTargetsAtRelease = value });
                    new DebugUI.BoolField { nameAndTooltip = Strings.DisablePassCulling, getter = () => disablePassCulling, setter = value => disablePassCulling = value },
                    new DebugUI.BoolField { nameAndTooltip = Strings.ImmediateMode, getter = () => immediateMode, setter = value => immediateMode = value },
                    new DebugUI.BoolField { nameAndTooltip = Strings.EnableLogging, getter = () => enableLogging, setter = value => enableLogging = value },
                    new DebugUI.Button
                    {
                        nameAndTooltip = Strings.LogFrameInformation,
                        action = () =>
                        {
                            if (!enableLogging)
                                Debug.Log("You must first enable logging before this logging frame information.");
                            logFrameInformation = true;
            #if UNITY_EDITOR
                            UnityEditor.SceneView.RepaintAll();
            #endif
                        }
                    },
                    new DebugUI.Button
                    {
                        nameAndTooltip = Strings.LogResources,
                        action = () =>
                        {
                            if (!enableLogging)
                                Debug.Log("You must first enable logging before this logging resources.");
                            logResources = true;
                            logResources = true;
            #if UNITY_EDITOR
                            UnityEditor.SceneView.RepaintAll();
            #endif
                        }
                    },
                    new DebugUI.Button
                    {
                        nameAndTooltip = Strings.LogGraphVis,
                        action = () =>
                        {
                            if (!enableLogging)
                                Debug.Log("You must first enable logging before this logging resources.");
                            logGraphVis = true;
            #if UNITY_EDITOR
                            UnityEditor.SceneView.RepaintAll();
            #endif
                        }
                    }
                }
            });

            m_DebugItems = list.ToArray();
            m_DebugPanel = debugPanel != null ? debugPanel : DebugManager.instance.GetPanel(name.Length == 0 ? "Render Graph" : name, true);
            m_DebugPanel.children.Add(m_DebugItems);
        }

        public void UnRegisterDebug(string name)
        {
            //DebugManager.instance.RemovePanel(name.Length == 0 ? "Render Graph" : name);
            m_DebugPanel.children.Remove(m_DebugItems);
            m_DebugPanel = null;
            m_DebugItems = null;
        }
    }

    internal class RenderGraphDebugData
    {
        [DebuggerDisplay("PassDebug: {name}")]
        public struct PassDebugData
        {
            public string name;
            public List<int>[] resourceReadLists;
            public List<int>[] resourceWriteLists;
            public bool culled;
            // We have this member instead of removing the pass altogether because we need the full list of passes in order to be able to remap them correctly when we remove them from display in the viewer.
            public bool generateDebugData;
        }

        [DebuggerDisplay("ResourceDebug: {name} [{creationPassIndex}:{releasePassIndex}]")]
        public struct ResourceDebugData
        {
            public string name;
            public bool imported;
            public int creationPassIndex;
            public int releasePassIndex;

            public List<int> consumerList;
            public List<int> producerList;
        }

        public List<PassDebugData> passList = new List<PassDebugData>();
        public List<ResourceDebugData>[] resourceLists = new List<ResourceDebugData>[(int)RenderGraphResourceType.Count];

        public void Clear()
        {
            passList.Clear();

            // Create if needed
            if (resourceLists[0] == null)
            {
                for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                    resourceLists[i] = new List<ResourceDebugData>();
            }

            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                resourceLists[i].Clear();
        }
    }

    public partial class RenderGraph
    {

        /// <summary>
        /// Register the render graph to the debug window.
        /// </summary>
        /// <param name="panel"></param>
        public void RegisterDebug(DebugUI.Panel panel = null)
        {
            m_DebugParameters.RegisterDebug(name, panel);
        }

        /// <summary>
        /// Unregister render graph from the debug window.
        /// </summary>
        public void UnRegisterDebug()
        {
            m_DebugParameters.UnRegisterDebug(this.name);
        }

        /// <summary>
        /// Get the list of all registered render graphs.
        /// </summary>
        /// <returns>The list of all registered render graphs.</returns>
        public static List<RenderGraph> GetRegisteredRenderGraphs()
        {
            return s_RegisteredGraphs;
        }

        /// <summary>
        /// Returns the last rendered frame debug data. Can be null if requireDebugData is set to false.
        /// </summary>
        /// <returns>The last rendered frame debug data</returns>
        internal RenderGraphDebugData GetDebugData(string executionName)
        {
            if (m_DebugData.TryGetValue(executionName, out var debugData))
                return debugData;

            return null;
        }

        private void EndDebugFrame()
        {
            if (m_DebugParameters.logFrameInformation)
            {
                Debug.Log(m_FrameInformationLogger.GetAllLogs());
                m_DebugParameters.logFrameInformation = false;
            }
            if (m_DebugParameters.logResources)
            {
                m_Resources.FlushLogs();
                m_DebugParameters.logResources = false;
            }
            if (m_DebugParameters.logGraphVis)
            {
                try
                {
                    string graphVisCode = m_GraphVisLogger.GetAllLogs();
                    var tempFile = System.IO.Path.GetTempFileName();
                    var tempSvgFile = tempFile + ".svg";
                    System.IO.File.WriteAllText(tempFile, graphVisCode);
                    var proc = System.Diagnostics.Process.Start("dot", string.Format("-Tsvg \"{0}\" -o \"{1}\"", tempFile, tempSvgFile));
                    proc.WaitForExit();
                    if (System.IO.File.Exists(tempSvgFile))
                    {
                        System.Diagnostics.Process.Start(tempSvgFile);
                    }
                }
                finally
                {
                    m_DebugParameters.logGraphVis = false;
                }

            }
        }

        string PassIdentifier(RenderGraphPass pass)
        {
            if (pass == null) return "null";
            return "pass_" + pass.index;
        }

        private void LogGraphVis()
        {
            if (!m_DebugParameters.logGraphVis)
            {
                return;
            }

            m_GraphVisLogger.LogLine("digraph H {{\n");
            m_GraphVisLogger.LogLine("\tnode [shape=cds style=\"filled\"];");
            m_GraphVisLogger.LogLine("\trankdir = \"LR\"");
            m_GraphVisLogger.LogLine("bgcolor = \"gray60\"");//Dark theme => profit!

            var colorList = new string[] { "maroon4", "navy" };

            // true = Draw the graph in declaration order
            // false = allow the graph to be drawn purely based on data dependencies
            bool linearize = false;

            // If we linearize the graph we put all the main pass nodes in a single cluster
            // this improves the lay-out as clusters are laid out first then inter-cluster nodes/edges
            m_GraphVisLogger.LogLine("subgraph cluster0 {{");
            CompiledPassInfo? prev = null;

            // Create graph nodes for all passes
            foreach (var info in m_CompiledPassInfos)
            {
                // Gather lists of transients and first writes
                var transientList = "";
                var firstWriteList = "";
                for (int iType = 0; iType < (int)RenderGraphResourceType.Count; ++iType)
                {
                    var resList = info.pass.transientResourceList[iType];
                    var edgeColor = colorList[iType];
                    foreach (var res in resList)
                    {
                        var name = m_Resources.GetRenderGraphResourceName(res);
                        transientList += name + "\\l";
                    }

                    resList = info.pass.resourceWriteLists[iType];
                    foreach (var res in resList)
                    {
                        var pointTo = LastPassWrittenBefore(info.pass, res);
                        if (pointTo != null) continue;

                        var name = m_Resources.GetRenderGraphResourceName(res);
                        firstWriteList += name + "\\l";
                    }
                }

                // Add a node for this pass
                var color = "white";
                if (info.culled)
                {
                    color = "lightcoral";
                }
                else if (info.hasSideEffect)
                {
                    color = "greenyellow";
                }
                var passSourceCodeUrl = "vscode://file/" + info.pass.file.Replace('\\', '/') + ':' + info.pass.line;

                var label = info.pass.name;
                if (transientList.Length > 0)
                {
                    label = label + "| Transients:\\n" + transientList;
                }
                if (firstWriteList.Length > 0)
                {
                    label = label + "| Allocates:\\n" + firstWriteList;
                }

                m_GraphVisLogger.LogLine("{0} [ label=\"{1}\" fillcolor=\"{2}\"  URL=\"{3}\" shape=record];", PassIdentifier(info.pass), label, color, passSourceCodeUrl);

                // Add a fake dependency edge to force the lay out of the graph to be linear
                // Higher weights will cause all the passes to be drawn more and more on a single straight line
                // be more difficult to read as the data depencency arrows may become very tangled up
                // It's usually better to let it us a bit more vertical space to improve the readability
                // even if the "linear timeline" view gets a bit lost.
                if (linearize && prev != null)
                {
                    m_GraphVisLogger.LogLine("{0} -> {1} [ weight=20 style=dotted ];", PassIdentifier(prev.Value.pass), PassIdentifier(info.pass));
                }
                prev = info;
            }
            m_GraphVisLogger.LogLine("}}");

            // Loop over all the resources accessed by this node and add links (and possibly dummy nodes if the resources are imported etc)
            foreach (var info in m_CompiledPassInfos)
            {
                for (int iType = 0; iType < (int)RenderGraphResourceType.Count; ++iType)
                {
                    var edgeColor = colorList[iType];

                    // Resources read by this node => point to either a previous node that has written it
                    // or creates a dummy node if this is the first node to read an imported, shared, or texture created outside of the pass.
                    var resList = info.pass.resourceReadLists[iType];
                    foreach (var res in resList)
                    {
                        var pointTo = LastPassWrittenBefore(info.pass, res);
                        var pointToIdentifier = PassIdentifier(pointTo);
                        var name = m_Resources.GetRenderGraphResourceName(res) + " v" + res.version;
                        // Create a dummy source node if no previous pass was found writing this.
                        // This usually means the resource is imported
                        if (pointTo == null)
                        {
                            if (WritesResource(info.pass, res))
                            {
                                // We already have a dummy node for this generated by the writes above
                                // so don't add another dummy node
                                pointTo = info.pass;
                                pointToIdentifier = PassIdentifier(pointTo);
                            }
                            else
                            {
                                var dummyColor = "red"; // sort of an error to see this, it means the below logic doesn't catch it iall
                                var dummyShape = "cylinder";
                                var dummyName = name;
                                if (m_Resources.IsRenderGraphResourceImported(res))
                                {
                                    dummyColor = "turquoise";
                                    dummyName = "Import: " + dummyName;
                                }
                                else if (m_Resources.IsRenderGraphResourceShared(res.type, res.index))
                                {
                                    dummyColor = "thistle";
                                    dummyName = "Shared: " + dummyName;
                                }
                                pointToIdentifier = "resource_" + res.iType + "_" + res.index;
                                m_GraphVisLogger.LogLine("{0} [ label=\"{1}\" fillcolor=\"{2}\" shape=\"{3}\"];", pointToIdentifier, dummyName, dummyColor, dummyShape);
                            }
                        }
                        var toolTip = name + ": ";
                        if (pointTo != null)
                        {
                            toolTip += "Written by: " + pointTo.name + ", ";
                        }
                        toolTip += "Read by: " + info.pass.name;

                        string constr = "weight=" + ((pointTo == null) ? 1 : 10);
                        m_GraphVisLogger.LogLine("{0} -> {1} [ label=\"{2}\" color=\"{3}\" tooltip=\"{4}\" edgetooltip=\"{4}\" labeltooltip=\"{4}\" {5}];", pointToIdentifier, PassIdentifier(info.pass), name, edgeColor, toolTip, constr);
                    }
                }
            }

            foreach (var info in m_CompiledPassInfos)
            {

            }

            m_GraphVisLogger.LogLine("}}\n");
        }


        void LogFrameInformation()
        {
            if (m_DebugParameters.enableLogging)
            {
                m_FrameInformationLogger.LogLine($"==== Staring render graph frame for: {m_CurrentExecutionName} ====");

                if (!m_DebugParameters.immediateMode)
                    m_FrameInformationLogger.LogLine("Number of passes declared: {0}\n", m_RenderPasses.Count);
            }
        }

        void LogRendererListsCreation()
        {
            if (m_DebugParameters.enableLogging)
            {
                m_FrameInformationLogger.LogLine("Number of renderer lists created: {0}\n", m_RendererLists.Count);
            }
        }

        void LogRenderPassBegin(in CompiledPassInfo passInfo)
        {
            if (m_DebugParameters.enableLogging)
            {
                RenderGraphPass pass = passInfo.pass;

                m_FrameInformationLogger.LogLine("[{0}][{1}] \"{2}\"", pass.index, pass.enableAsyncCompute ? "Compute" : "Graphics", pass.name);
                using (new RenderGraphLogIndent(m_FrameInformationLogger))
                {
                    if (passInfo.syncToPassIndex != -1)
                        m_FrameInformationLogger.LogLine("Synchronize with [{0}]", passInfo.syncToPassIndex);
                }
            }
        }

        void LogCulledPasses()
        {
            if (m_DebugParameters.enableLogging)
            {
                m_FrameInformationLogger.LogLine("Pass Culling Report:");
                using (new RenderGraphLogIndent(m_FrameInformationLogger))
                {
                    for (int i = 0; i < m_CompiledPassInfos.size; ++i)
                    {
                        if (m_CompiledPassInfos[i].culled)
                        {
                            var pass = m_RenderPasses[i];
                            m_FrameInformationLogger.LogLine("[{0}] {1}", pass.index, pass.name);
                        }
                    }
                    m_FrameInformationLogger.LogLine("\n");
                }
            }
        }
    }
}
