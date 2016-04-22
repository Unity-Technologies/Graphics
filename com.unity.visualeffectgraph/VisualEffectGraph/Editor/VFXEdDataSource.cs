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

            // TODO Remove that it is deprecated
            var dataEdge = e as DataEdge;
            if (dataEdge != null)
            {
                VFXEdDataAnchor anchor = dataEdge.Right;

                // TODO : Refactor needed : as VFXEdNodeBlock doesn't implement Model, as Model in this case should be VFXParamBindable
                if(anchor.FindParent<VFXEdProcessingNodeBlock>() != null)
                {
                    VFXEdProcessingNodeBlock node = anchor.FindParent<VFXEdProcessingNodeBlock>();
                    if (node != null)
                        node.Model.GetSlot(anchor.Index).Unlink();
                }
                else if (anchor.FindParent<VFXEdContextNodeBlock>() != null)
                {
                    VFXEdContextNodeBlock node = anchor.FindParent<VFXEdContextNodeBlock>();
                    if (node != null)
                        node.Model.GetSlot(anchor.Index).Unlink();
                }
            }

            // This is the new path
            var propertyEdge = e as VFXUIPropertyEdge;
            if (propertyEdge != null)
            {
                VFXUIPropertyAnchor inputAnchor = propertyEdge.Right;
                ((VFXInputSlot)inputAnchor.Slot).Unlink();
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

        [Obsolete]
        public void ConnectData(VFXEdDataAnchor a, VFXEdDataAnchor b)
        {
            if (a.GetDirection() == Direction.Input)
            {
                VFXEdDataAnchor tmp = a;
                a = b;
                b = tmp;
            }

            VFXOutputSlot output = a.FindParent<VFXEdNodeBlockParameterField>().Value as VFXOutputSlot;
            
            // TODO : Refactor needed : as VFXEdNodeBlock doesn't implement Model, as Model in this case should be VFXParamBindable
            if(b.FindParent<VFXEdProcessingNodeBlock>() != null)
            {
                VFXBlockModel model = b.FindParent<VFXEdProcessingNodeBlock>().Model;
                RemoveConnectedEdges<DataEdge, VFXEdDataAnchor>(b);
                model.GetSlot(b.Index).Link(output);
            }
            else if(b.FindParent<VFXEdContextNodeBlock>() != null)
            {
                VFXContextModel model = b.FindParent<VFXEdContextNodeBlock>().Model;
                RemoveConnectedEdges<DataEdge, VFXEdDataAnchor>(b);
                model.GetSlot(b.Index).Link(output);
            }

            m_Elements.Add(new DataEdge(this, a, b));
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
            ((VFXInputSlot)b.Slot).Link((VFXOutputSlot)a.Slot);
            m_Elements.Add(new VFXUIPropertyEdge(this, a, b));
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

