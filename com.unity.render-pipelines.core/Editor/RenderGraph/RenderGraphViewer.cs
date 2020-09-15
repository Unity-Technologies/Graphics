using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using System.Collections.Generic;

public class RenderGraphViewer : EditorWindow
{
    public const float kRenderPassWidth = 20.0f;
    public const float kResourceHeight = 15.0f;

    static class Style
    {
        public static readonly GUIContent title = EditorGUIUtility.TrTextContent("Render Graph Viewer");
    }

    [MenuItem("Window/Render Pipeline/Render Graph Viewer", false, 10006)]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        var window = GetWindow<RenderGraphViewer>();
        window.titleContent = new GUIContent("Render Graph Viewer");
    }

    RenderGraph m_CurrentRenderGraph;

    VisualElement m_Root;
    VisualElement m_HeaderElement;
    VisualElement m_GraphViewerElement;

    StyleColor m_ResourceColorRead = new StyleColor(new Color(0.2f, 1.0f, 0.2f));
    StyleColor m_ResourceColorWrite = new StyleColor(new Color(1.0f, 0.2f, 0.2f));
    StyleColor m_ImportedResourceColor = new StyleColor(new Color(0.3f, 0.75f, 0.75f));
    StyleColor m_CulledPassColor = new StyleColor(Color.black);
    StyleColor m_OriginalResourceLifeColor;
    StyleColor m_OriginalPassColor;

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

    void MouseEnterPassCallback(MouseEnterEvent evt, int index)
    {
        var resourceLifeTimeElement = m_GraphViewerElement.Q<VisualElement>("GraphViewer.Resources.ResourceLifeTime");

        var debugData = m_CurrentRenderGraph.GetDebugData();
        var pass = debugData.passList[index];

        if (pass.culled)
            return;

        foreach (int resourceRead in pass.resourceReadLists[0])
        {
            VisualElement resourceLifetime = resourceLifeTimeElement.ElementAt(resourceRead);
            resourceLifetime.style.backgroundColor = m_ResourceColorRead;
        }

        foreach (int resourceWrite in pass.resourceWriteLists[0])
        {
            VisualElement resourceLifetime = resourceLifeTimeElement.ElementAt(resourceWrite);
            resourceLifetime.style.backgroundColor = m_ResourceColorWrite;
        }
    }

    void MouseLeavePassCallback(MouseLeaveEvent evt, int index)
    {
        var resourceLifeTimeElement = m_GraphViewerElement.Q<VisualElement>("GraphViewer.Resources.ResourceLifeTime");

        var debugData = m_CurrentRenderGraph.GetDebugData();
        var pass = debugData.passList[index];

        if (pass.culled)
            return;

        foreach (int resourceRead in pass.resourceReadLists[0])
        {
            VisualElement resourceLifetime = resourceLifeTimeElement.ElementAt(resourceRead);
            resourceLifetime.style.backgroundColor = m_OriginalResourceLifeColor;
        }

        foreach (int resourceWrite in pass.resourceWriteLists[0])
        {
            VisualElement resourceLifetime = resourceLifeTimeElement.ElementAt(resourceWrite);
            resourceLifetime.style.backgroundColor = m_OriginalResourceLifeColor;
        }
    }

    void MouseEnterResourceCallback(MouseEnterEvent evt, int index)
    {
        var passNamesElement = m_GraphViewerElement.Q<VisualElement>("GraphViewer.TopRowElement.PassNames");

        var debugData = m_CurrentRenderGraph.GetDebugData();
        var resource = debugData.resourceLists[0][index];

        foreach (int consumer in resource.consumerList)
        {
            VisualElement passButton = passNamesElement.ElementAt(consumer).Q("PassNameButton");
            passButton.style.backgroundColor = m_ResourceColorRead;
        }

        foreach (int producer in resource.producerList)
        {
            VisualElement passButton = passNamesElement.ElementAt(producer).Q("PassNameButton");
            passButton.style.backgroundColor = m_ResourceColorWrite;
        }
    }

    void MouseLeaveResourceCallback(MouseLeaveEvent evt, int index)
    {
        var passNamesElement = m_GraphViewerElement.Q<VisualElement>("GraphViewer.TopRowElement.PassNames");

        var debugData = m_CurrentRenderGraph.GetDebugData();
        var resource = debugData.resourceLists[0][index];

        foreach (int consumer in resource.consumerList)
        {
            VisualElement passButton = passNamesElement.ElementAt(consumer).Q("PassNameButton");
            passButton.style.backgroundColor = m_OriginalPassColor;
        }

        foreach (int producer in resource.producerList)
        {
            VisualElement passButton = passNamesElement.ElementAt(producer).Q("PassNameButton");
            passButton.style.backgroundColor = m_OriginalPassColor;
        }
    }

    VisualElement CreateRenderPassLabel(string name, int index, bool culled)
    {
        var labelContainer = new VisualElement();
        labelContainer.style.width = kRenderPassWidth;
        labelContainer.style.overflow = Overflow.Visible;
        labelContainer.style.flexDirection = FlexDirection.ColumnReverse;
        labelContainer.style.minWidth = kRenderPassWidth;

        var button = new Button();
        button.name = "PassNameButton";
        button.style.marginBottom = 0.0f;
        button.style.marginLeft = 0.0f;
        button.style.marginRight = 0.0f;
        button.style.marginTop = 0.0f;
        button.RegisterCallback<MouseEnterEvent, int>(MouseEnterPassCallback, index);
        button.RegisterCallback<MouseLeaveEvent, int>(MouseLeavePassCallback, index);
        if (culled)
            button.style.backgroundColor = m_CulledPassColor;
        labelContainer.Add(button);

        m_OriginalPassColor = button.style.backgroundColor;

        var label = new Label(name);
        label.transform.rotation = Quaternion.Euler(new Vector3(0.0f, 0.0f, -45.0f));
        labelContainer.Add(label);

        label.RegisterCallback<GeometryChangedEvent>(RenderPassLabelChanged);

        return labelContainer;
    }

    void ResourceNamesContainerChanged(GeometryChangedEvent evt)
    {
        var cornerElement = m_GraphViewerElement.Q<VisualElement>("GraphViewer.Corner");
        cornerElement.style.width = evt.newRect.width;
        cornerElement.style.minWidth = evt.newRect.width;
    }

    VisualElement CreateResourceLabel(string name, bool imported)
    {
        var label = new Label(name);
        label.style.height = kResourceHeight;
        if (imported)
            label.style.color = m_ImportedResourceColor;

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
        m_HeaderElement.Add(popup);

        var captureButton = new Button(OnCaptureGraph);
        captureButton.text = "Capture Graph";
        //captureButton.disable = renderGraphList.Count != 0 ? true : false;
        m_HeaderElement.Add(captureButton);

        m_HeaderElement.Add(CreateColorLegend("Resource Read", m_ResourceColorRead));
        m_HeaderElement.Add(CreateColorLegend("Resource Write", m_ResourceColorWrite));
        m_HeaderElement.Add(CreateColorLegend("Culled Pass", m_CulledPassColor));
        m_HeaderElement.Add(CreateColorLegend("Imported Resource", m_ImportedResourceColor));
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

    void RebuildGraphViewerUI()
    {
        m_GraphViewerElement.Clear();

        m_CurrentRenderGraph = GetCurrentRenderGraph();
        if (m_CurrentRenderGraph == null)
            return;

        var horizontalScrollView = new ScrollView(ScrollViewMode.Horizontal);

        var graphViewerElement = new VisualElement();
        graphViewerElement.style.flexDirection = FlexDirection.Column;

        var topRowElement = new VisualElement();
        topRowElement.name = "GraphViewer.TopRowElement";
        topRowElement.style.flexDirection = FlexDirection.Row;

        var cornerElement = new VisualElement();
        cornerElement.name = "GraphViewer.Corner";

        topRowElement.Add(cornerElement);

        var passNamesElement = new VisualElement();
        passNamesElement.name = "GraphViewer.TopRowElement.PassNames";
        passNamesElement.style.flexDirection = FlexDirection.Row;

        var debugData = m_CurrentRenderGraph.GetDebugData();

        int passIndex = 0;
        foreach(var pass in debugData.passList)
        {
            passNamesElement.Add(CreateRenderPassLabel(pass.name, passIndex++, pass.culled));
        }

        topRowElement.Add(passNamesElement);

        var resourceScrollView = new ScrollView(ScrollViewMode.Vertical);

        var resourceElement = new VisualElement();
        resourceElement.name = "GraphViewer.Resources";
        resourceElement.style.flexDirection = FlexDirection.Row;

        var resourceNamesContainer = new VisualElement();
        resourceNamesContainer.name = "GraphViewer.Resources.ResourceNames";
        resourceNamesContainer.style.flexDirection = FlexDirection.Column;
        resourceNamesContainer.style.overflow = Overflow.Hidden;
        resourceNamesContainer.style.alignItems = Align.FlexEnd;
        resourceNamesContainer.RegisterCallback<GeometryChangedEvent>(ResourceNamesContainerChanged);

        var resourcesLifeTimeElement = new VisualElement();
        resourcesLifeTimeElement.name = "GraphViewer.Resources.ResourceLifeTime";
        resourcesLifeTimeElement.style.flexDirection = FlexDirection.Column;
        resourcesLifeTimeElement.style.width = kRenderPassWidth * debugData.passList.Count;

        int index = 0;
        foreach (var resource in debugData.resourceLists[0])
        {
            // Remove unused resource.
            if (resource.releasePassIndex == -1 && resource.creationPassIndex == -1)
            {
                index++;
                continue;
            }

            resourceNamesContainer.Add(CreateResourceLabel(resource.name, resource.imported));

            var newButton = new Button();
            newButton.style.position = Position.Relative;
            newButton.style.left = resource.creationPassIndex * kRenderPassWidth;
            newButton.style.width = (resource.releasePassIndex - resource.creationPassIndex + 1) * kRenderPassWidth;
            newButton.style.marginBottom = 0.0f;
            newButton.style.marginLeft = 0.0f;
            newButton.style.marginRight = 0.0f;
            newButton.style.marginTop = 0.0f;
            newButton.style.height = kResourceHeight;

            newButton.RegisterCallback<MouseEnterEvent, int>(MouseEnterResourceCallback, index);
            newButton.RegisterCallback<MouseLeaveEvent, int>(MouseLeaveResourceCallback, index);

            resourcesLifeTimeElement.Add(newButton);

            m_OriginalResourceLifeColor = newButton.style.color;
            index++;
        }

        resourceElement.Add(resourceNamesContainer);
        resourceElement.Add(resourcesLifeTimeElement);
        resourceScrollView.Add(resourceElement);

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
        m_HeaderElement.style.minHeight = 25.0f;
        m_HeaderElement.style.marginBottom = 1.0f;
        m_HeaderElement.style.marginTop = 1.0f;
        m_HeaderElement.style.borderTopWidth = 1.0f;
        m_HeaderElement.style.borderBottomWidth = 1.0f;

        RebuildHeaderUI();

        m_GraphViewerElement = new VisualElement();
        m_GraphViewerElement.name = "GraphViewer";
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
