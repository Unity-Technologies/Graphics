using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdDataSource : ScriptableObject, ICanvasDataSource
    {
        private List<CanvasElement> m_Elements = new List<CanvasElement>();
        private List<VFXEdDataNode> m_DataNodes = new List<VFXEdDataNode>();
        private List<VFXEdContextNode> m_ContextNodes = new List<VFXEdContextNode>();
        private List<FlowEdge> m_FlowEdges = new List<FlowEdge>();
        private List<VFXUIPropertyEdge> m_PropertyEdges = new List<VFXUIPropertyEdge>();


        public void OnEnable()
        {
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

        public void AddElement(CanvasElement e) {
            m_Elements.Add(e);
        }

        public void AddElement(VFXEdDataNode datanode)
        {
            m_DataNodes.Add(datanode);
            m_Elements.Add(datanode);
        }

        public void AddElement(VFXEdContextNode contextnode)
        {
            m_ContextNodes.Add(contextnode);
            m_Elements.Add(contextnode);
        }

        public void AddElement(FlowEdge flowedge)
        {
            m_FlowEdges.Add(flowedge);
            m_Elements.Add(flowedge);
        }

        public void AddElement(VFXUIPropertyEdge propertyEdge)
        {
            m_PropertyEdges.Add(propertyEdge);
            m_Elements.Add(propertyEdge);
        }
        


        public void DeleteElement(VFXEdDataNode node)
        {
            Canvas2D canvas = node.ParentCanvas();
            m_DataNodes.Remove(node);
            m_Elements.Remove(node);
            canvas.ReloadData();
            canvas.Repaint();
        }

        public void DeleteElement(VFXEdContextNode node)
        {
            Canvas2D canvas = node.ParentCanvas();
            m_ContextNodes.Remove(node);
            m_Elements.Remove(node);
            canvas.ReloadData();
            canvas.Repaint();
        }

        public void DeleteElement(FlowEdge edge)
        {

            VFXEdFlowAnchor anchor = edge.Right;
            var node = anchor.FindParent<VFXEdContextNode>();
            if (node != null)
            {
                VFXSystemModel owner = node.Model.GetOwner();
                int index = owner.GetIndex(node.Model);

                VFXSystemModel newSystem = new VFXSystemModel();
                while (owner.GetNbChildren() > index)
                    owner.GetChild(index).Attach(newSystem);
                newSystem.Attach(VFXEditor.AssetModel);
            }

            Canvas2D canvas = edge.ParentCanvas();
            m_FlowEdges.Remove(edge);
            m_Elements.Remove(edge);
            canvas.ReloadData();
            canvas.Repaint();
        }

        public void DeleteElement(VFXUIPropertyEdge edge)
        {
            VFXUIPropertyAnchor inputAnchor = edge.Right;
            ((VFXInputSlot)inputAnchor.Slot).Unlink();
            edge.Left.Invalidate();
            edge.Right.Invalidate();

            Canvas2D canvas = edge.ParentCanvas();
            m_PropertyEdges.Remove(edge);
            m_Elements.Remove(edge);
            canvas.ReloadData();
            canvas.Repaint();
        }

        public void DeleteElement(CanvasElement e)
        {
            Canvas2D canvas = e.ParentCanvas();
            m_Elements.Remove(e);
            canvas.ReloadData();
            canvas.Repaint();
        }

        public void RemoveConnectedEdges<T, U>(U anchor) 
            where T : Edge<U> 
            where U : CanvasElement, IConnect 
        {
            var edgesToRemove = GetConnectedEdges<T,U>(anchor);

            foreach (var edge in edgesToRemove)
                DeleteElement(edge);
        }

        public List<T> GetConnectedEdges<T, U>(U anchor) 
            where T : Edge<U> 
            where U : CanvasElement, IConnect 
        {
            var edges = new List<T>();
            foreach (CanvasElement element in m_Elements)
            {
                T edge = element as T;
                if (edge != null && (edge.Left == anchor || edge.Right == anchor))
                    edges.Add(edge);
            }
            return edges;
        }

        public void ConnectData(VFXUIPropertyAnchor a, VFXUIPropertyAnchor b)
        {
            // Swap to get a as output and b as input
            if (a.GetDirection() == Direction.Input)
            {
                VFXUIPropertyAnchor tmp = a;
                a = b;
                b = tmp;
            }

            RemoveConnectedEdges<VFXUIPropertyEdge, VFXUIPropertyAnchor>(b);

            // Disconnect connected children anchors and collapse
            b.Owner.DisconnectChildren();
            b.Owner.CollapseChildren(true);    

            ((VFXInputSlot)b.Slot).Link((VFXOutputSlot)a.Slot);
            AddElement(new VFXUIPropertyEdge(this, a, b));

            b.Invalidate();
        }

        public bool ConnectFlow(VFXEdFlowAnchor a, VFXEdFlowAnchor b)
        {
            if (a.GetDirection() == Direction.Input)
            {
                VFXEdFlowAnchor tmp = a;
                a = b;
                b = tmp;
            }

            RemoveConnectedEdges<FlowEdge, VFXEdFlowAnchor>(a);
            RemoveConnectedEdges<FlowEdge, VFXEdFlowAnchor>(b);

            VFXEdContextNode context0 = a.FindParent<VFXEdContextNode>();
            VFXEdContextNode context1 = b.FindParent<VFXEdContextNode>();

            if (context0 != null && context1 != null)
            {

                VFXContextModel model0 = context0.Model;
                VFXContextModel model1 = context1.Model;

                if (!VFXSystemModel.ConnectContext(model0, model1))
                    return false;
            }

            AddElement(new FlowEdge(this, a, b));
            return true;
        }


        /// <summary>
        /// Spawn node is called from context menu, object is expected to be a VFXEdSpawner
        /// </summary>
        /// <param name="o"> param that should be a VFXEdSpawner</param>
        public void SpawnNode(object o)
        {
            VFXEdSpawner spawner = o as VFXEdSpawner;
            if(spawner != null)
            {
                spawner.Spawn();
            }
        }


    }
}

