using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class GraphDropTarget : Manipulator
    {
        AbstractMaterialGraph m_Graph;
        MaterialGraphView m_GraphView;
        List<ISelectable> m_Selection;

        public GraphDropTarget(AbstractMaterialGraph graph)
        {
            m_Graph = graph;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            m_GraphView = target as MaterialGraphView;
            target.RegisterCallback<IMGUIEvent>(OnIMGUIEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<IMGUIEvent>(OnIMGUIEvent);
        }

        bool ValidateObject(Object obj)
        {
            return EditorUtility.IsPersistent(obj)
                && (obj is Texture2D || obj is Cubemap || obj is MaterialSubGraphAsset);
        }

        void CreateNode(object obj, Vector2 nodePosition)
        {
            var texture2D = obj as Texture2D;
            if (texture2D != null)
            {
                m_Graph.owner.RegisterCompleteObjectUndo("Drag Texture");

                bool isNormalMap = false;
                if (EditorUtility.IsPersistent(texture2D)
                    && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture2D)))
                {
                    var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture2D)) as TextureImporter;
                    if (importer != null)
                        isNormalMap = importer.textureType == TextureImporterType.NormalMap;
                }

                var node = new SampleTexture2DNode();
                if (isNormalMap)
                    node.textureType = TextureType.Normal;

                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                m_Graph.AddNode(node);
                var inputslot = node.FindSlot<Texture2DInputMaterialSlot>(SampleTexture2DNode.TextureInputId);
                if (inputslot != null)
                    inputslot.texture = texture2D;
            }

            var cubemap = obj as Cubemap;
            if (cubemap != null)
            {
                m_Graph.owner.RegisterCompleteObjectUndo("Drag Cubemap");
                var property = new CubemapShaderProperty { displayName = cubemap.name, value = { cubemap = cubemap } };
                m_Graph.AddShaderProperty(property);
                var node = new SampleCubemapNode();
                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                m_Graph.AddNode(node);
                var inputslot = node.FindSlot<CubemapInputMaterialSlot>(SampleCubemapNode.CubemapInputId);
                if (inputslot != null)
                    inputslot.cubemap = cubemap;
            }

            var subGraphAsset = obj as MaterialSubGraphAsset;
            if (subGraphAsset != null)
            {
                m_Graph.owner.RegisterCompleteObjectUndo("Drag Sub-Graph");
                var node = new SubGraphNode();
                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                node.subGraphAsset = subGraphAsset;
                m_Graph.AddNode(node);
            }

            var blackboardField = obj as BlackboardField;
            if (blackboardField != null)
            {
                var property = blackboardField.userData as IShaderProperty;
                if (property != null)
                {
                    m_Graph.owner.RegisterCompleteObjectUndo("Drag Shader Property");
                    var node = new PropertyNode();
                    var drawState = node.drawState;
                    drawState.position = new Rect(nodePosition, drawState.position.size);
                    node.drawState = drawState;
                    m_Graph.AddNode(node);
                    node.propertyGuid = property.guid;
                }
            }
        }

        void OnIMGUIEvent(IMGUIEvent evt)
        {
            if (evt.imguiEvent.type == EventType.DragUpdated || evt.imguiEvent.type == EventType.DragPerform)
            {
                try
                {
                    var currentTarget = evt.currentTarget as VisualElement;
                    if (currentTarget == null)
                        return;
                    var pickElement = currentTarget.panel.Pick(target.LocalToWorld(evt.imguiEvent.mousePosition));
                    if (m_Selection == null)
                        m_Selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
                    if (!(pickElement is MaterialGraphView))
                        return;
                    var objects = DragAndDrop.objectReferences;

                    if (m_Selection != null)
                    {
                        // Handle drop of UIElements
                        if (m_Selection.OfType<BlackboardField>().Count() != 1)
                        {
                            m_Selection = null;
                            return;
                        }
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        if (evt.imguiEvent.type == EventType.DragPerform)
                        {
                            var nodePosition = m_GraphView.contentViewContainer.transform.matrix.inverse.MultiplyPoint3x4(m_GraphView.panel.visualTree.ChangeCoordinatesTo(m_GraphView, Event.current.mousePosition));
                            CreateNode(m_Selection.First(), nodePosition);
                            DragAndDrop.AcceptDrag();
                        }
                    }
                    else
                    {
                        // Handle drop of Unity objects
                        Object draggedObject = null;
                        foreach (var obj in objects)
                        {
                            if (ValidateObject(obj))
                            {
                                draggedObject = obj;
                                break;
                            }
                        }
                        if (draggedObject != null)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                            if (evt.imguiEvent.type == EventType.DragPerform)
                            {
                                var nodePosition = m_GraphView.contentViewContainer.transform.matrix.inverse.MultiplyPoint3x4(m_GraphView.panel.visualTree.ChangeCoordinatesTo(m_GraphView, Event.current.mousePosition));
                                CreateNode(draggedObject, nodePosition);
                                DragAndDrop.AcceptDrag();
                            }
                        }
                    }
                }
                finally
                {
                    if (evt.imguiEvent.type == EventType.DragPerform || evt.imguiEvent.type == EventType.DragExited)
                        m_Selection = null;
                }

            }
        }
    }
}
