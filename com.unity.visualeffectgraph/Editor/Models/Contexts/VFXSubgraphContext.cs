using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;

namespace UnityEditor.VFX
{
    class VFXSubgraphContext : VFXContext
    {
        [VFXSetting, SerializeField]
        protected VisualEffectAsset m_Subgraph;

        [NonSerialized]
        VFXModel[] m_SubChildren;
        VFXGraph m_UsedSubgraph;

        public VisualEffectAsset subgraph
        {
            get { return m_Subgraph; }
        }

        public static void CallOnGraphChanged(VFXGraph graph)
        {
            if (OnGraphChanged != null)
                OnGraphChanged(graph);
        }

        public IEnumerable<VFXModel> subChildren
        { get { return m_SubChildren; } }

        static Action<VFXGraph> OnGraphChanged;

        public VFXSubgraphContext() : base(VFXContextType.Subgraph, VFXDataType.SpawnEvent, VFXDataType.None)
        {
        }

        public override void GetImportDependentAssets(HashSet<int> dependencies)
        {
            base.GetImportDependentAssets(dependencies);
            if (!object.ReferenceEquals(m_Subgraph, null))
                dependencies.Add(m_Subgraph.GetInstanceID());
        }

        void GraphParameterChanged(VFXGraph graph)
        {
            VisualEffectAsset asset = graph != null && graph.GetResource() != null ? graph.GetResource().asset : null;
            if (m_Subgraph == asset && GetParent() != null)
                RecreateCopy();
        }

        public const int s_MaxInputFlow = 5;
        protected override int inputFlowCount { get { return m_InputFlowNames.Count > s_MaxInputFlow ? s_MaxInputFlow : m_InputFlowNames.Count; } }

        public sealed override string name { get { return m_Subgraph != null ? m_Subgraph.name : "Subgraph"; } }

        void RefreshSubgraphObject()
        {
            if (m_Subgraph == null && !object.ReferenceEquals(m_Subgraph, null))
            {
                string assetPath = AssetDatabase.GetAssetPath(m_Subgraph.GetInstanceID());

                var newSubgraph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
                if (newSubgraph != null)
                {
                    m_Subgraph = newSubgraph;
                }
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                RefreshSubgraphObject();
                if (m_SubChildren == null && m_Subgraph != null) // if the subasset exists but the subchildren has not been recreated yet, return the existing slots
                {
                    foreach (var slot in inputSlots)
                        yield return new VFXPropertyWithValue(slot.property);
                }

                if (m_Subgraph != null)
                {
                    foreach (var param in GetSortedInputParameters())
                        yield return VFXSubgraphUtility.GetPropertyFromInputParameter(param);
                }
            }
        }


