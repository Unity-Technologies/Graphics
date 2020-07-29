using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

public class RenderGraphViewer : EditorWindow
{
    public const float kRenderPassWidth = 25.0f;
    public const float kResourceHeight = 20.0f;

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

    VisualElement m_Root;
    VisualElement m_EmptyCorner;
    VisualElement m_ResourceLifeTimeContainer;
    float m_PassNamesContainerHeight = 0.0f;

    void RenderPassLabelChanged(GeometryChangedEvent evt)
    {
        var label = evt.currentTarget as Label;
        Vector2 textSize = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined, 10, VisualElement.MeasureMode.Undefined);
        float textWidth = Mathf.Max(kRenderPassWidth, textSize.x);
        float desiredHeight = Mathf.Sqrt(textWidth * textWidth - kRenderPassWidth * kRenderPassWidth);
        // Should be able to do that and rely on the parent layout but for some reason flex-end does not work so I set the parent's parent height instead.
        //label.parent.style.height = desiredHeight;
        m_PassNamesContainerHeight = Mathf.Max(label.parent.parent.style.height.value.value, desiredHeight);
        label.parent.parent.style.height = m_PassNamesContainerHeight;

        m_EmptyCorner.style.height = m_PassNamesContainerHeight;
    }

    int[] resourceReads = { 1, 4, 3 };
    int[] resourceWrites = { 2, 4, 5 };

    StyleColor m_ResourceColorRead = new StyleColor(new Color(0.2f, 1.0f, 0.2f));
    StyleColor m_ResourceColorWrite = new StyleColor(new Color(1.0f, 0.2f, 0.2f));
    StyleColor m_OriginalResourceLifeColor;


    void MouseEnterPassCallback(MouseEnterEvent evt, int index)
    {
        foreach (int resourceRead in resourceReads)
        {
            VisualElement resourceLifetime = m_ResourceLifeTimeContainer.ElementAt(resourceRead);
            resourceLifetime.style.backgroundColor = m_ResourceColorRead;
        }

        foreach (int resourceWrite in resourceWrites)
        {
            VisualElement resourceLifetime = m_ResourceLifeTimeContainer.ElementAt(resourceWrite);
            resourceLifetime.style.backgroundColor = m_ResourceColorWrite;
        }
    }

    void MouseLeavePassCallback(MouseLeaveEvent evt, int index)
    {
        foreach (int resourceRead in resourceReads)
        {
            VisualElement resourceLifetime = m_ResourceLifeTimeContainer.ElementAt(resourceRead);
            resourceLifetime.style.backgroundColor = m_OriginalResourceLifeColor;
        }

        foreach (int resourceWrite in resourceWrites)
        {
            VisualElement resourceLifetime = m_ResourceLifeTimeContainer.ElementAt(resourceWrite);
            resourceLifetime.style.backgroundColor = m_OriginalResourceLifeColor;
        }
    }

    VisualElement CreateRenderPassLabel(string name, int index)
    {
        var labelContainer = new VisualElement();
        labelContainer.style.width = kRenderPassWidth;
        //labelContainer.style.backgroundColor = new StyleColor(new Color(0.3f, 0.1f, 0.1f));
        labelContainer.style.overflow = Overflow.Visible;
        labelContainer.style.flexDirection = FlexDirection.ColumnReverse;

        var button = new Button();
        button.style.marginBottom = 0.0f;
        button.style.marginLeft = 0.0f;
        button.style.marginRight = 0.0f;
        button.style.marginTop = 0.0f;
        button.RegisterCallback<MouseEnterEvent, int>(MouseEnterPassCallback, index);
        button.RegisterCallback<MouseLeaveEvent, int>(MouseLeavePassCallback, index);
        labelContainer.Add(button);

        var label = new Label(name);
        label.transform.rotation = Quaternion.Euler(new Vector3(0.0f, 0.0f, -45.0f));
        //label.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
        labelContainer.Add(label);

        label.RegisterCallback<GeometryChangedEvent>(RenderPassLabelChanged);

        return labelContainer;
    }

    void ResourceNamesContainerChanged(GeometryChangedEvent evt)
    {
        var container = evt.currentTarget as VisualElement;
        //var width = container.style.width;
        m_EmptyCorner.style.width = evt.newRect.width;
    }

    VisualElement CreateResourceLabel(string name)
    {
        var label = new Label(name);
        label.style.height = kResourceHeight;
        //label.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));

        //label.RegisterCallback<GeometryChangedEvent>(ResourceLabelChanged);

        return label;
    }

    void OnEnable()
    {
        titleContent = Style.title;

        m_Root = new VisualElement();

        var topRowContainer = new VisualElement();
        topRowContainer.name = "TopRowContainer";
        topRowContainer.style.flexDirection = FlexDirection.Row;

        m_EmptyCorner = new VisualElement();
        m_EmptyCorner.name = "EmptyCorner";
        //m_EmptyCorner.style.width = 100.0f;
        topRowContainer.Add(m_EmptyCorner);

        var passNamesContainer = new VisualElement();
        passNamesContainer.name = "PassNamesContainer";
        passNamesContainer.style.flexDirection = FlexDirection.Row;
        //passNamesPanel.style.justifyContent = Justify.FlexStart;
        //passNamesPanel.style.alignContent = Align.FlexEnd;
        //passNamesPanel.style.height = 100.0f;
        //passNamesPanel.style.backgroundColor = new StyleColor(new Color(0.1f, 0.3f, 0.1f));

        string[] passNames = { "name", "name 2", "very long name I don't really blabla", "another quite long name", "oh", "aahaha", "blabla blibli" }; // 7
        string[] resourceNames = { "pwette", "pwette 2", "very long resource I don't really care about", "another resource LOO", "oh", "aahaha", "blabla blibli" }; // 7
        (int begin, int end)[] resourceLifeTimes = { (0, 2), (1, 3), (0, 6), (3, 5), (2, 6), (1, 2), (3, 6) };

        int passIndex = 0;
        foreach (var passName in passNames)
        {
            passNamesContainer.Add(CreateRenderPassLabel(passName, passIndex++));
        }

        topRowContainer.Add(passNamesContainer);

        var resourceContainer = new VisualElement();
        resourceContainer.name = "ResourceContainer";
        resourceContainer.style.flexDirection = FlexDirection.Row;

        var resourceNamesContainer = new VisualElement();
        resourceNamesContainer.name = "ResourceNamesContainer";
        resourceNamesContainer.style.flexDirection = FlexDirection.Column;
        resourceNamesContainer.RegisterCallback<GeometryChangedEvent>(ResourceNamesContainerChanged);

        m_ResourceLifeTimeContainer = new VisualElement();
        m_ResourceLifeTimeContainer.name = "ResourceLifeTimeContainer";
        m_ResourceLifeTimeContainer.style.flexDirection = FlexDirection.Column;
        m_ResourceLifeTimeContainer.style.width = kRenderPassWidth * passNames.Length;

        foreach (var resourceName in resourceNames)
        {
            resourceNamesContainer.Add(CreateResourceLabel(resourceName));
        }

        foreach (var resourceLifeTime in resourceLifeTimes)
        {
            var newButton = new Button();
            newButton.style.position = Position.Relative;
            newButton.style.left = resourceLifeTime.begin * kRenderPassWidth;
            newButton.style.width = (resourceLifeTime.end - resourceLifeTime.begin + 1) * kRenderPassWidth;
            newButton.style.marginBottom = 0.0f;
            newButton.style.marginLeft = 0.0f;
            newButton.style.marginRight = 0.0f;
            newButton.style.marginTop = 0.0f;
            newButton.style.height = kResourceHeight;

            m_ResourceLifeTimeContainer.Add(newButton);

            m_OriginalResourceLifeColor = newButton.style.color;
        }

        resourceContainer.Add(resourceNamesContainer);
        resourceContainer.Add(m_ResourceLifeTimeContainer);


        m_Root.Add(topRowContainer);
        m_Root.Add(resourceContainer);
        rootVisualElement.Add(m_Root);
    }
}
