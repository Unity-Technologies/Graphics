using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Graphing;
using System.Reflection;

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

        public Direction direction      { get { return m_Direction; } }
        public VFXProperty property     { get { return m_Property; } }
        public override string name     { get { return m_Property.name; } }

        protected VFXSlot() {}

        public object value 
        { 
            get
            {
                if (GetParent() == null)
                {
                    return m_Value.Get();
                }
                else
                {
                    object parentValue = GetParent().value;

                    Type type = GetParent().property.type;
                    FieldInfo info = type.GetField(name);

                    return info.GetValue(parentValue);
                }
            }
            set
            {
                if (GetParent() == null)
                {
                    m_Value.Set(value);
                    owner.Invalidate(InvalidationCause.kParamChanged);
                }
                else
                {
                    object parentValue = GetParent().value;

                    Type type = GetParent().property.type;
                    FieldInfo info = type.GetField(name);

                    info.SetValue(parentValue, value);

                    GetParent().value = parentValue;
                }
            }       
        }    

        public VFXExpression GetExpression()
        {
            if (!m_ExpressionTreeUpToDate)
                RecomputeExpressionTree();

            return m_OutExpression; 
        }

        public void SetExpression(VFXExpression expr)
        {
            if (!expr.Equals(m_LinkedInExpression))
            {
                PropagateToTree(s => s.m_LinkedInExpression = s.DefaultExpr);
                m_LinkedInExpression = expr;
                InvalidateExpressionTree();
            }
        }

        public VFXExpression DefaultExpr
        {
            get
            {
                if (m_DefaultExpression == null)
                    InitDefaultExpression();
                return m_DefaultExpression;
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

        public IVFXSlotContainer owner { get { return m_Owner as IVFXSlotContainer; } }

        public VFXSlot GetTopMostParent() // TODO Cache this instead of walking the hierarchy every time
        {
            if (GetParent() == null)
                return this;
            else
                return GetParent().GetTopMostParent();
        }

        // Create and return a slot hierarchy from a property info
        public static VFXSlot Create(VFXProperty property, Direction direction, object value = null)
        {
            var slot = CreateSub(property, direction, value); // First create slot tree  
            return slot;
        }
     
        private static VFXSlot CreateSub(VFXProperty property, Direction direction, object value)
        {
            var desc = VFXLibrary.GetSlot(property.type);
            if (desc != null)
            {
                var slot = desc.CreateInstance();
                slot.m_Direction = direction;
                slot.m_Property = property;

                slot.m_Value = new VFXSerializableObject(property.type,value);

                foreach (var subInfo in property.SubProperties())
                {
                    var subSlot = CreateSub(subInfo, direction, null);
                    if (subSlot != null)
                        subSlot.Attach(slot,false);
                }

                return slot;
            }

            throw new InvalidOperationException(string.Format("Unable to create slot for property {0} of type {1}",property.name,property.type));
        }

        private void InitDefaultExpression()
        {
            if (GetNbChildren() == 0)
            {
                m_DefaultExpression = DefaultExpression();
            }
            else
            {
                // Depth first
                foreach (var child in children)
                    child.InitDefaultExpression();

                m_DefaultExpression = ExpressionFromChildren(children.Select(c => c.m_DefaultExpression).ToArray());
            }

            if (m_LinkedInExpression == null)
                m_LinkedInExpression = m_DefaultExpression;
        }

        private void ResetExpression()
        {
            if (GetNbChildren() == 0)
                SetExpression(m_DefaultExpression);
            else
            {
                foreach (var child in children)
                    child.ResetExpression();
            }  
        }

        protected override void Invalidate(VFXModel model,InvalidationCause cause)
        {
            if (m_Owner != null && direction == Direction.kInput)
                m_Owner.Invalidate(cause);
        }

        protected override void OnAdded()
        {
            base.OnAdded();

        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
        }

        public override T Clone<T>()
        {
            var clone = base.Clone<T>();
            var cloneSlot = clone as VFXSlot;

            cloneSlot.m_LinkedSlots.Clear();
            return clone;
        }
    
        public int GetNbLinks() { return m_LinkedSlots.Count; }
        public bool HasLink() { return GetNbLinks() != 0; }
        
        public bool CanLink(VFXSlot other)
        {
            return direction != other.direction && !m_LinkedSlots.Contains(other) &&
                ((direction == Direction.kInput && CanConvertFrom(other.property.type)) || (other.CanConvertFrom(property.type)));
        }

        public bool Link(VFXSlot other, bool notify = true)
        {
            if (other == null)
                return false;

            if (!CanLink(other) || !other.CanLink(this)) // can link
                return false;

            if (direction == Direction.kOutput)
                InnerLink(this, other, notify);
            else
                InnerLink(other, this, notify);

            if (notify)
            {
                Invalidate(InvalidationCause.kConnectionChanged);
                other.Invalidate(InvalidationCause.kConnectionChanged);
            }

            return true;
        }

        public void Unlink(VFXSlot other, bool notify = true)
        {
            if (m_LinkedSlots.Contains(other))
            {
                if (direction == Direction.kOutput)
                    InnerUnlink(this, other, notify);
                else
                    InnerUnlink(other, this, notify);

                if (notify)
                {
                    Invalidate(InvalidationCause.kConnectionChanged);
                    other.Invalidate(InvalidationCause.kConnectionChanged);
                }
            }
        }

        protected void PropagateToOwner(Action<IVFXSlotContainer> func)
        {
            if (owner != null)
                func(owner);
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
                return owner;
        }

        private void RecomputeExpressionTree()
        {
            // Start from the top most parent
            var masterSlot = GetTopMostParent();

            // init default expression if needed
            if (masterSlot.m_DefaultExpression == null)
                masterSlot.InitDefaultExpression();

            // Mark all slots in tree as not up to date
            masterSlot.PropagateToChildren(s => s.m_ExpressionTreeUpToDate = false );

            if (direction == Direction.kInput) // For input slots, linked expression are directly taken from linked slots
                masterSlot.PropagateToChildren(s => s.m_LinkedInExpression = s.HasLink() ? s.refSlot.GetExpression() : s.DefaultExpr); // this will trigger recomputation of linked expressions if needed
            else
            {
                var owner = GetOwner();
                if (owner != null)
                    owner.UpdateOutputs();
                else
                    ResetExpression();
            }

            List<VFXSlot> startSlots = new List<VFXSlot>();
            masterSlot.PropagateToChildren( s => {
                if (s.m_LinkedInExpression != s.DefaultExpr) 
                    startSlots.Add(s); 
            });

            if (startSlots.Count == 0) // Default expression
                masterSlot.PropagateToChildren(s => s.m_InExpression = s.DefaultExpr);
            else
            {
                // build expression trees by propagating from start slots
                foreach (var startSlot in startSlots)
                {
                    startSlot.m_InExpression = startSlot.ConvertExpression(startSlot.m_LinkedInExpression); // TODO Handle structural modification;
                    startSlot.PropagateToParent(s => s.m_InExpression = s.ExpressionFromChildren(s.children.Select(c => c.m_InExpression).ToArray()));

                    startSlot.PropagateToChildren(s =>
                    {
                        var exp = s.ExpressionToChildren(s.m_InExpression);
                        for (int i = 0; i < s.GetNbChildren(); ++i)
                            s.GetChild(i).m_InExpression = exp != null ? exp[i] : s.refSlot.GetChild(i).GetExpression(); // Not sure about that
                    });
                }
            }

            var toInvalidate = new HashSet<VFXSlot>();
            masterSlot.SetOutExpression(masterSlot.m_InExpression,toInvalidate);
            masterSlot.PropagateToChildren(s => {
                var exp = s.ExpressionToChildren(s.m_OutExpression);
                for (int i = 0; i < s.GetNbChildren(); ++i)
                    s.GetChild(i).SetOutExpression(exp != null ? exp[i] : s.GetChild(i).m_InExpression,toInvalidate);
            });  

            foreach (var slot in toInvalidate)
                slot.InvalidateExpressionTree();
        }

        private void SetOutExpression(VFXExpression exp,HashSet<VFXSlot> toInvalidate)
        {
            if (m_OutExpression != exp)
            {
                m_OutExpression = exp;
                if (direction == Direction.kInput)
                {
                    var owner = GetOwner();
                    if (owner != null)
                        toInvalidate.UnionWith(owner.outputSlots);
                }
                else
                    toInvalidate.UnionWith(LinkedSlots);
            }

            m_ExpressionTreeUpToDate = true;
        }

        private string GetOwnerType()
        {
            var owner = GetOwner();
            if (owner != null)
                return owner.GetType().Name;
            else
                return "No Owner";
        }

        private void InvalidateExpressionTree()
        {
            var masterSlot = GetTopMostParent();

            masterSlot.PropagateToChildren(s => {
                if (s.m_ExpressionTreeUpToDate)
                {
                    s.m_ExpressionTreeUpToDate = false;
                    if (s.direction == Direction.kOutput)
                        foreach (var linkedSlot in LinkedSlots)
                            linkedSlot.InvalidateExpressionTree();
                }
            });

            if (masterSlot.direction == Direction.kInput)
            {
                var owner = masterSlot.GetOwner();
                if (owner != null)
                {
                    foreach (var slot in owner.outputSlots)
                        slot.InvalidateExpressionTree();
                }
            }
        }

        public void UnlinkAll(bool notify = true)
        {
            var currentSlots = new List<VFXSlot>(m_LinkedSlots);
            foreach (var slot in currentSlots)
                Unlink(slot,notify);
        }

        private static void InnerLink(VFXSlot output,VFXSlot input,bool notify = false)
        {     
            input.UnlinkAll(false); // First disconnect any other linked slot
            input.PropagateToTree(s => s.UnlinkAll(false)); // Unlink other links in tree

            input.m_LinkedSlots.Add(output);
            output.m_LinkedSlots.Add(input);

            input.InvalidateExpressionTree();
        }

        private static void InnerUnlink(VFXSlot output, VFXSlot input, bool notify = false)
        {
            output.m_LinkedSlots.Remove(input);
            if (input.m_LinkedSlots.Remove(output))
                input.InvalidateExpressionTree();
        }

        protected virtual bool CanConvertFrom(VFXExpression expr)
        {
            return expr == null || DefaultExpr.ValueType == expr.ValueType;
        }

        protected virtual bool CanConvertFrom(Type type)
        {
            return type == null || property.type == type;
        }

        protected virtual VFXExpression ConvertExpression(VFXExpression expression)
        {
            return expression;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (m_LinkedSlots == null)
                m_LinkedSlots = new List<VFXSlot>();

            int nbRemoved = m_LinkedSlots.RemoveAll(c => c == null);// Remove bad references if any
            if (nbRemoved > 0)
                Debug.Log(String.Format("Remove {0} linked slot(s) that couldnt be deserialized from {1} of type {2}", nbRemoved, name, GetType()));

            m_ExpressionTreeUpToDate = false;
        }

        protected virtual VFXExpression[] ExpressionToChildren(VFXExpression exp)   { return null; }
        protected virtual VFXExpression ExpressionFromChildren(VFXExpression[] exp) { return null; }

        protected virtual VFXValue DefaultExpression() 
        {
            return null; 
        }

        // Expression cache
        private VFXExpression m_DefaultExpression; // The default expression
        private VFXExpression m_LinkedInExpression; // The current linked expression to the slot
        private VFXExpression m_InExpression; // correctly converted expression
        private VFXExpression m_OutExpression; // output expression that can be fetched

        [NonSerialized] // This must not survive domain reload !
        private bool m_ExpressionTreeUpToDate = false;

        // TODO currently not used
        [Serializable]
        private class MasterData : ISerializationCallbackReceiver
        {
            public VFXModel m_Owner;
            [NonSerialized]
            public object m_Value;
            [SerializeField]
            public SerializationHelper.JSONSerializedElement m_SerializedValue;

            public virtual void OnBeforeSerialize()
            {
                if (m_Value != null)
                    m_SerializedValue = SerializationHelper.Serialize(m_Value);
                else
                    m_SerializedValue.Clear();
            }

            public virtual void OnAfterDeserialize()
            {
                m_Value = !m_SerializedValue.Empty ? SerializationHelper.Deserialize<object>(m_SerializedValue, null) : null;
            }
        }

        [SerializeField]
        private VFXSlot m_MasterSlot;
        [SerializeField]
        private MasterData m_MasterData;

        [SerializeField]
        public VFXModel m_Owner;

        [SerializeField]
        private VFXProperty m_Property;

        [SerializeField]
        private Direction m_Direction;

        [SerializeField]
        private List<VFXSlot> m_LinkedSlots;

        [SerializeField]
        private VFXSerializableObject m_Value;
    }
}
