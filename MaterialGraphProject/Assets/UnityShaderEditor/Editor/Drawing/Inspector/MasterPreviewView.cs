using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing.Inspector
{
    public class MasterPreviewView : VisualElement
    {
        AbstractMaterialGraph m_Graph;

        PreviewRenderData m_PreviewRenderHandle;
        PreviewTextureView m_PreviewTextureView;

        Vector2 m_PreviewScrollPosition;
        ObjectField m_PreviewMeshPicker;

        MasterNode m_MasterNode;

        public MasterPreviewView(string assetName, PreviewManager previewManager, AbstractMaterialGraph graph)
        {
            m_Graph = graph;

            AddStyleSheetPath("Styles/MaterialGraph");

            m_PreviewRenderHandle = previewManager.masterRenderData;
            m_PreviewRenderHandle.onPreviewChanged += OnPreviewChanged;

            var topContainer = new VisualElement() { name = "top" };
            {
                var title = new Label(assetName + " master node preview") { name = "title" };
                topContainer.Add(title);
            }
            Add(topContainer);

            var middleContainer = new VisualElement {name = "middle"};
            {
                m_PreviewTextureView = new PreviewTextureView { name = "preview", image = Texture2D.blackTexture };
                m_PreviewTextureView.AddManipulator(new Draggable(OnMouseDragPreviwMesh, true));
                middleContainer.Add(m_PreviewTextureView);

                m_PreviewScrollPosition = new Vector2(0f, 0f);

                middleContainer.Add(m_PreviewTextureView);
            }
            Add(middleContainer);

            var bottomContainer = new VisualElement() { name = "bottom" };
            {
                m_PreviewMeshPicker = new ObjectField { name = "picker", objectType = typeof(Mesh) };
                m_PreviewMeshPicker.OnValueChanged(OnPreviewMeshChanged);

                bottomContainer.Add(m_PreviewMeshPicker);
            }
            Add(bottomContainer);
        }

        MasterNode masterNode
        {
            get { return m_PreviewRenderHandle.shaderData.node as MasterNode; }
        }

        void OnPreviewChanged()
        {
            m_PreviewTextureView.image = m_PreviewRenderHandle.texture ?? Texture2D.blackTexture;
            m_PreviewTextureView.Dirty(ChangeType.Repaint);
        }

        void OnPreviewMeshChanged(ChangeEvent<Object> changeEvent)
        {
            Mesh changedMesh = changeEvent.newValue as Mesh;

            masterNode.Dirty(ModificationScope.Node);

            if (m_Graph.previewData.serializedMesh.mesh != changedMesh)
            {
                m_Graph.previewData.rotation = Quaternion.identity;
            }

            m_Graph.previewData.serializedMesh.mesh = changedMesh;
        }

        void OnMouseDragPreviwMesh(Vector2 deltaMouse)
        {
            Vector2 previewSize = m_PreviewTextureView.contentRect.size;

            m_PreviewScrollPosition -= deltaMouse * (Event.current.shift ? 3f : 1f) / Mathf.Min(previewSize.x, previewSize.y) * 140f;
            m_PreviewScrollPosition.y = Mathf.Clamp(m_PreviewScrollPosition.y, -90f, 90f);
            Quaternion previewRotation = Quaternion.Euler(m_PreviewScrollPosition.y, 0, 0) * Quaternion.Euler(0, m_PreviewScrollPosition.x, 0);
            m_Graph.previewData.rotation = previewRotation;

            masterNode.Dirty(ModificationScope.Node);
        }
    }
}
