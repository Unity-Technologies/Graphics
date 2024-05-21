using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor.Experimental.GraphView;

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.UIElements;


namespace UnityEditor.VFX.UI
{
    abstract class VFXParameterDataAnchorController : VFXDataAnchorController
    {
        public VFXParameterDataAnchorController(VFXSlot model, VFXParameterNodeController sourceNode, bool hidden) : base(model, sourceNode, hidden)
        {
        }

        public override string name
        {
            get
            {
                if (depth == 0)
                    return sourceNode.exposedName;
                return base.name;
            }
        }

        public new VFXParameterNodeController sourceNode
        {
            get { return base.sourceNode as VFXParameterNodeController; }
        }
        public override bool expandedSelf
        {
            get
            {
                return sourceNode.infos.expandedSlots != null && sourceNode.infos.expandedSlots.Contains(model);
            }
        }
        public override void ExpandPath()
        {
            if (sourceNode.infos.expandedSlots == null)
                sourceNode.infos.expandedSlots = new List<VFXSlot>();
            bool changed = false;
            if (!sourceNode.infos.expandedSlots.Contains(model))
            {
                sourceNode.infos.expandedSlots.Add(model);
                changed = true;
            }

            if (SlotShouldSkipFirstLevel(model))
            {
                VFXSlot firstChild = model.children.First();
                if (!sourceNode.infos.expandedSlots.Contains(firstChild))
                {
                    sourceNode.infos.expandedSlots.Add(firstChild);
                    changed = true;
                }
            }
            if (changed)
            {
                sourceNode.model.Invalidate(VFXModel.InvalidationCause.kUIChanged);
            }
        }

        public override void RetractPath()
        {
            if (sourceNode.infos.expandedSlots != null)
            {
                bool changed = sourceNode.infos.expandedSlots.Remove(model);
                if (SlotShouldSkipFirstLevel(model))
                {
                    changed |= sourceNode.infos.expandedSlots.Remove(model.children.First());
                }
                if (changed)
                {
                    sourceNode.model.Invalidate(VFXModel.InvalidationCause.kUIChanged);
                }
            }
        }
    }
    class VFXParameterOutputDataAnchorController : VFXParameterDataAnchorController
    {
        public VFXParameterOutputDataAnchorController(VFXSlot model, VFXParameterNodeController sourceNode, bool hidden) : base(model, sourceNode, hidden)
        {
        }

        public override Direction direction
        { get { return Direction.Output; } }
    }
    class VFXParameterInputDataAnchorController : VFXParameterDataAnchorController
    {
        public VFXParameterInputDataAnchorController(VFXSlot model, VFXParameterNodeController sourceNode, bool hidden) : base(model, sourceNode, hidden)
        {
        }

        public override Direction direction
        { get { return Direction.Input; } }
    }

    class VFXParameterNodeController : VFXNodeController, IPropertyRMProvider
    {
        VFXParameterController m_ParentController;

        int m_Id;

        public VFXParameterNodeController(VFXParameterController controller, VFXParameter.Node infos, VFXViewController viewController) : base(controller.model, viewController)
        {
            m_ParentController = controller;
            m_Id = infos.id;
        }

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            base.ModelChanged(obj);

            foreach (var port in inputPorts)
            {
                port.ApplyChanges(); // call the port ApplyChange because expanded states are stored in the VFXParameter.Node
            }

            foreach (var port in outputPorts)
            {
                port.ApplyChanges(); // call the port ApplyChange because expanded states are stored in the VFXParameter.Node
            }
        }

        public VFXParameter.Node infos
        {
            get { return m_ParentController.model.GetNode(m_Id); }
        }

        protected override VFXDataAnchorController AddDataAnchor(VFXSlot slot, bool input, bool hidden)
        {
            VFXDataAnchorController newAnchor;
            if (input)
                newAnchor = new VFXParameterInputDataAnchorController(slot, this, hidden);
            else
                newAnchor = new VFXParameterOutputDataAnchorController(slot, this, hidden);

            newAnchor.portType = slot.property.type;
            return newAnchor;
        }

        public override string title
        {
            get { return parentController.isOutput ? inputPorts.First().name : outputPorts.First().name; }
        }

        public string exposedName
        {
            get { return m_ParentController.model.exposedName; }
        }
        public bool exposed
        {
            get { return m_ParentController.model.exposed; }
        }

        public int order
        {
            get { return m_ParentController.model.order; }
        }

        public override bool expanded
        {
            get => infos.expanded;
            set => infos.expanded = value;
        }

        public override bool superCollapsed
        {
            get => infos.supecollapsed;
            set
            {
                infos.supecollapsed = value;
                model.Invalidate(VFXModel.InvalidationCause.kUIChanged);
            }
        }

        public VFXSpace space
        {
            get
            {
                return m_ParentController.space;
            }

            set
            {
                m_ParentController.space = value;
            }
        }

        public bool spaceableAndMasterOfSpace
        {
            get
            {
                return m_ParentController.spaceableAndMasterOfSpace;
            }
        }

