using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    [CustomEditor(typeof(MaterialGraphAsset))]
    public class MaterialGraphInspector : AbstractGraphInspector
    {
        private bool m_RequiresTime;

        private AbstractMaterialNode m_PreviewNode;

        private AbstractMaterialNode previewNode
        {
            get { return m_PreviewNode; }
            set
            {
                if (value == m_PreviewNode)
                    return;
                ForEachChild(m_PreviewNode, (node) => node.onModified -= OnPreviewNodeModified);
                m_PreviewNode = value;
                m_NodePreviewPresenter.Initialize(value);
                m_RequiresTime = false;
                ForEachChild(m_PreviewNode,
                             (node) =>
                             {
                                 node.onModified += OnPreviewNodeModified;
                                 m_RequiresTime |= node is IRequiresTime;
                             });
            }
        }

        private NodePreviewDrawData m_NodePreviewPresenter;

        private UnityEngine.MaterialGraph.MaterialGraph materialGraph
        {
            get { return m_GraphAsset.graph as UnityEngine.MaterialGraph.MaterialGraph; }
        }

        private void OnPreviewNodeModified(INode node, ModificationScope scope)
        {
            m_NodePreviewPresenter.modificationScope = scope;
            Repaint();
        }

        public override void OnEnable()
        {
            m_NodePreviewPresenter = CreateInstance<NodePreviewDrawData>();
            base.OnEnable();
            previewNode = materialGraph.masterNode as AbstractMaterialNode;
        }

        protected override void AddTypeMappings(Action<Type, Type> map)
        {
            map(typeof(AbstractSurfaceMasterNode), typeof(SurfaceMasterNodeInspector));
        }

        public override bool HasPreviewGUI()
        {
            return m_PreviewNode != null;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type == EventType.Repaint)
            {
                var size = Mathf.Min(r.width, r.height);
                var image = m_NodePreviewPresenter.Render(new Vector2(size, size));
                GUI.DrawTexture(r, image, ScaleMode.ScaleToFit);
                m_NodePreviewPresenter.modificationScope = ModificationScope.Node;
            }
        }

        public override void OnPreviewSettings()
        {
            GUI.enabled = m_SelectedNodes.Count == 1 && m_SelectedNodes.FirstOrDefault() != previewNode;
            if (GUILayout.Button("Pin selected", "preButton"))
                previewNode = m_SelectedNodes.FirstOrDefault() as AbstractMaterialNode;
            GUI.enabled = true;
        }

        public override bool RequiresConstantRepaint()
        {
            return m_RequiresTime;
        }

        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent(m_PreviewNode.name);
        }

        private void ForEachChild(INode node, Action<INode> action)
        {
            if (node == null)
                return;
            var childNodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(childNodes, node);
            foreach (var childNode in childNodes)
            {
                action(childNode);
            }
            ListPool<INode>.Release(childNodes);
        }
    }
}