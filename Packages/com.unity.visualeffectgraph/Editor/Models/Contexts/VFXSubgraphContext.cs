using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.UI;

namespace UnityEditor.VFX
{
    [ExcludeFromPreset]
    class VFXSubgraphContext : VFXContext, IVFXSubgraphModel
    {
        [VFXSetting, SerializeField]
        protected VisualEffectAsset m_Subgraph;

        // Cached resource copy
        [NonSerialized]
        private VisualEffectResource m_ResourceCopy;
        [NonSerialized]
        private VFXModel[] m_SubChildren;

        public VisualEffectResource resourceCopy => m_ResourceCopy;

        public VisualEffectAsset subgraph => m_Subgraph;

        public IEnumerable<VFXModel> subChildren => m_SubChildren;

        public VFXSubgraphContext() : base(VFXContextType.Subgraph, VFXDataType.SpawnEvent, VFXDataType.None)
        {
        }

        void OnDestroy()
        {
            ClearCopy();
        }

        public sealed override bool IsDependentOnAnyOf(HashSet<EntityId> dependencies)
        {
            if (base.IsDependentOnAnyOf(dependencies))
                return true;

            if (!ReferenceEquals(m_Subgraph, null) && dependencies.Contains(m_Subgraph.GetEntityId()))
                return true;

            return false;
        }

        protected override int inputFlowCount { get { return m_InputFlowNames.Count; } }

        public sealed override string name { get { return m_Subgraph != null ? m_Subgraph.name : "Subgraph"; } }

        void RefreshSubgraphObject()
        {
            if (m_Subgraph == null && !object.ReferenceEquals(m_Subgraph, null))
            {
                string assetPath = AssetDatabase.GetAssetPath(m_Subgraph.GetEntityId());

                var newSubgraph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
                if (newSubgraph != null)
                {
                    m_Subgraph = newSubgraph;
                }
            }
        }