        IEnumerable<VFXParameter> GetSortedInputParameters()
        {
            var resource = m_Subgraph.GetResource();
            if (resource != null)
            {
                var graph = resource.GetOrCreateGraph();
                if (graph != null)
                {
                    var UIInfos = graph.UIInfos;
                    var categoriesOrder = UIInfos.categories;
                    if (categoriesOrder == null)
                        categoriesOrder = new List<VFXUI.CategoryInfo>();
                    return GetParameters(t => InputPredicate(t)).OrderBy(t => categoriesOrder.FindIndex(u => u.name == t.category)).ThenBy(t => t.order);
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

        static bool InputPredicate(VFXParameter param)
        {
            return param.exposed && !param.isOutput;
        }

        static bool OutputPredicate(VFXParameter param)
        {
            return param.isOutput;
        }

        IEnumerable<VFXParameter> GetParameters(Func<VFXParameter, bool> predicate)
        {
            if (m_SubChildren == null) return Enumerable.Empty<VFXParameter>();
            return m_SubChildren.OfType<VFXParameter>().Where(t => predicate(t)).OrderBy(t => t.order);
        }

        private new void OnEnable()
        {
            base.OnEnable();

            OnGraphChanged += GraphParameterChanged;
        }

        public override void Sanitize(int version)
        {
            base.Sanitize(version);

            RecreateCopy();
        }

        void SubChildrenOnInvalidate(VFXModel model, InvalidationCause cause)
        {
            Invalidate(this, cause);
        }

        private void OnDisable()
        {
            DetachFromOriginal();
            OnGraphChanged -= GraphParameterChanged;
        }

        public void RecreateCopy()
        {
            DetachFromOriginal();
            RefreshSubgraphObject();

            if (m_Subgraph == null)
            {
                m_SubChildren = null;
                m_UsedSubgraph = null;
                return;
            }

            var resource = m_Subgraph.GetResource();
            if (resource == null)
            {
                m_SubChildren = null;
                m_UsedSubgraph = null;
                return;
            }

            var graph = resource.GetOrCreateGraph();
            HashSet<ScriptableObject> dependencies = new HashSet<ScriptableObject>();
            graph.CollectDependencies(dependencies);
            dependencies.RemoveWhere(o => o == null); //script is missing should be removed from the list before copy.

            var duplicated = VFXMemorySerializer.DuplicateObjects(dependencies.ToArray());
            m_SubChildren = duplicated.OfType<VFXModel>().Where(t => t is VFXContext || t is VFXOperator || t is VFXParameter).ToArray();
            m_UsedSubgraph = graph;
            foreach (var child in duplicated.Zip(dependencies, (a, b) => new { copy = a, original = b }))
            {
                child.copy.hideFlags = HideFlags.HideAndDontSave;
                if (child.copy is VFXSlot)
                {
                    var original = child.original as VFXSlot;
                    var copy = child.copy as VFXSlot;
                    if (original.direction == VFXSlot.Direction.kInput || original.owner is VFXParameter)
                    {
                        m_OriginalToCopy[original] = copy;
                        original.onInvalidateDelegate += OnOriginalSlotModified;
                    }
                }
                else if (child.copy is VFXSubgraphBlock subgraphBlock)
                {
                    subgraphBlock.RecreateCopy();
                }
            }

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

            if (m_InputFlowNames == null || !newInputFlowNames.SequenceEqual(m_InputFlowNames) || inputFlowSlot.Length != inputFlowCount)
            {
                var oldLinks = new Dictionary<string,  List<VFXContextLink>>();

                for (int i = 0; i < inputFlowSlot.Count() && i < m_InputFlowNames.Count; ++i)
                {
                    oldLinks[GetInputFlowName(i)] = inputFlowSlot[i].link.ToList();
                }
                m_InputFlowNames = newInputFlowNames;
                RefreshInputFlowSlots();

                for (int i = 0; i < inputFlowSlot.Count(); ++i)
                {
                    List<VFXContextLink> ctxSlot;
                    if (oldLinks.TryGetValue(GetInputFlowName(i), out ctxSlot))
                        foreach (var link in ctxSlot)
                            LinkFrom(link.context, link.slotIndex, i);
                }
            }
            SyncSlots(VFXSlot.Direction.kInput, true);
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

        private void DetachFromOriginal()
        {
            if (m_SubChildren != null)
            {
                HashSet<ScriptableObject> deps = new HashSet<ScriptableObject>();
                foreach (var child in m_SubChildren)
                {
                    if (child != null)
                    {
                        child.onInvalidateDelegate -= SubChildrenOnInvalidate;

                        child.CollectDependencies(deps);

                        ScriptableObject.DestroyImmediate(child, true);
                    }
                }
                foreach (var obj in deps)
                {
                    ScriptableObject.DestroyImmediate(obj, true);
                }

                foreach (var kv in m_OriginalToCopy)
                {
                    kv.Key.onInvalidateDelegate -= OnOriginalSlotModified;
                }
                m_OriginalToCopy.Clear();
            }

            m_SubChildren = null;
        }

        public void OnOriginalSlotModified(VFXModel original, InvalidationCause cause)
        {
            if (cause == InvalidationCause.kParamChanged)
            {
                m_OriginalToCopy[original as VFXSlot].value = (original as VFXSlot).value;
                Invalidate(InvalidationCause.kParamChanged);
            }
        }

        Dictionary<VFXSlot, VFXSlot> m_OriginalToCopy = new Dictionary<VFXSlot, VFXSlot>();

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
            if (m_Subgraph != null)
            {
                var graph = GetGraph();
                if (graph != null)
                {
                    var otherGraph = m_Subgraph.GetResource().GetOrCreateGraph();
                    if (otherGraph == graph || otherGraph.subgraphDependencies.Contains(graph.GetResource().visualEffectObject))
                        m_Subgraph = null; // prevent cyclic dependencies.
                }
            }
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (cause == InvalidationCause.kSettingChanged || cause == InvalidationCause.kExpressionInvalidated)
            {
                if (cause == InvalidationCause.kSettingChanged)
                {
                    if (m_Subgraph != null)
                    {
                        var graph = GetGraph();
                        if (graph != null) // that case it will be checked in OnAdded
                        {
                            var otherGraph = m_Subgraph.GetResource().GetOrCreateGraph();
                            if (otherGraph == graph || otherGraph.subgraphDependencies.Contains(graph.GetResource().visualEffectObject))
                                m_Subgraph = null; // prevent cyclic dependencies.
                        }
                    }
                    if (m_Subgraph != null || object.ReferenceEquals(m_Subgraph, null) || m_UsedSubgraph == null || (m_Subgraph != null && m_UsedSubgraph != m_Subgraph.GetResource().GetOrCreateGraph()))  // do not recreate subchildren if the subgraph is not available but is not null
                        RecreateCopy();
                }

                base.OnInvalidate(model, cause);
                PatchInputExpressions();
            }
            else
                base.OnInvalidate(model, cause);
        }

        public override void CheckGraphBeforeImport()
        {
            base.CheckGraphBeforeImport();
            // If the graph is reimported it can be because one of its depedency such as the subgraphs, has been changed.

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
