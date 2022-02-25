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
    class NativePassCompiler
    {
        struct RenderGraphInstruction
        {

        }

        // Note pass=node in the graph, both are sometimes mixed up here
        // Datastructure that contains passes and dependencies and allow yo to iterate and reason on them more like a graph
        class CompilerContextData
        {
            public CompilerContextData(int numPasses, RenderGraphResourceRegistry numResources)
            {
                passData = new DynamicArray<PassData>(numPasses);
                inputData = new DynamicArray<PassInputData>(numPasses*2);
                outputData = new DynamicArray<PassOutputData>(numPasses*2);
                resources = new ResourcesData(numResources);
            }

            // Data per usage of a resource(version)
            public struct ResourceReaderData
            {
                public int passId; // Pass using this
                public int inputSlot; // Nth input of the pass is using this
            }

            // Data per resource(version)
            public struct ResourceData
            {
                public bool written; // This version of the resource is written by a pass (external resources may never be written by the graph for example)
                public int writePass; // Index in the pass array of the pass writing this version
                public int writeSlot; // Nth output of the pass is writing this
                public int numReaders; // Number of other passes reading this version
                public string name; // For debugging
                public bool isImported; // Imported graph resource
                public bool isShared; // Shared graph resource

                // Register the pass writing this resource version. A version can only be written by a single pass obviously.
                public void AddWriter(int passID, int slot)
                {
                    if (written) throw new Exception("Two passes write to the same resource version!");
                    writePass = passID;
                    writeSlot = slot;
                    written = true;
                }

                // Add an extra reader for this resource version. Resources can be read many times
                // The same pass can read a resource twice (say it is passed to two separate input slots)
                public void AddReader(CompilerContextData ctx, ResourceHandle h, int passId, int index)
                {
                    if (numReaders >= ResourcesData.MaxReaders)
                    {
                        throw new Exception("A maximum to 10 passes using a single output is supported for now");
                    }
                    ctx.resources.readerData[h.iType][ResourcesData.IndexReader(h, numReaders)] = new ResourceReaderData
                    {
                        passId = passId,
                        inputSlot = index
                    };
                    numReaders++;
                }

                // Remove all the reads for the given pass
                public void RemoveReader(CompilerContextData ctx, ResourceHandle h, int passId)
                {
                    for (int r=0; r < numReaders; )
                    {
                        if (ctx.resources.readerData[h.iType][ResourcesData.IndexReader(h, r)].passId == passId)
                        {
                            // It should be removed, switch with the end of the list if we're not already at the end of it
                            if (r < numReaders-1)
                            {
                                ctx.resources.readerData[h.iType][ResourcesData.IndexReader(h, r)] = ctx.resources.readerData[h.iType][ResourcesData.IndexReader(h, numReaders - 1)];
                            }
                            numReaders--;
                            continue;// Do not increment counter so we check the swapped element as well
                        }
                        r++;
                    }

                }
            }

            // Quick map of ResourceHandle -> ResourceData & ResourceReaderData
            // This is just a fully allocated array we assume there aren't too many resources & versions
            public class ResourcesData
            {
                public ResourceData[][] data; // Flattened fixed size array storing up to MaxVersions versions per resource id
                public DynamicArray<ResourceReaderData>[] readerData; // Flattened fixed size array storing up to MaxReaders per resource id Verssion
                public const int MaxVersions = 20;
                public const int MaxReaders = 10;

                public ResourcesData(RenderGraphResourceRegistry resources)
                {
                    data = new ResourceData[(int)RenderGraphResourceType.Count][];
                    readerData = new DynamicArray<ResourceReaderData>[(int)RenderGraphResourceType.Count];

                    for (int t = 0; t < (int)RenderGraphResourceType.Count; t++)
                    {
                        data[t] = new ResourceData[MaxVersions * resources.GetResourceCount((RenderGraphResourceType)t)];
                        readerData[t] = new DynamicArray<ResourceReaderData>(MaxVersions * resources.GetResourceCount((RenderGraphResourceType)t) * MaxReaders);

                        // Copy the names to the all the resource versions in the array
                        for (int r = 0; r < resources.GetResourceCount((RenderGraphResourceType)t); r++)
                        {
                            for (int v = 0; v < MaxVersions; v++)
                            {
                                var h = new ResourceHandle(r, (RenderGraphResourceType)t, false);
                                var vh = new ResourceHandle(h, v);
                                data[t][Index(vh)].name = resources.GetRenderGraphResourceName(h);
                                data[t][Index(vh)].isImported = resources.IsRenderGraphResourceImported(h);
                                data[t][Index(vh)].isShared = resources.IsRenderGraphResourceShared((RenderGraphResourceType)t, r);
                            }
                        }
                    }
                }

                // Flatten array index
                public static int Index(ResourceHandle h)
                {
                    if (h.version < 0 || h.version >= MaxVersions) throw new Exception("Invalid version: " + h.version);
                    return h.index * MaxVersions + h.version;
                }

                // Flatten array index
                public static int IndexReader(ResourceHandle h, int readerID)
                {
                    if (h.version < 0 || h.version >= MaxVersions) throw new Exception("Invalid version");
                    if (readerID < 0 || readerID >= MaxReaders) throw new Exception("Invalid reader");
                    return (h.index * MaxVersions + h.version) * MaxReaders + readerID;
                }

                // Lookup data for a given handle
                public ref ResourceData this[ResourceHandle h]
                {
                    get { return ref data[h.iType][Index(h)]; }
                }

            }

            public ResourcesData resources;

            // Iterate over all the readers of a particular resource
            public DynamicArray<ResourceReaderData>.RangeEnumerable Readers(ResourceHandle h)
            {
                int numReaders = resources[h].numReaders;
                return resources.readerData[h.iType].SubRange(ResourcesData.IndexReader(h, 0), numReaders);
            }

            // Get the i'th reader of a resource
            public ResourceReaderData ResourceReader(ResourceHandle h, int i)
            {
                int numReaders = resources[h].numReaders;
                if (i >= numReaders)
                {
                    throw new Exception("Invalid reader id");
                }
                return resources.readerData[h.iType][ResourcesData.IndexReader(h, 0) + i];
            }


            // Per pass output info
            public struct PassInputData
            {
                public ResourceHandle resource;
            }

            // Per pass input info
            public struct PassOutputData
            {
                public ResourceHandle resource;
            }

            // Data per pass
            public struct PassData
            {
                public bool hasSideEffects;
                public bool culled;
                public int tag; // Arbitrary per node int used by various passes
                public bool isSource;
                public bool isSink;
                public int firstInput;
                public int numInputs;
                public int firstOutput;
                public int numOutputs;
                public int passId;// Index of self in the passData list, can we calculate this somehow in c#?
                public string name;

                public string identifier
                {
                    get
                    {
                        return "pass_" + passId;
                    }
                }

                // Loop over this pass's outputs
                public DynamicArray<PassOutputData>.RangeEnumerable Outputs(CompilerContextData ctx)
                {
                    return ctx.outputData.SubRange(firstOutput, numOutputs);
                }

                // Loop over this pass's inputs
                public DynamicArray<PassInputData>.RangeEnumerable Inputs(CompilerContextData ctx)
                {
                    return ctx.inputData.SubRange(firstInput, numInputs);
                }

                // Helper to loop over nodes
                public struct InputNodeIterator
                {
                    CompilerContextData ctx;
                    int passId;
                    int input;

                    public InputNodeIterator(CompilerContextData ctx, int passId)
                    {
                        this.ctx = ctx;
                        this.passId = passId;
                        this.input = -1;
                    }

                    public int Current
                    {
                        get
                        {
                            return ctx.resources[ctx.inputData[ctx.passData[passId].firstInput + input].resource].writePass;
                        }
                    }

                    public bool MoveNext()
                    {
                        input++;
                        return input < ctx.passData[passId].numInputs;
                    }

                    public void Reset()
                    {
                        input = -1;
                    }

                    public InputNodeIterator GetEnumerator()
                    {
                        return this;
                    }
                }

                // Iterate over the passes (indexes into the pass array) that are connected to the inputs of this pass
                public InputNodeIterator InputNodes(CompilerContextData ctx)
                {
                    return new InputNodeIterator(ctx, passId);
                }

                public struct OutputNodeIterator
                {
                    CompilerContextData ctx;
                    int passId;
                    int output;
                    int outputUser;

                    public OutputNodeIterator(CompilerContextData ctx, int passId)
                    {
                        this.ctx = ctx;
                        this.passId = passId;
                        this.output = 0;
                        this.outputUser = -1;
                    }

                    public int Current
                    {
                        get
                        {
                            // Current output resource
                            var resHandle = ctx.outputData[ctx.passData[passId].firstOutput + output].resource;
                            // Select the outputUser for that resource
                            ResourceReaderData r = ctx.ResourceReader(resHandle, outputUser);
                            return r.passId;
                        }
                    }

                    public bool MoveNext()
                    {
                        ref var pass = ref ctx.passData[passId];

                        // Handle the empty list. Output == numOutputs == 0 so immediately return
                        if (output >= pass.numOutputs) return false;

                        // Move to the next user for the current output
                        outputUser++;

                        // Get number of users for the current output
                        ref ResourceData outputResource = ref ctx.resources[ctx.outputData[pass.firstOutput + output].resource];
                        int numUsers = outputResource.numReaders;

                        //We are past the user list. Go to the beginning of the reader list of the next output.
                        //If the next output has 0 users we skip to the next and so on untill there are no more
                        // outputs or one of them has non-zero users
                        while  (outputUser >= numUsers)
                        {
                            outputUser = 0;
                            output++;

                            if (output >= pass.numOutputs)
                            {
                                break;
                            }

                            // Update numUsers for this new list
                            ref ResourceData tmpRes = ref ctx.resources[ctx.outputData[pass.firstOutput + output].resource];
                            numUsers = tmpRes.numReaders;
                        }

                        // Beyond the last output > done
                        return output < pass.numOutputs && outputUser < numUsers;
                    }

                    public void Reset()
                    {
                        this.output = 0;
                        this.outputUser = -1;
                    }

                    public OutputNodeIterator GetEnumerator()
                    {
                        return this;
                    }
                }

                // Iterate over the passes (indexes into the pass array) that are connected to the outputs of this pass
                public OutputNodeIterator OutputNodes(CompilerContextData ctx)
                {
                    return new OutputNodeIterator(ctx, passId);
                }
            }

            // Flat arrays for speed
            public DynamicArray<PassData> passData;
            public DynamicArray<PassInputData> inputData; // Tightly packed lists
            public DynamicArray<PassOutputData> outputData;

            // Mark all passes as unvisited this is usefull for graph algorithms that do something with the tag
            public void TagAll(int value)
            {
                for (int passId = 0; passId < passData.size; passId++)
                {
                    passData[passId].tag = value;
                }
            }
        }

        // Used to tag distances from the source nodes to the sink nodes
        void TagDistance(CompilerContextData ctx, int node, int depth)
        {
            // We found a new longer path or the node was not visited  yet
            ref var pass = ref ctx.passData[node];
            if (pass.tag < depth)
            {
                pass.tag = depth;

                // Flood fill to children
                foreach (var n in pass.OutputNodes(ctx))
                {
                    TagDistance(ctx, n, depth + 1);
                }
            }
        }

        public void Compile(List<RenderGraphPass> passes, RenderGraphResourceRegistry resources)
        {
            CompilerContextData ctx = new CompilerContextData(passes.Count, resources);
            Stack<int> toVisit = new Stack<int>();

            // Build up the context graph and keep track of nodes we encounter that can't be culled
            for (int passId = 0; passId < passes.Count; passId++)
            {
                ref var pass = ref ctx.passData[passId];
                pass.hasSideEffects = !passes[passId].allowPassCulling;
                if (pass.hasSideEffects)
                {
                    toVisit.Push(passId);
                }
                pass.firstInput = ctx.inputData.size;
                pass.firstOutput = ctx.outputData.size;
                pass.passId = passId;
                pass.name = passes[passId].name;

                for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
                {
                    var resourceWrite = passes[passId].resourceWriteLists[type];
                    foreach (var resource in resourceWrite)
                    {
                        // Writing to an imported resource is a side effect
                        if (resources.IsRenderGraphResourceImported(resource))
                        {
                            if (pass.hasSideEffects == false)
                            {
                                pass.hasSideEffects = true;
                                toVisit.Push(passId);
                            }
                        }

                        // Mark this pass as writing to this version of the resource
                        ctx.resources[resource].AddWriter(passId,  pass.numOutputs);

                        ctx.outputData.Add(new CompilerContextData.PassOutputData
                        {
                            resource = resource
                        });
                        pass.numOutputs++;
                    }

                    var resourceRead = passes[passId].resourceReadLists[type];
                    foreach (var resource in resourceRead)
                    {
                        // Mark this pass as reading from this version of the resource
                        ctx.resources[resource].AddReader(ctx, resource, passId, pass.numInputs);

                        // Add an input
                        ctx.inputData.Add(new CompilerContextData.PassInputData
                        {
                            resource = resource
                        });

                        pass.numInputs++;
                    }
                }
            }

            // Source = input of the graph a starting point that takes no inputs itself : e.g. z-prepass
            // Sink = output of the graph an end point e.g. rendered frame Usually sinks will have to be
            // pinned or write to an external resource or the whole graph would get culled :-) 

            // Flood fill downstream to sources to all nodes starting from the pinned nodes
            ctx.TagAll(0);
            while (toVisit.Count != 0)
            {
                int passId = toVisit.Pop();

                // We already got to this node though another dependency chain
                if (ctx.passData[passId].tag != 0) continue;

                // Flow upstream from this node
                foreach (var inputPassIdx in ctx.passData[passId].InputNodes(ctx))
                {
                    toVisit.Push(inputPassIdx);
                }

                // Mark this node as visited
                ctx.passData[passId].tag = 1;
            }

            DynamicArray<int> sources = new DynamicArray<int>();
            DynamicArray<int> sinks = new DynamicArray<int>();

            // Update graph based on freshly culled nodes, remove any connections
            foreach (ref var pass in ctx.passData)
            {
                // mark any unvisited passes as culled
                pass.culled = (pass.tag == 0);

                // Remove the connections from the list so they won't be visited again
                if (pass.culled)
                {
                    foreach (ref var inputResource in ctx.passData[pass.passId].Inputs(ctx))
                    {
                        ctx.resources[inputResource.resource].RemoveReader(ctx, inputResource.resource, pass.passId);
                    }
                }

                // Find graph sinks
                // Sinks have side effects (or they would have been culled already) and have no nodes reading them.
                // The can still have outputs but these will be imported resources.
                if (pass.hasSideEffects)
                {
                    foreach (ref var outputResource in ctx.passData[pass.passId].Outputs(ctx))
                    {
                        if (ctx.resources[outputResource.resource].numReaders == 0)
                        {
                            pass.isSink = true;
                            sinks.Add(pass.passId);
                        }
                    }
                }

                // Find Graph sources.
                // Sources have no inputs that are written by another node.
                // They can still have inputs but these will be imported resources.
                bool isSource = true;
                foreach (ref var inputResource in ctx.passData[pass.passId].Inputs(ctx))
                {
                    if (ctx.resources[inputResource.resource].written)
                    {
                        isSource = false;
                        break;
                    }
                }
                if (isSource)
                {
                    pass.isSource = true;
                    sources.Add(pass.passId);
                }
            }

            //Determine distance from source to sink. The node with highest distance is the "last" node in the graph
            //there can be several sinks e.g. "final color" , "Vt output" some of the sinks may be "last"
            //say vt output happens but then some color passes still run meaning "final color" is the furthest
            ctx.TagAll(0);
            foreach (var sourceId in sources)
            {
                TagDistance(ctx, sourceId, 1);
            }

            m_GraphVisLogger = new RenderGraphLogger();
            m_GraphVisLogger.Initialize("GraphVis");
            LogGraphVis(ctx);

            // Output the thing
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
            }
        }

        RenderGraphLogger m_GraphVisLogger = new RenderGraphLogger();

        private void LogGraphVis(CompilerContextData ctx)
        {
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
            CompilerContextData.PassData? prev = null;

            // Create graph nodes for all passes
            foreach (ref var pass in ctx.passData)
            {
                // Gather lists of transients and first writes
                //var transientList = "";
                var firstWriteList = "";

                foreach (ref var res in pass.Inputs(ctx))
                {

                    var pointTo = ctx.resources[res.resource];
                    if (!pointTo.written) continue;

                    var name = pointTo.name;
                    firstWriteList += name + "\\l";
                }

                // Add a node for this pass
                var color = "white";
                if (pass.culled)
                {
                    color = "lightcoral";
                }
                else if (pass.hasSideEffects)
                {
                    color = "greenyellow";
                }
                var passSourceCodeUrl = "";//"vscode://file/" + info.pass.file.Replace('\\', '/') + ':' + info.pass.line;

                var label = pass.name;
                if (pass.isSink) label = label + "(Sink)";
                if (pass.isSource) label = label + "(Source)";

                //                 if (transientList.Length > 0)
                //                 {
                //                     label = label + "| Transients:\\n" + transientList;
                //                 }
                if (firstWriteList.Length > 0)
                {
                    label = label + "| Allocates:\\n" + firstWriteList;
                }

                label = label + "| Tag:\\n" + pass.tag;

                m_GraphVisLogger.LogLine("{0} [ label=\"{1}\" fillcolor=\"{2}\"  URL=\"{3}\" shape=record];", pass.identifier, label, color, passSourceCodeUrl);

                // Add a fake dependency edge to force the lay out of the graph to be linear
                // Higher weights will cause all the passes to be drawn more and more on a single straight line
                // be more difficult to read as the data depencency arrows may become very tangled up
                // It's usually better to let it us a bit more vertical space to improve the readability
                // even if the "linear timeline" view gets a bit lost.
                if (linearize && prev != null)
                {
                    m_GraphVisLogger.LogLine("{0} -> {1} [ weight=20 style=dotted ];", prev.Value.identifier, pass.identifier);
                }
                prev = pass;
            }
            m_GraphVisLogger.LogLine("}}");

            // Loop over all the resources accessed by this node and add links (and possibly dummy nodes if the resources are imported etc)
            foreach (ref var pass in ctx.passData)
            {
                // Resources read by this node => point to either a previous node that has written it
                // or creates a dummy node if this is the first node to read an imported, shared, or texture created outside of the pass.
                foreach (ref var res in pass.Inputs(ctx))
                {
                    ref var resource = ref ctx.resources[res.resource];
                    var pointToId = resource.writePass;
                    var pointToIdentifier = ctx.passData[pointToId].identifier;
                    var name = resource.name + " v" + res.resource.version;
                    // Create a dummy source node if no previous pass was found writing this.
                    // This usually means the resource is imported
                    if (!resource.written)
                    {
                        var dummyColor = "red"; // sort of an error to see this, it means the below logic doesn't catch it iall
                        var dummyShape = "cylinder";
                        var dummyName = name;
                        if (resource.isImported)
                        {
                            dummyColor = "turquoise";
                            dummyName = "Import: " + dummyName;
                        }
                        else if (resource.isShared)
                        {
                            dummyColor = "thistle";
                            dummyName = "Shared: " + dummyName;
                        }
                        pointToIdentifier = "resource_" + res.resource.iType + "_" + res.resource.index;
                        m_GraphVisLogger.LogLine("{0} [ label=\"{1}\" fillcolor=\"{2}\" shape=\"{3}\"];", pointToIdentifier, dummyName, dummyColor, dummyShape);
                    }
                    var toolTip = name + ": ";
                    if (resource.written)
                    {
                        toolTip += "Written by: " + ctx.passData[pointToId].name + ", ";
                    }
                    toolTip += "Read by: " + pass.name;

                    string constr = "weight=" + ((resource.written) ? 1 : 10);
                    m_GraphVisLogger.LogLine("{0} -> {1} [ label=\"{2}\" color=\"{3}\" tooltip=\"{4}\" edgetooltip=\"{4}\" labeltooltip=\"{4}\" {5}];", pointToIdentifier, pass.identifier, name, colorList[0], toolTip, constr);
                }
            }
            m_GraphVisLogger.LogLine("}}\n");
        }

    }
}
