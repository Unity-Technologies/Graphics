using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule.NativeRenderPassCompiler;
using UnityEngine.Rendering;
using UnityEngine.UIElements;


internal class RenderGraphWindow : EditorWindow
{
    internal static RenderGraphView OpenGraphVisualizer()
    {
        var window = GetWindow<RenderGraphWindow>();
        window.titleContent = new GUIContent("Render Graph Visualizer");

        return window.GetView();
    }

    private static RenderGraphView _view;
    private static RenderGraphResourceView _resourceWindow;

    internal struct ResourceDebugData
    {
        internal int width;
        internal int height;
        internal int samples;
        internal bool clearBuffer;
        internal bool isImported;
        internal bool bindMS;
        internal bool isMemoryless;
        internal GraphicsFormat format;
    }

    internal struct PassDebugData
    {
        internal int tag;
        internal List<String> allocations;
        internal List<String> releases;
        internal List<String> lastWrites;
        internal string syncList;
        internal string nativeRPInfo;
        internal int width;
        internal int height;
        internal int samples;
        internal bool hasDepth;
        internal bool asyncCompute;
        internal bool isCulled;
    }

    private void OnCaptureClicked()
    {
        RenderGraphDebugger.instance.captureNextGraph = true;
    }

    private RenderGraphView GetView()
    {
        // Free the old view if there is one
        if (_view != null)
        {
            _view.Reset();
            rootVisualElement.Clear();
            _view = null;
        }

        // Generate the new view
        if (_view == null)
        {
            var resources = new RenderGraphResourceView();

            _view = new RenderGraphView
            {
                name = "Render Graph",
                m_ResourceWindow = resources,
            };
            //_view.StretchToParentSize();
            _view.Stop();
            //rootVisualElement.Add(_view);

            resources.style.width = 650;
            resources.style.height = 620;
            resources.style.backgroundColor = new StyleColor(Color.grey);
            //rootVisualElement.Add(resources);

            var s = new StickyNote(new Vector2(30, 30));
            s.contents = "";
            s.fontSize = StickyNoteFontSize.Small;
            s.visible = false;
           //rootVisualElement.Add(s);
            _view.m_MergeInfo = s;

            // Toolbar with capture buttons
            var captureToolbar = new VisualElement();
            captureToolbar.style.flexDirection = FlexDirection.Row;

            var captureButton = new Button(OnCaptureClicked);
            captureButton.text = "Capture Next Graph Execution";
            captureToolbar.Add(captureButton);

            // The graph vis
            TwoPaneSplitView graphAndResourcesView = new TwoPaneSplitView(1, 64.0f, TwoPaneSplitViewOrientation.Vertical);
            graphAndResourcesView.Add(_view);
            graphAndResourcesView.Add(resources);
            graphAndResourcesView.style.flexGrow = 1;

            // The root element, toolbar with content below
            var rootElement = new VisualElement();
            rootElement.style.flexDirection = FlexDirection.Column;
            rootElement.style.flexGrow = 1;
            rootElement.Add(captureToolbar);
            rootElement.Add(graphAndResourcesView);
            rootVisualElement.Add(rootElement);
        }

        return _view;
    }

    private void OnDisable()
    {
        _view?.Stop();
        _view = null;
    }
}

internal class RenderGraphView : GraphView
{
    internal StickyNote m_MergeInfo;
    internal RenderGraphEdge m_SelectedEdge;

    internal RenderGraphView()
    {
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        this.AddManipulator(new ContentZoomer());
    }

    internal void SetMergeInfo(string mergeInfo)
    {
        if (mergeInfo == "")
        {
            m_MergeInfo.visible = false;
        }
        else
        {
            m_MergeInfo.visible = true;
            m_MergeInfo.contents = mergeInfo;
        }
    }

    internal void SelectEdge(RenderGraphEdge edge)
    {
        m_MergeInfo.visible = true;
        m_MergeInfo.contents = edge.tooltip;
        m_SelectedEdge = edge;
    }

