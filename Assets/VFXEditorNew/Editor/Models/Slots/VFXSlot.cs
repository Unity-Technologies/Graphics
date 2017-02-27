using System;
using System.Collections.Generic;
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
        public static VFXSlot Create(VFXProperty info, Direction direction)
        {
            var desc = VFXLibrary.GetSlot(info.type);
            if (desc != null)
            {
                var slot = desc.CreateInstance();
                slot.m_Direction = direction;
                slot.m_Property = info;

                foreach (var subInfo in info.SubProperties())
                {
                    var subSlot = Create(subInfo, direction);
                    if (subSlot != null)
                        subSlot.Attach(slot);
                }

                return slot;
            }

            // TODO log error
            return null;
        }
    
        public int GetNbLinks() { return m_LinkedSlots.Count; }
        public bool HasLink() { return GetNbLinks() != 0; }
        
        public bool CanLink(VFXSlot other)
        {
            return direction != other.direction && !m_LinkedSlots.Contains(other);
        }

        public bool Link(VFXSlot other)
        {
            if (!CanLink(other) || !other.CanLink(this))
                return false;

            InnerLink(other);
            if (other != null)
                other.InnerLink(this);

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
                UnlinkAll();

            m_LinkedSlots.Add(other);
            Invalidate(InvalidationCause.kConnectionChanged);
        }

        private void InnerUnlink(VFXSlot other)
        {
            m_LinkedSlots.Remove(other);
            Invalidate(InvalidationCause.kConnectionChanged);
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (direction == Direction.kOutput)
            {
                if (cause == InvalidationCause.kConnectionChanged) // If connection has changed, propagate to linked inputs
                    foreach (var slot in m_LinkedSlots)
                        slot.Invalidate(model, InvalidationCause.kConnectionPropagated);

                if (cause == InvalidationCause.kParamChanged) // If param has changed, propagate to linked inputs
                    foreach (var slot in m_LinkedSlots)
                        slot.Invalidate(model, InvalidationCause.kParamPropagated);
            }
        }

        public virtual void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_LinkedSlotRefs = m_LinkedSlots.Select(slot => slot.id.ToString()).ToList();
        }

       /* public virtual void OnAfterDeserialize()
        {
            base.OnBeforeSerialize();
        }*/

        [NonSerialized]
        public IVFXSlotContainer m_Owner; // Dont set that directly! Only called by SlotContainer!

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
