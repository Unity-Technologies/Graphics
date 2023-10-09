using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

namespace UnityEditor.VFX
{
    static class VFXSubgraphUtility
    {
        public static int TransferExpressionToParameters(IList<VFXExpression> inputExpression, IEnumerable<VFXParameter> parameters, List<VFXExpression> backedUpExpressions = null)
        {
            int cptSlot = 0;
            foreach (var param in parameters)
            {
                VFXSlot outputSlot = param.outputSlots[0];

                param.subgraphMode = true;
                if (inputExpression.Count <= cptSlot)
                    continue;

                foreach (var slot in outputSlot.GetExpressionSlots())
                {
                    if (backedUpExpressions != null)
                        backedUpExpressions.Add(slot.GetExpression());
                    slot.SetExpression(inputExpression[cptSlot]);
                    cptSlot += 1;
                }
            }

            return cptSlot;
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
    }
    [VFXInfo]
    class VFXSubgraphOperator : VFXOperator
    {
        bool m_IsMissing;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected VisualEffectSubgraphOperator m_Subgraph;

        public VFXSubgraphOperator()
        {
            // This allow to detect when a resource is deleted or restored
            EditorApplication.projectChanged += OnProjectOrHierarchyChanged;
        }

        public void OnDestroy()
        {
            EditorApplication.projectChanged -= OnProjectOrHierarchyChanged;
        }

        public VisualEffectSubgraphOperator subgraph
        {
            get
            {
                if (m_Subgraph == null && !object.ReferenceEquals(m_Subgraph, null))
                {
                    string assetPath = AssetDatabase.GetAssetPath(m_Subgraph.GetInstanceID());

                    var newSubgraph = AssetDatabase.LoadAssetAtPath<VisualEffectSubgraphOperator>(assetPath);
                    if (newSubgraph != null)
                    {
                        m_Subgraph = newSubgraph;
                    }

                    m_IsMissing = m_Subgraph == null;
                }
                return m_Subgraph;
            }
        }

        [NonSerialized]
        VFXModel[] m_SubChildren;

        public void RecreateCopy()
        {
            ClearCopy();

            if (subgraph == null)
            {
                m_SubChildren = null;
                return;
            }

            var graph = m_Subgraph.GetResource().GetOrCreateGraph();
            HashSet<ScriptableObject> dependencies = new HashSet<ScriptableObject>();

            foreach (var child in graph.children.Where(t => t is VFXOperator || t is VFXParameter))
            {
                dependencies.Add(child);
                child.CollectDependencies(dependencies);
            }

            var copy = VFXMemorySerializer.DuplicateObjects(dependencies.ToArray());
            m_SubChildren = copy.OfType<VFXModel>().Where(t => t is VFXOperator || t is VFXParameter).ToArray();

            foreach (var child in copy)
            {
                child.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private void ClearCopy()
        {
            if (m_SubChildren != null)
            {
                foreach (var child in m_SubChildren)
                {
                    if (child != null)
                    {
                        ScriptableObject.DestroyImmediate(child, true);
                    }
                }
                m_SubChildren = null;
            }
        }

        public sealed override string name => m_Subgraph != null ? ObjectNames.NicifyVariableName(m_Subgraph.name) : "Empty Subgraph Operator";

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                if (m_SubChildren == null)
                    RecreateCopy();

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

        public override void GetImportDependentAssets(HashSet<int> dependencies)
        {
            base.GetImportDependentAssets(dependencies);
            if (!object.ReferenceEquals(m_Subgraph, null))
            {
                dependencies.Add(m_Subgraph.GetInstanceID());
            }
        }

        protected internal override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            if (cause == InvalidationCause.kSettingChanged)
            {
                var graph = GetGraph();

                if (graph != null && m_Subgraph != null && m_Subgraph.GetResource() is {} resource)
                {
                    var otherGraph = resource.GetOrCreateGraph();
                    if (otherGraph == graph || otherGraph.subgraphDependencies.Contains(graph.GetResource().visualEffectObject))
                        m_Subgraph = null; // prevent cyclic dependencies.

                    if (graph.GetResource().isSubgraph) // BuildSubgraphDependencies is called for vfx by recompilation, but in subgraph we must call it explicitly
                        graph.BuildSubgraphDependencies();

                    RecreateCopy();
                }
            }

            base.Invalidate(model, cause);
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

            m_Subgraph.GetResource().GetOrCreateGraph().CollectDependencies(objs, false);
        }

        public override void CheckGraphBeforeImport()
        {
            base.CheckGraphBeforeImport();

            // If the graph is reimported it can be because one of its dependency such as the subgraphs, has been changed.
            if (!VFXGraph.explicitCompile)
                ResyncSlots(true);
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            if (subgraph == null)
                return Array.Empty<VFXExpression>();

            if (m_SubChildren == null)
                RecreateCopy();

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

        void OnProjectOrHierarchyChanged()
        {
            var wasMissing = m_IsMissing;
            var temp = subgraph;
            if (wasMissing != m_IsMissing)
            {
                Invalidate(InvalidationCause.kExpressionGraphChanged);
            }
        }
    }
}