    internal void UnSelectEdge(RenderGraphEdge edge)
    {
        if (m_SelectedEdge == edge)
        {
            m_MergeInfo.visible = false;
            m_MergeInfo.contents = "";
            m_SelectedEdge = null;
        }
    }

    internal void Stop()
    {
        m_Passes.Clear();
    }

    internal void Reset()
    {
        m_Passes.Clear();
        m_Resources.Clear();
        Clear();

        m_ResourceWindow?.Reset();
    }

    public RenderGraphResourceView m_ResourceWindow;
    private List<RenderGraphNode> m_Passes = new List<RenderGraphNode>();
    private List<ResourceNode> m_Resources = new List<ResourceNode>();

    internal void SelectNode(ResourceNode node, bool clearCurrentSelection)
    {
        if (clearCurrentSelection)
            ClearSelection();

        if (m_Resources.Contains(node))
        {
            foreach (var pass in m_Passes)
            {
                if (pass.m_Reads.Find(graphNode => graphNode.title == node.title) != null ||
                    pass.m_Writes.Find(graphNode => graphNode.title == node.title) != null)
                {
                    AddToSelection(pass);
                }
            }
        }
    }

    // We're not allowed to use Linq anymore but our ui frameworks return IEnumerable so we have to roll our own.
    private class LinqButNotLinq {

        public static bool Any<T>(IEnumerable<T> yey)
        {
            using var enumerator = yey.GetEnumerator();
            if (enumerator.MoveNext())
            {
                return true;
            }

            return false;
        }

        public static T First<T>(IEnumerable<T> yey)
        {
            using var enumerator = yey.GetEnumerator();
            if (enumerator.MoveNext())
            {
                return enumerator.Current;
            }

            throw new InvalidOperationException("Sequence is empty");
        }

        public static T Last<T>(List<T> yey)
        {
            if ( yey.Count == 0 )
                throw new InvalidOperationException("Sequence is empty");

            return yey[yey.Count - 1];
        }
    }

    internal void AddNRP(string nrpString)
    {
        if (nrpString.IndexOf('|') == -1)
            return;

        string[] passNames = nrpString.Split('|');
        List<RenderGraphNode> nrpPasses = new List<RenderGraphNode>();
        foreach (var name in passNames)
        {
            nrpPasses.Add(m_Passes.Find(graphNode => graphNode.m_ID == name));
        }

        var nrp = new NRPNode();
        nrp.m_MergedPasses = nrpPasses;
        nrp.m_EndTag = LinqButNotLinq.Last(nrpPasses).tag;
        nrp.m_NRPState = PassMergeState.Begin;
        nrp.m_NativeRPInfo = nrpPasses[0].m_NativeRPInfo;
        nrp.tag = nrpPasses[0].tag;
        nrp.mainContainer.style.backgroundColor = new StyleColor(new Color(0.1f,0.1f,0.2f));

        ScrollView scroll = new ScrollView(ScrollViewMode.Horizontal);

        foreach (var pass in nrpPasses)
        {
            nrp.title += pass.title + "  ";
            m_Passes.Remove(pass);
            MoveConnections(pass, nrp, nrp.m_MergedPasses);
        }

        for (int i = 0; i < nrp.m_InputPort.Count; ++i)
        {
            if (LinqButNotLinq.Any(nrp.m_InputPort[i].connections) == false)
            {
                nrp.inputContainer.Remove(nrp.m_InputPort[i]);
                nrp.m_InputPort.RemoveAt(i);
            }
        }

        for (int i = 0; i < nrp.m_OutputPort.Count; ++i)
        {
            if (LinqButNotLinq.Any(nrp.m_OutputPort[i].connections) == false)
            {
                nrp.outputContainer.Remove(nrp.m_OutputPort[i]);
                nrp.m_OutputPort.RemoveAt(i);
            }
        }

        m_Passes.Insert(GetInsertIndex(nrp.tag), nrp);
    }

