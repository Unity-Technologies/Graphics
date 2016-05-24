using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental;
using UnityEditor.Experimental.VFX;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdDataSource : ScriptableObject, ICanvasDataSource, VFXModelController
    {
        private List<CanvasElement> m_Elements = new List<CanvasElement>();
        private Dictionary<VFXElementModel, VFXModelHolder> m_ModelToUI = new Dictionary<VFXElementModel, VFXModelHolder>();
        private Dictionary<VFXPropertySlot, VFXUIPropertyAnchor> m_SlotToUI = new Dictionary<VFXPropertySlot, VFXUIPropertyAnchor>();

        public void OnEnable()
        {

        }

        public void ClearUI()
        {
            m_Elements.Clear();
            m_ModelToUI.Clear();
            m_SlotToUI.Clear();
        }

        public void ResyncViews()
        {
            ClearUI();

            for (int i = 0; i < VFXEditor.Graph.systems.GetNbChildren(); ++i)
                SyncView(VFXEditor.Graph.systems.GetChild(i), true);

            for (int i = 0; i < VFXEditor.Graph.models.GetNbChildren(); ++i)
                SyncView(VFXEditor.Graph.models.GetChild(i), true);
        }

        public VFXContextModel CreateContext(VFXContextDesc desc,Vector2 pos)
        {
            VFXContextModel context = new VFXContextModel(desc);
            context.UpdatePosition(pos);

            // Create a tmp system to hold the newly created context
            VFXSystemModel system = new VFXSystemModel();
            system.AddChild(context);
            VFXEditor.Graph.systems.AddChild(system);

            SyncView(context);
            SyncView(system);

            return context;
        }

        public VFXDataNodeModel CreateDataNode(Vector2 pos)
        {
            VFXDataNodeModel model = new VFXDataNodeModel();
            model.UpdatePosition(pos);
            VFXEditor.Graph.models.AddChild(model);
            
            SyncView(model);

            return model;
        }

        public void Create(VFXElementModel model,VFXElementModel owner,int index = -1)
        {
            owner.AddChild(model, index);
            SyncView(model);
            SyncView(owner);
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

        public void Register(VFXPropertySlot slot,VFXUIPropertyAnchor anchor)
        {
            m_SlotToUI.Add(slot, anchor);
        }

        public void SyncView(VFXPropertySlot slot, bool recursive = false)
        {
            SyncSlot(slot,recursive);
        }

        public void SyncView(VFXElementModel model, bool recursive = false)
        {
            Type modelType = model.GetType();
            if (modelType == typeof(VFXSystemModel))
                SyncSystem((VFXSystemModel)model,recursive);
            else if (modelType == typeof(VFXContextModel))
                SyncContext((VFXContextModel)model,recursive);
            else if (modelType == typeof(VFXBlockModel))
                SyncBlock((VFXBlockModel)model,recursive);
            else if (modelType == typeof(VFXDataNodeModel))
                SyncDataNode((VFXDataNodeModel)model,recursive);
            else if (modelType == typeof(VFXDataBlockModel))
                SyncDataBlock((VFXDataBlockModel)model,recursive);   
        }

        public void SyncSystem(VFXSystemModel model,bool recursive = false)
        {
            List<VFXContextModel> children = new List<VFXContextModel>();
            for (int i = 0; i < model.GetNbChildren(); ++i)
                children.Add(model.GetChild(i));

            // Collect all contextUI in the system
            List<VFXEdContextNode> childrenUI = CollectOrCreateUI<VFXContextModel, VFXEdContextNode>(children);

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

            if (recursive)
                SyncChildren(model);
        }

        public void SyncContext(VFXContextModel model,bool recursive = false)
        {
            var system = model.GetOwner();

            VFXEdContextNode contextUI = TryGetUI<VFXEdContextNode>(model);

            if (system == null) // We must delete the contextUI as it is no longer bound to a system
            {
                for (int i = 0; i < model.GetNbChildren(); ++i)
                    Remove(model.GetChild(i));

                for (int i = 0; i < model.GetNbSlots(); ++i)
                    RemoveSlot(model.GetSlot(i));

                if (contextUI != null)
                {
                    DeleteContextUI(contextUI);
                    m_ModelToUI.Remove(model);
                }
            }
            else  // Create the context UI if it does not exist
            {
                if (contextUI == null)
                {
                    contextUI = CreateContextUI(model);
                    m_ModelToUI.Add(model, contextUI);
                }

                // Reset UI data
                contextUI.translation = model.UIPosition;

                // Collect all blocks in the context
                List<VFXBlockModel> children = new List<VFXBlockModel>();
                for (int i = 0; i < model.GetNbChildren(); ++i)
                    children.Add(model.GetChild(i));

                // Collect all contextUI in the system
                List<VFXEdProcessingNodeBlock> childrenUI = CollectOrCreateUI<VFXBlockModel, VFXEdProcessingNodeBlock>(children);
                VFXEdNodeBlockContainer container = contextUI.NodeBlockContainer;

                // Remove all blocks
                container.ClearNodeBlocks();

                // Then add them again
                foreach (var child in childrenUI)
                    container.AddNodeBlock(child);

                if (recursive)
                {
                    SyncChildren(model);

                    // Sync context block UI
                    for (int i = 0; i < model.GetNbSlots(); ++i)
                        SyncView(model.GetSlot(i), true);
                }

                contextUI.Layout();
                contextUI.Invalidate();
            }         
        }

        public void SyncBlock(VFXBlockModel model, bool recursive = false)
        {
            var context = model.GetOwner();

            VFXEdProcessingNodeBlock blockUI = TryGetUI<VFXEdProcessingNodeBlock>(model);

            if (context == null) // We must delete the contextUI as it is no longer bound to a system
            {
                for (int i = 0; i < model.GetNbSlots(); ++i)
                    RemoveSlot(model.GetSlot(i));

                m_ModelToUI.Remove(model);
            }
            else if (blockUI == null)
                m_ModelToUI.Add(model, blockUI = new VFXEdProcessingNodeBlock(model, this));

            // Reset UI data
            blockUI.collapsed = model.UICollapsed;
            blockUI.Invalidate();

            if (recursive)
                for (int i = 0; i < model.GetNbSlots(); ++i)
                    SyncView(model.GetSlot(i), true);
        }

        public void SyncDataNode(VFXDataNodeModel model, bool recursive)
        {
            var owner = model.GetOwner();
            VFXEdDataNode nodeUI = TryGetUI<VFXEdDataNode>(model);

            if (owner == null) // We must delete the contextUI as it is no longer bound to a system
            {
                for (int i = 0; i < model.GetNbChildren(); ++i)
                    Remove(model.GetChild(i));

                if (nodeUI != null)
                {
                    nodeUI.OnRemove();
                    m_ModelToUI.Remove(model);
                    m_Elements.Remove(nodeUI);
                }
            }
            else
            {
                if (nodeUI == null)
                {
                    nodeUI = new VFXEdDataNode(model, this);
                    m_ModelToUI.Add(model, nodeUI);
                    AddElement(nodeUI);
                }

                // Reset UI data
                nodeUI.translation = model.UIPosition;
                nodeUI.exposed = model.Exposed;

                // Collect all blocks in the context
                List<VFXDataBlockModel> children = new List<VFXDataBlockModel>();
                for (int i = 0; i < model.GetNbChildren(); ++i)
                    children.Add(model.GetChild(i));

                // Collect all contextUI in the system
                List<VFXEdDataNodeBlock> childrenUI = CollectOrCreateUI<VFXDataBlockModel, VFXEdDataNodeBlock>(children);
                VFXEdNodeBlockContainer container = nodeUI.NodeBlockContainer;

                // Remove all blocks
                container.ClearNodeBlocks();

                // Then add them again
                foreach (var child in childrenUI)
                    container.AddNodeBlock(child);

                if (recursive)
                    SyncChildren(model);

                nodeUI.Layout();
                nodeUI.Invalidate();
            }
        }

        public void SyncDataBlock(VFXDataBlockModel model, bool recursive = false)
        {
            var owner = model.GetOwner();

            VFXEdDataNodeBlock blockUI = TryGetUI<VFXEdDataNodeBlock>(model);

            if (owner == null) // We must delete the contextUI as it is no longer bound to a system
            {
                m_ModelToUI.Remove(model);
                RemoveSlot(model.Slot);
            }
            else if (blockUI == null)
                m_ModelToUI.Add(model, blockUI = new VFXEdDataNodeBlock(model, this, ""));

            // Reset UI data
            blockUI.collapsed = model.UICollapsed;
            blockUI.Invalidate();

            if (recursive)
                SyncView(model.Slot, true);
        }

        public void SyncSlot(VFXPropertySlot slot, bool recursive = true)
        {
            var anchor = GetUIAnchor(slot); // Must have been linked

            // Collect all contextUI in the system
            List<VFXUIPropertyAnchor> linkedAnchors = new List<VFXUIPropertyAnchor>();
            var connectedSlots = slot.GetConnectedSlots();
            foreach (var connectedSlot in connectedSlots)
            {
                var linkedAnchor = TryGetUIAnchor(connectedSlot);
                if (linkedAnchor != null) // Anchors cannot be created from here, they must be registered
                    linkedAnchors.Add(linkedAnchor);
            }

            RemoveConnectedEdges<VFXUIPropertyEdge, VFXUIPropertyAnchor>(anchor);

            foreach (var linkedAnchor in linkedAnchors)
            {
                if (anchor.GetDirection() == Direction.Output)
                    m_Elements.Add(new VFXUIPropertyEdge(this, anchor, linkedAnchor));
                else
                    m_Elements.Add(new VFXUIPropertyEdge(this, linkedAnchor, anchor));
                linkedAnchor.Invalidate();
            }

            if (anchor.Owner != null)
                anchor.Owner.CollapseChildren(slot.UICollapsed);

            if (recursive)
                for (int i = 0; i < slot.GetNbChildren(); ++i)
                    SyncView(slot.GetChild(i), true);

            anchor.Invalidate();
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

        private void RemoveSlot(VFXPropertySlot slot)
        {
            for (int i = 0; i < slot.GetNbChildren(); ++i)
                RemoveSlot(slot.GetChild(i));

            slot.UnlinkAll();
            SyncView(slot);
            m_SlotToUI.Remove(slot);
        }

        private List<U> CollectOrCreateUI<T,U>(List<T> models) where T : VFXElementModel where U : VFXModelHolder 
        {
            List<U> modelsUI = new List<U>();
            foreach (var model in models)
            {
                var modelUI = TryGetUI<U>(model);
                if (modelUI == null) // Recreate if necessary
                {
                    SyncView(model);
                    modelUI = GetUI<U>(model);
                }
                modelsUI.Add(modelUI);
            }
            return modelsUI;
        }

        private void SyncChildren(VFXElementModel model)
        {
            for (int i = 0; i < model.GetNbChildren(); ++i)
                SyncView(model.GetChild(i), true);
        }

        public VFXUIPropertyAnchor GetUIAnchor(VFXPropertySlot slot)
        {
            return m_SlotToUI[slot];
        }

        public VFXUIPropertyAnchor TryGetUIAnchor(VFXPropertySlot slot)
        {
            VFXUIPropertyAnchor ui = null;
            m_SlotToUI.TryGetValue(slot, out ui);
            return ui;
        }

        public T GetUI<T>(VFXElementModel model) where T : VFXModelHolder
        {
            return (T)m_ModelToUI[model];
        }

        public T TryGetUI<T>(VFXElementModel model) where T : VFXModelHolder
        {
            VFXModelHolder ui = null;
            m_ModelToUI.TryGetValue(model, out ui);
            return (T)ui;
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

                SyncView(propertyEdge.Left.Slot);
                SyncView(propertyEdge.Right.Slot);
            }

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
            {
                edge.Left.Invalidate();
                edge.Right.Invalidate();
                m_Elements.Remove(edge);
            }
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

            b.Owner.CollapseChildren(true);    

            ((VFXInputSlot)b.Slot).Link((VFXOutputSlot)a.Slot);
            SyncView(b.Slot,true);
        }

        public bool ConnectContext(VFXContextModel a, VFXContextModel b)
        {
            return VFXSystemModel.ConnectContext(a, b, this);
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

                if (!ConnectContext(model0, model1))
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
            VFXEdSpawner spawner = (VFXEdSpawner)o;
            if(spawner != null)
            {
                spawner.Spawn();
            }
        }

    }
}

