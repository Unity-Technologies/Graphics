using UnityEngine;using UnityEditor;using System.Collections;using Object = UnityEngine.Object;namespace UnityEditor.Experimental{
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
	}}