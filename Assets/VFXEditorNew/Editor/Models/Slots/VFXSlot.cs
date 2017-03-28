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

        public VFXExpression expression 
        { 
            set { SetInExpression(value); }
            get { return m_OutExpression; }
        }

        // Explicit setter to be able to not notify
        public void SetExpression(VFXExpression expr, bool notify = true)
        {
            SetInExpression(expr,true,notify);
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

        public VFXSlot GetTopMostParent()
        {
            if (GetParent() == null)
                return this;
            else
                return GetParent().GetTopMostParent();
        }

        protected VFXSlot() {} // For serialization only
        
        // TODO Remove that. Slot must be created via the static create method in order to correctly build the slot tree
        private VFXSlot(Direction direction)
        {
            m_Direction = direction;
        }

        // Create and return a slot hierarchy from a property info
        public static VFXSlot Create(VFXProperty property, Direction direction)
        {
            var slot = CreateSub(property, direction); // First create slot tree
            slot.InitExpression(); // Initialize expressions
            return slot;
        }
     
        private static VFXSlot CreateSub(VFXProperty property, Direction direction)
        {
            var desc = VFXLibrary.GetSlot(property.type);
            if (desc != null)
            {
                var slot = desc.CreateInstance();
                slot.m_Direction = direction;
                slot.m_Property = property;
                //slot.m_Value = System.Activator.CreateInstance(slot.m_Property.type);

                foreach (var subInfo in property.SubProperties())
                {
                    var subSlot = CreateSub(subInfo, direction);
                    if (subSlot != null)
                        subSlot.Attach(slot,false);
                }

                return slot;
            }

            throw new InvalidOperationException(string.Format("Unable to create slot for property {0} of type {1}",property.name,property.type));
        }

        private void InitExpression()
        {
            if (GetNbChildren() == 0)
            {
                m_DefaultExpression = DefaultExpression();
                //TODOPAUL : be able to create VFXExpression from FloatN reference value
                //if (m_DefaultExpression == null)
                //    throw new NotImplementedException("Default expression must return a VFXValue for slot of type: " + this.ToString());
            }
            else
            {
                // Depth first
                foreach (var child in children)
                    child.InitExpression();

                m_DefaultExpression = ExpressionFromChildren(children.Select(c => c.m_InExpression).ToArray());
            }

            m_InExpression = m_OutExpression = m_DefaultExpression;
        }

        private void ResetExpression()
        {
            if (GetNbChildren() == 0)
                SetInExpression(m_DefaultExpression,false,false);
            else
            {
                foreach (var child in children)
                    child.ResetExpression();
            }  
        }
    
        public int GetNbLinks() { return m_LinkedSlots.Count; }
        public bool HasLink() { return GetNbLinks() != 0; }
        
        public bool CanLink(VFXSlot other)
        {
            return direction != other.direction && !m_LinkedSlots.Contains(other) && 
                ((direction == Direction.kInput && CanConvertFrom(other.expression)) || (other.CanConvertFrom(expression)));
        }

        public bool Link(VFXSlot other, bool notify = true)
        {
            if (other == null)
                return false;

            if (!CanLink(other) || !other.CanLink(this)) // can link
                return false;

            //if (other.direction == Direction.kInput)
           // {
                InnerLink(other, notify);
                other.InnerLink(this, notify);
            //}
            /*else
            {
                other.InnerLink(this, notify);
                InnerLink(other, notify);
            }*/

            return true;
        }

        public void Unlink(VFXSlot other, bool notify = true)
        {
            if (m_LinkedSlots.Contains(other))
            {
                InnerUnlink(other,notify);
                other.InnerUnlink(this,notify);
            }
        }

        protected void PropagateToOwner(Action<IVFXSlotContainer> func)
        {
            if (m_Owner != null)
                func(m_Owner);
            else
            {
                var parent = GetParent();
                if (parent != null)
                    parent.PropagateToOwner(func);
            }
        }

        protected void PropagateToParent(Action<VFXSlot> func)
        {
            var parent = GetParent();
            if (parent != null)
            {
                func(parent);
                parent.PropagateToParent(func);   
            }
        }

        protected void PropagateToChildren(Action<VFXSlot> func)
        {
            func(this);
            foreach (var child in children) 
                child.PropagateToChildren(func);
        }

        protected void PropagateToTree(Action<VFXSlot> func)
        {
            PropagateToParent(func);
            PropagateToChildren(func);
        }

        protected IVFXSlotContainer GetOwner()
        {
            var parent = GetParent();
            if (parent != null)
                return parent.GetOwner();
            else
                return m_Owner;
        }

        private static void RecomputeExpressionTree(VFXSlot slot,bool notify = true)
        {
            // Start from the top most parent
            var masterSlot = slot.GetTopMostParent();

            List<VFXSlot> startSlots = new List<VFXSlot>();

            // First set the linked expression in case of output nodes (For input linked expression is set explicitly)
            if (masterSlot.direction == Direction.kOutput)
                masterSlot.PropagateToChildren( s => {
                    s.m_LinkedInExpression = s.HasLink() ? s.refSlot.m_OutExpression : s.m_DefaultExpression;
            });

            // Collect linked expression
            masterSlot.PropagateToChildren( s => {
                if (s.m_LinkedInExpression != s.m_DefaultExpression) 
                    startSlots.Add(s); 
            });

            bool earlyOut = true;

            // build expression trees by propagating from start slots
            foreach (var startSlot in startSlots)
            {
                if (!startSlot.CanConvertFrom(startSlot.m_LinkedInExpression))
                    throw new ArgumentException("Cannot convert expression");

                var newExpression = startSlot.ConvertExpression(startSlot.m_LinkedInExpression);
                if (newExpression == startSlot.m_InExpression) // already correct, early out
                    continue;

                earlyOut = false;
                startSlot.m_InExpression = newExpression;
                startSlot.PropagateToParent(s => s.m_InExpression = s.ExpressionFromChildren(s.children.Select(c => c.m_InExpression).ToArray()));

                startSlot.PropagateToChildren(s => {
                    var exp = s.ExpressionToChildren(s.m_InExpression);
                    for (int i = 0; i < s.GetNbChildren(); ++i)
                        s.GetChild(i).m_InExpression = exp != null ? exp[i] : s.refSlot.GetChild(i).expression;
                });
            }

            if (earlyOut)
                return;

            List<VFXSlot> toPropagate = new List<VFXSlot>();

            // Finally derive output expressions
            if (masterSlot.SetOutExpression(masterSlot.m_InExpression))
                toPropagate.Add(masterSlot);
            masterSlot.PropagateToChildren(s => {
                var exp = s.ExpressionToChildren(s.m_OutExpression);
                for (int i = 0; i < s.GetNbChildren(); ++i)
                {
                    var child = s.GetChild(i);
                    if (child.SetOutExpression(exp != null ? exp[i] : child.m_InExpression))
                        toPropagate.Add(child);
                }
            });  
 
            // Set expression to be up to date
            masterSlot.PropagateToChildren( s => s.m_ExpressionUpToDate = true );   

            if (notify && masterSlot.m_Owner != null)
                masterSlot.m_Owner.Invalidate(InvalidationCause.kStructureChanged);

            var dirtyMasterSlots = new HashSet<VFXSlot>(toPropagate.Select(s => s.GetTopMostParent()));
            foreach (var dirtySlot in dirtyMasterSlots)
                RecomputeExpressionTree(dirtySlot,notify);
        }

        private void SetInExpression(VFXExpression expression, bool propagateDown = true, bool notify = true)
        {
            if (!CanConvertFrom(expression))
                throw new ArgumentException("Cannot convert expression");

            var newExpression = ConvertExpression(expression);
            if (newExpression == m_InExpression)
                return; // No change, early out

            // First propagate to tree up and down from modified slot
            m_InExpression = newExpression;
            PropagateToParent(s => s.m_InExpression = s.ExpressionFromChildren(s.children.Select(c => c.m_InExpression).ToArray()));
            if (propagateDown)
                PropagateToChildren(s => {
                    var exp = s.ExpressionToChildren(s.m_InExpression);
                    if (exp != null)
                        for (int i = 0; i < s.GetNbChildren(); ++i)
                            s.GetChild(i).m_InExpression = exp[i];
                    else //if (s.GetNbChildren() == s.refSlot.GetNbChildren()) // TODO tmp. Not the right test, we must ensure connected slot children are compatible
                    {
                        for (int i = 0; i < s.GetNbChildren(); ++i)
                            s.GetChild(i).m_InExpression = s.refSlot.GetChild(i).m_InExpression;
                    }
                    /*else
                        for (int i = 0; i < s.GetNbChildren(); ++i)
                            s.GetChild(i).m_InExpression = null;*/
                });

            // Then find top most slot and propagate back to children
            var topParent = GetTopMostParent();

            HashSet<VFXSlot> modifiedLinks = new HashSet<VFXSlot>();

            topParent.SetOutExpression(topParent.m_InExpression);
            topParent.PropagateToChildren(s => {
                var exp = s.ExpressionToChildren(s.m_OutExpression);
                if (exp != null)
                {
                    for (int i = 0; i < s.GetNbChildren(); ++i)
                        if (s.GetChild(i).SetOutExpression(exp[i]))
                            foreach (var slot in s.GetChild(i).LinkedSlots)
                                modifiedLinks.Add(slot);
                }
                else
                {
                    for (int i = 0; i < s.GetNbChildren(); ++i)
                        if (s.GetChild(i).SetOutExpression(s.GetChild(i).m_InExpression))
                            foreach (var slot in s.GetChild(i).LinkedSlots)
                                modifiedLinks.Add(slot);
                }
            });

            // Finally notify owner
            if (notify)
            {
                NotifyOwner();
            }
        }

        private void NotifyOwner()
        {
            PropagateToOwner(o => o.Invalidate(VFXModel.InvalidationCause.kConnectionChanged));
        }

        private bool SetOutExpression(VFXExpression expr)
        {
            if (m_OutExpression != expr)
            {
                m_OutExpression = expr;

                if (direction == Direction.kOutput)
                {
                    var toRemove = LinkedSlots.Where(s => !s.CanConvertFrom(expr)); // Break links that are no more valid
                    foreach (var slot in toRemove) 
                        Unlink(slot);

                    foreach (var slot in LinkedSlots)
                        SetInExpression(m_OutExpression, true, true);
                }
            }
            return true;
        }

        public void UnlinkAll(bool notify = true)
        {
            var currentSlots = new List<VFXSlot>(m_LinkedSlots);
            foreach (var slot in currentSlots)
                Unlink(slot,notify);
        }

        private void InnerLink(VFXSlot other,bool notify)
        {
            if (direction == Direction.kInput)
            {
                UnlinkAll(false); // First disconnect any other linked slot
                PropagateToTree(s => s.UnlinkAll(false)); // Unlink other links in tree
                m_LinkedSlots.Add(other);
                SetInExpression(other.m_OutExpression,true, notify);
            }
            else
                m_LinkedSlots.Add(other);
        }

        private void InnerUnlink(VFXSlot other, bool notify)
        {
            if (m_LinkedSlots.Remove(other))
            {
                if (direction == Direction.kInput)
                {
                    ResetExpression();

                    //if (notify)
                        PropagateToOwner(o => o.Invalidate(VFXModel.InvalidationCause.kConnectionChanged));
                }
                else
                {
                    // TODO
                }
            }
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (cause == VFXModel.InvalidationCause.kStructureChanged)
            {
                if (HasLink())
                    throw new InvalidOperationException();

                SetInExpression(ExpressionFromChildren(children.Select(c => c.m_InExpression).ToArray()));
            }
        }

        protected virtual bool CanConvertFrom(VFXExpression expression)
        {
            return expression == null || m_InExpression.ValueType == expression.ValueType;
        }

        protected virtual VFXExpression ConvertExpression(VFXExpression expression)
        {
            return expression;
        }

        protected virtual VFXExpression[] ExpressionToChildren(VFXExpression exp)   { return null; }
        protected virtual VFXExpression ExpressionFromChildren(VFXExpression[] exp) { return null; }

        protected virtual VFXValue DefaultExpression() 
        {
            return null; 
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

        private VFXExpression m_DefaultExpression;
        private VFXExpression m_LinkedInExpression;
        private VFXExpression m_InExpression;
        private VFXExpression m_OutExpression;
        private bool m_ExpressionUpToDate = false;

        private VFXSlot m_MasterSlot;

        [NonSerialized]
        public IVFXSlotContainer m_Owner; // Don't set that directly! Only called by SlotContainer!

        [SerializeField]
        private VFXProperty m_Property;

        private object m_Value;

        [SerializeField]
        private Direction m_Direction;

        private List<VFXSlot> m_LinkedSlots = new List<VFXSlot>();
        [SerializeField]
        private List<string> m_LinkedSlotRefs;
    }
}
