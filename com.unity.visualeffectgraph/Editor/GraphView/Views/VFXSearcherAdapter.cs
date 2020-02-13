using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Searcher;


using UnityObject = UnityEngine.Object;
using PositionType = UnityEngine.UIElements.Position;


namespace UnityEditor.VFX.UI
{
    class VFXSearcherAdapter : SearcherAdapter
    {
        protected VFXView m_View;
        protected VisualElement m_NodeShape;
        protected VFXNodeController m_Controller;
        protected VFXNodeUI m_Node;
        protected VisualElement m_GlassPane;

        protected string m_DragType;
        protected object m_DragObject;


        public bool hasModel
        {
            get { return m_Controller != null && m_Controller.model != null; }
        }

        public VFXModel model
        {
            get { return m_Controller.model; }
        }

        public VFXSearcherAdapter(string title, VFXView view) : base(title) { m_View = view; }

        public override void InitDetailsPanel(VisualElement detailsPanel)
        {
            m_NodeShape = new VisualElement();
            m_NodeShape.style.alignItems = Align.Center;
            m_NodeShape.style.justifyContent = Justify.Center;
            m_NodeShape.style.overflow = Overflow.Hidden;
            detailsPanel.Add(m_NodeShape);

            m_GlassPane = new VisualElement();
            m_GlassPane.style.position = PositionType.Absolute;
            m_GlassPane.style.top = 0;
            m_GlassPane.style.left = 0;
            m_GlassPane.style.bottom = 0;
            m_GlassPane.style.right = 0;
            m_GlassPane.RegisterCallback<MouseDownEvent>(OnMouseDown);

            base.InitDetailsPanel(detailsPanel);
        }

        protected virtual void OnMouseDown(MouseDownEvent e)
        {
            DragAndDrop.StartDrag("Node");
            DragAndDrop.SetGenericData(m_DragType, m_DragObject);
        }

        public override void OnSelectionChanged(IEnumerable<SearcherItem> items)
        {
            base.OnSelectionChanged(items);
            ReleaseExample();
        }

        public virtual void ReleaseExample()
        {
            if (m_Node != null)
            {
                var model = m_Controller.model;
                m_Node.RemoveFromHierarchy();
                m_Controller.OnDisable();

                m_Node = null;
                m_Controller = null;

                UnityObject.DestroyImmediate(model);
            }
        }
    }
}
