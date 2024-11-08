using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXBlock : VFXSlotContainerModel<VFXContext, VFXModel>, IVFXDataGetter, IVFXAttributeUsage
    {
        public readonly static string activationSlotName = "_vfx_enabled";

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
        private VFXSlot m_ActivationSlot;
        public override VFXSlot activationSlot => m_ActivationSlot;

        private bool m_CachedEnableState = true;
        private bool m_EnableStateUpToDate = false;
        public bool enabled
        {
            get
            {
                if (!m_EnableStateUpToDate)
                    UpdateEnableState();

                return m_CachedEnableState;
            }
        }

        public virtual bool isValid
        {
            get
            {
                if (GetParent() == null)
                    return true;

                return GetParent().Accept(this);
            }
        }

        public bool isActive => enabled && isValid;

        public abstract VFXContextType compatibleContexts { get; }
        public abstract VFXDataType compatibleData { get; }
        public virtual IEnumerable<VFXAttribute> usedAttributes => attributes.Select(x => x.attrib);
        public virtual IEnumerable<VFXAttributeInfo> attributes { get { return Enumerable.Empty<VFXAttributeInfo>(); } }
        public virtual IEnumerable<VFXNamedExpression> parameters { get { return GetExpressionsFromSlots(this); } }
        public VFXExpression activationExpression => m_ActivationSlot.GetExpression();
        public virtual IEnumerable<string> defines { get { return Enumerable.Empty<string>(); } }
        public virtual string source => null;

        public override void OnEnable()
        {
            base.OnEnable();
            CreateActivationSlotIfNeeded();
        }

        public override void OnUnknownChange()
        {
            base.OnUnknownChange();
            m_EnableStateUpToDate = false;
        }

        public override void Sanitize(int version)
        {
            if (CreateActivationSlotIfNeeded())
                Invalidate(InvalidationCause.kStructureChanged);
            base.Sanitize(version);
        }

        /// <summary>
        /// Copy input links from source to destination. The input slots must be compatible and in same order between source and destination
        /// </summary>
        public static void CopyInputLinks(VFXBlock dst, VFXBlock src, bool notify = true)
        {
            for (var i = 0; i < src.GetNbInputSlots(); i++)
            {
                VFXSlot.CopyLinksAndValue(dst.inputSlots[i], src.inputSlots[i], notify);
            }
            VFXSlot.CopyLinksAndValue(dst.activationSlot, src.activationSlot, notify);
        }

        public virtual void Rename(string oldName, string newName)
        {
            throw new NotSupportedException($"Should not be called on this object type: {GetType()}");
        }

        private bool CreateActivationSlotIfNeeded()
        {
            if (m_ActivationSlot == null)
            {
                var prop = new VFXPropertyWithValue(new VFXProperty(typeof(bool), activationSlotName), !m_Disabled);
                m_ActivationSlot = VFXSlot.Create(prop, VFXSlot.Direction.kInput);
                m_ActivationSlot.SetOwner(this);
                return true;
            }
            return false;
        }

        private void UpdateEnableState()
        {
            // fast path for not connected activation slots
            if (!activationSlot.HasLink())
                m_CachedEnableState = (bool)activationSlot.value;
            else
            {
                var enableExp = activationSlot.GetExpression();

                var context = new VFXExpression.Context(VFXExpressionContextOption.ConstantFolding);
                context.RegisterExpression(enableExp);
                context.Compile();

                enableExp = context.GetReduced(enableExp);
                if (enableExp.Is(VFXExpression.Flags.Constant))
                    m_CachedEnableState = enableExp.Get<bool>();
                else
                    m_CachedEnableState = true;
            }

            m_EnableStateUpToDate = true;
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            base.OnInvalidate(model, cause);
            if (model == activationSlot &&
               (cause == InvalidationCause.kParamChanged || // This does not account for param/connection changed upstream
                cause == InvalidationCause.kConnectionChanged ||
                cause == InvalidationCause.kExpressionInvalidated ||
                cause == InvalidationCause.kExpressionValueInvalidated))
            {
                bool oldEnableState = m_CachedEnableState;
                UpdateEnableState();

                if (m_CachedEnableState != oldEnableState)
                    Invalidate(InvalidationCause.kEnableChanged);
            }
        }

        public override void CollectDependencies(HashSet<ScriptableObject> objs, bool ownedOnly = true)
        {
            base.CollectDependencies(objs, ownedOnly);
            if (m_ActivationSlot != null)
                objs.Add(m_ActivationSlot);
            m_ActivationSlot.CollectDependencies(objs, ownedOnly);
        }

        public IEnumerable<VFXAttributeInfo> mergedAttributes
        {
            get
            {
                foreach (var attrib in attributes.GroupBy(x => x.attrib))
                {
                    yield return new VFXAttributeInfo(attrib.Key, attrib.Aggregate(VFXAttributeMode.None, (acc, x) => acc | x.mode));
                }
            }
        }

        public VFXData GetData()
        {
            if (GetParent() != null)
                return GetParent().GetData();
            return m_TransientData;
        }

        internal void SetTransientData(VFXData data)
        {
            Debug.Assert(GetParent() == null, "SetTransientData should only be called on implicit blocks, that have no parents.");
            m_TransientData = data;
        }

        public override void RefreshErrors()
        {
            if (enabled)
            {
                base.RefreshErrors();
            }
        }

        internal override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);
            if (GetParent() is VFXBlockSubgraphContext)
            {
                var notUndefinedSpace = inputSlots.Where(o => o.space != VFXSpace.None);
                if (notUndefinedSpace.Any())
                {
                    report.RegisterError("SubgraphBlockSpaceIsIgnored", VFXErrorType.Warning, "Space Local/World are ignored in subgraph blocks.", this);
                }
            }
        }

        protected VFXSpace GetOwnerSpace()
        {
            if (GetParent() != null)
                return GetParent().space;
            return VFXSpace.None;
        }

        public override VFXSpace GetOutputSpaceFromSlot(VFXSlot slot)
        {
            //For block, space is directly inherited from parent context
            //Override with care: most block are assuming expression are in same space than owner (and doesn't conversion afterwards).
            return GetOwnerSpace();
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
