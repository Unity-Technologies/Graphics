using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing.Drawing;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    public abstract class AbstractMaterialGraphInspector : Editor
    {
        ScriptableObjectFactory<INode, AbstractNodeInspector, BasicNodeInspector> m_InspectorFactory;

        List<INode> m_SelectedNodes = new List<INode>();

        protected IEnumerable<INode> selectedNodes
        {
            get { return m_SelectedNodes; }
        }

        List<AbstractNodeInspector> m_Inspectors = new List<AbstractNodeInspector>();

        protected IGraphAsset graphAsset
        {
            get { return target as IGraphAsset; }
        }

        bool m_RequiresTime;

        protected GUIContent m_Title = new GUIContent();

        NodePreviewPresenter m_NodePreviewPresenter;

        AbstractMaterialNode m_PreviewNode;

        protected AbstractMaterialNode previewNode
        {
            get { return m_PreviewNode; }
            set
            {
                if (value == m_PreviewNode)
                    return;
                // ReSharper disable once DelegateSubtraction
                // This is only an issue when subtracting a list of callbacks
                // (which will subtract that specific subset, i.e. `{A B C} - {A B} = {C}`, but `{A B C} - {A C} -> {A B C}`)
                ForEachChild(m_PreviewNode, (node) => node.onModified -= OnPreviewNodeModified);
                m_PreviewNode = value;
                m_NodePreviewPresenter.Initialize(value);
                m_Title.text = m_PreviewNode.name;
                m_RequiresTime = false;
                ForEachChild(m_PreviewNode,
                    (node) =>
                    {
                        node.onModified += OnPreviewNodeModified;
                        m_RequiresTime |= node is IRequiresTime;
                    });
            }
        }

        protected AbstractMaterialGraphInspector()
        {
            m_InspectorFactory = new ScriptableObjectFactory<INode, AbstractNodeInspector, BasicNodeInspector>(new[]
            {
                new TypeMapping(typeof(AbstractSurfaceMasterNode), typeof(SurfaceMasterNodeInspector)),
                new TypeMapping(typeof(PropertyNode), typeof(PropertyNodeInspector)),
                new TypeMapping(typeof(SubGraphInputNode), typeof(SubgraphInputNodeInspector)),
                new TypeMapping(typeof(SubGraphOutputNode), typeof(SubgraphOutputNodeInspector))
            });
        }

        public override void OnInspectorGUI()
        {
            UpdateSelection();

            foreach (var inspector in m_Inspectors)
            {
                inspector.OnInspectorGUI();
            }
        }

        void UpdateSelection()
        {
            if (graphAsset == null)
                return;

            using (var nodes = ListPool<INode>.GetDisposable())
            {
                nodes.value.AddRange(graphAsset.drawingData.selection.Select(graphAsset.graph.GetNodeFromGuid));
                if (m_SelectedNodes == null || m_Inspectors.Any(i => i.node == null) || !nodes.value.SequenceEqual(m_SelectedNodes))
                    OnSelectionChanged(nodes.value);
            }
        }

        protected virtual void OnSelectionChanged(IEnumerable<INode> selectedNodes)
        {
            m_SelectedNodes.Clear();
            m_SelectedNodes.AddRange(selectedNodes);
            m_Inspectors.Clear();
            foreach (var node in m_SelectedNodes.OfType<SerializableNode>())
            {
                var inspector = m_InspectorFactory.Create(node);
                inspector.Initialize(node);
                m_Inspectors.Add(inspector);
            }
        }

        void OnPreviewNodeModified(INode node, ModificationScope scope)
        {
            m_NodePreviewPresenter.modificationScope = scope;
            Repaint();
        }

        public virtual void OnEnable()
        {
            m_NodePreviewPresenter = CreateInstance<NodePreviewPresenter>();
            UpdateSelection();
            previewNode = null;
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (m_PreviewNode != null)
                {
                    var size = Mathf.Min(r.width, r.height);
                    var image = m_NodePreviewPresenter.Render(new Vector2(size, size));
                    GUI.DrawTexture(r, image, ScaleMode.ScaleToFit);
                   // m_NodePreviewPresenter.modificationScope = ModificationScope.Node;
                }
                else
                {
                    EditorGUI.DropShadowLabel(r, "No node pinned");
                }
            }
        }

        public override void OnPreviewSettings()
        {
            GUI.enabled = selectedNodes.Count() <= 1 && selectedNodes.FirstOrDefault() != previewNode;
            if (GUILayout.Button("Pin selected", "preButton"))
                previewNode = selectedNodes.FirstOrDefault() as AbstractMaterialNode;
            GUI.enabled = true;
        }

        public override bool RequiresConstantRepaint()
        {
            return m_RequiresTime;
        }

        public override GUIContent GetPreviewTitle()
        {
            return m_Title;
        }

        void ForEachChild(INode node, Action<INode> action)
        {
            if (node == null)
                return;
            using (var childNodes = ListPool<INode>.GetDisposable())
            {
                NodeUtils.DepthFirstCollectNodesFromNode(childNodes.value, node);
                foreach (var childNode in childNodes.value)
                {
                    action(childNode);
                }
            }
        }
    }
}