    void MoveConnections(RenderGraphNode origin, RenderGraphNode destination, List<RenderGraphNode> mergedNodes)
    {
        foreach (var input in origin.m_Inputs)
        {
            if(input != destination && !mergedNodes.Contains(input))
                destination.m_Inputs.Add(input);
        }
        foreach (var output in origin.m_Outputs)
        {
            if(output != destination && !mergedNodes.Contains(output))
                destination.m_Outputs.Add(output);
        }

        destination.m_Allocations.AddRange(origin.m_Allocations);
        destination.m_Releases.AddRange(origin.m_Releases);
        destination.m_Writes.AddRange(origin.m_Writes);
        destination.m_Reads.AddRange(origin.m_Reads);

        foreach (var port in origin.m_InputPort)
        {
            Port destPort = destination.GetPort(Direction.Input, port.name);
            if (destPort == null)
            {
                destPort = destination.AddPort(Direction.Input, port.name);
            }

            foreach (var edge in port.connections)
            {
                edge.input = destPort;
                var prevNode = edge.output.node as RenderGraphNode;
                if (mergedNodes.Contains(prevNode))
                {
                    RemoveElement(edge);
                }
                else
                {
                    if (prevNode != destination)
                    {
                        destPort.Connect(edge);
                        prevNode.m_Outputs.Remove(origin);
                        prevNode.m_Outputs.Add(destination);
                    }
                }
            }

            port.DisconnectAll();
        }

        foreach (var port in origin.m_OutputPort)
        {
            var destPort = destination.GetPort(Direction.Output, port.name);
            if (destPort == null)
            {
                destPort = destination.AddPort(Direction.Output, port.name);
            }

            foreach (var edge in port.connections)
            {
                edge.output = destPort;
                var pointToNode = edge.input.node as RenderGraphNode;
                if (mergedNodes.Contains(pointToNode))
                {
                    RemoveElement(edge);
                }
                else
                {
                    if (pointToNode != destination)
                    {
                        destPort.Connect(edge);
                        pointToNode.m_Inputs.Remove(origin);
                        pointToNode.m_Inputs.Add(destination);
                    }
                }
            }

            port.DisconnectAll();
        }

    }

    internal void AddPass(string passID, string passName, RenderGraphWindow.PassDebugData data)
    {
        List<ResourceNode> allocationNodes = new List<ResourceNode>();
        string allocationString = "";
        foreach (var resourceName in data.allocations)
        {
            ResourceNode n = m_Resources.Find(graphNode => graphNode.title == resourceName);

            allocationNodes.Add(n);
            allocationString += resourceName + '\n';
        }

        List<ResourceNode> releaseNodes = new List<ResourceNode>();
        string releaseString = "";
        foreach (var resourceName in data.releases)
        {
            ResourceNode n = m_Resources.Find(graphNode => graphNode.title == resourceName);

            releaseNodes.Add(n);
            releaseString += resourceName + '\n';
        }

        List<ResourceNode> lastWrieNodes = new List<ResourceNode>();
        string lastWriteString = "";
        foreach (var resourceName in data.lastWrites)
        {
            ResourceNode n = m_Resources.Find(graphNode => graphNode.title == resourceName);

            lastWrieNodes.Add(n);
            lastWriteString += resourceName + '\n';
        }

        var node = new RenderGraphNode
        {
            title = passName,
            tag = data.tag,
            m_ID = passID,
            m_NativeRPInfo = data.nativeRPInfo,
            m_Allocations = allocationNodes,
            m_Releases = releaseNodes,
            m_ReleaseString = releaseString,
            m_AllocString = allocationString,
            m_LastWriteString = lastWriteString,
            m_HasDepth = data.hasDepth,
            m_AsyncCompute = data.asyncCompute,
            m_IsCulled = data.isCulled,
            m_Width = data.width,
            m_Height = data.height,
            m_Samples = data.samples,
            m_SyncList = data.syncList,
            view = this
        };

        node.mainContainer.style.backgroundColor = new StyleColor(Color.black);
        m_Passes.Insert(GetInsertIndex(data.tag), node);
    }

