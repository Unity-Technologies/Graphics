using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Reflection;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXStickyNoteController : VFXUIController<VFXUI.StickyNoteInfo>
    {
        public VFXStickyNoteController(VFXViewController viewController, VFXUI ui, int index) : base(viewController,ui,index)
        {
        }
        public string contents
        {
            get{
                if( m_Index < 0) return "";

                return m_UI.stickyNoteInfos[m_Index].contents;
            }
            set
            {
                if( m_Index < 0) return;

                m_UI.stickyNoteInfos[m_Index].contents = value;

                m_ViewController.IncremenentGraphUndoRedoState(null, VFXModel.InvalidationCause.kUIChanged);
            }
        }
        override protected VFXUI.StickyNoteInfo[] infos{get{return m_UI.stickyNoteInfos;}}
        public string theme
        {
            get{
                return m_UI.stickyNoteInfos[m_Index].theme;
            }
            set
            {
                m_UI.stickyNoteInfos[m_Index].theme = value;
                m_ViewController.IncremenentGraphUndoRedoState(null, VFXModel.InvalidationCause.kUIChanged);
            }
        }
        public string textSize
        {
            get{
                return m_UI.stickyNoteInfos[m_Index].textSize;
            }
            set
            {
                m_UI.stickyNoteInfos[m_Index].textSize = value;
                m_ViewController.IncremenentGraphUndoRedoState(null, VFXModel.InvalidationCause.kUIChanged);
            }
        }
    }

    class VFXStickyNote : StickyNote, IControlledElement<VFXStickyNoteController>, IVFXMovable,IVFXResizable
    {
        public void OnMoved()
        {
            controller.position = new Rect(style.positionLeft,style.positionTop,style.width,style.height);
        }

        public void OnResized()
        {
            controller.position = new Rect(style.positionLeft,style.positionTop,style.width,style.height);
        }

        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXStickyNoteController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != null)
                {
                    m_Controller.UnregisterHandler(this);
                }
                m_Controller = value;
                if (m_Controller != null)
                {
                    m_Controller.RegisterHandler(this);
                }
            }
        }

        VFXStickyNoteController m_Controller;
        public VFXStickyNote() : base(Vector2.zero)
        {
            this.RegisterCallback<StickyNodeChangeEvent>(OnUIChange);
            this.RegisterCallback<ControllerChangedEvent>(OnControllerChange);
        }

        void OnUIChange(StickyNodeChangeEvent e)
        {
            if( m_Controller == null) return;

            switch(e.change)
            {
                case StickyNodeChangeEvent.Change.title:
                    m_Controller.title = title;
                break;
                case StickyNodeChangeEvent.Change.contents:
                    m_Controller.contents = contents;
                break;
                case StickyNodeChangeEvent.Change.theme:
                    m_Controller.theme = theme.ToString();
                break;
                case StickyNodeChangeEvent.Change.textSize:
                    m_Controller.textSize = textSize.ToString();
                break;
            }

            e.StopPropagation();
        }
        void OnControllerChange(ControllerChangedEvent e)
        {
            title = controller.title;
            contents = controller.contents;

            if( ! string.IsNullOrEmpty(controller.theme))
            {
                try
                {
                    theme = (Theme)System.Enum.Parse(typeof(Theme),controller.theme,true);
                }
                catch(System.ArgumentException)
                {
                    controller.theme = Theme.Classic.ToString();
                    Debug.LogError("Unknown theme name");
                }
            }

            if( ! string.IsNullOrEmpty(controller.textSize))
            {
                try
                {
                    textSize = (TextSize)System.Enum.Parse(typeof(TextSize),controller.textSize,true);
                }
                catch(System.ArgumentException)
                {
                    controller.theme = TextSize.small.ToString();
                    Debug.LogError("Unknown text size name");
                }
            }

            //SetPosition(controller.position);

            style.positionLeft = controller.position.x;
            style.positionTop = controller.position.y;

            style.width = controller.position.width;
            style.height = controller.position.height;
        }
    }
}
