using UnityEngine;
using UnityEditor; // Shouldnt be included!
using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.VFX
{
    public interface VFXPropertyNodeOwner
    {
        void OnUpdated(VFXPropertyNode node);
    }

    public class VFXPropertyNode
    {
        protected VFXPropertyNode m_Input;
        protected List<VFXPropertyValue> m_Outputs;

        public abstract VFXPropertyTypeSemantics Semantics { get; }

        public bool AddOutput(VFXPropertyValue node)
        {
            if (node == null || node == this)
                throw new ArgumentException("Bad output");

            if (!node.Semantics.CanTransform(Semantics))
                throw new ArgumentException("Property nodes are incompatible");

            if (m_Outputs.IndexOf(node) != -1) // Already bound
                return false;

            if (node.m_Input != null)
                node.RemoveInput();

            m_Outputs.Add(node);
            node.Refresh();
            return true;
        }

        public void RemoveOutput(VFXPropertyValue node)
        {
            if (m_Outputs.Remove(node))
                node.m_Input = null;
            node.Refresh();
        }
     
        public virtual bool IsKnown()       { return false; } // Is the value known at compile time within the asset? If unknown, an expression value must be propagated
        public virtual bool IsConstant()    { return false; } // Can the value be considered as constant within the asset. Allow optimization via constant propagation
    }

    class VFXPropertyValue : VFXPropertyNode
    {
        private VFXPropertyNodeOwner m_Owner;

        private VFXPropertyValue m_Parent;
        protected VFXPropertyValue[] m_Children;

        private VFXProperty m_Desc;
        private VFXValue m_Value;

        // Used during processing of values
        private bool m_BeingProcessed = false;
        private VFXValue m_OldValue; // Keep old value for propagation
        private bool m_Constraining = false; // Currently in constrained mode (needs that to avoid infinite pingponging)

        public VFXPropertyValue(VFXProperty desc,VFXPropertyNodeOwner owner = null)
        {
            m_Owner = owner;
            m_Desc = desc;
            m_Value = VFXValue.Create(Semantics.GetValueType());
            Semantics.Default(this);
            m_Outputs = new List<VFXPropertyValue>();

            VFXProperty[] children = Semantics.GetChildren();
            if (children != null)
            {
                int nbChildren = children.Length;
                m_Children = new VFXPropertyValue[nbChildren];
                for (int i = 0; i < nbChildren; ++i)
                    m_Children[i] = new VFXPropertyValue(this,children[i],m_Owner);
            }
            else
                m_Children = new VFXPropertyValue[0];
            
        }

        // Called from inside to create 
        private VFXPropertyValue(VFXPropertyValue parent,VFXProperty desc,VFXPropertyNodeOwner owner = null)
            : this(desc,owner)
        {
            m_Parent = parent;
        }

        public override bool IsKnown()      { return true; }
        public override bool IsConstant()   { return false; } // TODO

        private void Constrain()
        {
            if (!m_Constraining) // If not already constraining
            {
                m_Constraining = true;
                Semantics.Constrain(this); // Constrained from bottom to top, parent is supposed to keep children constraints !
                m_Parent.Constrain();
                m_Constraining = false;
            }

            string verbatim = @"this
                is a test";
        }

        private VFXPropertyValue GetRoot()
        {
            return m_Parent != null ? m_Parent.GetRoot() : this;
        }

        private void MarkBeingProcessedRecursively(bool beginProcessed)
        {
            m_BeingProcessed = beginProcessed;
            foreach (var child in m_Children)
                MarkBeingProcessedRecursively(beginProcessed);
        }

        // Useful to be called when a series of Set is performed to avoid notify outputs/owner after each set
        public bool BeginUpdateProcess()
        {
            if (m_BeingProcessed)
                return false;

            GetRoot().MarkBeingProcessedRecursively(true);
            return true;
        }

        // Will trigger propagation if anything has changed
        public void EndUpdateProcess()
        {
            var root = GetRoot();
            root.MarkBeingProcessedRecursively(false);
            root.PropagateChanges();
        }

        private bool PropagateChanges()
        {
            bool dirty = m_OldValue != null && !m_OldValue.Equals(m_Value);
            m_OldValue = null;

            foreach (var child in m_Children)
                dirty |= child.PropagateChanges();

            if (dirty)
            {
                if (m_Owner != null)
                    m_Owner.OnUpdated(this);
                foreach (var output in m_Outputs)
                    output.Refresh();
            }

            return dirty;
        }

        public void Set<T>(T val)
        {
            var value = VFXValue.Create<T>(); // TODO Needs a pool for shader value
            Set(value);
        }

        public T Get<T>()
        {
            return GetValue().Get<T>();
        }

        public void SetValue(VFXValue value)
        {
            if (!m_Value.Equals(value)) // Only if value has changed
            {
                bool initialChange = BeginUpdateProcess(); // If this is the initial change, this object is responsible to propagate the changes later on

                if (m_Value != null && m_OldValue == null) // If value not already cached
                    m_OldValue = m_Value.Clone(); // TODO Needs a pool for shader value

                m_Value.SetValue(value);
                Constrain(); // This may invalidate other values, hence the old value caching       

                if (initialChange) // Now trigger refresh for linked nodes from the initial change
                    EndUpdateProcess();
            }
        }

        public VFXValue GetValue()
        {
            return m_Value;
        }

        public void Refresh()
        {
            if (m_Input != null)
            {
                BeginUpdateProcess();
                Semantics.Transform(this, m_Input); // This is not supposed to throw as the compatibility was ensured when linking
                EndUpdateProcess();
            }
        }

        public void SetInput(VFXPropertyValue link)
        {
            if (m_Input != link && link.Semantics.CanTransform(Semantics))
            {
                if (link != null)
                    link.m_Outputs.Remove(this);

                m_Input = this;
                link.m_Outputs.Add(this);
                Refresh();
            }
        }

        public VFXPropertyTypeSemantics Semantics
        {
            get { return m_Desc.m_Type; }
        }

        public void SetInput(VFXPropertyNode node)
        {
            node.AddOutput(this);
        }
  
        public void RemoveInput()
        {
            if (m_Input != null)
                m_Input.RemoveOutput(this);
        }
    }
}