    internal void AddConnection(string from, string to, string resourceName,
        string resourceNameNoVersion, string mergeMessage, RenderGraphDebugger.InputUsageType use)
    {
        var fromNode = m_Passes.Find(node => node.m_ID == from);
        var toNode = m_Passes.Find(node => node.m_ID == to);

        ResourceNode resourceNode = m_Resources.Find(node => node.title == resourceNameNoVersion);

        if (resourceNode != null)
        {
            if (fromNode != null && !fromNode.m_Writes.Contains(resourceNode))
            {
                fromNode.m_Writes.Add(resourceNode);
                fromNode.m_WriteString += $"{resourceName} ({use})\n";
            }

            if (toNode != null && !toNode.m_Reads.Contains(resourceNode))
            {
                toNode.m_Reads.Add(resourceNode);
                toNode.m_ReadString += $"{resourceName} ({use})\n";
            }
        }

        if (fromNode == null || toNode == null)
        {
            return;
        }

        //find if ports for this resource already exist, if not create ports
        Port fromPort = fromNode.GetPort(Direction.Output, resourceNameNoVersion);
        if (fromPort == null)
        {
            fromPort = fromNode.AddPort(Direction.Output, resourceNameNoVersion);
        }

        Port toPort = toNode.GetPort(Direction.Input, resourceNameNoVersion);
        if (toPort == null)
        {
            toPort = toNode.AddPort(Direction.Input, resourceNameNoVersion);
        }

        //connect ports
        var edge = new RenderGraphEdge();
        edge.view = this;
        edge.tooltip = mergeMessage;
        edge.input = toPort;
        edge.output = fromPort;
        toPort.Connect(edge);
        fromPort.Connect(edge);

        //save input/output connections to make sorting the view easier
        if (!fromNode.m_Outputs.Contains(toNode))
        {
            fromNode.m_Outputs.Add(toNode);
        }

        if (!toNode.m_Inputs.Contains(toNode))
        {
            toNode.m_Inputs.Add(fromNode);
        }

        AddElement(edge);
    }

    internal ResourceNode AddResource(string resourceID, string label, RenderGraphWindow.ResourceDebugData data)
    {
        if (m_Resources.Find(node => node.title == label) != null)
            return null;

        var node = new ResourceNode
        {
            m_ID = resourceID,
            title = label,
            m_IsImported = data.isImported,
            m_Width = data.width,
            m_Height = data.height,
            m_Samples = data.samples,
            m_Clear = data.clearBuffer,
            m_Format = data.format,
            m_BindMS = data.bindMS,
            m_IsMemoryless = data.isMemoryless,
            tag = -1,
            view = this
        };

        m_Resources.Add(node);
        return node;
    }


