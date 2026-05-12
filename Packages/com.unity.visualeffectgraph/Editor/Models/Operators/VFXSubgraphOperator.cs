using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    static class VFXSubgraphUtility
    {
        public static bool IsSubgraphModel(VFXModel model) => model is VFXSubgraphBlock or VFXSubgraphContext or VFXSubgraphOperator;

        public static int TransferExpressionToParameter(IList<VFXExpression> inputExpressions, int expressionOffset, VFXParameter param, List<VFXExpression> backedUpExpressions = null)
        {
            var outputSlot = param.outputSlots[0];
            param.subgraphMode = true;

            foreach (var slot in outputSlot.GetExpressionSlots())
            {
                backedUpExpressions?.Add(slot.GetExpression());
                slot.SetExpression(inputExpressions[expressionOffset++]);
            }

            return expressionOffset;
        }

        public static int TransferExpressionToParameters(IList<VFXExpression> inputExpression, IEnumerable<VFXParameter> parameters, List<VFXExpression> backedUpExpressions = null)
        {
            int expressionOffset = 0;
            foreach (var param in parameters)
            {
                expressionOffset = TransferExpressionToParameter(inputExpression, expressionOffset, param, backedUpExpressions);
            }

            return expressionOffset;
        }

        public static VFXPropertyWithValue GetPropertyFromInputParameter(VFXParameter param)
        {
            List<object> attributes = new List<object>();
            if (!string.IsNullOrEmpty(param.tooltip))
                attributes.Add(new TooltipAttribute(param.tooltip));

            if (param.valueFilter == VFXValueFilter.Range)
                attributes.Add(new RangeAttribute((float)VFXConverter.ConvertTo(param.min, typeof(float)), (float)VFXConverter.ConvertTo(param.max, typeof(float))));
            else if (param.valueFilter == VFXValueFilter.Enum)
                attributes.Add(new EnumAttribute(param.enumValues.ToArray()));

            return new VFXPropertyWithValue(new VFXProperty(param.type, param.exposedName, attributes.ToArray()), param.value);
        }

        public static bool InputPredicate(VFXParameter param)
        {
            return param.exposed && !param.isOutput;
        }

        public static bool OutputPredicate(VFXParameter param)
        {
            return param.isOutput;
        }

        public static IEnumerable<VFXParameter> GetParameters(IEnumerable<VFXModel> models, Func<VFXParameter, bool> predicate)
        {
            return models.OfType<VFXParameter>().Where(predicate).OrderBy(t => t.order);
        }

        public static void ResyncCustomAttributes(VFXGraph mainGraph, VFXGraph subGraph)
        {
            if (mainGraph == null || subGraph == null)
            {
                return;
            }

            foreach (var customAttribute in subGraph.customAttributes)
            {
                if (!mainGraph.attributesManager.Exist(customAttribute.attributeName))
                {
                    mainGraph.TryAddCustomAttribute(customAttribute.attributeName, CustomAttributeUtility.GetValueType(customAttribute.type), customAttribute.description, true, out _);
                    mainGraph.Invalidate(VFXModel.InvalidationCause.kExpressionGraphChanged);
                }
                else
                {
                    mainGraph.TryUpdateCustomAttribute(customAttribute.attributeName, customAttribute.type, customAttribute.description, true);
                }
                mainGraph.SetCustomAttributeDirty();
            }
        }

        public static void SetSubgraphFlattenParentsDeep(VFXGraph graph)
        {
            if (graph == null)
                throw new ArgumentNullException();

            foreach (var child in graph.children)
            {
                if (child is IVFXSubgraphModel subGraph)
                    subGraph.SetSubmodelsFlattenedParents(graph);

                else
                {
                    if (child is VFXContext context)
                    {
                        foreach (var block in context.children)
                            if (block is VFXSubgraphBlock subBlock)
                                subBlock.SetSubmodelsFlattenedParents(context);
                    }
                }
            }
        }
    }


    [VFXHelpURL("Subgraph")]
    [VFXInfo(name = "Empty Subgraph Operator")]
    class VFXSubgraphOperator : VFXOperator, IVFXAttributeUsage, IVFXSubgraphModel
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected VisualEffectSubgraphOperator m_Subgraph;

        // Cached resource copy
        [NonSerialized]
        private VisualEffectResource m_ResourceCopy;
        [NonSerialized]
        private VFXModel[] m_SubChildren;

        public VisualEffectResource resourceCopy => m_ResourceCopy;

        public void OnDestroy()
        {
            ClearCopy();
        }

        public VisualEffectSubgraphOperator subgraph
        {
            get
            {
                if (m_Subgraph == null && !object.ReferenceEquals(m_Subgraph, null))
                {
                    string assetPath = AssetDatabase.GetAssetPath(m_Subgraph.GetEntityId());

                    var newSubgraph = AssetDatabase.LoadAssetAtPath<VisualEffectSubgraphOperator>(assetPath);
                    if (newSubgraph != null)
                    {
                        m_Subgraph = newSubgraph;
                    }
                }
                return m_Subgraph;
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
                Debug.Log($"VfxSubgraphOperator::RecreateCopy for {name} ({GetEntityId()}) of type {GetType()}. Path: {AssetDatabase.GetAssetPath(m_Subgraph.GetEntityId())}. COPY ID: {m_ResourceCopy.GetEntityId()}");

            var copyGraph = m_ResourceCopy.GetGraph();
            if (copyGraph == null)
                throw new InvalidOperationException("GetResourceAtPathAndForget failure");

            copyGraph.SanitizeGraph();

            var dependencies = new HashSet<ScriptableObject>();
            foreach (var child in copyGraph.children.Where(t => t is VFXOperator || t is VFXParameter))
            {
                dependencies.Add(child);
                child.CollectDependencies(dependencies);
            }

            m_SubChildren = dependencies.OfType<VFXModel>().Where(t => t is VFXOperator || t is VFXParameter).ToArray();
            var usedSubgraph = copyGraph;
            usedSubgraph.SyncCustomAttributes();

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
                throw new Exception("Bad internal state for VFXSubgraphOperator");
        }

        public sealed override string name => m_Subgraph != null ? ObjectNames.NicifyVariableName(m_Subgraph.name) : "Empty Subgraph Operator";

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                RecreateCopyIfNeeded();
                return GetParameters(VFXSubgraphUtility.InputPredicate)
                    .OrderBy(x => x.order)
                    .Select(VFXSubgraphUtility.GetPropertyFromInputParameter);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                foreach (var param in GetParameters(VFXSubgraphUtility.OutputPredicate).OrderBy(t => t.order))
                {
                    if (!string.IsNullOrEmpty(param.tooltip))
                        yield return new VFXPropertyWithValue(new VFXProperty(param.type, param.exposedName, new TooltipAttribute(param.tooltip)));
                    else
                        yield return new VFXPropertyWithValue(new VFXProperty(param.type, param.exposedName));
                }
            }
        }

        public sealed override bool IsDependentOnAnyOf(HashSet<EntityId> dependencies)
        {
            if (base.IsDependentOnAnyOf(dependencies))
                return true;

            if (!ReferenceEquals(m_Subgraph, null) && dependencies.Contains(m_Subgraph.GetEntityId()))
                return true;

            return false;
        }

        public void SetSubmodelsFlattenedParents(VFXModel parent)
        {
            // Nothing is needed for sub operators atm
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (cause == InvalidationCause.kSettingChanged)
            {
                var graph = GetGraph();

                if (graph != null && m_Subgraph != null && m_Subgraph.GetResource() is {} resource)
                {
                    var otherGraph = resource.GetGraph();
                    if (otherGraph == graph || otherGraph.subgraphDependencies.Contains(graph.GetResource().visualEffectObject))
                        m_Subgraph = null; // prevent cyclic dependencies.

                    if (graph.GetResource().isSubgraph) // BuildSubgraphDependencies is called for vfx by recompilation, but in subgraph we must call it explicitly
                        graph.BuildSubgraphDependencies();

                    RecreateCopy();
                }
            }

            base.OnInvalidate(model, cause);
        }

        IEnumerable<VFXParameter> GetParameters(Func<VFXParameter, bool> predicate)
        {
            return m_Subgraph != null
                ? VFXSubgraphUtility.GetParameters(m_SubChildren, predicate)
                : Enumerable.Empty<VFXParameter>();
        }

        public override void CollectDependencies(HashSet<ScriptableObject> objs, bool ownedOnly = true)
        {
            base.CollectDependencies(objs, ownedOnly);

            if (ownedOnly || m_Subgraph == null)
                return;

            m_Subgraph.GetResource().GetGraph().CollectDependencies(objs, false);
        }

        public override void ResyncDependencies()
        {
            base.ResyncDependencies();

            ClearCopy();

            MarkOutputExpressionsAsOutOfDate();
            ResyncSlots(true);
            if (m_Subgraph != null)
                VFXSubgraphUtility.ResyncCustomAttributes(GetGraph(), m_Subgraph.GetResource().GetGraph());
        }

        protected override void OnAdded()
        {
            base.OnAdded();
            if (m_Subgraph != null)
            {
                VFXSubgraphUtility.ResyncCustomAttributes(GetGraph(), GetOrCreateResourceCopy().GetGraph());
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            if (subgraph == null)
                return Array.Empty<VFXExpression>();

            RecreateCopyIfNeeded();

            // Change all the inputExpressions of the parameters.
            var parameters = GetParameters(VFXSubgraphUtility.InputPredicate).OrderBy(t => t.order);

            var backedUpExpressions = new List<VFXExpression>();

            VFXSubgraphUtility.TransferExpressionToParameters(inputExpression, parameters, backedUpExpressions);

            List<VFXExpression> outputExpressions = new List<VFXExpression>();
            foreach (var param in GetParameters(VFXSubgraphUtility.OutputPredicate))
            {
                outputExpressions.AddRange(param.inputSlots[0].GetExpressionSlots().Select(t => t.GetExpression()));
            }

            return outputExpressions.ToArray();
        }

        public IEnumerable<VFXAttribute> usedAttributes
        {
            get
            {
                if (m_Subgraph != null)
                {
                    var usedSubgraph = GetOrCreateResourceCopy().GetGraph();
                    foreach (var customAttribute in usedSubgraph.customAttributes)
                    {
                        if (usedSubgraph.attributesManager.TryFind(customAttribute.attributeName, out var attribute))
                        {
                            yield return attribute;
                        }
                    }
                }
            }
        }

        public void Rename(string oldName, string newName)
        {
            throw new NotSupportedException("The subgraph operator can use attributes, but cannot rename them");
        }
    }
}
