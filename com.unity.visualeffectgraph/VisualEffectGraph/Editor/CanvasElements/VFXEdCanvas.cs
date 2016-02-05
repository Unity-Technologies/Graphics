using UnityEngine;using UnityEditor;using System.Collections;using Object = UnityEngine.Object;namespace UnityEditor.Experimental{	internal class VFXEdCanvas : Canvas2D {		public VFXEdCanvas(Object target, EditorWindow host, ICanvasDataSource dataSource) : base (target, host, dataSource)		{		}


	}}