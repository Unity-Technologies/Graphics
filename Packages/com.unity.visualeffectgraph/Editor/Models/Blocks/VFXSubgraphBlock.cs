using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEditor.VFX.Block;

namespace UnityEditor.VFX
{
    interface IVFXSubgraphModel
    {
        public VisualEffectResource resourceCopy { get; }
        public void SetSubmodelsFlattenedParents(VFXModel parent);
    }


    [VFXHelpURL("Subgraph")]
    [VFXInfo(name = "Empty Subgraph Block")]
    class VFXSubgraphBlock : VFXBlock, IVFXSubgraphModel
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected VisualEffectSubgraphBlock m_Subgraph;

        // Cached resource copy
        [NonSerialized]
        private VisualEffectResource m_ResourceCopy;
        [NonSerialized]
        private VFXModel[] m_SubChildren;
        [NonSerialized]
        private VFXBlock[] m_SubBlocks;

        public VisualEffectResource resourceCopy => m_ResourceCopy;

        public VisualEffectSubgraphBlock subgraph => m_Subgraph;

        public sealed override bool IsDependentOnAnyOf(HashSet<EntityId> dependencies)
        {
            if (base.IsDependentOnAnyOf(dependencies))
                return true;

            if (!ReferenceEquals(m_Subgraph, null) && dependencies.Contains(m_Subgraph.GetEntityId()))
                return true;

            return false;
        }

        void OnDestroy()
        {
            ClearCopy();
        }

        public override void ResyncDependencies()
        {
            base.ResyncDependencies();

            ClearCopy();

            ResyncSlots(true);
            if (m_Subgraph != null)
                VFXSubgraphUtility.ResyncCustomAttributes(GetGraph(), GetOrCreateResourceCopy().GetGraph());
            Invalidate(InvalidationCause.kUIChangedTransient); // if a subgraph block has changed, we need to update it's visual valid state
        }

