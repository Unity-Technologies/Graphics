using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.VFX
{
    [Serializable]
    class VFXSlot : VFXModel<VFXSlot, VFXSlot>
    {
        public enum Direction
        {
            kInput,
            kOutput,
        }

        public Direction direction { get { return m_Direction; } }
        public VFXProperty property { get { return m_Property; } }
        public override string name { get { return m_Property.name; } }

        public virtual VFXExpression expression 
        { 
            get
            {
                return m_CurrentExpression;
            }
            set 
            {
                if (m_CurrentExpression != value)
                {
                    if (!CanConvert(value))
                    {
                        throw new Exception();
                    }

                    m_CurrentExpression = ConvertExpression(value);
                    Invalidate(InvalidationCause.kConnectionChanged); // trigger a rebuild
                }
            }
        }

        public ReadOnlyCollection<VFXSlot> LinkedSlots
        {
            get
            {
                return m_LinkedSlots.AsReadOnly();
            }
        }

        public VFXSlot refSlot
        { 
            get 
            {
                if (direction == Direction.kOutput || !HasLink())
                    return this;
                return m_LinkedSlots[0];
            } 
        }

        public IVFXSlotContainer owner { get { return m_Owner; } }

        protected VFXSlot() {} // For serialization only
        public VFXSlot(Direction direction)
        {
            m_Direction = direction;
        }

        // Create and return a slot hierarchy from a property info
        public static VFXSlot Create(VFXProperty property, Direction direction, VFXExpression defaultExpression = null)
        {
            var desc = VFXLibrary.GetSlot(property.type);
            if (desc != null)
            {
                var slot = desc.CreateInstance();
                slot.m_Direction = direction;
                slot.m_Property = property;
                slot.m_DefaultExpression = defaultExpression;
                slot.m_CurrentExpression = slot.m_DefaultExpression;

                foreach (var subInfo in property.SubProperties())
                {
                    var subSlot = Create(subInfo, direction, null /* TODO : transform base expression to subproperty */);
                    if (subSlot != null)
                        subSlot.Attach(slot);
                }

                return slot;
            }

            throw new InvalidOperationException();
        }
    
        public int GetNbLinks() { return m_LinkedSlots.Count; }
        public bool HasLink() { return GetNbLinks() != 0; }
        
        public bool CanLink(VFXSlot other)
        {
            return direction != other.direction && !m_LinkedSlots.Contains(other) && CanConvert(other.expression);
        }

        public bool Link(VFXSlot other)
        {
            if (other == null)
                return false;

            if (!CanLink(other) || !other.CanLink(this)) // can link
                return false;

            if (other.direction == Direction.kInput)
            {
                InnerLink(other);
                other.InnerLink(this);
            }
            else
            {
                other.InnerLink(this);
                InnerLink(other);
            }

            return true;
        }

        public void Unlink(VFXSlot other)
        {
            if (m_LinkedSlots.Contains(other))
            {
                InnerUnlink(other);
                other.InnerUnlink(this);
            }
        }

        public void UnlinkAll()
        {
            var currentSlots = new List<VFXSlot>(m_LinkedSlots);
            foreach (var slot in currentSlots)
                Unlink(slot);
        }

        private void InnerLink(VFXSlot other)
        {
            // inputs can only be linked to one output at a time
            if (direction == Direction.kInput)
            {
                UnlinkAll();

                // We need to unlink any potential slots link in the hierarchy
                var currentParent = GetParent();
                while (currentParent != null)
                {
                    currentParent.UnlinkAll();
                    currentParent = currentParent.GetParent();
                }

                foreach (var child in children)
                    child.UnlinkAll();
            }

            m_LinkedSlots.Add(other);
            Invalidate(InvalidationCause.kConnectionChanged);
        }

        private void InnerUnlink(VFXSlot other)
        {
            m_LinkedSlots.Remove(other);
            Invalidate(InvalidationCause.kConnectionChanged);
        }

        protected override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            base.Invalidate(model, cause);

            // Propagate to the owner if any
            if (direction == Direction.kInput)
            {
                if (HasLink())
                {
                    if (!CanConvert(refSlot.expression))
                    {
                        //Invalid link, disconnect it
                        UnlinkAll();
                    }
                    else
                    {
                        //Reapply expression
                        expression = refSlot.expression;
                    }
                }
                else
                {
                    //No link anymore, default fallback
                    expression = m_DefaultExpression;
                }

                if (m_Owner != null)
                {
                    m_Owner.Invalidate(cause);
                }
                
            }
            else
            {
                foreach (var link in m_LinkedSlots)
                {
                    link.Invalidate(cause);
                }
            }
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (direction == Direction.kOutput)
            {
                var linkedSlot = m_LinkedSlots.ToArray();
                if (cause == InvalidationCause.kConnectionChanged) // If connection has changed, propagate to linked inputs
                    foreach (var slot in linkedSlot)
                        slot.Invalidate(model, InvalidationCause.kConnectionPropagated);

                if (cause == InvalidationCause.kParamChanged) // If param has changed, propagate to linked inputs
                    foreach (var slot in linkedSlot)
                        slot.Invalidate(model, InvalidationCause.kParamPropagated);
            }
            else // input
            {
                BuildExpression();
            }
        }

        private VFXExpression BuildExpression()
        {
            return null;
        }

        protected virtual bool CanConvert(VFXExpression expression)
        {
            return m_DefaultExpression == null || expression == null || m_DefaultExpression.ValueType == expression.ValueType;
        }
        protected virtual VFXExpression ConvertExpression(VFXExpression expression)
        {
            return expression;
        }


        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_LinkedSlotRefs = m_LinkedSlots.Select(slot => slot.id.ToString()).ToList();
        }

       /* public virtual void OnAfterDeserialize()
        {
            base.OnBeforeSerialize();
        }*/

        //private VFXExpression m_CachedExpression;
        private VFXExpression m_CurrentExpression;
        private VFXExpression m_DefaultExpression;

        [NonSerialized]
        public IVFXSlotContainer m_Owner; // Don't set that directly! Only called by SlotContainer!

        [SerializeField]
        private VFXProperty m_Property;

        [SerializeField]
        private Direction m_Direction;

        private List<VFXSlot> m_LinkedSlots = new List<VFXSlot>();
        [SerializeField]
        private List<string> m_LinkedSlotRefs;
    }

    /*class VFXSlotFloat : VFXSlot<float>
    {

    }*/
}
