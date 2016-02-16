using UnityEngine;
using UnityEditor;
using System.Collections;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdCanvas : Canvas2D
    {
        public VFXEdNodeBlock SelectedNodeBlock
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
        private VFXEdNodeBlock m_SelectedNodeBlock;

        public VFXEdCanvas(Object target, EditorWindow host, ICanvasDataSource dataSource) : base(target, host, dataSource)
        {
            MouseDown += ManageSelection;
            ContextClick += ManageRightClick;

			// Debug
			KeyDown += DumpModel;
        }

        private bool ManageRightClick(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            VFXEdContextMenu.CanvasMenu(this, parent.MouseToCanvas(e.mousePosition), dataSource as VFXEdDataSource).ShowAsContext();
            e.Use();
            return true;
        }

        public void SetSelectedNodeBlock(VFXEdNodeBlock block)
        {
            if (m_SelectedNodeBlock != null)
                m_SelectedNodeBlock.parent.Invalidate();
            m_SelectedNodeBlock = block;
        }

        public bool ManageSelection(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            // Unselecting
            if (e.type == EventType.MouseDown && e.button == 0 && element is Canvas2D && SelectedNodeBlock != null)
            {
                SetSelectedNodeBlock(null);
                return true;
            }
            return false;
        }

		private bool DumpModel(CanvasElement element, Event e, Canvas2D parent)
		{
			if (e.character == 'd')
			{
				VFXEditor.Log("Nb Systems: " + VFXEditor.AssetModel.GetNbChildren());
				for (int i = 0; i < VFXEditor.AssetModel.GetNbChildren(); ++i)
				{
					VFXSystemModel system = VFXEditor.AssetModel.GetChild(i);
					VFXEditor.Log("\tSystem " + i);
					for (int j = 0; j < system.GetNbChildren(); ++j)
					{
						VFXContextModel context = system.GetChild(j);
						VFXEditor.Log("\t\tContext " + j + " " + context.GetContextType());
						for (int k = 0; k < context.GetNbChildren(); ++k)
							VFXEditor.Log("\t\t\t" + k + " " + context.GetChild(k).Desc.m_Name);
					}
				}

				Repaint();
				return true;
			}

			return false;
		}
    }

}