        public sealed override string name => m_Subgraph != null ? ObjectNames.NicifyVariableName(m_Subgraph.name) : "Empty Subgraph Block";

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                if (m_isInOnEnable) // Recreate copy cannot be called in OnEnable because the subgraph my not have been enabled itself so in OnEnable send back the previous input properties
                {
                    if (subgraph != null)
                    {
                        foreach (var inputSlot in inputSlots)
                            yield return new VFXPropertyWithValue(inputSlot.property, inputSlot.value);
                    }
                }
                else
                {
                    if (m_Subgraph == null && !object.ReferenceEquals(m_Subgraph, null))
                        m_Subgraph = EditorUtility.EntityIdToObject(m_Subgraph.GetEntityId()) as VisualEffectSubgraphBlock;
                    if (m_SubChildren == null && subgraph != null && GetGraph() != null) // if the subasset exists but the subchildren has not been recreated yet, return the existing slots
                        RecreateCopy();

                    foreach (var param in GetParameters(VFXSubgraphUtility.InputPredicate).OrderBy(t => t.order))
                    {
                        yield return VFXSubgraphUtility.GetPropertyFromInputParameter(param);
                    }
                }
            }
        }

        IEnumerable<VFXParameter> GetParameters(Func<VFXParameter, bool> predicate)
        {
            if (m_SubChildren == null) return Enumerable.Empty<VFXParameter>();
            return m_SubChildren.OfType<VFXParameter>().Where(predicate).OrderBy(t => t.order);
        }

        bool m_isInOnEnable;
        private new void OnEnable()
        {
            m_isInOnEnable = true;
            base.OnEnable();
            m_isInOnEnable = false;
        }

        public override IEnumerable<VFXAttribute> usedAttributes => m_SubChildren?.OfType<IVFXAttributeUsage>().SelectMany(x => x.usedAttributes) ?? Array.Empty<VFXAttribute>();

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                if (m_SubBlocks != null)
                {
                    foreach (var block in m_SubBlocks)
                    {
                        foreach (var attribute in block.attributes)
                            yield return attribute;
                    }
                }
            }
        }

        private VisualEffectResource GetOrCreateResourceCopy(bool forceRecreate = false)
        {
            RecreateCopyIfNeeded(forceRecreate);
            return m_ResourceCopy;
        }

        public void RecreateCopy()
        {
            RecreateCopyIfNeeded(true);
        }

        public void RecreateCopyIfNeeded(bool force = false)
        {
            if (force)
                ClearCopy();

            if (subgraph == null || m_ResourceCopy != null)
                return;

            m_ResourceCopy = m_Subgraph.GetResourceAtPathAndForget();

            if (VFXViewPreference.advancedLogs)
                Debug.Log($"VfxSubgraphBlock::RecreateCopy for {name} ({GetEntityId()}) of type {GetType()}. Path: {AssetDatabase.GetAssetPath(m_Subgraph.GetEntityId())}. COPY ID: {m_ResourceCopy?.GetEntityId()}");

            var copyGraph = m_ResourceCopy.GetGraph();
            if (copyGraph == null)
                throw new InvalidOperationException("Unexpected failure of GetResourceAtPathAndForget");
            
            copyGraph.SanitizeGraph();

            var context = copyGraph.children.OfType<VFXBlockSubgraphContext>().FirstOrDefault();
            if (context == null)
            {
                ClearCopy();
                return;
            }

            VFXSubgraphUtility.ResyncCustomAttributes(GetGraph(), copyGraph);
            m_SubBlocks = copyGraph.children.OfType<VFXContext>().SelectMany(o => o.children).ToArray();
            m_SubChildren = m_SubBlocks.Concat(copyGraph.children.Where(t => t is VFXOperator || t is VFXParameter)).ToArray();

            copyGraph.SyncCustomAttributes();
            
            foreach (var subgraphBlocks in m_SubBlocks.OfType<VFXSubgraphBlock>())
                subgraphBlocks.RecreateCopyIfNeeded();

            SyncSlots(VFXSlot.Direction.kInput, true);

            if (GetParent() is not VFXBlockSubgraphContext) // Propagate flattened parent context only from root
                SetSubmodelsFlattenedParents(GetParent());

            // Remove that as it causes some recursivity issues
            // TODO: Inplement custom attribute sync correctly
            //if (GetGraph() is { } mainGraph)
            //{
            //    mainGraph.SyncCustomAttributes();
            //}
        }

        private void ClearCopy()
        {
            if (m_ResourceCopy != null)
            {
                m_ResourceCopy.DestroyTransientResourceDeep();
                m_ResourceCopy = null;
                m_SubChildren = null;
                m_SubBlocks = null;
            }
            else if (m_SubChildren != null || m_SubBlocks != null)
                throw new Exception("Bad internal state for VFXSubgraphBlock");
        }

        public void PatchInputExpressions()
        {
            if (m_SubChildren == null) return;

            var inputExpressions = new List<VFXExpression>();

            foreach (var slot in inputSlots.SelectMany(t => t.GetExpressionSlots()))
            {
                inputExpressions.Add(slot.GetExpression());
            }

            VFXSubgraphUtility.TransferExpressionToParameters(inputExpressions, GetParameters(t => VFXSubgraphUtility.InputPredicate(t)).OrderBy(t => t.order));
        }

        public VFXModel[] subChildren => m_SubChildren;

        public IEnumerable<VFXBlock> recursiveSubBlocks
        {
            get
            {
                return m_SubBlocks == null || !isActive ? Enumerable.Empty<VFXBlock>() : (m_SubBlocks.SelectMany(t => t is VFXSubgraphBlock ? (t as VFXSubgraphBlock).recursiveSubBlocks : Enumerable.Repeat(t, 1)));
            }
        }
        public override bool isValid
        {
            get
            {
                if (m_Subgraph == null)
                    return true;

                var subGraph = GetOrCreateResourceCopy().GetGraph();
                var blockContext = subGraph.children.OfType<VFXBlockSubgraphContext>().FirstOrDefault();
                if (blockContext == null)
                    return false;

                return base.isValid;
            }
        }

        public override VFXContextType compatibleContexts { get { return (GetOrCreateResourceCopy() != null) ? GetOrCreateResourceCopy().GetGraph().children.OfType<VFXBlockSubgraphContext>().First().compatibleContextType : VFXContextType.All; } }
        public override VFXDataType compatibleData { get { return (GetOrCreateResourceCopy() != null) ? GetOrCreateResourceCopy().GetGraph().children.OfType<VFXBlockSubgraphContext>().First().ownedType : VFXDataType.Particle | VFXDataType.SpawnEvent; } }

        public override void CollectDependencies(HashSet<ScriptableObject> objs, bool ownedOnly = true)
        {
            base.CollectDependencies(objs, ownedOnly);

            if (m_SubChildren == null || ownedOnly)
                return;

            foreach (var child in m_SubChildren)
            {
                if (!(child is VFXParameter))
                {
                    objs.Add(child);

                    if (child is VFXModel)
                        (child as VFXModel).CollectDependencies(objs, false);
                }
            }
        }

        public void SetSubmodelsFlattenedParents(VFXModel parent)
        {
            if (m_SubBlocks == null)
                return;

            foreach (var block in m_SubBlocks)
            {
                block.flattenedParent = parent;
                if (block is VFXSubgraphBlock subgraphBlock)
                    subgraphBlock.SetSubmodelsFlattenedParents(parent);
            }
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            switch (cause)
            {
                // Recreate subgraph copy
                case InvalidationCause.kSettingChanged:
                {
                    var graph = GetGraph();

                    if (graph != null && subgraph != null)
                    {
                        var otherGraph = m_Subgraph.GetResource().GetGraph();
                        if (otherGraph == graph || otherGraph.subgraphDependencies.Contains(graph.GetResource().visualEffectObject))
                            m_Subgraph = null; // prevent cyclic dependencies.

                        if (graph.GetResource().isSubgraph) // BuildSubgraphDependencies is called for vfx by recompilation, but in subgraph we must call it explicitely
                            graph.BuildSubgraphDependencies();
                    }
                    else if (GetOrCreateResourceCopy() != null)
                        RecreateCopy();
                }
                break;

                // Propagate change in expressions to subgraph
                case InvalidationCause.kExpressionInvalidated:
                case InvalidationCause.kConnectionChanged:
                {
                    VFXSlot slot = model as VFXSlot;
                    if (slot != null && slot.IsMasterSlot()) // Check master to avoid multi invalidation when walking through slot hierarchy
                        PatchInputExpressions(); // this will propagate invalidation to subgraphs
                }
                break;

                // Propagate change in expression values to subgraph
                case InvalidationCause.kParamChanged:
                case InvalidationCause.kExpressionValueInvalidated:
                {
                    VFXSlot slot = model as VFXSlot;
                    if (slot != null && slot.IsMasterSlot())
                    {
                        int slotIndex = GetSlotIndex(slot);
                        if (slotIndex >= 0) // Not a toggle slot
                        {
                            var parameter = m_SubChildren.OfType<VFXParameter>().FirstOrDefault(m => m.order == slotIndex);
                            if (parameter != null)
                                parameter.GetOutputSlot(0).Invalidate(InvalidationCause.kExpressionValueInvalidated); // Propagate value change event to subblock parameter
                        }
                    }
                }
                break;
            }

            base.OnInvalidate(model, cause);
        }

        protected override void OnAdded()
        {
            base.OnAdded();
            SetSubmodelsFlattenedParents(GetParent() is VFXBlockSubgraphContext ? GetParent().flattenedParent : GetParent());
            if (m_Subgraph != null)
            {
                VFXSubgraphUtility.ResyncCustomAttributes(GetGraph(), GetOrCreateResourceCopy().GetGraph());
            }
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            SetSubmodelsFlattenedParents(null);
        }
    }
}
