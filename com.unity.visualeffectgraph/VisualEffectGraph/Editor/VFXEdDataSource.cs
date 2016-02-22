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

        public void OnEnable()
        {
            VFXEditor.BlockLibrary.Load(); // Force a reload
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

        public void RemoveDataConnectionsTo(VFXEdDataAnchor anchor)
        {
            var edgesToErase = new List<DataEdge>();

            foreach (CanvasElement element in m_Elements)
            {
                DataEdge edge = element as DataEdge;
                if (edge != null && (edge.Left == anchor || edge.Right == anchor))
                    edgesToErase.Add(edge);
            }

            foreach(DataEdge edge in edgesToErase) {
                m_Elements.Remove(edge);
            }
        }

        public void DeleteElement(CanvasElement e)
        {
            Canvas2D canvas = e.ParentCanvas();

            // Handle model update when deleting edge here
            var edge = e as FlowEdge;
            if (edge != null)
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
            }

            var dataEdge = e as DataEdge;
            if (dataEdge != null)
            {
                VFXEdDataAnchor anchor = dataEdge.Right;
                var node = anchor.FindParent<VFXEdProcessingNodeBlock>();
                if (node != null)
                    node.Model.UnbindParam(anchor.Index);
            }

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
                m_Elements.Remove(edge);
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


        public void ConnectData(VFXEdDataAnchor a, VFXEdDataAnchor b)
        {
            if (a.GetDirection() == Direction.Input)
            {
                VFXEdDataAnchor tmp = a;
                a = b;
                b = tmp;
            }

            VFXParamValue paramValue = a.FindParent<VFXEdNodeBlockParameterField>().Value;
            VFXBlockModel model = b.FindParent<VFXEdProcessingNodeBlock>().Model;

            model.BindParam(paramValue, b.Index);

            RemoveConnectedEdges<DataEdge, VFXEdDataAnchor>(b);

            m_Elements.Add(new DataEdge(this, a, b));
        }

        public bool ConnectFlow(VFXEdFlowAnchor a, VFXEdFlowAnchor b)
        {
            if (a.GetDirection() == Direction.Input)
            {
                VFXEdFlowAnchor tmp = a;
                a = b;
                b = tmp;
            }

            VFXEdContextNode context0 = a.FindParent<VFXEdContextNode>();
            VFXEdContextNode context1 = b.FindParent<VFXEdContextNode>();

            if (context0 != null && context1 != null)
            {

                VFXContextModel model0 = context0.Model;
                VFXContextModel model1 = context1.Model;

                if (!VFXSystemModel.ConnectContext(model0, model1))
                    return false;

            }

            RemoveConnectedEdges<FlowEdge, VFXEdFlowAnchor>(a);
            RemoveConnectedEdges<FlowEdge, VFXEdFlowAnchor>(b);

            m_Elements.Add(new FlowEdge(this, a, b));
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