    private void AddGraphNodeFoldout(VisualElement root, RenderGraphNode node)
    {
        // Generate the string
        string extra = String.Empty;

        extra += $"Attachment Dimensions: {node.m_Width}x{node.m_Height}x{node.m_Samples}\n";
        extra += $"Depth: {node.m_HasDepth} Async Compute: {node.m_AsyncCompute}\n";

        if (node.m_Reads.Count > 0)
        {
            extra += "\nReads: \n";
            extra += node.m_ReadString;
        }

        if (node.m_Writes.Count > 0)
        {
            extra += "\nWrites: \n";
            extra += node.m_WriteString;
        }

        if (node.m_AllocString.Length > 0)
        {
            extra += "\nAllocations: \n";
            extra += node.m_AllocString;
        }

        if (node.m_LastWriteString.Length > 0)
        {
            extra += "\nLast Writes: \n";
            extra += node.m_LastWriteString;
        }

        if (node.m_ReleaseString.Length > 0)
        {
            extra += "\nReleases: \n";
            extra += node.m_ReleaseString;
        }

        if (node.m_SyncList.Length > 0)
        {
            extra += "\nSyncs: \n";
            extra += node.m_SyncList;
        }

        // Text box with above text
        var text = new TextElement();
        text.text = extra;
        text.style.overflow = Overflow.Visible;
        text.style.textOverflow = TextOverflow.Clip;
        text.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 0.8f));
        text.style.marginLeft = 3;

        // Foldout
        Foldout fo = new Foldout();
        fo.text = node.title + "(Render Graph Pass)";
        fo.value = false;
        fo.contentContainer.Add(text);

        root.Add(fo);
    }

    private void AddNativePassNodeFoldout(VisualElement root, RenderGraphNode node)
    {
        // Generate the string
        string extra = String.Empty;

        if (!string.IsNullOrEmpty(node.m_NativeRPInfo))
        {
            extra += node.m_NativeRPInfo;
        }

        // Text box with above text
        var text = new TextElement();
        text.text = extra;
        text.style.overflow = Overflow.Visible;
        text.style.textOverflow = TextOverflow.Clip;
        text.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 0.8f));
        text.style.marginLeft = 3;

        // Foldout
        Foldout fo = new Foldout();
        fo.text = node.title + "(Native Pass)";
        fo.value = false;
        fo.contentContainer.Add(text);

        root.Add(fo);
    }

    internal void UpdateNodeVisualisation()
    {
        int currentTag = -1;
        float x = 0, y = 0;
        float distanceBetweenTags = 540f;

        float maxY = 0;

        ReOrder();

        m_ResourceWindow.DrawResources(m_Resources, m_Passes, this);

        float resX = 0, resY = -90f;
        foreach (var node in m_Resources)
        {
            node.SetPosition(new Rect(resX, resY, 200, 150));
            node.RefreshExpandedState();
            this.AddElement(node);

            resX += distanceBetweenTags;
        }

        foreach (var pass in m_Passes)
        {
            if (pass.m_IsCulled)
            {
                pass.title = pass.title + "(CULLED)";
                pass.mainContainer.style.backgroundColor = new StyleColor(new Color(0.4f,0.1f,0.1f));
            }

            if (pass.tag != currentTag)
            {
                currentTag = pass.tag;
                y = 0;
                x += distanceBetweenTags;
            }

            bool nodeIsWide = pass.m_Inputs.Count > 0 || pass.m_Outputs.Count > 0;
            pass.SetPosition(new Rect(x, y, 230 + (nodeIsWide ? 320 : 0), 150));
            this.AddElement(pass);

            if (pass is NRPNode nrp)
            {
                foreach (var subPass in nrp.m_MergedPasses)
                {
                    AddGraphNodeFoldout(nrp.extensionContainer, subPass);
                }

                AddNativePassNodeFoldout(nrp.extensionContainer, nrp);

                nrp.expanded = true;
                nrp.RefreshExpandedState();
            }
            else
            {
                AddGraphNodeFoldout(pass.extensionContainer, pass);
                if (!string.IsNullOrEmpty(pass.m_NativeRPInfo))
                {
                    AddNativePassNodeFoldout(pass.extensionContainer, pass);
                }

                pass.expanded = true;
                pass.RefreshExpandedState();
            }
            if (currentTag == pass.tag)
            {
                y += distanceBetweenTags / 3f;
                maxY = Mathf.Max(y, maxY);
            }

        }

        foreach (var resource in m_Resources)
        {
            Color color;
            if (resource.m_IsImported)
            {
                color = Color.blue;
            }
            else
            {
                color = Color.white;
            }

            LinqButNotLinq.First(resource.Children()).style.borderBottomColor = color;
            LinqButNotLinq.First(resource.Children()).style.borderLeftColor = color;
            LinqButNotLinq.First(resource.Children()).style.borderTopColor = color;
            LinqButNotLinq.First(resource.Children()).style.borderRightColor = color;
        }
    }

    string CollectInfoString(RenderGraphNode pass, ref float nodeInfoLength)
    {
        string extra = String.Empty;
        bool hasInfo = false;

        if (pass.m_Width > 0)
        {
            extra = pass.m_Width + "x" + pass.m_Height + "\n \n";
            nodeInfoLength += pass.m_Writes.Count * 15.0f;
            hasInfo = true;
        }

        if (pass.m_Reads.Count > 0)
        {
            extra += "Reads: \n";
            extra += pass.m_ReadString;
            nodeInfoLength += pass.m_Writes.Count * 15.0f + 10.0f;
        }

        if (pass.m_Writes.Count > 0)
        {
            extra += "\nWrites: \n";
            extra += pass.m_WriteString;
            nodeInfoLength += pass.m_Writes.Count * 15.0f + 10.0f;
        }

        if (pass.m_LastWriteString.Length > 0)
        {
            extra += "\nLast Writes: \n";
            extra += pass.m_LastWriteString;
            nodeInfoLength += pass.m_Writes.Count * 15.0f + 10.0f;
        }

        if (pass.m_AllocString.Length > 0)
        {
            extra += "\nAllocations: \n";
            extra += pass.m_AllocString;
            nodeInfoLength += pass.m_Writes.Count * 15.0f + 10.0f;
        }

        if (pass.m_ReleaseString.Length > 0)
        {
            extra += "\nReleases: \n";
            extra += pass.m_ReleaseString;
            nodeInfoLength += pass.m_Writes.Count * 15.0f + 10.0f;
        }

        if (pass.m_SyncList.Length > 0)
        {
            extra += "\nSyncs: \n";
            extra += pass.m_SyncList;
            nodeInfoLength += 30.0f;
        }

        if (pass.m_NativeRPInfo != String.Empty)
        {
            extra += pass.m_NativeRPInfo;
            nodeInfoLength += pass.m_Writes.Count * 15.0f + 10.0f;
        }

        if (hasInfo)
        {
            extra += "\n Depth enabled: " + pass.m_HasDepth + '\n';
            extra += "\n Async Compute: " + pass.m_AsyncCompute + '\n';
            extra += pass.m_Info;

            nodeInfoLength += 150.0f;
        }

        return extra;
    }



    //Sorting heuristic
    void ReOrder()
    {
        SquashTags();

        List<RenderGraphNode> m_UnboundNodes = new List<RenderGraphNode>();
        foreach (var node in m_Passes)
        {
            if (node.m_Inputs.Count == 0 && node.m_Outputs.Count == 0)
            {
                m_UnboundNodes.Add(node);
            }
        }

        foreach (var node in m_UnboundNodes)
        {
            node.tag = -1;
            m_Passes.Remove(node);
            m_Passes.Insert(GetInsertIndex(-1), node);
        }
    }

    //make graph more compact where possible
    void SquashTags()
    {
        //this dictionary points to the first pass in the graph that it its connected to
        Dictionary<RenderGraphNode, int> minimumTags = new Dictionary<RenderGraphNode, int>();

        foreach (var pass in m_Passes)
        {
            if (pass.m_Outputs.Count == 0)
                CheckMinimumTag(pass, ref minimumTags);
        }

        foreach (var pass in minimumTags)
        {
            if (pass.Key.tag != pass.Value)
            {
                pass.Key.tag = pass.Value;
                m_Passes.Remove(pass.Key);
                m_Passes.Insert(GetInsertIndex(pass.Value), pass.Key);
            }
        }
    }

    int CheckMinimumTag(RenderGraphNode pass, ref Dictionary<RenderGraphNode, int> minimumTags)
    {
        int minimumTag = 1;

        foreach (var inputs in pass.m_Inputs)
        {
            if (!m_Passes.Contains(inputs))
            {
                Debug.Log("oeps");
            }
            int possibleTag = CheckMinimumTag(inputs, ref minimumTags) + 1;
            minimumTag = Math.Max(possibleTag, minimumTag);
        }

        minimumTags[pass] = minimumTag;
        return minimumTag;
    }

    int GetInsertIndex(int tag)
    {
        int idx = m_Passes.Count - 1;
        while (idx >= 0)
        {
            if (m_Passes[idx].tag <= tag)
            {
                return idx + 1;
            }

            --idx;
        }

        return 0;
    }
}

