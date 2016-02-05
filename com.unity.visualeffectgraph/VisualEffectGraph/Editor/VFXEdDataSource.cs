using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
	internal class VFXEdDataSource : ScriptableObject, ICanvasDataSource
	{
		private List<CanvasElement> m_Elements = new List<CanvasElement>();
		private VFXAsset m_Asset;


		public VFXEdDataSource()
		{
			// WIP Default layout (assetless), would have to deserialize selected asset here
			AddNode(new VFXEdNode(Vector2.zero, new Vector2(320.0f,180.0f),this));
			// TODO : Add deserialization logic, and provide default layout for non-selected assets.
		}

		public VFXEdDataSource(VFXAsset asset)
		{
			this.m_Asset = asset;
		}

		public void AddNode(VFXEdNode n)
		{
			m_Elements.Add(n);
		}

		public void UndoSnapshot(string Message)
		{
			// TODO : Make RecordObject work (not working, no errors, have to investigate)
			Undo.RecordObject(this, Message);
		}

		public CanvasElement[] FetchElements()
		{
			return m_Elements.ToArray();
		}

		public void DeleteElement(CanvasElement e)
		{
			Canvas2D canvas = e.ParentCanvas();
			m_Elements.Remove(e);
			canvas.ReloadData();
			canvas.Repaint();

		}

		public void Connect(VFXEdDataAnchor a, VFXEdDataAnchor b)
		{
			m_Elements.Add(new Edge<VFXEdDataAnchor>(this, a, b));
		}

		public void ConnectFlow(VFXEdFlowAnchor a, VFXEdFlowAnchor b)
		{
			m_Elements.Add(new Edge<VFXEdFlowAnchor>(this, a, b));
		}
	}
}

