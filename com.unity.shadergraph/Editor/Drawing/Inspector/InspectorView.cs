using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.Drawing
{
    class EnumPropertyDrawer
    {
        private Enum _fieldToDraw;
        private string _fieldLabel;
        private Enum _fieldDefaultValue;

        public delegate void EnumValueSetter(Enum newValue);

        public EnumPropertyDrawer(Enum fieldToDraw, string fieldLabel, Enum fieldDefaultValue)
        {
            this._fieldToDraw = fieldToDraw;
            this._fieldLabel = fieldLabel;
            this._fieldDefaultValue = fieldDefaultValue;
        }

        public PropertyRow CreatePropertyRowForField(EnumValueSetter valueChangedCallback)
        {
            var row = new PropertyRow(new Label(this._fieldLabel));
            row.Add(new EnumField(this._fieldDefaultValue), (field) =>
            {
                field.value = this._fieldToDraw;
                field.RegisterValueChangedCallback(evt => valueChangedCallback(evt.newValue));
            });

            return row;
        }
    }

    class InspectorView : VisualElement
    {
        // References
        GraphData m_GraphData;
        GraphView m_GraphView;

        // GenericView
        const string kTitle = "Inspector";
        const string kElementName = "inspectorView";
        const string kStyleName = "InspectorView";
        const string kLayoutKey = "ShaderGraph.Inspector";
        WindowDockingLayout m_Layout;
        WindowDockingLayout m_DefaultLayout = new WindowDockingLayout
        {
            dockingTop = true,
            dockingLeft = true,
            verticalOffset = 16,
            horizontalOffset = 16,
            size = new Vector2(200, 400),
        };

        // Context
        Label m_ContextTitle;
        VisualElement m_PropertyContainer;

        // Preview
        PreviewManager m_PreviewManager;
        PreviewRenderData m_PreviewRenderData;
        Image m_PreviewImage;
        Vector2 m_PreviewScrollPosition;
        Vector2 m_ExpandedPreviewSize = new Vector2(256f, 256f);
        Mesh m_PreviousPreviewMesh;
        static Type s_ObjectSelector = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypesOrNothing()).FirstOrDefault(t => t.FullName == "UnityEditor.ObjectSelector");

        // Passing both the manager and the data here is really bad
        // Inspector preview should be directly reactive to the preview manager
        public InspectorView(GraphData graphData, GraphView graphView, PreviewManager previewManager)
        {
            m_GraphData = graphData;
            m_GraphView = graphView;

            m_PreviewManager = previewManager;
            m_PreviewRenderData = previewManager.masterRenderData;
            if (m_PreviewRenderData != null)
                m_PreviewRenderData.onPreviewChanged += OnPreviewChanged;

            BuildView();
            DeserializeLayout();
        }

#region GenericView
        // All code in this region is generic to any floating tool window
        // This should be extracted for document based tool modes

        void BuildView()
        {
            name = kElementName;
            styleSheets.Add(Resources.Load<StyleSheet>($"Styles/{kStyleName}"));
            m_GraphView.Add(this);

            BuildTitleContainer();
            BuildContentContainer();
            BuildManipulators();
        }

        void BuildTitleContainer()
        {
            var titleContainer = new VisualElement() { name = "titleContainer" };
            {
                m_ContextTitle = new Label(" ") { name = "titleLabel" };
                var titleLabel = new Label(kTitle) { name = "titleValue" };

                titleContainer.Add(m_ContextTitle);
                titleContainer.Add(titleLabel);
            }
            Add(titleContainer);
        }

        void BuildContentContainer()
        {
            var contentContainer = new VisualElement() { name = "contentContainer" };
            BuildContent(contentContainer);
            Add(contentContainer);
        }

        void BuildManipulators()
        {
            var resizeBorderFrame = new ResizeBorderFrame(this) { name = "resizeBorderFrame" };
            resizeBorderFrame.OnResizeFinished += SerializeLayout;
            Add(resizeBorderFrame);

            var windowDraggable = new WindowDraggable(null, m_GraphView);
            windowDraggable.OnDragFinished += SerializeLayout;
            this.AddManipulator(windowDraggable);
        }

        void SerializeLayout()
        {
            m_Layout.CalculateDockingCornerAndOffset(layout, m_GraphView.layout);
            m_Layout.ClampToParentWindow();

            var serializedLayout = JsonUtility.ToJson(m_Layout);
            EditorUserSettings.SetConfigValue(kLayoutKey, serializedLayout);
        }

        void DeserializeLayout()
        {
            var serializedLayout = EditorUserSettings.GetConfigValue(kLayoutKey);
            if (!string.IsNullOrEmpty(serializedLayout))
                m_Layout = JsonUtility.FromJson<WindowDockingLayout>(serializedLayout);
            else
                m_Layout = m_DefaultLayout;

            m_Layout.ApplyPosition(this);
            m_Layout.ApplySize(this);
        }
#endregion

#region Content
        void BuildContent(VisualElement container)
        {
            BuildPreview(container);
        }

        public void UpdateSelection(List<ISelectable> selection)
        {
            // Remove current properties
            var propertyItemCount = m_PropertyContainer.childCount;
            for(int i = 0; i < propertyItemCount; i++)
                m_PropertyContainer.RemoveAt(0);

            if(selection.Count == 0)
            {
                SetSelectionToGraph();
                return;
            }

            if(selection.Count > 1)
            {
                m_ContextTitle.text = $"{selection.Count} Objects.";
                var sheet = new PropertySheet();
                sheet.Add(new PropertyRow(new Label("Multi-editing not supported.")));
                m_PropertyContainer.Add(sheet);
                return;
            }

            //if (selection.FirstOrDefault() is IInspectable inspectable)
            //{
            //    m_ContextTitle.text = inspectable.displayName;
            //    m_PropertyContainer.Add(inspectable.GetInspectorContent());
            //}
            // These would require SG specific view implementations
            // For now just handle manually
            //else if(selection.FirstOrDefault() is UnityEditor.Experimental.GraphView.Edge edge)
            //{
            //    m_ContextTitle.text = "(Edge)";
            //}
            //else if(selection.FirstOrDefault() is UnityEditor.Experimental.GraphView.Group group)
            //{
            //    m_ContextTitle.text = "(Group)";
            //}

            var propertySheet = new PropertySheet();

            foreach (var selectable in selection)
            {
                // #TODO : Need to remove SG dependency here as VFX Graph has their own node structure
                var nodeView = (MaterialNodeView) selectable;

                if (nodeView == null)
                    return;
                var node = nodeView.node;

                PropertyInfo[] properties = node.GetType().GetProperties();

                foreach (var propertyInfo in properties)
                {
                    var attribute = propertyInfo.GetCustomAttribute<Inspectable>();
                    if (attribute == null)
                        return;
                    var propertyType = propertyInfo.PropertyType;

                    // Based on property type call the appropriate property type drawer
                    if (propertyType.IsEnum)
                    {
                        var enumPropertyDrawer = new EnumPropertyDrawer(
                            (Enum) propertyInfo.GetValue(node),
                            attribute._labelName,
                            (Enum) attribute._defaultValue);

                        var newPropertyRow = enumPropertyDrawer.CreatePropertyRowForField(newEnumValue =>
                            propertyInfo.GetSetMethod(true).Invoke(node, new object[] {newEnumValue}));
                        propertySheet.Add(newPropertyRow);
                    }
                }
            }

            m_PropertyContainer.Add(propertySheet);
            m_PropertyContainer.MarkDirtyRepaint();
        }

        void SetSelectionToGraph()
        {
            var graphEditorView = m_GraphView.GetFirstAncestorOfType<GraphEditorView>();
            if(graphEditorView == null)
                return;

            m_ContextTitle.text = $"{graphEditorView.assetName} (Graph)";

            // #TODO - Refactor
            var precisionField = new EnumField((Enum)m_GraphData.concretePrecision);
            precisionField.RegisterValueChangedCallback(evt =>
            {
                m_GraphData.owner.RegisterCompleteObjectUndo("Change Precision");
                if (m_GraphData.concretePrecision == (ConcretePrecision)evt.newValue)
                    return;

                m_GraphData.concretePrecision = (ConcretePrecision)evt.newValue;
                var nodeList = m_GraphView.Query<MaterialNodeView>().ToList();
                graphEditorView.colorManager.SetNodesDirty(nodeList);

                m_GraphData.ValidateGraph();
                graphEditorView.colorManager.UpdateNodeViews(nodeList);
                foreach (var node in m_GraphData.GetNodes<AbstractMaterialNode>())
                {
                    node.Dirty(ModificationScope.Graph);
                }
            });

            var sheet = new PropertySheet();
            sheet.Add(new PropertyRow(new Label("Precision")), (row) =>
            {
                row.Add(precisionField);
            });
            m_PropertyContainer.Add(sheet);
        }
#endregion

#region Preview
        void BuildPreview(VisualElement container)
        {
            m_PropertyContainer = new VisualElement { name = "propertyContainer" };
            container.Add(m_PropertyContainer);

            var previewTitleContainer = new VisualElement() { name = "previewTitleContainer" };
            {
                var titleLabel = new Label("Preview") { name = "previewTitleLabel" };
                previewTitleContainer.Add(titleLabel);
            }
            container.Add(previewTitleContainer);

            var previewContainer = new VisualElement { name = "previewContainer" };
            {
                CreatePreviewImage(Texture2D.blackTexture);
                previewContainer.Add(m_PreviewImage);
                previewContainer.style.height = 200;
            }
            container.Add(previewContainer);

            var draggable = new Draggable(s =>
            {
                previewContainer.style.height = new StyleLength(previewContainer.style.height.value.value - s.y);
            }, true);
            previewTitleContainer.AddManipulator(draggable);
        }

        void CreatePreviewImage(Texture texture)
        {
            if (m_PreviewRenderData?.texture != null)
                texture = m_PreviewRenderData.texture;

            m_PreviewImage = new Image { name = "previewImage", image = texture };

            // Manipulators
            var contextMenu = (IManipulator)Activator.CreateInstance(typeof(ContextualMenuManipulator), (Action<ContextualMenuPopulateEvent>)BuildPreviewContextMenu);
            m_PreviewImage.AddManipulator(contextMenu);
            m_PreviewImage.AddManipulator(new Scrollable(OnPreviewScroll));
            m_PreviewImage.AddManipulator(new Draggable(OnPreviewDrag, true));
            m_PreviewImage.RegisterCallback<GeometryChangedEvent>(OnPreviewGeometryChanged);
        }

        void ChangePreviewMesh(Mesh mesh)
        {
            m_GraphData.outputNode.Dirty(ModificationScope.Node);

            if (m_GraphData.previewData.serializedMesh.mesh != mesh)
            {
                m_GraphData.previewData.rotation = Quaternion.identity;
                m_PreviewScrollPosition = Vector2.zero;
                m_GraphData.previewData.serializedMesh.mesh = mesh;
            }
        }

        void BuildPreviewContextMenu(ContextualMenuPopulateEvent evt)
        {
            foreach (var primitiveTypeName in Enum.GetNames(typeof(PrimitiveType)))
            {
                evt.menu.AppendAction(primitiveTypeName, e =>
                {
                    Mesh mesh = Resources.GetBuiltinResource(typeof(Mesh), string.Format("{0}.fbx", primitiveTypeName)) as Mesh;
                    ChangePreviewMesh(mesh);
                }, DropdownMenuAction.AlwaysEnabled);
            }

            // #TODO - COMMENTS PLEASE FOR THE LOVE OF GOD
            // #TODO - Doesn't return an actual value, returns null
            // #TODO - Uses reflection to access methods/properties of an internal class
            MethodInfo test = s_ObjectSelector.GetMethod("Show",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, Type.DefaultBinder,
                new[] {typeof(UnityEngine.Object), typeof(Type), typeof(SerializedProperty), typeof(bool), typeof(List<int>), typeof(Action<UnityEngine.Object>), typeof(Action<UnityEngine.Object>)},
                new ParameterModifier[7]);

            test.Invoke(GetMeshSelectionWindow(), new object[] { null, typeof(Mesh), null, false, null, (Action<UnityEngine.Object>)OnPreviewMeshChanged, (Action<UnityEngine.Object>)OnPreviewMeshChanged });

            evt.menu.AppendAction("Custom Mesh", e =>
            {
                MethodInfo ShowMethod = s_ObjectSelector.GetMethod("Show", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, Type.DefaultBinder, new[] {typeof(UnityEngine.Object), typeof(Type), typeof(SerializedProperty), typeof(bool), typeof(List<int>), typeof(Action<UnityEngine.Object>), typeof(Action<UnityEngine.Object>)}, new ParameterModifier[7]);
                m_PreviousPreviewMesh = m_GraphData.previewData.serializedMesh.mesh;
                ShowMethod.Invoke(GetMeshSelectionWindow(), new object[] { null, typeof(Mesh), null, false, null, (Action<UnityEngine.Object>)OnPreviewMeshChanged, (Action<UnityEngine.Object>)OnPreviewMeshChanged });
            });
        }

        EditorWindow GetMeshSelectionWindow()
        {
            PropertyInfo P = s_ObjectSelector.GetProperty("get", BindingFlags.Public | BindingFlags.Static);
            return P.GetValue(null, null) as EditorWindow;
        }

        void OnPreviewMeshChanged(UnityEngine.Object obj)
        {
            var mesh = obj as Mesh;
            if (mesh == null)
                mesh = m_PreviousPreviewMesh;
            ChangePreviewMesh(mesh);
        }

        void OnPreviewChanged()
        {
            m_PreviewImage.image = m_PreviewRenderData?.texture ?? Texture2D.blackTexture;
            if (m_PreviewRenderData != null && m_PreviewRenderData.shaderData.isCompiling)
                m_PreviewImage.tintColor = new Color(1.0f, 1.0f, 1.0f, 0.3f);
            else
                m_PreviewImage.tintColor = Color.white;
            m_PreviewImage.MarkDirtyRepaint();
        }

        void OnPreviewScroll(float scrollValue)
        {
            var rescaleMultiplier = 0.03f;
            var rescaleAmount = -scrollValue * rescaleMultiplier;
            m_GraphData.previewData.scale = Mathf.Clamp(m_GraphData.previewData.scale + rescaleAmount, 0.2f, 5f);
            m_GraphData.outputNode.Dirty(ModificationScope.Node);
        }

        void OnPreviewDrag(Vector2 mouseDelta)
        {
            var previewSize = m_PreviewImage.contentRect.size;

            m_PreviewScrollPosition -= mouseDelta * (Event.current.shift ? 3f : 1f) / Mathf.Min(previewSize.x, previewSize.y) * 140f;
            m_PreviewScrollPosition.y = Mathf.Clamp(m_PreviewScrollPosition.y, -90f, 90f);
            Quaternion previewRotation = Quaternion.Euler(m_PreviewScrollPosition.y, 0, 0) * Quaternion.Euler(0, m_PreviewScrollPosition.x, 0);
            m_GraphData.previewData.rotation = previewRotation;
            m_GraphData.outputNode.Dirty(ModificationScope.Node);
        }

        void OnPreviewGeometryChanged(GeometryChangedEvent evt)
        {
            var currentWidth = m_PreviewRenderData?.texture != null ? m_PreviewRenderData.texture.width : -1;
            var currentHeight = m_PreviewRenderData?.texture != null ? m_PreviewRenderData.texture.height : -1;

            var targetWidth = Mathf.Max(1f, m_PreviewImage.contentRect.width);
            var targetHeight = Mathf.Max(1f, m_PreviewImage.contentRect.height);

            if (Mathf.Approximately(currentWidth, targetHeight) && Mathf.Approximately(currentHeight, targetWidth))
                return;

            m_PreviewManager.ResizeMasterPreview(new Vector2(targetWidth, targetHeight));
        }
#endregion
    }
}
