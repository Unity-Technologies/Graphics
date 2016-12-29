using System;
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

        [SerializeField]
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

        private AbstractMaterialNode m_SelectedNode;

        [SerializeField]
        private NodePreviewDrawData m_NodePreviewPresenter;

        [SerializeField]
        private bool m_NodePinned;

        private UnityEngine.MaterialGraph.MaterialGraph m_MaterialGraph;

        private void OnPreviewNodeModified(INode node, ModificationScope scope)
        {
            m_NodePreviewPresenter.modificationScope = scope;
            Repaint();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            m_NodePreviewPresenter = CreateInstance<NodePreviewDrawData>();
            if (m_GraphAsset != null)
                m_MaterialGraph = m_GraphAsset.graph as UnityEngine.MaterialGraph.MaterialGraph;
        }

        protected override void AddTypeMappings(Action<Type, Type> map)
        {
            map(typeof(AbstractSurfaceMasterNode), typeof(SurfaceMasterNodeInspector));
        }

        protected override void UpdateInspectors()
        {
            base.UpdateInspectors();

            m_SelectedNode = m_Inspectors.Select(i => i.node).OfType<AbstractMaterialNode>().FirstOrDefault();

            if (m_MaterialGraph == null || m_NodePinned)
                return;

            previewNode = m_SelectedNode ?? m_MaterialGraph.masterNode as AbstractMaterialNode;
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
                var previewDimension = new Vector2(size, size);
                var previewPosition = new Vector2(r.x + r.width*0.5f - size*0.5f, r.y + r.height*0.5f - size*0.5f);
                var image = m_NodePreviewPresenter.Render(previewDimension);
                GUI.DrawTexture(new Rect(previewPosition, previewDimension), image);
                m_NodePreviewPresenter.modificationScope = ModificationScope.Node;
            }
        }

        public override void OnPreviewSettings()
        {
            if (m_Inspectors.Any() && !(m_NodePinned && m_SelectedNode == m_PreviewNode))
            {
                if (GUILayout.Button("Pin selected", "preButton"))
                {
                    previewNode = m_SelectedNode;
                    m_NodePinned = true;
                }
            }
            else if (!m_NodePinned)
            {
                if (GUILayout.Button("Pin", "preButton"))
                    m_NodePinned = true;
            }

            if (m_NodePinned && GUILayout.Button("Unpin", "preButton"))
                m_NodePinned = false;
        }

        public override bool RequiresConstantRepaint()
        {
            return m_RequiresTime;
        }

        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent(m_PreviewNode.name);
        }
    }
}