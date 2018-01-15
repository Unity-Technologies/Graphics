using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using System;

namespace UnityEditor.VFX.UI
{
    class VFXGroupNodeController : Controller<VFXUI>
    {
        [SerializeField]
        int m_Index;

        [SerializeField]
        VFXUI m_UI;

        protected void OnEnable()
        {
        }

        VFXViewController m_ViewController;

        public int index
        {
            get { return m_Index; }
            set { m_Index = value; }
        }

        protected override void ModelChanged(UnityEngine.Object obj)
        {
        }

        public VFXGroupNodeController(VFXViewController viewController, VFXUI ui, int index) : base(ui)
        {
            m_UI = ui;
            m_Index = index;
            m_ViewController = viewController;
        }

        public Rect position
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
                    m_UI.groupInfos[m_Index].title = value;
                }
            }
        }

        public override void ApplyChanges()
        {
            ModelChanged(model);
        }

        public IEnumerable<VFXNodeController> nodes
        {
            get
            {
                if (m_UI.groupInfos[m_Index].content != null)
                    return m_UI.groupInfos[m_Index].content.Select(t => m_ViewController.GetControllerFromModel(t));
                return new VFXNodeController[0];
            }
            set { m_UI.groupInfos[m_Index].content = value.Select(t => t.model).ToArray(); }
        }


        public void AddNode(VFXNodeController controller)
        {
            if (controller == null)
                return;

            if (m_UI.groupInfos[m_Index].content != null)
                m_UI.groupInfos[m_Index].content = m_UI.groupInfos[m_Index].content.Concat(Enumerable.Repeat(controller.model, 1)).Distinct().ToArray();
            else
                m_UI.groupInfos[m_Index].content = new VFXModel[] { controller.model };
        }

        public void RemoveNode(VFXNodeController controller)
        {
            if (controller == null)
                return;

            if (m_UI.groupInfos[m_Index].content != null)
                m_UI.groupInfos[m_Index].content = m_UI.groupInfos[m_Index].content.Where(t => t != controller.model).ToArray();
        }

        public bool ContainsNode(VFXNodeController controller)
        {
            if (m_UI.groupInfos[m_Index].content != null)
            {
                return m_UI.groupInfos[m_Index].content.Contains(controller.model);
            }
            return false;
        }
    }
}
