using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    class VFXGroupNodePresenter : GraphElementPresenter
    {
        [SerializeField]
        int m_Index;

        [SerializeField]
        VFXUI m_UI;


        public override Object[] GetObjectsToWatch()
        {
            return new Object[] { this, m_UI };
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            hideFlags = HideFlags.HideAndDontSave;
            capabilities |= Capabilities.Deletable;
        }

        VFXViewPresenter m_ViewPresenter;

        public int index
        {
            get { return m_Index; }
            set { m_Index = value; }
        }


        public void Init(VFXViewPresenter viewPresenter, VFXUI ui, int index)
        {
            m_UI = ui;
            m_Index = index;
            m_ViewPresenter = viewPresenter;
        }

        public override Rect position
        {
            get { return m_UI.groupInfos[m_Index].position; }
            set { m_UI.groupInfos[m_Index].position = value; }
        }
        public string title
        {
            get { return m_UI.groupInfos[m_Index].title; }
            set
            {
                if (title != value)
                {
                    Undo.RecordObject(m_UI, "VFX");
                    m_UI.groupInfos[m_Index].title = value;
                }
            }
        }

        public IEnumerable<VFXNodePresenter> nodes
        {
            get
            {
                if (m_UI.groupInfos[m_Index].content != null)
                    return m_UI.groupInfos[m_Index].content.Select(t => m_ViewPresenter.GetPresenterFromModel(t));
                return new VFXNodePresenter[0];
            }
            //set { m_UI.groupInfos[m_Index].content = value.Select(t=> t.model).ToArray(); }
        }


        public void AddNode(VFXNodePresenter presenter)
        {
            if (presenter == null)
                return;

            Undo.RecordObject(m_UI, "VFX");
            if (m_UI.groupInfos[m_Index].content != null)
                m_UI.groupInfos[m_Index].content = m_UI.groupInfos[m_Index].content.Concat(Enumerable.Repeat(presenter.model, 1)).Distinct().ToArray();
            else
                m_UI.groupInfos[m_Index].content = new VFXModel[] { presenter.model };
        }

        public void RemoveNode(VFXNodePresenter presenter)
        {
            if (presenter == null)
                return;

            Undo.RecordObject(m_UI, "VFX");
            if (m_UI.groupInfos[m_Index].content != null)
                m_UI.groupInfos[m_Index].content = m_UI.groupInfos[m_Index].content.Where(t => t != presenter.model).ToArray();
        }
    }
}
