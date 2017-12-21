using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Graphing;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    interface IVFXSlotContainer
    {
        ReadOnlyCollection<VFXSlot> inputSlots     { get; }
        ReadOnlyCollection<VFXSlot> outputSlots    { get; }

        int GetNbInputSlots();
        int GetNbOutputSlots();

        VFXSlot GetInputSlot(int index);
        VFXSlot GetOutputSlot(int index);

        void AddSlot(VFXSlot slot);
        void RemoveSlot(VFXSlot slot);

        void UpdateOutputExpressions();

        void Invalidate(VFXModel.InvalidationCause cause);

        void SetSettingValue(string name, object value);

        bool collapsed { get; set; }
    }

    abstract class VFXSlotContainerModel<ParentType, ChildrenType> : VFXModel<ParentType, ChildrenType>, IVFXSlotContainer
        where ParentType : VFXModel
        where ChildrenType : VFXModel
    {
        public virtual ReadOnlyCollection<VFXSlot> inputSlots  { get { return m_InputSlots.AsReadOnly(); } }
        public virtual ReadOnlyCollection<VFXSlot> outputSlots { get { return m_OutputSlots.AsReadOnly(); } }

        public virtual int GetNbInputSlots()            { return m_InputSlots.Count; }
        public virtual int GetNbOutputSlots()           { return m_OutputSlots.Count; }

        public virtual VFXSlot GetInputSlot(int index)  { return m_InputSlots[index]; }
        public virtual VFXSlot GetOutputSlot(int index) { return m_OutputSlots[index]; }

        protected virtual IEnumerable<VFXPropertyWithValue> inputProperties { get { return PropertiesFromType(GetInputPropertiesTypeName()); } }
        protected virtual IEnumerable<VFXPropertyWithValue> outputProperties { get { return PropertiesFromType(GetOutputPropertiesTypeName()); } }

        // Get properties with value from nested class fields
        protected IEnumerable<VFXPropertyWithValue> PropertiesFromType(string typeName)
        {
            return PropertiesFromType(GetType().GetNestedType(typeName));
        }

        // Get properties with value from type fields
        protected static IEnumerable<VFXPropertyWithValue> PropertiesFromType(Type type)
        {
            if (type == null)
                return Enumerable.Empty<VFXPropertyWithValue>();

            var instance = System.Activator.CreateInstance(type);
            return type.GetFields()
                .Where(f => !f.IsStatic)
                .Select(f => {
                    var p = new VFXPropertyWithValue();
                    p.property = new VFXProperty(f);
                    p.value = f.GetValue(instance);
                    return p;
                });
        }

        // Get properties with values from slots
        protected static IEnumerable<VFXPropertyWithValue> PropertiesFromSlots(IEnumerable<VFXSlot> slots)
        {
            return slots.Select(s =>
                {
                    var p = new VFXPropertyWithValue();
                    p.property = s.property;
                    p.value = s.value;
                    return p;
                });
        }

        // Get properties with values from slots if any or initialize from default inner class name
        protected IEnumerable<VFXPropertyWithValue> PropertiesFromSlotsOrDefaultFromClass(VFXSlot.Direction direction)
        {
            bool isInput = direction == VFXSlot.Direction.kInput;
            var slots = isInput ? inputSlots : outputSlots;
            if (slots.Count() == 0)
                return PropertiesFromType(isInput ? GetInputPropertiesTypeName() : GetOutputPropertiesTypeName());
            else
                return PropertiesFromSlots(slots);
        }

        protected static string GetInputPropertiesTypeName()
        {
            return "InputProperties";
        }

        protected static string GetOutputPropertiesTypeName()
        {
            return "OutputProperties";
        }

        public virtual void AddSlot(VFXSlot slot) { InnerAddSlot(slot, true); }
        private void InnerAddSlot(VFXSlot slot, bool notify)
        {
            var slotList = slot.direction == VFXSlot.Direction.kInput ? m_InputSlots : m_OutputSlots;

            if (!slot.IsMasterSlot())
                throw new ArgumentException("InnerAddSlot expect only a masterSlot");

            if (slot.owner != this as IVFXSlotContainer)
            {
                if (slot.owner != null)
                    slot.owner.RemoveSlot(slot);

                slotList.Add(slot);
                slot.SetOwner(this);
                if (notify)
                    Invalidate(InvalidationCause.kStructureChanged);
            }
        }

        public virtual void RemoveSlot(VFXSlot slot) { InnerRemoveSlot(slot, true); }
        private void InnerRemoveSlot(VFXSlot slot, bool notify)
        {
            var slotList = slot.direction == VFXSlot.Direction.kInput ? m_InputSlots : m_OutputSlots;

            if (!slot.IsMasterSlot())
                throw new ArgumentException();

            if (slot.owner == this as IVFXSlotContainer)
            {
                slotList.Remove(slot);
                slot.SetOwner(null);
                if (notify)
                    Invalidate(InvalidationCause.kStructureChanged);
            }
        }

        protected static void CopyLink(VFXSlot from, VFXSlot to)
        {
            var linkedSlots = from.LinkedSlots.ToArray();
            for (int iLink = 0; iLink < linkedSlots.Length; ++iLink)
            {
                to.Link(linkedSlots[iLink]);
            }

            var fromChild = from.children.ToArray();
            var toChild = to.children.ToArray();
            fromChild = fromChild.Take(toChild.Length).ToArray();
            toChild = toChild.Take(fromChild.Length).ToArray();
            for (int iChild = 0; iChild < toChild.Length; ++iChild)
            {
                CopyLink(fromChild[iChild], toChild[iChild]);
            }
        }

        protected VFXSlotContainerModel()
        {}

        public override void OnEnable()
        {
            base.OnEnable();

            if (m_InputSlots == null)
                m_InputSlots = new List<VFXSlot>();
            else
            {
                int nbRemoved = m_InputSlots.RemoveAll(c => c == null);// Remove bad references if any
                if (nbRemoved > 0)
                    Debug.Log(String.Format("Remove {0} input slot(s) that couldnt be deserialized from {1} of type {2}", nbRemoved, name, GetType()));
            }

            if (m_OutputSlots == null)
                m_OutputSlots = new List<VFXSlot>();
            else
            {
                int nbRemoved = m_OutputSlots.RemoveAll(c => c == null);// Remove bad references if any
                if (nbRemoved > 0)
                    Debug.Log(String.Format("Remove {0} output slot(s) that couldnt be deserialized from {1} of type {2}", nbRemoved, name, GetType()));
            }

            ResyncSlots(false);
        }

        public override void CollectDependencies(HashSet<Object> objs)
        {
            base.CollectDependencies(objs);
            foreach (var slot in m_InputSlots.Concat(m_OutputSlots))
            {
                objs.Add(slot);
                slot.CollectDependencies(objs);
            }
        }

        public override T Clone<T>()
        {
            var clone = base.Clone<T>() as VFXSlotContainerModel<ParentType, ChildrenType>;

            var settings = GetSettings(true);
            foreach (var setting in settings)
            {
                clone.SetSettingValue(setting.Name, setting.GetValue(this), false);
            }

            clone.m_InputSlots.Clear();
            clone.m_OutputSlots.Clear();
            foreach (var slot in inputSlots.Concat(outputSlots))
            {
                var cloneSlot = slot.Clone<VFXSlot>();
                clone.InnerAddSlot(cloneSlot, false);
            }
            return clone as T;
        }

        protected virtual void ResyncSlots(bool notify)
        {
            SyncSlots(VFXSlot.Direction.kInput, notify);
            SyncSlots(VFXSlot.Direction.kOutput, notify);
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (model == this && cause == InvalidationCause.kSettingChanged)
                ResyncSlots(true);

            base.OnInvalidate(model, cause);
        }

        static public IEnumerable<VFXNamedExpression> GetExpressionsFromSlots(IVFXSlotContainer slotContainer)
        {
            foreach (var master in slotContainer.inputSlots)
                foreach (var slot in master.GetExpressionSlots())
                    yield return new VFXNamedExpression(slot.GetExpression(), slot.fullName);
        }

        protected void InitSlotsFromProperties(IEnumerable<VFXPropertyWithValue> properties, VFXSlot.Direction direction)
        {
            foreach (var p in properties)
            {
                var slot = VFXSlot.Create(p, direction);
                InnerAddSlot(slot, false);
            }
        }

        private static void TransferLinks(VFXSlot dst, VFXSlot src, bool notify)
        {
            var links = src.LinkedSlots.ToArray();
            int index = 0;
            while (index < links.Count())
            {
                var link = links[index];
                if (dst.CanLink(link))
                {
                    dst.Link(link, notify);
                    src.Unlink(link, notify);
                }
                ++index;
            }

            if (src.property.type == dst.property.type && src.GetNbChildren() == dst.GetNbChildren())
            {
                int nbSubSlots = src.GetNbChildren();
                for (int i = 0; i < nbSubSlots; ++i)
                    TransferLinks(dst[i], src[i], notify);
            }
        }

        protected bool SyncSlots(VFXSlot.Direction direction, bool notify)
        {
            bool isInput = direction == VFXSlot.Direction.kInput;

            var expectedProperties = (isInput ? inputProperties : outputProperties).ToArray();
            int nbSlots = isInput ? GetNbInputSlots() : GetNbOutputSlots();
            var currentSlots = isInput ? inputSlots : outputSlots;

            bool recreate = false;
            if (nbSlots != expectedProperties.Length)
                recreate = true;
            else
            {
                for (int i = 0; i < nbSlots; ++i)
                    if (!currentSlots[i].property.Equals(expectedProperties[i].property))
                    {
                        recreate = true;
                        break;
                    }
            }

            if (recreate)
            {
                var existingSlots = new List<VFXSlot>(nbSlots);

                // First remove and register all existing slots
                for (int i = nbSlots - 1; i >= 0; --i)
                {
                    existingSlots.Add(currentSlots[i]);
                    InnerRemoveSlot(currentSlots[i], false);
                }
                existingSlots.Reverse();

                // Reuse slots that already exists or create a new one if not
                foreach (var p in expectedProperties)
                {
                    var slot = existingSlots.Find(s => p.property.Equals(s.property));
                    if (slot != null)
                        existingSlots.Remove(slot);
                    else
                        slot = VFXSlot.Create(p, direction);
                    InnerAddSlot(slot, false);
                }

                var currentSlot = isInput ? inputSlots : outputSlots;

                // Try to keep links for slots of same name and compatible types
                foreach (var slot in existingSlots)
                {
                    if (slot.HasLink(true))
                    {
                        var candidates = currentSlots.Where(s => s.property.name == slot.property.name);
                        foreach (var candidate in candidates)
                            TransferLinks(candidate, slot, notify);
                    }
                }

                // Keep link for slots of same types and different names
                foreach (var slot in existingSlots)
                {
                    if (slot.HasLink(true))
                    {
                        var candidate = currentSlots.FirstOrDefault(s => !s.HasLink(true) && s.property.type == slot.property.type);
                        if (candidate != null)
                            TransferLinks(candidate, slot, notify);
                    }
                }

                // Finally remove all remaining links
                foreach (var slot in existingSlots)
                    slot.UnlinkAll(true, notify);

                if (notify)
                    Invalidate(InvalidationCause.kStructureChanged);
            }

            currentSlots = isInput ? inputSlots : outputSlots;
            nbSlots = isInput ? GetNbInputSlots() : GetNbOutputSlots();

            for (int i = 0; i < nbSlots; ++i)
            {
                VFXProperty prop = currentSlots[i].property;

                currentSlots[i].UpdateAttributes(expectedProperties[i].property.attributes);
            }


            return recreate;
        }

        public void ExpandPath(string fieldPath)
        {
            m_expandedPaths.Add(fieldPath);
            Invalidate(InvalidationCause.kParamChanged);
        }

        public void RetractPath(string fieldPath)
        {
            m_expandedPaths.Remove(fieldPath);
            Invalidate(InvalidationCause.kParamChanged);
        }

        public bool IsPathExpanded(string fieldPath)
        {
            return m_expandedPaths.Contains(fieldPath);
        }

        protected override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            base.Invalidate(model, cause);
        }

        public virtual void UpdateOutputExpressions() {}

        //[SerializeField]
        HashSet<string> m_expandedPaths = new HashSet<string>();

        [SerializeField]
        List<VFXSlot> m_InputSlots;

        [SerializeField]
        List<VFXSlot> m_OutputSlots;
    }
}
