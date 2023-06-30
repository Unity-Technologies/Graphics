using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


public class RenderGraphResourceView : VisualElement
{
    VisualElement m_GraphViewerElement;
    private RenderGraphView m_RGView;
    private Label m_ResourceInfo;
    internal class CellElement : VisualElement
    {
        public ResourceNode m_Resource;
        public CellElement(int idxStart, int idxEnd, ResourceNode resource, float size)
        {
            style.borderBottomLeftRadius = style.borderTopLeftRadius = style.borderBottomRightRadius = style.borderTopRightRadius = 5;
            style.borderBottomWidth = style.borderTopWidth = style.borderLeftWidth = style.borderRightWidth = 1f;
            style.borderBottomColor = style.borderTopColor = style.borderLeftColor = style.borderRightColor = new Color(0f, 0f, 0f, 1f);
            style.backgroundColor = (Color)new Color32(88, 88, 88, 255);
            style.height = size;
            style.left = idxStart * kRenderPassWidth;
            style.width = (idxEnd - idxStart + 1) * kRenderPassWidth;

            m_Resource = resource;
        }

        public void SetColor(StyleColor color)
        {
            style.backgroundColor = color;
        }
    }

    internal void DrawResources(List<ResourceNode> resources, List<RenderGraphNode> passes, RenderGraphView view)
    {
        Clear();
        m_RGView = view;

        m_GraphViewerElement = new VisualElement();
        m_GraphViewerElement.name = "Graph Resources";
        m_GraphViewerElement.style.marginLeft = 20.0f; // Margin on the left of resource labels.
        m_GraphViewerElement.style.flexDirection = FlexDirection.Column;

        var horizontalScrollView = new ScrollView(ScrollViewMode.Horizontal);
        horizontalScrollView.name = "GraphViewer.HorizontalScrollView";

        var graphViewerElement = new VisualElement();
        graphViewerElement.name = "GraphViewer.ViewerContainer";
        graphViewerElement.style.flexDirection = FlexDirection.Column;

        var topRowElement = CreateTopRowWithPasses(passes);
        graphViewerElement.Add(topRowElement);

        Add(m_GraphViewerElement);

        var resourceScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
        resourceScrollView.name = "GraphViewer.ResourceScrollView";

        var resourceView = CreateResourceViewer(resources, passes);
        resourceScrollView.Add(resourceView);
        resourceScrollView.style.borderLeftWidth =
            resourceScrollView.style.borderRightWidth =
            resourceScrollView.style.borderTopWidth =
            resourceScrollView.style.borderBottomWidth = 1;
        resourceScrollView.style.borderLeftColor =
            resourceScrollView.style.borderRightColor=
            resourceScrollView.style.borderTopColor =
            resourceScrollView.style.borderBottomColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));

        topRowElement.style.marginLeft = 200;
        topRowElement.style.marginRight = 200;
        topRowElement.style.height = 150.0f;

        graphViewerElement.Add(resourceScrollView);
        horizontalScrollView.Add(graphViewerElement);
        m_GraphViewerElement.Add(horizontalScrollView);
        m_ResourceInfo = new Label();
        m_ResourceInfo.style.height = 150.0f;
        m_GraphViewerElement.Add(m_ResourceInfo);

        style.borderLeftWidth = 2;
        style.borderRightWidth = 2;
        style.borderTopWidth = 2;
        style.borderBottomWidth = 2;
        //Add(new ResizableElement());
    }

    const float kRenderPassWidth = 20.0f;
    const float kResourceHeight = 0.02f;
    readonly StyleColor m_ResourceColorRead = new StyleColor(new Color(0.2f, 1.0f, 0.2f));
    readonly StyleColor m_ResourceColorWrite = new StyleColor(new Color(1.0f, 0.2f, 0.2f));
    readonly StyleColor m_ImportedResourceColor = new StyleColor(new Color(0.3f, 0.75f, 0.75f));
    readonly StyleColor m_CulledPassColor = new StyleColor(Color.black);
    readonly StyleColor m_ResourceHighlightColor = new StyleColor(Color.white);
    readonly StyleColor m_ResourceLifeHighLightColor = new StyleColor(new Color32(103, 103, 103, 255));
    StyleColor m_OriginalResourceLifeColor;
    StyleColor m_OriginalPassColor;
    StyleColor m_OriginalResourceColor;


    VisualElement CreateTopRowWithPasses(List<RenderGraphNode> passes)
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
        passNamesElement.style.marginBottom = 4;

        int largestName = 0;
        foreach (var pass in passes)
        {
            var passElement = CreateRenderPass(pass.title, false);
            passNamesElement.Add(passElement);
            largestName = Mathf.Max(pass.title.Length, largestName);
        }

        passNamesElement.style.marginTop = largestName *2.0f;
        topRowElement.Add(passNamesElement);

        return topRowElement;
    }

    VisualElement CreateRenderPass(string name, bool culled)
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
        cell.style.marginTop = 2.0f;

        m_OriginalPassColor = cell.style.backgroundColor;

        if (culled)
            cell.style.backgroundColor = m_CulledPassColor;

        container.Add(cell);

        var label = new Label(name);
        label.name = "RenderPass.Label";
        label.text = name;
        label.style.marginTop = name.Length + 10;
        label.transform.position += new Vector3(0.0f, -16.0f, 0.0f);
        label.transform.rotation = Quaternion.Euler(new Vector3(0.0f, 0.0f, -40.0f));

        container.Add(label);

        return container;
    }

    VisualElement CreateResourceViewer(List<ResourceNode> resources, List<RenderGraphNode> passes)
    {
        var resourceElement = new VisualElement();
        resourceElement.name = "GraphViewer.Resources";
        resourceElement.style.flexDirection = FlexDirection.Row;

        var resourceNamesContainer = new VisualElement();
        resourceNamesContainer.name = "GraphViewer.Resources.ResourceNames";
        resourceNamesContainer.style.flexDirection = FlexDirection.Column;
        resourceNamesContainer.style.overflow = Overflow.Hidden;
        resourceNamesContainer.style.alignItems = Align.FlexEnd;
        resourceNamesContainer.style.width = 200;

        var resourcesLifeTimeElement = new VisualElement();
        resourcesLifeTimeElement.name = "GraphViewer.Resources.ResourceLifeTime";
        resourcesLifeTimeElement.style.flexDirection = FlexDirection.Column;
        resourcesLifeTimeElement.style.width = kRenderPassWidth * passes.Count;

        foreach (var resource in resources)
        {
            float size = 1.0f;
            if (!resource.m_IsMemoryless)
            {
                float baseByteSize = 1024.0f * 1024.0f * 4.0f;
                float byteSize = GraphicsFormatUtility.ComputeMipmapSize(resource.m_Width, resource.m_Height, resource.m_Format) * resource.m_Samples;
                size = (byteSize / baseByteSize) * 25.0f;
                size = Mathf.Max(size, 1.0f);
            }

            float minSize = Mathf.Max(size, 20.0f);

            var label = CreateResourceLabel(resource.title, resource.m_IsImported, minSize);
            resourceNamesContainer.Add(label);

            int allocator = -1, releaser = -1;
            for (int i = 0; i < passes.Count; ++i)
            {
                if (passes[i].m_Allocations.Contains(resource))
                {
                    allocator = i;
                }

                if (passes[i].m_Releases.Contains(resource))
                {
                    releaser = i;
                }

                if (allocator != -1 && releaser != -1)
                {
                    var wrapper = new VisualElement();
                    wrapper.style.height = minSize;
                    var newCell = new CellElement(allocator, releaser, resource, size);
                    resource.m_Cell = newCell;
                    newCell.RegisterCallback<MouseDownEvent, ResourceNode>(MouseClickCallback, resource);
                    m_OriginalResourceLifeColor = newCell.style.backgroundColor;

                    wrapper.Add(newCell);
                    resourcesLifeTimeElement.Add(wrapper);
                    break;
                }
            }

            //if no allocator/releaser are found then it's an imported resource
            if (allocator == -1 && releaser == -1)
            {
                var cell = new CellElement(0, passes.Count-1, resource, size);
                resource.m_Cell = cell;
                cell.RegisterCallback<MouseDownEvent, ResourceNode>(MouseClickCallback, resource);
                m_OriginalResourceLifeColor = cell.style.backgroundColor;
                resourcesLifeTimeElement.Add(cell);
            }

        }

        resourceElement.Add(resourceNamesContainer);
        resourceElement.Add(resourcesLifeTimeElement);

        return resourceElement;
    }

    internal void Reset()
    {
        m_GraphViewerElement?.Clear();
    }
    VisualElement CreateResourceLabel(string name, bool imported, float size)
    {
        var label = new Label(name);
        label.style.height = size;
        label.style.overflow = Overflow.Hidden;
        label.style.textOverflow = TextOverflow.Ellipsis;
        label.style.unityTextOverflowPosition = TextOverflowPosition.End;
        if (imported)
            label.style.color = m_ImportedResourceColor;
        else
            m_OriginalResourceColor = label.style.color;

        return label;
    }

    private ResourceNode m_ClickedResource;
    void MouseClickCallback(MouseDownEvent evt, ResourceNode resource)
    {
        SelectCell(resource);
    }

    internal void SelectCell(ResourceNode resource)
    {
        if (m_ClickedResource != null)
            m_ClickedResource.m_Cell.style.backgroundColor = m_OriginalPassColor;

        m_RGView.SelectNode(resource, true);
        m_ClickedResource = resource;
        m_ClickedResource.m_Cell.style.backgroundColor = Color.blue;

        string info = '\n' + resource.title + '\n' + resource.m_Width + 'x' + resource.m_Height  + 'x' + resource.m_Samples + "\n\n";
        if (resource.m_IsImported)
        {
            info += "Imported Resource \n";
        }

        info += "Clear: " + resource.m_Clear + '\n';
        info += "BindMS: " + resource.m_BindMS + '\n';
        info += "Format: " + resource.m_Format + '\n';
        info += "Memoryless: " + resource.m_IsMemoryless + '\n';

        m_ResourceInfo.text = info;
    }

    public void SelectionChanged()
    {
        if(m_ClickedResource != null)
            m_ClickedResource.m_Cell.style.backgroundColor = (Color)new Color32(88, 88, 88, 255);

        m_ClickedResource = null;
        m_ResourceInfo.text = "";
    }
}
#endif