        public bool IsSpaceInherited()
        {
            return m_ParentController.IsSpaceInherited();
        }

        bool IPropertyRMProvider.expanded
        {
            get
            {
                return false;
            }
        }
        bool IPropertyRMProvider.editable
        {
            get { return true; }
        }

        bool IPropertyRMProvider.expandable { get { return false; } }
        bool IPropertyRMProvider.expandableIfShowsEverything { get { return false; } }


        IEnumerable<int> IPropertyRMProvider.filteredOutEnumerators { get { return null; } }
        public object value
        {
            get
            {
                return m_ParentController.value;
            }
            set
            {
                m_ParentController.value = value;
            }
        }

        string IPropertyRMProvider.name { get { return "Value"; } }

        object[] IPropertyRMProvider.customAttributes { get { return new object[] { }; } }

        VFXPropertyAttributes IPropertyRMProvider.attributes { get { return new VFXPropertyAttributes(); } }

        public Type portType
        {
            get
            {
                return m_ParentController.model.type;
            }
        }

        int IPropertyRMProvider.depth { get { return 0; } }

        void IPropertyRMProvider.ExpandPath()
        {
            throw new NotImplementedException();
        }

        void IPropertyRMProvider.RetractPath()
        {
            throw new NotImplementedException();
        }

        void IPropertyRMProvider.StartLiveModification() { }
        void IPropertyRMProvider.EndLiveModification() { }

        public override void CollectGizmos()
        {
            if (parentController.isOutput)
                return;
            m_GizmoableAnchors.Clear();
            if (VFXGizmoUtility.HasGizmo(m_ParentController.portType))
            {
                m_GizmoableAnchors.Add(m_ParentController);
            }
        }

        public override void DrawGizmos(VisualEffect component)
        {
            if (currentGizmoable is VFXParameterController gizmoable)
            {
                gizmoable.DrawGizmos(component);
            }
        }

        public override void OnEdgeFromInputGoingToBeRemoved(VFXDataAnchorController myInput)
        {
            base.OnEdgeFromInputGoingToBeRemoved(myInput);
            if (parentController.isOutput)
                infos.linkedSlots.RemoveAll(t => t.inputSlot == myInput.model);
        }

        public override void OnEdgeFromOutputGoingToBeRemoved(VFXDataAnchorController myOutput, VFXDataAnchorController otherInput)
        {
            base.OnEdgeFromOutputGoingToBeRemoved(myOutput, otherInput);
            if (!parentController.isOutput)
                infos.linkedSlots.RemoveAll(t => t.outputSlot == myOutput.model && t.inputSlot == otherInput.model);
        }

        public override Bounds GetGizmoBounds(VisualEffect component)
        {
            return m_ParentController.GetGizmoBounds(component);
        }

        public override int id
        {
            get { return m_Id; }
        }

        public override Vector2 position
        {
            get
            {
                return infos.position;
            }
            set
            {
                infos.position = value;
                model.Invalidate(VFXModel.InvalidationCause.kUIChanged);
            }
        }

        public VFXParameterController parentController
        {
            get { return m_ParentController; }
        }

        public VFXInlineOperator ConvertToInline()
        {
            if (parentController.isOutput)
                return null;
            VFXInlineOperator op = ScriptableObject.CreateInstance<VFXInlineOperator>();
            op.SetSettingValue("m_Type", (SerializableType)parentController.model.type);

            viewController.graph.AddChild(op);

            op.position = position;

            if (infos.linkedSlots != null)
            {
                foreach (var link in infos.linkedSlots.ToArray())
                {
                    var ancestors = new List<VFXSlot>();
                    ancestors.Add(link.outputSlot);
                    VFXSlot parent = link.outputSlot.GetParent();
                    while (parent != null)
                    {
                        ancestors.Add(parent);

                        parent = parent.GetParent();
                    }
                    int index = parentController.model.GetSlotIndex(ancestors.Last());

                    if (index >= 0 && index < op.GetNbOutputSlots())
                    {
                        VFXSlot slot = op.outputSlots[index];
                        for (int i = ancestors.Count() - 2; i >= 0; --i)
                        {
                            int subIndex = ancestors[i + 1].GetIndex(ancestors[i]);

                            if (subIndex >= 0 && subIndex < slot.GetNbChildren())
                            {
                                slot = slot[subIndex];
                            }
                            else
                            {
                                slot = null;
                                break;
                            }
                        }
                        if (slot.path != link.outputSlot.path.Substring(1)) // parameters output are still named 0, inline outputs have no name.
                        {
                            Debug.LogError("New inline don't have the same subslot as old parameter");
                        }
                        else
                        {
                            link.outputSlot.Unlink(link.inputSlot);
                            slot.Link(link.inputSlot);
                        }
                    }
                }
            }

            op.inputSlots[0].value = value;
            viewController.LightApplyChanges();
            viewController.PutInSameGroupNodeAs(viewController.GetNodeController(op, 0), this);
            viewController.RemoveElement(this);
            return op;
        }
    }
}
