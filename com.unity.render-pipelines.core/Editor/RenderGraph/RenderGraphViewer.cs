using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using System.Collections.Generic;

public class RenderGraphViewer : EditorWindow
{
    static class Style
    {
        public static readonly GUIContent title = EditorGUIUtility.TrTextContent("Render Graph Viewer");
    }

    const float kRenderPassWidth = 20.0f;
    const float kResourceHeight = 15.0f;

    class CellElement : VisualElement
    {
        public CellElement(int idxStart, int idxEnd)
        {
            style.borderBottomLeftRadius = style.borderTopLeftRadius = style.borderBottomRightRadius = style.borderTopRightRadius = 5;
            style.borderBottomWidth = style.borderTopWidth = style.borderLeftWidth = style.borderRightWidth = 1f;
            style.borderBottomColor = style.borderTopColor = style.borderLeftColor = style.borderRightColor = new Color(0f, 0f, 0f, 1f);
            style.backgroundColor = (Color)new Color32(88, 88, 88, 255);
            style.height = kResourceHeight;
            style.left = idxStart * kRenderPassWidth;
            style.width = (idxEnd - idxStart + 1) * kRenderPassWidth;
        }

        public void SetColor(StyleColor color)
        {
            style.backgroundColor = color;
        }
    }

    [MenuItem("Window/Render Pipeline/Render Graph Viewer", false, 10006)]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        var window = GetWindow<RenderGraphViewer>();
        window.titleContent = new GUIContent("Render Graph Viewer");
    }

    [System.Flags]
    enum Filter
    {
        ImportedResources = 1 << 0,
        CulledPasses = 1 << 1,
        Textures = 1 << 2,
        ComputeBuffers = 1 << 3,
    }

    struct ResourceElementInfo
    {
        public VisualElement lifetime;
        public VisualElement resourceLabel;

        public void Reset()
        {
            lifetime = null;
            resourceLabel = null;
        }
    }

    struct PassElementInfo
    {
        public VisualElement pass;
        public int remap;

        public void Reset()
        {
            pass = null;
            remap = -1;
        }
    }

    RenderGraph m_CurrentRenderGraph;

    VisualElement m_Root;
    VisualElement m_HeaderElement;
    VisualElement m_GraphViewerElement;

    readonly StyleColor m_ResourceColorRead = new StyleColor(new Color(0.2f, 1.0f, 0.2f));
    readonly StyleColor m_ResourceColorWrite = new StyleColor(new Color(1.0f, 0.2f, 0.2f));
    readonly StyleColor m_ImportedResourceColor = new StyleColor(new Color(0.3f, 0.75f, 0.75f));
    readonly StyleColor m_CulledPassColor = new StyleColor(Color.black);
    readonly StyleColor m_ResourceHighlightColor = new StyleColor(Color.white);
    readonly StyleColor m_ResourceLifeHighLightColor = new StyleColor(new Color32(103, 103, 103, 255));
    StyleColor m_OriginalResourceLifeColor;
    StyleColor m_OriginalPassColor;
    StyleColor m_OriginalResourceColor;

    DynamicArray<ResourceElementInfo>[] m_ResourceElementsInfo = new DynamicArray<ResourceElementInfo>[(int)RenderGraphResourceType.Count];
    DynamicArray<PassElementInfo> m_PassElementsInfo = new DynamicArray<PassElementInfo>();

    Filter m_Filter = Filter.Textures | Filter.ComputeBuffers;

    void RenderPassLabelChanged(GeometryChangedEvent evt)
    {
        var label = evt.currentTarget as Label;
        Vector2 textSize = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined, 10, VisualElement.MeasureMode.Undefined);
        float textWidth = Mathf.Max(kRenderPassWidth, textSize.x);
        float desiredHeight = Mathf.Sqrt(textWidth * textWidth - kRenderPassWidth * kRenderPassWidth);
        // Should be able to do that and rely on the parent layout but for some reason flex-end does not work so I set the parent's parent height instead.
        //label.parent.style.height = desiredHeight;
        var passNamesContainerHeight = Mathf.Max(label.parent.parent.style.height.value.value, desiredHeight);
        label.parent.parent.style.height = passNamesContainerHeight;
        label.parent.parent.style.minHeight = passNamesContainerHeight;

        var topRowElement = m_GraphViewerElement.Q<VisualElement>("GraphViewer.TopRowElement");
        topRowElement.style.minHeight = passNamesContainerHeight;

    }

    void LastRenderPassLabelChanged(GeometryChangedEvent evt)
    {
        var label = evt.currentTarget as Label;
        Vector2 textSize = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined, 10, VisualElement.MeasureMode.Undefined);
        float textWidth = Mathf.Max(kRenderPassWidth, textSize.x);

        // Keep a margin on the right of the container to avoid label being clipped.
        var viewerContainer = m_GraphViewerElement.Q<VisualElement>("GraphViewer.ViewerContainer");
        viewerContainer.style.marginRight = Mathf.Max(viewerContainer.style.marginRight.value.value, (textWidth - kRenderPassWidth));
    }

    void UpdateResourceLifetimeColor(int passIndex, StyleColor colorRead, StyleColor colorWrite)
    {
        var debugData = m_CurrentRenderGraph.GetDebugData();
        var pass = debugData.passList[passIndex];

        if (pass.culled)
            return;

        for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
        {
            foreach (int resourceRead in pass.resourceReadLists[type])
            {
                CellElement resourceLifetime = m_ResourceElementsInfo[type][resourceRead].lifetime as CellElement;
                if (resourceLifetime != null)
                    resourceLifetime.SetColor(colorRead);
            }

            foreach (int resourceWrite in pass.resourceWriteLists[type])
            {
                CellElement resourceLifetime = m_ResourceElementsInfo[type][resourceWrite].lifetime as CellElement;
                if (resourceLifetime != null)
                    resourceLifetime.SetColor(colorWrite);
            }
        }
    }

    void MouseEnterPassCallback(MouseEnterEvent evt, int index)
    {
        UpdateResourceLifetimeColor(index, m_ResourceColorRead, m_ResourceColorWrite);
    }

    void MouseLeavePassCallback(MouseLeaveEvent evt, int index)
    {
        UpdateResourceLifetimeColor(index, m_OriginalResourceLifeColor, m_OriginalResourceLifeColor);
    }

    void UpdatePassColor((int index, int resourceType) resInfo, StyleColor colorRead, StyleColor colorWrite)
    {
        var debugData = m_CurrentRenderGraph.GetDebugData();
        var resource = debugData.resourceLists[resInfo.resourceType][resInfo.index];

        foreach (int consumer in resource.consumerList)
        {
            var passDebugData = debugData.passList[consumer];
            if (passDebugData.culled)
                continue;

            VisualElement passElement = m_PassElementsInfo[consumer].pass;
            if (passElement != null)
            {
                VisualElement passButton = passElement.Q("RenderPass.Cell");
                passButton.style.backgroundColor = colorRead;
            }
        }

        foreach (int producer in resource.producerList)
        {
            var passDebugData = debugData.passList[producer];
            if (passDebugData.culled)
                continue;

            VisualElement passElement = m_PassElementsInfo[producer].pass;
            if (passElement != null)
            {
                VisualElement passButton = passElement.Q("RenderPass.Cell");
                passButton.style.backgroundColor = colorWrite;
            }
        }
    }

    void UpdateResourceLabelColor((int index, int resourceType) resInfo, StyleColor color)
    {
        var label = m_ResourceElementsInfo[resInfo.resourceType][resInfo.index].resourceLabel;
        if (label != null)
        {
            label.style.color = color;
        }
    }

    void MouseEnterResourceCallback(MouseEnterEvent evt, (int index, int resourceType) info)
    {
        CellElement resourceLifetime = m_ResourceElementsInfo[info.resourceType][info.index].lifetime as CellElement;
        resourceLifetime.SetColor(m_ResourceLifeHighLightColor);

        UpdatePassColor(info, m_ResourceColorRead, m_ResourceColorWrite);
        UpdateResourceLabelColor(info, m_ResourceHighlightColor);
    }

    void MouseLeaveResourceCallback(MouseLeaveEvent evt, (int index, int resourceType) info)
    {
        CellElement resourceLifetime = m_ResourceElementsInfo[info.resourceType][info.index].lifetime as CellElement;
        resourceLifetime.SetColor(m_OriginalResourceLifeColor);

        var resource = m_CurrentRenderGraph.GetDebugData().resourceLists[info.resourceType][info.index];
        UpdatePassColor(info, m_OriginalPassColor, m_OriginalPassColor);
        UpdateResourceLabelColor(info, resource.imported ? m_ImportedResourceColor : m_OriginalResourceColor); ;
    }

    VisualElement CreateRenderPass(string name, int index, bool culled)
    {
        var container = new VisualElement();
        container.name = "RenderPass";
        container.style.width = kRenderPassWidth;
        container.style.overflow = Overflow.Visible;
        container.style.flexDirection = FlexDirection.ColumnReverse;
        container.style.minWidth = kRenderPassWidth;

        var cell = new Button();
        cell.name = "RenderPass.Cell";
        cell.style.marginBottom = 0.0f;
        cell.style.marginLeft = 0.0f;
        cell.style.marginRight = 0.0f;
        cell.style.marginTop = 0.0f;
        cell.RegisterCallback<MouseEnterEvent, int>(MouseEnterPassCallback, index);
        cell.RegisterCallback<MouseLeaveEvent, int>(MouseLeavePassCallback, index);

        m_OriginalPassColor = cell.style.backgroundColor;

        if (culled)
            cell.style.backgroundColor = m_CulledPassColor;

        container.Add(cell);

        var label = new Label(name);
        label.name = "RenderPass.Label";
        label.transform.rotation = Quaternion.Euler(new Vector3(0.0f, 0.0f, -45.0f));
        container.Add(label);

        label.RegisterCallback<GeometryChangedEvent>(RenderPassLabelChanged);

        return container;
    }

    void ResourceNamesContainerChanged(GeometryChangedEvent evt)
    {
        var label = evt.currentTarget as Label;
        float textWidth = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined, 10, VisualElement.MeasureMode.Undefined).x;

        var cornerElement = m_GraphViewerElement.Q<VisualElement>("GraphViewer.Corner");
        cornerElement.style.width = Mathf.Max(textWidth, cornerElement.style.width.value.value);
        cornerElement.style.minWidth = Mathf.Max(textWidth, cornerElement.style.minWidth.value.value);

        // We need to make sure all resource types have the same width
        m_GraphViewerElement.Query("GraphViewer.Resources.ResourceNames").Build().ForEach((elem) =>
        {
            elem.style.width = Mathf.Max(textWidth, elem.style.width.value.value);
            elem.style.minWidth = Mathf.Max(textWidth, elem.style.minWidth.value.value);
        });

        m_GraphViewerElement.Query("GraphViewer.Resources.ResourceTypeName").Build().ForEach((elem) =>
        {
            elem.style.width = Mathf.Max(textWidth, elem.style.width.value.value);
            elem.style.minWidth = Mathf.Max(textWidth, elem.style.minWidth.value.value);
        });
    }

    VisualElement CreateResourceLabel(string name, bool imported)
    {
        var label = new Label(name);
        label.style.height = kResourceHeight;
        label.style.overflow = Overflow.Hidden;
        label.style.textOverflow = TextOverflow.Ellipsis;
        label.style.unityTextOverflowPosition = TextOverflowPosition.End;
        if (imported)
            label.style.color = m_ImportedResourceColor;
        else
            m_OriginalResourceColor = label.style.color;

        return label;
    }

    VisualElement CreateColorLegend(string name, StyleColor color)
    {
        VisualElement legend = new VisualElement();
        legend.style.flexDirection = FlexDirection.Row;
        Button button = new Button();
        button.style.width = kRenderPassWidth;// * 2;
        button.style.backgroundColor = color;
        legend.Add(button);
        var label = new Label(name);
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        legend.Add(label);
        return legend;
    }

    string RenderGraphPopupCallback(RenderGraph rg)
    {
        return rg.name;
    }

    string EmptyRenderGraphPopupCallback(RenderGraph rg)
    {
        return "NotAvailable";
    }

    void OnCaptureGraph()
    {
        RebuildGraphViewerUI();
    }

    void RebuildHeaderUI()
    {
        m_HeaderElement.Clear();

        var controlsElement = new VisualElement();
        controlsElement.name = "Header.Controls";
        controlsElement.style.flexDirection = FlexDirection.Row;

        var renderGraphList = new List<RenderGraph>(RenderGraph.GetRegisteredRenderGraphs());

        PopupField<RenderGraph> popup = null;
        if (renderGraphList.Count != 0)
        {
            popup = new PopupField<RenderGraph>("Current Graph", renderGraphList, 0, RenderGraphPopupCallback, RenderGraphPopupCallback);
        }
        else
        {
            renderGraphList.Add(null);
            popup = new PopupField<RenderGraph>("Current Graph", renderGraphList, 0, EmptyRenderGraphPopupCallback, EmptyRenderGraphPopupCallback);
        }

        popup.labelElement.style.minWidth = 0;
        popup.name = "Header.RenderGraphPopup";
        controlsElement.Add(popup);

        var captureButton = new Button(OnCaptureGraph);
        captureButton.text = "Capture Graph";
        controlsElement.Add(captureButton);

        var filters = new EnumFlagsField("Filters", m_Filter);
        filters.labelElement.style.minWidth = 0;
        filters.labelElement.style.alignItems = Align.Center;
        filters.RegisterCallback<ChangeEvent<System.Enum>>((evt) =>
        {
            m_Filter = (Filter)evt.newValue;
            RebuildGraphViewerUI();
        });
        controlsElement.Add(filters);

        m_HeaderElement.Add(controlsElement);

        var legendsElement = new VisualElement();
        legendsElement.name = "Header.Legends";
        legendsElement.style.flexDirection = FlexDirection.Row;
        legendsElement.style.alignContent = Align.FlexEnd;

        legendsElement.Add(CreateColorLegend("Resource Read", m_ResourceColorRead));
        legendsElement.Add(CreateColorLegend("Resource Write", m_ResourceColorWrite));
        legendsElement.Add(CreateColorLegend("Culled Pass", m_CulledPassColor));
        legendsElement.Add(CreateColorLegend("Imported Resource", m_ImportedResourceColor));

        m_HeaderElement.Add(legendsElement);
    }

    RenderGraph GetCurrentRenderGraph()
    {
        var popup = m_HeaderElement.Q<PopupField<RenderGraph>>("Header.RenderGraphPopup");
        if (popup != null)
        {
            return popup.value;
        }

        return null;
    }

    VisualElement CreateTopRowWithPasses(RenderGraphDebugData debugData, out int finalPassCount)
    {
        var topRowElement = new VisualElement();
        topRowElement.name = "GraphViewer.TopRowElement";
        topRowElement.style.flexDirection = FlexDirection.Row;

        var cornerElement = new VisualElement();
        cornerElement.name = "GraphViewer.Corner";

        topRowElement.Add(cornerElement);

        var passNamesElement = new VisualElement();
        passNamesElement.name = "GraphViewer.TopRowElement.PassNames";
        passNamesElement.style.flexDirection = FlexDirection.Row;

        int passIndex = 0;
        finalPassCount = 0;
        int lastValidPassIndex = -1;
        foreach (var pass in debugData.passList)
        {
            if ((pass.culled && !m_Filter.HasFlag(Filter.CulledPasses)) || !pass.generateDebugData)
            {
                m_PassElementsInfo[passIndex].Reset();
            }
            else
            {
                var passElement = CreateRenderPass(pass.name, passIndex, pass.culled);
                m_PassElementsInfo[passIndex].pass = passElement;
                m_PassElementsInfo[passIndex].remap = finalPassCount;
                passNamesElement.Add(passElement);
                finalPassCount++;
                lastValidPassIndex = passIndex;
            }
            passIndex++;
        }

        if (lastValidPassIndex > 0)
        {
            var label = m_PassElementsInfo[lastValidPassIndex].pass.Q<Label>("RenderPass.Label");
            label.RegisterCallback<GeometryChangedEvent>(LastRenderPassLabelChanged);
        }

        topRowElement.Add(passNamesElement);

        return topRowElement;
    }

    VisualElement CreateResourceViewer(RenderGraphDebugData debugData, int resourceType, int passCount)
    {
        var resourceElement = new VisualElement();
        resourceElement.name = "GraphViewer.Resources";
        resourceElement.style.flexDirection = FlexDirection.Row;

        var resourceNamesContainer = new VisualElement();
        resourceNamesContainer.name = "GraphViewer.Resources.ResourceNames";
        resourceNamesContainer.style.flexDirection = FlexDirection.Column;
        resourceNamesContainer.style.overflow = Overflow.Hidden;
        resourceNamesContainer.style.alignItems = Align.FlexEnd;

        var resourcesLifeTimeElement = new VisualElement();
        resourcesLifeTimeElement.name = "GraphViewer.Resources.ResourceLifeTime";
        resourcesLifeTimeElement.style.flexDirection = FlexDirection.Column;
        resourcesLifeTimeElement.style.width = kRenderPassWidth * passCount;

        int index = 0;
        foreach (var resource in debugData.resourceLists[resourceType])
        {
            // Remove unused resource.
            if (resource.releasePassIndex == -1 && resource.creationPassIndex == -1
                || (resource.imported && !m_Filter.HasFlag(Filter.ImportedResources)))
            {
                m_ResourceElementsInfo[resourceType][index++].Reset();
                continue;
            }

            var label = CreateResourceLabel(resource.name, resource.imported);
            m_ResourceElementsInfo[resourceType][index].resourceLabel = label;
            resourceNamesContainer.Add(label);

            var newCell = new CellElement(m_PassElementsInfo[resource.creationPassIndex].remap, m_PassElementsInfo[resource.releasePassIndex].remap);
            newCell.RegisterCallback<MouseEnterEvent, (int, int)>(MouseEnterResourceCallback, (index, resourceType));
            newCell.RegisterCallback<MouseLeaveEvent, (int, int)>(MouseLeaveResourceCallback, (index, resourceType));
            m_OriginalResourceLifeColor = newCell.style.backgroundColor;
            resourcesLifeTimeElement.Add(newCell);

            m_ResourceElementsInfo[resourceType][index++].lifetime = newCell;
        }

        resourceElement.Add(resourceNamesContainer);
        resourceElement.Add(resourcesLifeTimeElement);

        return resourceElement;
    }

    void RebuildGraphViewerUI()
    {
        m_GraphViewerElement.Clear();

        m_CurrentRenderGraph = GetCurrentRenderGraph();
        if (m_CurrentRenderGraph == null)
            return;

        var debugData = m_CurrentRenderGraph.GetDebugData();
        if (debugData.passList.Count == 0)
            return;

        for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
            m_ResourceElementsInfo[i].Resize(debugData.resourceLists[i].Count);
        m_PassElementsInfo.Resize(debugData.passList.Count);

        var horizontalScrollView = new ScrollView(ScrollViewMode.Horizontal);
        horizontalScrollView.name = "GraphViewer.HorizontalScrollView";

        var graphViewerElement = new VisualElement();
        graphViewerElement.name = "GraphViewer.ViewerContainer";
        graphViewerElement.style.flexDirection = FlexDirection.Column;

        var topRowElement = CreateTopRowWithPasses(debugData, out int finalPassCount);

        var resourceScrollView = new ScrollView(ScrollViewMode.Vertical);
        resourceScrollView.name = "GraphViewer.ResourceScrollView";

        // Has to match RenderGraphModule.RenderGraphResourceType order.
        Filter[] resourceFilterFlags = { Filter.Textures, Filter.ComputeBuffers };
        string[] resourceNames = { "Textures Resources", "Compute Buffer Resources" };

        for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
        {
            if (m_Filter.HasFlag(resourceFilterFlags[i]))
            {
                var resourceViewerElement = CreateResourceViewer(debugData, i, finalPassCount);
                var resourceNameLabel = new Label(resourceNames[i]);
                resourceNameLabel.name = "GraphViewer.Resources.ResourceTypeName";
                resourceNameLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                resourceNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                resourceNameLabel.style.fontSize = 13;
                resourceNameLabel.RegisterCallback<GeometryChangedEvent>(ResourceNamesContainerChanged);
                resourceScrollView.Add(resourceNameLabel);
                resourceScrollView.Add(resourceViewerElement);

                VisualElement separator = new VisualElement();
                separator.style.minHeight = kResourceHeight;
                resourceScrollView.Add(separator);
            }
        }

        graphViewerElement.Add(topRowElement);
        graphViewerElement.Add(resourceScrollView);

        horizontalScrollView.Add(graphViewerElement);

        m_GraphViewerElement.Add(horizontalScrollView);
    }

    void RebuildUI()
    {
        rootVisualElement.Clear();

        titleContent = Style.title;

        m_Root = new VisualElement();
        m_Root.name = "Root";
        m_Root.style.flexDirection = FlexDirection.Column;

        m_HeaderElement = new VisualElement();
        m_HeaderElement.name = "Header";
        m_HeaderElement.style.flexDirection = FlexDirection.Row;
        m_HeaderElement.style.justifyContent = Justify.SpaceBetween;
        m_HeaderElement.style.minHeight = 25.0f;
        m_HeaderElement.style.marginBottom = 1.0f;
        m_HeaderElement.style.marginTop = 1.0f;
        m_HeaderElement.style.borderTopWidth = 1.0f;
        m_HeaderElement.style.borderBottomWidth = 1.0f;

        RebuildHeaderUI();

        m_GraphViewerElement = new VisualElement();
        m_GraphViewerElement.name = "GraphViewer";
        m_GraphViewerElement.style.marginLeft = 20.0f; // Margin on the left of resource labels.
        m_GraphViewerElement.style.flexDirection = FlexDirection.Column;

        RebuildGraphViewerUI();

        m_Root.Add(m_HeaderElement);
        m_Root.Add(m_GraphViewerElement);
        rootVisualElement.Add(m_Root);
    }

    void OnGraphRegistered(RenderGraph graph)
    {
        RebuildHeaderUI();
    }

    void OnGraphUnregistered(RenderGraph graph)
    {
        RebuildHeaderUI();
    }

    void OnEnable()
    {
        for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
            m_ResourceElementsInfo[i] = new DynamicArray<ResourceElementInfo>();

        RenderGraph.requireDebugData = true;
        RenderGraph.onGraphRegistered += OnGraphRegistered;
        RenderGraph.onGraphRegistered += OnGraphUnregistered;

        RebuildUI();
    }

    void OnDisable()
    {
        RenderGraph.requireDebugData = false;
        RenderGraph.onGraphRegistered -= OnGraphRegistered;
        RenderGraph.onGraphRegistered -= OnGraphUnregistered;
    }
}
