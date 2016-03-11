using UnityEngine;
using UnityEditor;
using System.Collections;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdCanvas : Canvas2D
    {
        public VFXEdNodeBlockDraggable SelectedNodeBlock
        {
            get
            {
                return m_SelectedNodeBlock;
            }
            set
            {
                SetSelectedNodeBlock(value);
            }
        }

        private VFXEdNodeBlockDraggable m_SelectedNodeBlock;

        public VFXEdCanvas(Object target, EditorWindow host, ICanvasDataSource dataSource)
            : base(target, host, dataSource)
        {

            // draggable manipulator allows to move the canvas around. Note that individual elements can have the draggable manipulator on themselves
            AddManipulator(new Draggable(2, EventModifiers.None));
            AddManipulator(new Draggable(0, EventModifiers.Alt));

            // make the canvas zoomable
            AddManipulator(new Zoomable(Zoomable.ZoomType.AroundMouse));

            // allow framing the selection when hitting "F" (frame) or "A" (all). Basically shows how to trap a key and work with the canvas selection
            AddManipulator(new Frame(Frame.FrameType.All));
            AddManipulator(new Frame(Frame.FrameType.Selection));

            // The following manipulator show how to work with canvas2d overlay and background rendering
            AddManipulator(new RectangleSelect());
            AddManipulator(new ScreenSpaceGrid());

            
            MouseDown += ManageNodeBlockSelection;
            ContextClick += ManageRightClick;

            // Debug
            KeyDown += DumpModel;
            KeyDown += TogglePhaseShift;
        }

        private bool ManageRightClick(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            VFXEdContextMenu.CanvasMenu(this, parent.MouseToCanvas(e.mousePosition), dataSource as VFXEdDataSource);
            e.Use();
            return true;
        }

        public void SetSelectedNodeBlock(VFXEdNodeBlockDraggable block)
        {

            m_SelectedNodeBlock = block;

            if (m_SelectedNodeBlock != null)
            {
                m_SelectedNodeBlock.parent.Invalidate();
                ClearSelection();
                AddToSelection(m_SelectedNodeBlock);
            }

        }

        public override void DebugDraw()
        {
            base.DebugDraw();
            Invalidate();
        }

        public bool ManageNodeBlockSelection(CanvasElement element, Event e, Canvas2D parent)
        {

            // TODO: Due to Selection only managing m_selected, nodeblocks aren't unselected normally. I have to catch used events in order to unselect nodeblocks :(

            // Manage Right Click : abort.
            if (e.type == EventType.MouseDown && e.button == 1)
                return false;

            // Unselecting
            if (element is Canvas2D && SelectedNodeBlock != null)
            {
                //Debug.Log("Unselecting");
                SetSelectedNodeBlock(null);
                return true;
            }
            return false;
        }

        private bool TogglePhaseShift(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.character == 'p')
            {
                VFXEditor.AssetModel.PhaseShift = !VFXEditor.AssetModel.PhaseShift;
                Repaint();
                return true;
            }

            return false;
        }

        private bool DumpModel(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.character == 'd')
            {
                const string TAB = "         ";
                VFXEditor.Log("\nNb Systems: " + VFXEditor.AssetModel.GetNbChildren());
                for (int i = 0; i < VFXEditor.AssetModel.GetNbChildren(); ++i)
                {
                    VFXSystemModel system = VFXEditor.AssetModel.GetChild(i);
                    VFXEditor.Log(TAB + "System " + i);
                    for (int j = 0; j < system.GetNbChildren(); ++j)
                    {
                        VFXContextModel context = system.GetChild(j);
                        VFXEditor.Log(TAB + TAB + j + " " + context.GetContextType());
                        for (int k = 0; k < context.GetNbChildren(); ++k)
                        {
                            VFXBlockModel block = context.GetChild(k);
                            VFXBlock blockDesc = context.GetChild(k).Desc;
                            VFXEditor.Log(TAB + TAB + TAB + k + " " + blockDesc.m_Name);
                            for (int l = 0; l < blockDesc.m_Params.Length; ++l)
                                VFXEditor.Log(TAB + TAB + TAB + TAB + blockDesc.m_Params[l].m_Name + ": " + block.GetParamValue(l).ToString());
                        }
                    }
                }

                Repaint();
                return true;
            }

            return false;
        }
    }

}
