using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;
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

        public void Remove()
        {
            m_Index = -1;
        }

        VFXViewController m_ViewController;

        public int index
        {
            get { return m_Index; }
            set { m_Index = value; }
        }

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            if (m_Index == -1) return;

            NotifyChange(AnyThing);
        }

        public VFXGroupNodeController(VFXViewController viewController, VFXUI ui, int index) : base(ui)
        {
            m_UI = ui;
            m_Index = index;
            m_ViewController = viewController;
        }

        public Rect position
        {
            get
            {
                if (m_Index < 0)
                {
                    return Rect.zero;
                }
                return m_UI.groupInfos[m_Index].position;
            }
            set
            {
                if (m_Index < 0) return;
                m_UI.groupInfos[m_Index].position = value;
                m_ViewController.IncremenentGraphUndoRedoState(null, VFXModel.InvalidationCause.kUIChanged);
            }
        }
        public string title
        {
            get
            {
                if (m_Index < 0)
                {
                    return "";
                }
                return m_UI.groupInfos[m_Index].title;
            }
            set
            {
                if (title != value && m_Index >= 0)
                {
                    m_UI.groupInfos[m_Index].title = value;
                    m_ViewController.IncremenentGraphUndoRedoState(null, VFXModel.InvalidationCause.kUIChanged);
                }
            }
        }

        public override void ApplyChanges()
        {
            if (m_Index == -1) return;

            ModelChanged(model);
        }

        public IEnumerable<VFXNodeController> nodes
        {
            get
            {
                if (m_Index == -1) return Enumerable.Empty<VFXNodeController>();

                if (m_UI.groupInfos[m_Index].content != null)
                    return m_UI.groupInfos[m_Index].content.Where(t => t != null).Select(t => m_ViewController.GetControllerFromModel(t)).Where(t => t != null);
                return new VFXNodeController[0];
            }
            set { m_UI.groupInfos[m_Index].content = value.Select(t => t.model).ToArray(); }
        }


        public void AddNode(VFXNodeController controller)
        {
            if (controller == null || m_Index < 0)
                return;

            if (m_UI.groupInfos[m_Index].content != null)
                m_UI.groupInfos[m_Index].content = m_UI.groupInfos[m_Index].content.Concat(Enumerable.Repeat(controller.model, 1)).Distinct().ToArray();
            else
                m_UI.groupInfos[m_Index].content = new VFXModel[] { controller.model };
            m_ViewController.IncremenentGraphUndoRedoState(null, VFXModel.InvalidationCause.kUIChanged);


            VFXUI ui = VFXMemorySerializer.DuplicateObjects(new ScriptableObject[] {model})[0] as VFXUI;


            if (ui.groupInfos.Length > 0)
            {
                Debug.Log("toto");
            }
        }

        public void RemoveNode(VFXNodeController controller)
        {
            if (controller == null || m_Index < 0)
                return;

            if (m_UI.groupInfos[m_Index].content != null)
                m_UI.groupInfos[m_Index].content = m_UI.groupInfos[m_Index].content.Where(t => t != controller.model).ToArray();
            m_ViewController.IncremenentGraphUndoRedoState(null, VFXModel.InvalidationCause.kUIChanged);
        }

        public bool ContainsNode(VFXNodeController controller)
        {
            if (m_Index == -1) return false;
            if (m_UI.groupInfos[m_Index].content != null)
            {
                return m_UI.groupInfos[m_Index].content.Contains(controller.model);
            }
            return false;
        }
    }
}
