using System;
using System.Linq;
using UnityEditor.Graphing.Drawing;
using UnityEditor.MaterialGraph.Drawing.NodeInspectors;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    [CustomEditor(typeof(MaterialGraphAsset))]
    public class MaterialGraphInspector : AbstractGraphInspector
    {
        private AbstractMaterialNode m_PreviewNode;
        private NodePreviewDrawData m_NodePreviewPresenter;

        public override void OnEnable()
        {
            base.OnEnable();
            m_NodePreviewPresenter = CreateInstance<NodePreviewDrawData>();
        }

        protected override void AddTypeMappings(Action<Type, Type> map)
        {
            map(typeof(AbstractSurfaceMasterNode), typeof(SurfaceMasterNodeInspector));
        }

        protected override void UpdateInspectors()
        {
            base.UpdateInspectors();
            var newPreviewNode = m_Inspectors.Select(i => i.node).OfType<AbstractMaterialNode>().FirstOrDefault();
            if (newPreviewNode != m_PreviewNode)
            {
                m_PreviewNode = newPreviewNode;
                m_NodePreviewPresenter.Initialize(m_PreviewNode);
            }
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

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent(m_Inspectors.Select(i => i.node.name).FirstOrDefault());
        }
    }
}