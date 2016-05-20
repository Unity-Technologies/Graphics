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
    internal class VFXEdDataSource : ScriptableObject, ICanvasDataSource, VFXModelObserver
    {
        private List<CanvasElement> m_Elements = new List<CanvasElement>();

        private Dictionary<VFXContextModel,VFXEdContextNode> m_ContextModelToUI = new Dictionary<VFXContextModel,VFXEdContextNode>();
        private Dictionary<VFXBlockModel, VFXEdProcessingNodeBlock> m_BlockModelToUI = new Dictionary<VFXBlockModel, VFXEdProcessingNodeBlock>();

        public void OnEnable()
        {

        }

        public void CreateContext(VFXContextDesc desc,Vector2 pos)
        {
            VFXContextModel context = new VFXContextModel(desc);
            context.UpdatePosition(pos);

            // Create a tmp system to hold the newly created context
            VFXSystemModel system = new VFXSystemModel();
            system.AddChild(context);
            VFXEditor.AssetModel.AddChild(system);

            context.Observer = this;
            system.Observer = this;
        }

        public void CreateBlock(VFXBlockDesc desc, VFXContextModel owner, int index)
        {
            VFXBlockModel block = new VFXBlockModel(desc);
            owner.AddChild(block, index);
        }

        public void OnModelUpdated(VFXElementModel model)
        {
            Type type = model.GetType();
            if (type == typeof(VFXSystemModel))
                OnSystemUpdated((VFXSystemModel)model);
            else if (type == typeof(VFXContextModel))
                OnContextUpdated((VFXContextModel)model);
            //else if (type == typeof(VFXEdProcessingNodeBlock))
            //    OnBlockUpdated((VFXBlockModel)model);
        }

        private void OnSystemUpdated(VFXSystemModel model)
        {
            List<VFXContextModel> children = new List<VFXContextModel>();
            for (int i = 0; i < model.GetNbChildren(); ++i)
                children.Add(model.GetChild(i));

            List<VFXEdContextNode> childrenUI = new List<VFXEdContextNode>();
            foreach (var child in children)
                childrenUI.Add(m_ContextModelToUI[child]);

            // First remove all edges
            foreach (var childUI in childrenUI)
            {
                RemoveConnectedEdges<FlowEdge, VFXEdFlowAnchor>(childUI.inputs[0]);
                RemoveConnectedEdges<FlowEdge, VFXEdFlowAnchor>(childUI.outputs[0]);
            }

            // Then recreate edges
            for (int i = 0; i < childrenUI.Count - 1; ++i)
            {
                var output = childrenUI[i].outputs[0];
                var input = childrenUI[i + 1].inputs[0];

                m_Elements.Add(new FlowEdge(this, output, input));
            }
        }

        private void OnContextUpdated(VFXContextModel model)
        {
            var system = model.GetOwner();

            VFXEdContextNode contextUI;
            m_ContextModelToUI.TryGetValue(model,out contextUI);

            if (system == null) // We must delete the contextUI
            {
                if (contextUI != null)
                {
                    DeleteContextUI(contextUI);
                    m_ContextModelToUI.Remove(model);
                }
            }
            else if (contextUI == null)
            {
                contextUI = CreateContextUI(model);
                m_ContextModelToUI.Add(model, contextUI);
            }  
        }

        private VFXEdContextNode CreateContextUI(VFXContextModel model)
        {
            var contextUI = new VFXEdContextNode(model, this);
            AddElement(contextUI);
            return contextUI;
        }

        private void DeleteContextUI(VFXEdContextNode contextUI)
        {
           /* var anchors = contextUI.FindChildren<VFXEdFlowAnchor>();
            foreach (var anchor in anchors)
                RemoveConnectedEdges<FlowEdge, VFXEdFlowAnchor>(anchor);*/

            contextUI.OnRemove();
            m_Elements.Remove(contextUI);
        }








        private void OnBlockUpdated(VFXBlockModel model)
        {
            var ownerModel = model.GetOwner();

            VFXEdProcessingNodeBlock blockUI;
            m_BlockModelToUI.TryGetValue(model, out blockUI);

            if (ownerModel != null && blockUI == null)
            {
                blockUI = new VFXEdProcessingNodeBlock(model.Desc, this); // TODO
                m_BlockModelToUI.Add(model, blockUI);
                AddElement(blockUI);
            }

            var parentUI = m_ContextModelToUI[ownerModel];
            ownerModel.GetIndex(model);
            
        }

        public VFXEdProcessingNodeBlock GetBlockUI(VFXBlockModel model)
        {
            return m_BlockModelToUI[model];
        }

        public VFXEdContextNode GetContextUI(VFXContextModel model)
        {
            return m_ContextModelToUI[model];
        }
            
        public void OnLinkUpdated(VFXPropertySlot slot)
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

        public void CreateContext(Vector2 pos,VFXContextDesc desc)
        {
            VFXContextModel context = new VFXContextModel(desc);
            context.UpdatePosition(pos);

            // Create a tmp system to hold the newly created context
            VFXSystemModel system = new VFXSystemModel();
            system.AddChild(context);
            VFXEditor.AssetModel.AddChild(system);

            CreateContext(context);
        }

        public void CreateContext(VFXContextModel context)
        {
            var contextUI = new VFXEdContextNode(context, this);
            m_ContextModelToUI.Add(context, contextUI);
            AddElement(contextUI);
        }








        public void RemoveBlock(VFXBlockModel block)
        {

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
                    VFXSystemModel.DisconnectContext(node.Model,this);
                //m_Elements.Remove(e);
                //return;
            }

            var propertyEdge = e as VFXUIPropertyEdge;
            if (propertyEdge != null)
            {
                VFXUIPropertyAnchor inputAnchor = propertyEdge.Right;
                ((VFXInputSlot)inputAnchor.Slot).Unlink();
                propertyEdge.Left.Invalidate();
                propertyEdge.Right.Invalidate();
            }

            m_Elements.Remove(e);
            if (canvas != null)
            {
                canvas.ReloadData();
                canvas.Repaint();
            }
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
            m_Elements.Add(new VFXUIPropertyEdge(this, a, b));

            a.Invalidate();
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

           // RemoveConnectedEdges<FlowEdge, VFXEdFlowAnchor>(a);
           // RemoveConnectedEdges<FlowEdge, VFXEdFlowAnchor>(b);*/

            VFXEdContextNode context0 = a.FindParent<VFXEdContextNode>();
            VFXEdContextNode context1 = b.FindParent<VFXEdContextNode>();

            if (context0 != null && context1 != null)
            {

                VFXContextModel model0 = context0.Model;
                VFXContextModel model1 = context1.Model;

                if (!VFXSystemModel.ConnectContext(model0, model1, this))
                    return false;
            }

           // m_Elements.Add(new FlowEdge(this, a, b));           
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

