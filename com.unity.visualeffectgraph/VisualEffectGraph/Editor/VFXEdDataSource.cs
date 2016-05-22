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
    internal class VFXEdDataSource : ScriptableObject, ICanvasDataSource, VFXModelController
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

            SyncContext(context);
            SyncSystem(system);
        }

        public void CreateBlock(VFXBlockDesc desc, VFXContextModel owner, int index)
        {
            VFXBlockModel block = new VFXBlockModel(desc);           
            owner.AddChild(block, index);

            SyncBlock(block);
            SyncContext(owner);
        }

        public void Remove(VFXElementModel model)
        {
            var oldOwner = model.GetOwner();
            model.Detach();
            SyncView(model);

            if (oldOwner != null)
                SyncView(oldOwner);
        }

        public void Attach(VFXElementModel model,VFXElementModel owner,int index = -1)
        {
            var oldOwner = model.GetOwner();
            if (owner != null)
            {
                owner.AddChild(model, index);
                SyncView(owner);
            }
            else
                model.Detach();

            if (oldOwner != null && oldOwner != owner)
                SyncView(oldOwner);
        }

        // This is called by the model when one element has been updated and the view therefore needs to synchronize
        /*public void OnModelUpdated(VFXElementModel model)
        {
            Type type = model.GetType();
            if (type == typeof(VFXSystemModel))
                OnSystemUpdated((VFXSystemModel)model);
            else if (type == typeof(VFXContextModel))
                OnContextUpdated((VFXContextModel)model);
            else if (type == typeof(VFXBlockModel))
                OnBlockUpdated((VFXBlockModel)model);
        }*/

        public void OnLinkUpdated(VFXPropertySlot slot)
        {
            // TODO
        }

        public void SyncView(VFXElementModel model, bool recursive = false)
        {
            Type modelType = model.GetType();
            if (modelType == typeof(VFXSystemModel))
                SyncSystem((VFXSystemModel)model);
            else if (modelType == typeof(VFXContextModel))
                SyncContext((VFXContextModel)model);
            else if (modelType == typeof(VFXBlockModel))
                SyncBlock((VFXBlockModel)model);

            if (recursive)
                for (int i = 0; i < model.GetNbChildren(); ++i)
                    SyncView(model.GetChild(i), true);
        }

        public void SyncSystem(VFXSystemModel model)
        {
            List<VFXContextModel> children = new List<VFXContextModel>();
            for (int i = 0; i < model.GetNbChildren(); ++i)
                children.Add(model.GetChild(i));

            // Collect all contextUI in the system
            List<VFXEdContextNode> childrenUI = new List<VFXEdContextNode>();
            foreach (var child in children)
                childrenUI.Add(m_ContextModelToUI[child]); // This should not throw

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

        public void SyncContext(VFXContextModel model)
        {
            var system = model.GetOwner();

            VFXEdContextNode contextUI;
            m_ContextModelToUI.TryGetValue(model,out contextUI);

            if (system == null) // We must delete the contextUI as it is no longer bound to a system
            {
                for (int i = 0; i < model.GetNbChildren(); ++i)
                    m_BlockModelToUI.Remove(model.GetChild(i));

                if (contextUI != null)
                {
                    DeleteContextUI(contextUI);
                    m_ContextModelToUI.Remove(model);
                }
            }
            else  // Create the context UI if it does not exist
            {
                if (contextUI == null)
                {
                    contextUI = CreateContextUI(model);
                    m_ContextModelToUI.Add(model, contextUI);
                }

                // Reset UI data
                contextUI.translation = model.UIPosition;

                // Collect all blocks in the context
                List<VFXBlockModel> children = new List<VFXBlockModel>();
                for (int i = 0; i < model.GetNbChildren(); ++i)
                    children.Add(model.GetChild(i));

                // Collect all contextUI in the system
                List<VFXEdProcessingNodeBlock> childrenUI = new List<VFXEdProcessingNodeBlock>();
                foreach (var child in children)
                    childrenUI.Add(m_BlockModelToUI[child]); // This should not throw

                VFXEdNodeBlockContainer container = contextUI.NodeBlockContainer;

                // Remove all blocks
                container.ClearNodeBlocks();

                // Then add them again
                foreach (var child in childrenUI)
                    container.AddNodeBlock(child);

                contextUI.Invalidate();
            }         
        }

        public void SyncBlock(VFXBlockModel model)
        {
            var context = model.GetOwner();

            VFXEdProcessingNodeBlock blockUI;
            m_BlockModelToUI.TryGetValue(model, out blockUI);

            if (context == null) // We must delete the contextUI as it is no longer bound to a system
                m_BlockModelToUI.Remove(model);
            else if (blockUI == null)
                m_BlockModelToUI.Add(model, blockUI = new VFXEdProcessingNodeBlock(model, this));

            // Reset UI data
            blockUI.collapsed = model.UICollapsed;
            blockUI.Invalidate();
        }

        private VFXEdContextNode CreateContextUI(VFXContextModel model)
        {
            var contextUI = new VFXEdContextNode(model, this);
            AddElement(contextUI);
            return contextUI;
        }

        private void DeleteContextUI(VFXEdContextNode contextUI)
        {
            contextUI.OnRemove();
            m_Elements.Remove(contextUI);
        }


        public VFXEdProcessingNodeBlock GetBlockUI(VFXBlockModel model)
        {
            return m_BlockModelToUI[model];
        }

        public VFXEdContextNode GetContextUI(VFXContextModel model)
        {
            return m_ContextModelToUI[model];
        }
            
        public CanvasElement[] FetchElements()
        {
            return m_Elements.ToArray();
        }

        public void AddElement(CanvasElement e) {
            m_Elements.Add(e);
        }


        // This is called by the UI when a component is deleted
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

            VFXEdContextNode context0 = a.FindParent<VFXEdContextNode>();
            VFXEdContextNode context1 = b.FindParent<VFXEdContextNode>();

            if (context0 != null && context1 != null)
            {

                VFXContextModel model0 = context0.Model;
                VFXContextModel model1 = context1.Model;

                if (!VFXSystemModel.ConnectContext(model0, model1, this))
                    return false;
            }
       
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

