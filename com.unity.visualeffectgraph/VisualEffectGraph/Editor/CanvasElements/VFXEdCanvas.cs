using UnityEngine;using UnityEditor;using System.Collections;using Object = UnityEngine.Object;namespace UnityEditor.Experimental{	internal class VFXEdCanvas : Canvas2D {

		public VFXEdNodeBlock SelectedNodeBlock {
			get
			{
				return m_SelectedNodeBlock;
			}
			set
			{
				SetSelectedNodeBlock(value);
			}
		}
		private VFXEdNodeBlock m_SelectedNodeBlock;		public VFXEdCanvas(Object target, EditorWindow host, ICanvasDataSource dataSource) : base (target, host, dataSource)		{			AllEvents += onEvents;		}

		public void SetSelectedNodeBlock(VFXEdNodeBlock block)
		{
			if (m_SelectedNodeBlock != null)
				m_SelectedNodeBlock.parent.Invalidate();
			m_SelectedNodeBlock = block;
		}

		public bool onEvents(CanvasElement element, Event e, Canvas2D parent)
		{
			if(e.type == EventType.MouseDown && element is Canvas2D)
			{
				SetSelectedNodeBlock(null);
				return true;
			}
			return false;
		}
	}}