internal class RenderGraphNode : Node
{
    public int tag;
    public string m_ID;

    public List<Port> m_OutputPort = new List<Port>();
    public List<Port> m_InputPort = new List<Port>();
    public PassMergeState m_NRPState = PassMergeState.End;
    public bool m_HasDepth;
    public bool m_AsyncCompute;
    public bool m_IsCulled;
    public int m_Width, m_Height, m_Samples;

    public string m_Info = String.Empty;
    public string m_NativeRPInfo = String.Empty;

    public List<ResourceNode> m_Allocations = new List<ResourceNode>();
    public List<ResourceNode> m_Releases = new List<ResourceNode>();
    public List<ResourceNode> m_Reads = new List<ResourceNode>();
    public List<ResourceNode> m_Writes = new List<ResourceNode>();
    public List<RenderGraphNode> m_Inputs = new List<RenderGraphNode>();
    public List<RenderGraphNode> m_Outputs = new List<RenderGraphNode>();

    public string m_SyncList = "";
    public string m_ReadString = "";
    public string m_WriteString = "";
    public string m_AllocString = "";
    public string m_LastWriteString = "";
    public string m_ReleaseString = "";

    public TextElement m_InfoElement;

    public RenderGraphView view;

    public override void OnSelected()
    {
        view?.m_ResourceWindow.SelectionChanged();
        this.BringToFront();
    }

