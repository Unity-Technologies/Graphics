using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using Type = System.Type;

namespace UnityEditor.VFX
{
    abstract class VFXBlock : VFXSlotContainerModel<VFXContext, VFXModel>
    {
        public VFXBlock()
        {
            m_UICollapsed = false;
        }

        public static T CreateImplicitBlock<T>(VFXData data) where T : VFXBlock
        {
            var block = ScriptableObject.CreateInstance<T>();
            block.m_TransientData = data;
            return block;
        }

        private VFXData m_TransientData = null;

        // Deprecated. But has to keep around it to initialize slot field 
        [SerializeField]
        private bool m_Disabled = false;

        [SerializeField]
        private VFXSlot m_EnabledSlot;
        public VFXSlot enabledSlot => m_EnabledSlot; 

        public bool enabled
        {
            get { return (bool)(m_EnabledSlot.value); }
            set
            {
                if (value != enabled)
                {
                    m_EnabledSlot.value = value;
                    Invalidate(InvalidationCause.kEnableChanged);
                }
            }
        }
        public virtual bool isValid
        {
            get
            {
                if (GetParent() == null) return true; // a block is invalid only if added to incompatible context.
                if ((compatibleContexts & GetParent().contextType) != GetParent().contextType)
                    return false;
                if (GetParent() is VFXBlockSubgraphContext subgraphContext)
                    return (subgraphContext.compatibleContextType & compatibleContexts) == subgraphContext.compatibleContextType;

                return true;
            }
        }

        public bool isActive
        {
            get { return enabled && isValid; }
        }

        public abstract VFXContextType compatibleContexts { get; }
        public abstract VFXDataType compatibleData { get; }
        public virtual IEnumerable<VFXAttributeInfo> attributes { get { return Enumerable.Empty<VFXAttributeInfo>(); } }
        public virtual IEnumerable<VFXNamedExpression> parameters { get { return GetExpressionsFromSlots(this); } }
        public VFXExpression enabledExpression => m_EnabledSlot.GetExpression();
        public virtual IEnumerable<string> includes { get { return Enumerable.Empty<string>(); } }
        public virtual string source { get { return null; } }

        public override void OnEnable()
        {
            base.OnEnable();

            if (m_EnabledSlot == null)
            {
                var prop = new VFXPropertyWithValue(new VFXProperty(typeof(bool), "enabled"), !m_Disabled);
                m_EnabledSlot = VFXSlot.Create(prop, VFXSlot.Direction.kInput);
                m_EnabledSlot.SetOwner(this);
            }
        }

        public override void CollectDependencies(HashSet<ScriptableObject> objs, bool ownedOnly = true)
        {
            base.CollectDependencies(objs, ownedOnly);
            objs.Add(m_EnabledSlot);
            m_EnabledSlot.CollectDependencies(objs, ownedOnly);
        }

        public IEnumerable<VFXAttributeInfo> mergedAttributes
        {
            get
            {
                var attribs = new Dictionary<VFXAttribute, VFXAttributeMode>();
                foreach (var a in attributes)
                {
                    VFXAttributeMode mode = VFXAttributeMode.None;
                    attribs.TryGetValue(a.attrib, out mode);
                    mode |= a.mode;
                    attribs[a.attrib] = mode;
                }
                return attribs.Select(kvp => new VFXAttributeInfo(kvp.Key, kvp.Value));
            }
        }

        public VFXData GetData()
        {
            if (GetParent() != null)
                return GetParent().GetData();
            return m_TransientData;
        }

        public sealed override VFXCoordinateSpace GetOutputSpaceFromSlot(VFXSlot slot)
        {
            /* For block, space is directly inherited from parent context, this method should remains sealed */
            if (GetParent() != null)
                return GetParent().space;
            return (VFXCoordinateSpace)int.MaxValue;
        }

        public VFXContext flattenedParent
        {
            get
            {
                return m_FlattenedParent != null ? m_FlattenedParent : GetParent();
            }
            set
            {
                m_FlattenedParent = value;
            }
        }

        private VFXContext m_FlattenedParent;
    }
}