        internal override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);
            if (inputFlowCount > VFXContext.kMaxFlowCount)
            {
                var message = $@"This subgraph handle too many flow anchor to be fully displayed. Maximum: {VFXContext.kMaxFlowCount}, Actual: {inputFlowCount}";
                report.RegisterError("MaxContextFlowCount", VFXErrorType.Error, message, this);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                RefreshSubgraphObject();
                
                if (m_Subgraph != null)
                {
                    RecreateCopyIfNeeded();
                    foreach (var param in GetSortedInputParameters())
                        yield return VFXSubgraphUtility.GetPropertyFromInputParameter(param);
                }
            }
        }


        IEnumerable<VFXParameter> GetSortedInputParameters()
        {
            var resourceCopy = GetOrCreateResourceCopy();
            if (resourceCopy != null)
            {
                var graph = resourceCopy.GetGraph();
                if (graph != null)
                {
                    var UIInfos = graph.UIInfos;
                    var categoriesOrder = UIInfos.categories;
                    if (categoriesOrder == null)
                        categoriesOrder = new List<VFXUI.CategoryInfo>();
                    return GetParameters(VFXSubgraphUtility.InputPredicate).OrderBy(t => categoriesOrder.FindIndex(u => u.name == t.category)).ThenBy(t => t.order);
                }
                else
                {
                    Debug.LogError("Can't find subgraph graph");
                }
            }
            else
            {
                Debug.LogError("Cant't find subgraph resource");
            }

            return Enumerable.Empty<VFXParameter>();
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            return null;
        }

        public override bool CanBeCompiled()
        {
            return subgraph != null;
        }

        IEnumerable<VFXParameter> GetParameters(Func<VFXParameter, bool> predicate)
        {
            if (m_SubChildren == null) return Enumerable.Empty<VFXParameter>();
            return m_SubChildren.OfType<VFXParameter>().Where(t => predicate(t)).OrderBy(t => t.order);
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

            RefreshSubgraphObject();
            if (m_Subgraph == null || m_ResourceCopy != null)
                return;

            m_ResourceCopy = m_Subgraph.GetResourceAtPathAndForget();

            if (VFXViewPreference.advancedLogs)
                Debug.Log($"VfxSubgraphContext::RecreateCopy for {name} ({GetEntityId()}) of type {GetType()}. Path: {AssetDatabase.GetAssetPath(m_Subgraph.GetEntityId())}. COPY ID: {m_ResourceCopy?.GetEntityId()}");

            if (m_ResourceCopy == null)
            {
                ClearCopy();
                return;
            }

            var copyGraph = m_ResourceCopy.GetGraph();
            copyGraph.flattenedParent = GetGraph();
            copyGraph.SanitizeGraph();
            HashSet<ScriptableObject> dependencies = new HashSet<ScriptableObject>();
            copyGraph.CollectDependencies(dependencies);
            dependencies.RemoveWhere(o => o == null || o is not VFXModel); //script is missing should be removed from the list before copy.
            var dependenciesArray = dependencies.ToArray();

            m_SubChildren = dependenciesArray.OfType<VFXModel>().Where(o => o is VFXContext || o is VFXOperator || o is VFXParameter).ToArray();
            
            List<string> newInputFlowNames = new List<string>();

            foreach (var basicEvent in m_SubChildren.OfType<VFXBasicEvent>())
            {
                if (!newInputFlowNames.Contains(basicEvent.eventName))
                    newInputFlowNames.Add(basicEvent.eventName);
            }

            bool hasStart = false;
            bool hasStop = false;

            foreach (var initialize in m_SubChildren.OfType<VFXBasicSpawner>())
            {
                if (!hasStart && initialize.inputFlowSlot[0].link.Count() == 0)
                {
                    hasStart = true;
                }
                if (!hasStop && initialize.inputFlowSlot[1].link.Count() == 0)
                {
                    hasStop = true;
                }
            }

            int directEventCount = newInputFlowNames.Count;

            foreach (var subContext in m_SubChildren.OfType<VFXSubgraphContext>())
            {
                for (int i = 0; i < subContext.inputFlowCount; ++i)
                {
                    string name = subContext.GetInputFlowName(i);
                    switch (name)
                    {
                        case VisualEffectAsset.PlayEventName:
                            hasStart = true;
                            break;
                        case VisualEffectAsset.StopEventName:
                            hasStop = true;
                            break;
                        default:
                            m_InputFlowNames.Add(name);
                            break;
                    }
                }
            }
            newInputFlowNames.Sort(0, directEventCount, Comparer<string>.Default);
            newInputFlowNames.Sort(directEventCount, newInputFlowNames.Count - directEventCount, Comparer<string>.Default);
            if (hasStop)
                newInputFlowNames.Insert(0, VisualEffectAsset.StopEventName);
            if (hasStart)
                newInputFlowNames.Insert(0, VisualEffectAsset.PlayEventName);

            // Don't notify while doing this else asset is considered dirty after each call at RecreateCopy
            if (m_InputFlowNames == null || !newInputFlowNames.SequenceEqual(m_InputFlowNames) || inputFlowSlot.Length != inputFlowCount)
            {
                var oldLinks = new Dictionary<string, List<VFXContextLink>>();

                for (int i = 0; i < inputFlowSlot.Count() && i < m_InputFlowNames.Count; ++i)
                {
                    oldLinks[GetInputFlowName(i)] = inputFlowSlot[i].link.ToList();
                }
                m_InputFlowNames = newInputFlowNames;

                DetachAllInputFlowSlots(false);

                for (int i = 0; i < inputFlowSlot.Count(); ++i)
                {
                    List<VFXContextLink> ctxSlot;
                    if (oldLinks.TryGetValue(GetInputFlowName(i), out ctxSlot))
                        foreach (var link in ctxSlot)
                            InnerLink(link.context, this, link.slotIndex, i, false);
                }
            }

            SyncSlots(VFXSlot.Direction.kInput, true);
            copyGraph.SyncCustomAttributes();
            VFXSubgraphUtility.ResyncCustomAttributes(GetGraph(), copyGraph);
        }

        private void ClearCopy()
        {
            if (m_ResourceCopy != null)
            {
                m_ResourceCopy.DestroyTransientResourceDeep();
                m_ResourceCopy = null;
                m_SubChildren = null;
            }
            else if (m_SubChildren != null)
                throw new Exception("Bad internal state for VFXSubgraphContext");
        }

        private void InitSubgraph()
        {
            RefreshSubgraphObject();
            if (m_Subgraph == null)
            {
                ClearCopy(); // Clear any former subgraph copy
            }
            else
            {
                var graph = GetGraph();
                if (graph != null)
                {
                    var subGraph = m_Subgraph.GetResource().GetGraph();
                    if (subGraph == graph ||
                        subGraph.subgraphDependencies.Contains(graph.GetResource().visualEffectObject))
                    {
                        m_Subgraph = null; // cyclic dependency detected
                    }
                    else
                    {
                        RecreateCopy();
                        PrepareSubgraph();
                    }
                }
            }
        }

        private void PrepareSubgraph()
        {
            var graph = GetGraph();

            if (graph == null || m_Subgraph == null)
                return;

            var subGraph = m_Subgraph.GetResource().GetGraph();
            SetSubmodelsFlattenedParents(graph);
            VFXSubgraphUtility.ResyncCustomAttributes(graph, subGraph);
            PatchInputExpressions();
            VFXGraph.RecurseSubgraphPatchInputExpression(subChildren);
            graph.BuildSubgraphDependencies();
        }

        public VFXContext GetEventContext(string eventName)
        {
            return m_SubChildren.OfType<VFXBasicEvent>().Where(t => t.eventName == eventName).FirstOrDefault();
        }

        public string GetInputFlowName(int index)
        {
            return m_InputFlowNames[index];
        }

        public int GetInputFlowIndex(string name)
        {
            return m_InputFlowNames.IndexOf(name);
        }

        [SerializeField]
        List<string> m_InputFlowNames = new List<string>();

        public void PatchInputExpressions()
        {
            if (m_SubChildren == null) return;

            var inputExpressions = new List<VFXExpression>();

            foreach (var subSlot in inputSlots.SelectMany(t => t.GetExpressionSlots()))
                inputExpressions.Add(subSlot.GetExpression());

            VFXSubgraphUtility.TransferExpressionToParameters(inputExpressions, GetSortedInputParameters());
        }

        protected override void OnAdded()
        {
            base.OnAdded();
            InitSubgraph();
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            SetSubmodelsFlattenedParents(null);
        }

        public void SetSubmodelsFlattenedParents(VFXModel parent)
        {
            if (m_Subgraph == null)
                return;

            var parentGraph = parent as VFXGraph;
            var graph = resourceCopy.GetGraph();

            graph.flattenedParent = parentGraph;
            VFXSubgraphUtility.SetSubgraphFlattenParentsDeep(graph);
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            switch (cause)
            {
                case InvalidationCause.kSettingChanged:
                {
                    InitSubgraph(); // only setting is subgraph field meaning subgraph asset has changed
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

        public override void ResyncDependencies()
        {
            base.ResyncDependencies();
            ClearCopy();
            ResyncSlots(true);
        }

        public override void CollectDependencies(HashSet<ScriptableObject> objs, bool ownedOnly = true)
        {
            base.CollectDependencies(objs, ownedOnly);

            if (ownedOnly)
                return;

            if (m_Subgraph != null && m_SubChildren == null)
                RecreateCopy();

            if (m_SubChildren != null)
            {
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
        }
    }
}