    public Port GetPort(Direction direction, string connectionName)
    {
        if (direction == Direction.Input)
        {
            foreach (var port in m_InputPort)
            {
                if (port.name == connectionName)
                    return port;
            }
        }
        else
        {
            foreach (var port in m_OutputPort)
            {
                if (port.name == connectionName)
                    return port;
            }
        }

        return null;
    }

    public Port AddPort(Direction portDirection, string connectionName,
        Port.Capacity capacity = Port.Capacity.Multi)
    {
        var n = InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(string));
        n.name = connectionName;
        n.portName = connectionName;
        if (portDirection == Direction.Output)
        {
            outputContainer.Add(n);
            m_OutputPort.Add(n);
        }
        else
        {
            inputContainer.Add(n);
            m_InputPort.Add(n);
        }

        return n;
    }
}

internal class NRPNode : RenderGraphNode
{
    public int m_EndTag;

    public List<RenderGraphNode> m_MergedPasses = new List<RenderGraphNode>();
}

internal class ResourceNode : Node
{
    public int tag;
    public string m_ID;

    public int m_Width, m_Height, m_Samples;
    public bool m_IsImported;
    public bool m_IsMemoryless;
    public bool m_Clear;
    public bool m_BindMS;
    public GraphicsFormat m_Format;

    public RenderGraphView view;
    public RenderGraphResourceView.CellElement m_Cell;

    public override void OnSelected()
    {
        view.m_ResourceWindow.SelectionChanged();
        view.SelectNode(this, false);
        view.m_ResourceWindow.SelectCell(this);
    }
}

internal class RenderGraphEdge : Edge
{
    public RenderGraphView view;

    public override void OnSelected()
    {
        view.SelectEdge(this);
    }

    public override void OnUnselected()
    {
        view.UnSelectEdge(this);
    }
}
#endif
