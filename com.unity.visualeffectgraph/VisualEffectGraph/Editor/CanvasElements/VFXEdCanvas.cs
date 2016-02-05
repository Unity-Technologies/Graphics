using UnityEngine;using UnityEditor;using System.Collections;using Object = UnityEngine.Object;namespace UnityEditor.Experimental{	internal class VFXEdCanvas : Canvas2D {		public string Lines;		public VFXEdCanvas(Object target, EditorWindow host, ICanvasDataSource dataSource) : base (target, host, dataSource)		{			this.OnLayout += VFXEdCanvas_OnLayout;		}

		private bool VFXEdCanvas_OnLayout(CanvasElement element, Event e, Canvas2D parent)
		{
			Lines = "";
			return true;
		}

		public void AddDebug(string Message)
		{
			this.Lines += "\n" + Message;
		}
		public string ShowDebug()
		{
			return Lines;
		}	}}