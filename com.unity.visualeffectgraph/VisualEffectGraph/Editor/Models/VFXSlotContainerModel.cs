using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
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

        void AddSlot(VFXSlot slot, int index = -1);
        void RemoveSlot(VFXSlot slot);

        int GetSlotIndex(VFXSlot slot);

        void UpdateOutputExpressions();

        void Invalidate(VFXModel.InvalidationCause cause);
        void Invalidate(VFXModel model, VFXModel.InvalidationCause cause);

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

        public virtual void AddSlot(VFXSlot slot, int index = -1) { InnerAddSlot(slot, index, true); }
        private void InnerAddSlot(VFXSlot slot, int index, bool notify)
        {
            var slotList = slot.direction == VFXSlot.Direction.kInput ? m_InputSlots : m_OutputSlots;


            if (!slot.IsMasterSlot())
                throw new ArgumentException("InnerAddSlot expect only a masterSlot");

            if (slot.owner != this as IVFXSlotContainer)
            {
                if (slot.owner != null)
                    slot.owner.RemoveSlot(slot);

                int realIndex = index == -1 ? slotList.Count : index;
                slotList.Insert(realIndex, slot);
                slot.SetOwner(this);
                if (notify)
                    Invalidate(InvalidationCause.kStructureChanged);
            }
        }

        void IVFXSlotContainer.Invalidate(VFXModel model, InvalidationCause cause)
        {
            Invalidate(model, cause);
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

        public int GetSlotIndex(VFXSlot slot)
        {
            var slotList = slot.direction == VFXSlot.Direction.kInput ? m_InputSlots : m_OutputSlots;
            return slotList.IndexOf(slot);
        }

        protected VFXSlotContainerModel()
        {}

        public override void OnEnable()
        {
            base.OnEnable();

            if (m_InputSlots == null)
            {
                m_InputSlots = new List<VFXSlot>();
                SyncSlots(VFXSlot.Direction.kInput, false); // Initial slot creation
            }
            else
            {
                int nbRemoved = m_InputSlots.RemoveAll(c => c == null);// Remove bad references if any
                if (nbRemoved > 0)
                    Debug.Log(String.Format("Remove {0} input slot(s) that couldnt be deserialized from {1} of type {2}", nbRemoved, name, GetType()));
            }

            if (m_OutputSlots == null)
            {
                m_OutputSlots = new List<VFXSlot>();
                SyncSlots(VFXSlot.Direction.kOutput, false); // Initial slot creation
            }
            else
            {
                int nbRemoved = m_OutputSlots.RemoveAll(c => c == null);// Remove bad references if any
                if (nbRemoved > 0)
                    Debug.Log(String.Format("Remove {0} output slot(s) that couldnt be deserialized from {1} of type {2}", nbRemoved, name, GetType()));
            }
        }

        public override void Sanitize()
        {
            base.Sanitize();
            if (ResyncSlots(true))
                Debug.Log(string.Format("Slots have been resynced in {0} of type {1}", name, GetType()));
        }

        public override void OnUnknownChange()
        {
            base.OnUnknownChange();
            SyncSlots(VFXSlot.Direction.kInput, false);
            SyncSlots(VFXSlot.Direction.kOutput, false);
        }

        public override void CollectDependencies(HashSet<ScriptableObject> objs)
        {
            base.CollectDependencies(objs);
            foreach (var slot in m_InputSlots.Concat(m_OutputSlots))
            {
                objs.Add(slot);
                slot.CollectDependencies(objs);
            }
        }

        public virtual bool ResyncSlots(bool notify)
        {
            bool changed = false;
            changed |= SyncSlots(VFXSlot.Direction.kInput, notify);
            changed |= SyncSlots(VFXSlot.Direction.kOutput, notify);
            return changed;
        }

        public void MoveSlots(VFXSlot.Direction direction, int movedIndex, int targetIndex)
        {
            VFXSlot movedSlot = m_InputSlots[movedIndex];
            if (movedIndex < targetIndex)
            {
                m_InputSlots.Insert(targetIndex, movedSlot);
                m_InputSlots.RemoveAt(movedIndex);
            }
            else
            {
                m_InputSlots.RemoveAt(movedIndex);
                m_InputSlots.Insert(targetIndex, movedSlot);
            }
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
            {
                var inheritSpace = CoordinateSpace.Local;
                if (slotContainer is VFXBlock)
                {
                    inheritSpace = (slotContainer as VFXBlock).GetParent().space;
                }
                else if (slotContainer is VFXContext)
                {
                    inheritSpace = (slotContainer as VFXContext).space;
                }
                else
                {
                    Debug.LogErrorFormat("Unable to retrieve inherited space from " + slotContainer);
                }

                foreach (var slot in master.GetExpressionSlots())
                {
                    var expression = slot.GetExpression();
                    if (slot.Spaceable)
                    {
                        if (slot.Space != inheritSpace)
                        {
                            if (slot.property.type == typeof(Position))
                            {
                                var matrix = inheritSpace == CoordinateSpace.Local ? VFXBuiltInExpression.WorldToLocal : VFXBuiltInExpression.LocalToWorld;
                                expression = new VFXExpressionTransformPosition(matrix, expression);
                            }
                            else
                            {
                                Debug.LogErrorFormat("It's hacky"); //TODOPAUL
                            }
                        }
                    }
                    yield return new VFXNamedExpression(expression, slot.fullName);
                }
            }
        }

        protected void InitSlotsFromProperties(IEnumerable<VFXPropertyWithValue> properties, VFXSlot.Direction direction)
        {
            foreach (var p in properties)
            {
                var slot = VFXSlot.Create(p, direction);
                InnerAddSlot(slot, -1, false);
            }
        }

        private static bool TransferLinks(VFXSlot dst, VFXSlot src, bool notify)
        {
            bool oneLinkTransfered = false;
            var links = src.LinkedSlots.ToArray();
            int index = 0;
            while (index < links.Count())
            {
                var link = links[index];
                if (dst.CanLink(link))
                {
                    dst.Link(link, notify);
                    src.Unlink(link, notify);
                    oneLinkTransfered = true;
                }
                ++index;
            }

            if (src.property.type == dst.property.type && src.GetNbChildren() == dst.GetNbChildren())
            {
                int nbSubSlots = src.GetNbChildren();
                for (int i = 0; i < nbSubSlots; ++i)
                    oneLinkTransfered |= TransferLinks(dst[i], src[i], notify);
            }

            return oneLinkTransfered;
        }

        protected bool SyncSlots(VFXSlot.Direction direction, bool notify)
        {
            bool isInput = direction == VFXSlot.Direction.kInput;

            var expectedProperties = (isInput ? inputProperties : outputProperties).ToArray();
            int nbSlots = isInput ? GetNbInputSlots() : GetNbOutputSlots();
            var currentSlots = isInput ? inputSlots : outputSlots;

            // check all slots owner
            for (int i = 0; i < nbSlots; ++i)
            {
                VFXSlot slot = currentSlots[i];
                var slotOwner = slot.owner as VFXSlotContainerModel<ParentType, ChildrenType>;
                if (slotOwner != this)
                {
                    Debug.LogError("Slot :" + slot.name + " of Container" + name + "Has a wrong owner.");
                    slot.SetOwner(this); // make sure everythiing work even if the owner was lost for some reason.
                }
            }

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
                    VFXSlot slot = currentSlots[i];
                    existingSlots.Add(slot);
                    InnerRemoveSlot(slot, false);
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
                    InnerAddSlot(slot, -1, false);
                }

                nbSlots = isInput ? GetNbInputSlots() : GetNbOutputSlots();

                if (nbSlots != expectedProperties.Length)
                {
                    Debug.LogError("Something wrong");
                }

                var currentSlot = isInput ? inputSlots : outputSlots;

                // Try to keep links for slots of same name and compatible types
                for (int i = 0; i < existingSlots.Count; ++i)
                {
                    var slot = existingSlots[i];
                    if (slot.HasLink(true))
                    {
                        //first check at the same index
                        if (currentSlots.Count > i && currentSlots[i].property.name == slot.property.name && TransferLinks(currentSlots[i], slot, notify))
                        {
                            break;
                        }
                        var candidates = currentSlots.Where(s => s.property.name == slot.property.name);
                        foreach (var candidate in candidates)
                            if (TransferLinks(candidate, slot, notify))
                                break;
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

            var currentSlotsCpy = currentSlots.ToArray();
            nbSlots = isInput ? GetNbInputSlots() : GetNbOutputSlots();


            for (int i = 0; i < nbSlots; ++i)
            {
                if (currentSlots.Count != nbSlots)
                {
                    Debug.Log("Collection changed while iterating");
                }
                VFXProperty prop = currentSlotsCpy[i].property;

                currentSlotsCpy[i].UpdateAttributes(expectedProperties[i].property.attributes);
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
