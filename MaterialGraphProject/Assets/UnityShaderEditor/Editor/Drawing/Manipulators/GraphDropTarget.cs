using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class GraphDropTarget : Manipulator
    {
        AbstractMaterialGraph m_Graph;
        MaterialGraphView m_GraphView;

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

        void CreateNode(Object obj, Vector2 nodePosition)
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

                var node = new Texture2DNode();
                if (isNormalMap)
                    node.textureType = TextureType.Normal;

                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                m_Graph.AddNode(node);
                var inputslot = node.FindSlot<Texture2DInputMaterialSlot>(Texture2DNode.TextureInputId);
                if (inputslot != null)
                    inputslot.texture = texture2D;
            }

            var cubemap = obj as Cubemap;
            if (cubemap != null)
            {
                m_Graph.owner.RegisterCompleteObjectUndo("Drag Cubemap");
                var property = new CubemapShaderProperty { displayName = cubemap.name, value = { cubemap = cubemap } };
                m_Graph.AddShaderProperty(property);
                var node = new CubemapNode();
                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                m_Graph.AddNode(node);
                var inputslot = node.FindSlot<CubemapInputMaterialSlot>(CubemapNode.CubemapInputId);
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
        }

        void OnIMGUIEvent(IMGUIEvent evt)
        {
            if (evt.imguiEvent.type == EventType.DragUpdated || evt.imguiEvent.type == EventType.DragPerform)
            {
                var currentTarget = evt.currentTarget as VisualElement;
                if (currentTarget == null)
                    return;
                var objects = DragAndDrop.objectReferences;
                Object draggedObject = null;
                foreach (var obj in objects)
                {
                    if (ValidateObject(obj))
                    {
                        draggedObject = obj;
                        break;
                    }
                }
                if (draggedObject == null)
                    return;

//                Debug.LogFormat("{0}: {1}", draggedObject.GetType().Name, draggedObject.name);
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
}
