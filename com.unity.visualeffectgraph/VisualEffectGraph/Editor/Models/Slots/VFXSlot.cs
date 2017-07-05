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

        public bool expanded = false;

        protected VFXSlot() {}

        public object value
        {
            get
            {
                try
                {
                    if (IsMasterSlot())
                    {
                        return GetMasterData().m_Value.Get();
                    }
                    else
                    {
                        object parentValue = GetParent().value;

                        Type type = GetParent().property.type;
                        FieldInfo info = type.GetField(name);

                        return info.GetValue(parentValue);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(string.Format("Exception while getting value for slot {0} of type {1}: {2}\n{3}", name, GetType(), e, e.StackTrace));
                }
                return null;
            }
            set
            {
                try
                {
                    if (IsMasterSlot())
                    {
                        GetMasterData().m_Value.Set(value);
                        UpdateDefaultExpressionValue();
                        if (owner != null)
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
                catch (Exception e)
                {
                    Debug.LogError(string.Format("Exception while setting value for slot {0} of type {1}: {2}\n{3}", name, GetType(), e, e.StackTrace));
                }
            }
        }

        public string path
        {
            get
            {
                if (GetParent() != null)
                    return string.Format("{0}.{1}", GetParent().path, name);
                else
                    return name;
            }
        }

        public int depth
        {
            get
            {
                if (GetParent() == null)
                {
                    return 0;
                }
                else
                {
                    return GetParent().depth + 1;
                }
            }
        }

        public string fullName
        {
            get
            {
                string name = property.name;
                if (GetParent() != null)
                    name = GetParent().fullName + "_" + name;
                return name;
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
                PropagateToTree(s => s.m_LinkedInExpression = null);
                m_LinkedInExpression = expr;
                InvalidateExpressionTree();
            }
        }

        // Get relevant expressions in the slot hierarchy
        public void GetExpressions(HashSet<VFXExpression> expressions)
        {
            var exp = GetExpression();
            if (exp != null)
                expressions.Add(exp);
            else
                foreach (var child in children)
                    child.GetExpressions(expressions);
        }

        // Get relevant slots
        public IEnumerable<VFXSlot> GetExpressionSlots()
        {
            var exp = GetExpression();
            if (exp != null)
                yield return this;
            else
                foreach (var child in children)
                {
                    var exps = child.GetExpressionSlots();
                    foreach (var e in exps)
                        yield return e;
                }
        }

        public VFXExpression DefaultExpr
        {
            get
            {
                return m_DefaultExpression;
            }
        }

        public IEnumerable<VFXSlot> LinkedSlots
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

        public IVFXSlotContainer owner { get { return GetMasterData().m_Owner as IVFXSlotContainer; } }

        public bool IsMasterSlot()          { return m_MasterSlot == this; }
        public VFXSlot GetMasterSlot()      { return m_MasterSlot; }
        private MasterData GetMasterData()  { return GetMasterSlot().m_MasterData; }

        // Never call this directly ! Called only by VFXSlotContainerModel
        public void SetOwner(VFXModel owner)
        {
            if (IsMasterSlot())
                m_MasterData.m_Owner = owner;
            else
                throw new InvalidOperationException();
        }

        // Create and return a slot hierarchy from a property info
        public static VFXSlot Create(VFXProperty property, Direction direction, object value = null)
        {
            var slot = CreateSub(property, direction); // First create slot tree

            var masterData = new MasterData();
            masterData.m_Owner = null;
            masterData.m_Value = new VFXSerializableObject(property.type, value);

            slot.PropagateToChildren(s => {
                    s.m_MasterSlot = slot;
                    s.m_MasterData = null;
                });

            slot.m_MasterData = masterData;
            slot.UpdateDefaultExpressionValue();

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

                foreach (var subInfo in property.SubProperties())
                {
                    var subSlot = CreateSub(subInfo, direction);
                    if (subSlot != null)
                    {
                        subSlot.Attach(slot, false);
                    }
                }

                return slot;
            }

            throw new InvalidOperationException(string.Format("Unable to create slot for property {0} of type {1}", property.name, property.type));
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

            if (!IsMasterSlot())
                m_MasterData = null; // Non master slot will always have a null master data
        }

        private void SetDefaultExpressionValue()
        {
            var val = value;
            if (m_DefaultExpression is VFXValue)
                ((VFXValue)m_DefaultExpression).SetContent(val);
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

            m_DefaultExpressionInitialized = true;
        }

        private void UpdateDefaultExpressionValue()
        {
            if (!m_DefaultExpressionInitialized)
                InitDefaultExpression();
            GetMasterSlot().PropagateToChildren(s => s.SetDefaultExpressionValue());
        }

        void InvalidateChildren(VFXModel model, InvalidationCause cause)
        {
            foreach (var child in children)
            {
                child.OnInvalidate(model, cause);
                child.InvalidateChildren(model, cause);
            }
        }

        protected override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            base.Invalidate(model, cause);

            // TODO this breaks the rule that invalidate propagate upwards only
            // Remove this and handle the downwards propagation in the delegate directly if needed!
            InvalidateChildren(model, cause);

            var owner = this.owner;
            if (owner != null  && direction == Direction.kInput)
                owner.Invalidate(cause);
        }

        public override T Clone<T>()
        {
            var clone = base.Clone<T>();
            var cloneSlot = clone as VFXSlot;

            cloneSlot.m_LinkedSlots.Clear();
            return clone;
        }

        protected override void OnAdded()
        {
            base.OnAdded();

            var parent = GetParent();
            PropagateToChildren(s =>
                {
                    s.m_MasterData = null;
                    s.m_MasterSlot = parent.m_MasterSlot;
                });
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();

            var masterData = new MasterData();
            masterData.m_Owner = null;
            masterData.m_Value = new VFXSerializableObject(property.type, value);

            PropagateToChildren(s => {
                    s.m_MasterData = null;
                    s.m_MasterSlot = this;
                });
            m_MasterData = masterData;
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
                InnerLink(this, other);
            else
                InnerLink(other, this);

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
                    InnerUnlink(this, other);
                else
                    InnerUnlink(other, this);

                if (notify)
                {
                    Invalidate(InvalidationCause.kConnectionChanged);
                    other.Invalidate(InvalidationCause.kConnectionChanged);
                }
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

        private void RecomputeExpressionTree()
        {
            // Start from the top most parent
            var masterSlot = GetMasterSlot();

            // When deserializing, default expression wont be initialized
            if (!m_DefaultExpressionInitialized)
                masterSlot.UpdateDefaultExpressionValue();

            // Mark all slots in tree as not up to date
            masterSlot.PropagateToChildren(s => { s.m_ExpressionTreeUpToDate = false; });

            if (direction == Direction.kInput) // For input slots, linked expression are directly taken from linked slots
                masterSlot.PropagateToChildren(s => s.m_LinkedInExpression = s.HasLink() ? s.refSlot.GetExpression() : null); // this will trigger recomputation of linked expressions if needed
            else
            {
                if (owner != null)
                {
                    owner.UpdateOutputs();
                    // Update outputs can trigger an invalidate, it can be reentrant. Just check if we're up to date after that and early out
                    if (m_ExpressionTreeUpToDate)
                        return;
                }
                else
                    masterSlot.PropagateToChildren(s => s.m_LinkedInExpression = null);
            }

            List<VFXSlot> startSlots = new List<VFXSlot>();
            masterSlot.PropagateToChildren(s => {
                    if (s.m_LinkedInExpression != null)
                        startSlots.Add(s);

                    // Initialize in expression to linked (will be overwritten later on for some slots)
                    s.m_InExpression = s.m_DefaultExpression;
                });

            // First pass set in expression and propagate to children
            foreach (var startSlot in startSlots)
            {
                startSlot.m_InExpression = startSlot.ConvertExpression(startSlot.m_LinkedInExpression); // TODO Handle structural modification
                startSlot.PropagateToChildren(s =>
                    {
                        var exp = s.ExpressionToChildren(s.m_InExpression);
                        for (int i = 0; i < s.GetNbChildren(); ++i)
                            s.GetChild(i).m_InExpression = exp != null ? exp[i] : s.refSlot.GetChild(i).GetExpression(); // Not sure about that
                    });
            }

            // Then propagate to parent
            foreach (var startSlot in startSlots)
                startSlot.PropagateToParent(s => s.m_InExpression = s.ExpressionFromChildren(s.children.Select(c => c.m_InExpression).ToArray()));

            var toInvalidate = new HashSet<VFXSlot>();
            masterSlot.SetOutExpression(masterSlot.m_InExpression, toInvalidate);
            masterSlot.PropagateToChildren(s => {
                    var exp = s.ExpressionToChildren(s.m_OutExpression);
                    for (int i = 0; i < s.GetNbChildren(); ++i)
                        s.GetChild(i).SetOutExpression(exp != null ? exp[i] : s.GetChild(i).m_InExpression, toInvalidate);
                });

            foreach (var slot in toInvalidate)
                slot.InvalidateExpressionTree();
        }

        private void SetOutExpression(VFXExpression exp, HashSet<VFXSlot> toInvalidate)
        {
            exp = VFXPropertyAttribute.ApplyToExpressionGraph(m_Property.attributes, exp);

            if (m_OutExpression != exp)
            {
                m_OutExpression = exp;
                if (direction == Direction.kInput)
                {
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
            if (owner != null)
                return owner.GetType().Name;
            else
                return "No Owner";
        }

        private void InvalidateExpressionTree()
        {
            var masterSlot = GetMasterSlot();

            masterSlot.PropagateToChildren(s => {
                    if (s.m_ExpressionTreeUpToDate)
                    {
                        s.m_ExpressionTreeUpToDate = false;
                        if (s.direction == Direction.kOutput)
                            foreach (var linkedSlot in LinkedSlots.ToArray()) // To array as this can be reentrant...
                                linkedSlot.InvalidateExpressionTree();
                    }
                });

            if (masterSlot.direction == Direction.kInput)
            {
                if (owner != null)
                {
                    foreach (var slot in owner.outputSlots.ToArray())
                        slot.InvalidateExpressionTree();
                }
            }

            if (owner != null && direction == Direction.kInput)
                owner.Invalidate(InvalidationCause.kExpressionInvalidated);
        }

        public void UnlinkAll(bool notify = true)
        {
            var currentSlots = new List<VFXSlot>(m_LinkedSlots);
            foreach (var slot in currentSlots)
                Unlink(slot, notify);
        }

        private static void InnerLink(VFXSlot output, VFXSlot input)
        {
            input.UnlinkAll(true); // First disconnect any other linked slot
            input.PropagateToTree(s => s.UnlinkAll(true)); // Unlink other links in tree

            input.m_LinkedSlots.Add(output);
            output.m_LinkedSlots.Add(input);

            input.InvalidateExpressionTree();
        }

        private static void InnerUnlink(VFXSlot output, VFXSlot input)
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
        [NonSerialized]
        private bool m_DefaultExpressionInitialized = false;

        [Serializable]
        private class MasterData
        {
            public VFXModel m_Owner;
            public VFXSerializableObject m_Value;
        }

        [SerializeField]
        private VFXSlot m_MasterSlot;
        [SerializeField]
        private MasterData m_MasterData; // always null for none master slots

        [SerializeField]
        private VFXProperty m_Property;

        [SerializeField]
        private Direction m_Direction;

        [SerializeField]
        private List<VFXSlot> m_LinkedSlots;
    }
}
