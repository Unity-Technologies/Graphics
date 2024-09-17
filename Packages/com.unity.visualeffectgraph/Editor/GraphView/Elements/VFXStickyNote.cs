using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXStickyNoteController : VFXUIController<VFXUI.StickyNoteInfo>
    {
        public VFXStickyNoteController(VFXViewController viewController, VFXUI ui, int index) : base(viewController, ui, index)
        {
        }

        public string contents
        {
            get
            {
                if (m_Index < 0) return "";

                return m_UI.stickyNoteInfos[m_Index].contents;
            }
            set
            {
                if (m_Index < 0) return;

                m_UI.stickyNoteInfos[m_Index].contents = value;

                Modified();
            }
        }
        override protected VFXUI.StickyNoteInfo[] infos { get { return m_UI.stickyNoteInfos; } }
        public string theme
        {
            get
            {
                return m_UI.stickyNoteInfos[m_Index].theme;
            }
            set
            {
                m_UI.stickyNoteInfos[m_Index].theme = value;
                Modified();
            }
        }
        public string fontSize
        {
            get
            {
                return m_UI.stickyNoteInfos[m_Index].textSize;
            }
            set
            {
                m_UI.stickyNoteInfos[m_Index].textSize = value;
                Modified();
            }
        }
    }

    class VFXStickyNote : StickyNote, IControlledElement<VFXStickyNoteController>, IVFXMovable
    {
        public void OnMoved()
        {
            controller.position = new Rect(resolvedStyle.left, resolvedStyle.top, resolvedStyle.width, resolvedStyle.height);
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
            styleSheets.Add(VFXView.LoadStyleSheet("VFXStickynote"));
            this.RegisterCallback<StickyNoteChangeEvent>(OnUIChange);
        }

        void OnUIChange(StickyNoteChangeEvent e)
        {
            if (m_Controller == null) return;

            switch (e.change)
            {
                case StickyNoteChange.Title:
                    controller.title = title;
                    break;
                case StickyNoteChange.Contents:
                    controller.contents = contents;
                    break;
                case StickyNoteChange.Theme:
                    controller.theme = theme.ToString();
                    break;
                case StickyNoteChange.FontSize:
                    controller.fontSize = fontSize.ToString();
                    break;
                case StickyNoteChange.Position:
                    controller.position = new Rect(resolvedStyle.left, resolvedStyle.top, style.width.value.value, style.height.value.value);
                    break;
            }
        }

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            title = controller.title;
            contents = controller.contents;

            if (!string.IsNullOrEmpty(controller.theme))
            {
                try
                {
                    theme = (StickyNoteTheme)System.Enum.Parse(typeof(StickyNoteTheme), controller.theme, true);
                }
                catch (System.ArgumentException)
                {
                    controller.theme = StickyNoteTheme.Classic.ToString();
                    Debug.LogError("Unknown theme name");
                }
            }

            if (!string.IsNullOrEmpty(controller.fontSize))
            {
                try
                {
                    fontSize = (StickyNoteFontSize)System.Enum.Parse(typeof(StickyNoteFontSize), controller.fontSize, true);
                }
                catch (System.ArgumentException)
                {
                    controller.theme = StickyNoteFontSize.Medium.ToString();
                    Debug.LogError("Unknown text size name");
                }
            }

            SetPosition(controller.position);
        }
    }
}
