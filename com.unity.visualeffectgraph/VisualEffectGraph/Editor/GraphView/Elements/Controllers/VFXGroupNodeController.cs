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
    abstract class VFXUIController<T> : Controller<VFXUI> where T : VFXUI.UIInfo
    {
        protected int m_Index;

        protected VFXUI m_UI;

        public void Remove()
        {
            m_Index = -1;
        }

        abstract protected T[] infos{get;}

        protected VFXViewController m_ViewController;

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

        public VFXUIController(VFXViewController viewController, VFXUI ui, int index) : base(ui)
        {
            m_UI = ui;
            m_Index = index;
            m_ViewController = viewController;
        }

        protected void ValidateRect(ref Rect r)
        {
            if (float.IsInfinity(r.x) || float.IsNaN(r.x))
            {
                r.x = 0;
            }
            if (float.IsInfinity(r.y) || float.IsNaN(r.y))
            {
                r.y = 0;
            }
            if (float.IsInfinity(r.width) || float.IsNaN(r.width))
            {
                r.width = 100;
            }
            if (float.IsInfinity(r.height) || float.IsNaN(r.height))
            {
                r.height = 100;
            }
        }

        public Rect position
        {
            get
            {
                if (m_Index < 0)
                {
                    return Rect.zero;
                }
                Rect result = infos[m_Index].position;
                ValidateRect(ref result);
                return result;
            }
            set
            {
                if (m_Index < 0) return;

                ValidateRect(ref value);

                infos[m_Index].position = value;
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
                return infos[m_Index].title;
            }
            set
            {
                if (title != value && m_Index >= 0)
                {
                    infos[m_Index].title = value;
                    m_ViewController.IncremenentGraphUndoRedoState(null, VFXModel.InvalidationCause.kUIChanged);
                }
            }
        }

        public override void ApplyChanges()
        {
            if (m_Index == -1) return;

            ModelChanged(model);
        }
    }

    class VFXGroupNodeController : VFXUIController<VFXUI.GroupInfo>
    {
        public VFXGroupNodeController(VFXViewController viewController, VFXUI ui, int index) : base(viewController,ui,index)
        {
        }

        public IEnumerable<VFXNodeController> nodes
        {
            get
            {
                if (m_Index == -1) return Enumerable.Empty<VFXNodeController>();

                if (m_UI.groupInfos[m_Index].contents != null)
                    return m_UI.groupInfos[m_Index].contents.Where(t => t.model != null).Select(t => m_ViewController.GetControllerFromModel(t.model, t.id)).Where(t => t != null);
                return new VFXNodeController[0];
            }
            set { m_UI.groupInfos[m_Index].contents = value.Select(t => new VFXNodeID(t.model, t.id)).ToArray(); }
        }


        override protected VFXUI.GroupInfo[] infos{get{return m_UI.groupInfos;}}

        public void AddNode(VFXNodeController controller)
        {
            if (controller == null || m_Index < 0)
                return;

            if (m_UI.groupInfos[m_Index].contents != null)
                m_UI.groupInfos[m_Index].contents = m_UI.groupInfos[m_Index].contents.Concat(Enumerable.Repeat(new VFXNodeID(controller.model, controller.id), 1)).Distinct().ToArray();
            else
                m_UI.groupInfos[m_Index].contents = new VFXNodeID[] { new VFXNodeID(controller.model, controller.id) };
            m_ViewController.IncremenentGraphUndoRedoState(null, VFXModel.InvalidationCause.kUIChanged);

            VFXUI ui = VFXMemorySerializer.DuplicateObjects(new ScriptableObject[] {model})[0] as VFXUI;
        }

        public void RemoveNode(VFXNodeController controller)
        {
            if (controller == null || m_Index < 0)
                return;

            if (m_UI.groupInfos[m_Index].contents != null)
                m_UI.groupInfos[m_Index].contents = m_UI.groupInfos[m_Index].contents.Where(t => t.model != controller.model || t.id != controller.id).ToArray();
            m_ViewController.IncremenentGraphUndoRedoState(null, VFXModel.InvalidationCause.kUIChanged);
        }

        public bool ContainsNode(VFXNodeController controller)
        {
            if (m_Index == -1) return false;
            if (m_UI.groupInfos[m_Index].contents != null)
            {
                return m_UI.groupInfos[m_Index].contents.Contains(new VFXNodeID(controller.model, controller.id));
            }
            return false;
        }
    }
}
