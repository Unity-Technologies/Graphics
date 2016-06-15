using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.VFX;
using System.Collections.Generic;
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
        private VFXEdDataSource m_DataSource;

        private Frame m_FocusManipulator; 

        public VFXEdCanvas(Object target, EditorWindow host, ICanvasDataSource dataSource)
            : base(target, host, dataSource)
        {
            m_DataSource = (VFXEdDataSource)dataSource;

            // draggable manipulator allows to move the canvas around. Note that individual elements can have the draggable manipulator on themselves
            AddManipulator(new Draggable(2, EventModifiers.None));
            AddManipulator(new Draggable(0, EventModifiers.Alt));

            // make the canvas zoomable
            AddManipulator(new Zoomable(Zoomable.ZoomType.AroundMouse));

            // allow framing the selection when hitting "F" (frame) or "A" (all). Basically shows how to trap a key and work with the canvas selection
            m_FocusManipulator = new Frame(Frame.FrameType.All);
            AddManipulator(m_FocusManipulator);
            AddManipulator(new Frame(Frame.FrameType.Selection));

            // add tooltips for all Systems
            AddManipulator(new TooltipManipulator(GetToolTipText));

            // The following manipulator show how to work with canvas2d overlay and background rendering
            AddManipulator(new RectangleSelect());
            AddManipulator(new ScreenSpaceGrid(VFXEditorMetrics.GridSpacing, VFXEditorMetrics.GridSteps));

            AddManipulator(new Watermark(VFXEditor.styles.Watermark));
            AddManipulator(new VFXFilterPopup());
            AddManipulator(new EditorKeyboardControl(this));

            
            MouseDown += ManageNodeBlockSelection;
            ContextClick += ManageRightClick;

            // Debug
            KeyDown += DumpModel;
        }

        public void FocusElements(bool animate)
        {
            m_FocusManipulator.Focus(this,animate);
        }

        public List<string> GetToolTipText()
        {
            List<string> lines = new List<string>();
            lines = VFXModelDebugInfoProvider.GetInfo(lines, VFXEditor.Graph.systems, VFXModelDebugInfoProvider.InfoFlag.kDefault);
            return lines;
        }

        private bool ManageRightClick(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            if (VFXEditor.Graph != null)
            {
                ShowCanvasMenu(e.mousePosition);
                e.Use();
                return true;
            }

            return false;
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

        private bool DumpModel(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.character == 'd')
            {
                Debug.Log("Resync View");
                m_DataSource.ResyncViews();
                ReloadData();
                Repaint();
            }

            if (e.character == 's')
            {
                /*Debug.Log("Serialize in XML");
                var str = ModelSerializer.Serialize(VFXEditor.Graph);
                Debug.Log(str);

                VFXEditor.Graph = ModelSerializer.Deserialize(str);
                (dataSource as VFXEdDataSource).ResyncViews();
                ReloadData();
                Repaint();

                Debug.Log("DONE");*/
            }



            return false;
        }


        public void ShowCanvasMenu(Vector2 position)
        {
            MiniMenu.MenuSet itemSet = new MiniMenu.MenuSet();
            itemSet.AddMenuEntry("Add...", "New ParticleSystem", NewParticleSystem, null);
            itemSet.AddMenuEntry("Add...", "New Node...", ShowNewNodePopup, null);
            itemSet.AddMenuEntry("Add...", "New Parameter...", ShowNewDataNodePopup, null);
            itemSet.AddMenuEntry("Add...", "New Comment", NewComment, null);
            itemSet.AddMenuEntry("Layout", "Layout All Systems", LayoutAllSystems, null);
            MiniMenu.Show(position, itemSet);
        }

        public void NewParticleSystem(Vector2 position, object o)
        {
            VFXEdUtility.NewParticleSystem(this, m_DataSource, position);
        }

        public void NewComment(Vector2 position, object o)
        {
            VFXEdUtility.NewComment(this, m_DataSource, position);
        }

        public void ShowNewNodePopup(Vector2 position, object o)
        {
            VFXFilterPopup.ShowNewNodePopup(position, this, true);
        }

        public void ShowNewDataNodePopup(Vector2 position, object o)
        {
            VFXFilterPopup.ShowNewDataNodePopup(position, this, true);
        }

        public void LayoutSystem(Vector2 position, object o)
        {
            VFXEdContextNode node = (VFXEdContextNode)o;
            VFXEdLayoutUtility.LayoutSystem(node.Model.GetOwner(),m_DataSource);
        }

        public void LayoutAllSystems(Vector2 position, object o)
        {
            var allSystemModel = VFXEditor.Graph.systems;
            VFXEdLayoutUtility.LayoutAllSystems(allSystemModel, m_DataSource, Vector2.zero);
        }

    }

}
