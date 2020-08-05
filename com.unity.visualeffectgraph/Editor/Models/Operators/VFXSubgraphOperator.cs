using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.VFX;
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
            List<VFXPropertyAttribute> attributes = new List<VFXPropertyAttribute>();
            if (!string.IsNullOrEmpty(param.tooltip))
                attributes.Add(new VFXPropertyAttribute(VFXPropertyAttribute.Type.kTooltip, param.tooltip));

            if (param.hasRange)
                attributes.Add(new VFXPropertyAttribute(VFXPropertyAttribute.Type.kRange, (float)VFXConverter.ConvertTo(param.m_Min.Get(), typeof(float)), (float)VFXConverter.ConvertTo(param.m_Max.Get(), typeof(float))));

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
            return models.OfType<VFXParameter>().Where(t => predicate(t)).OrderBy(t => t.order);
        }
    }
    [VFXInfo]
    class VFXSubgraphOperator : VFXOperator
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected VisualEffectSubgraphOperator m_Subgraph;

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
                }
                return m_Subgraph;
            }
        }

        public sealed override string name { get { return m_Subgraph != null ? m_Subgraph.name : "Empty Subgraph Operator"; } }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var param in GetParameters(t => VFXSubgraphUtility.InputPredicate(t)).OrderBy(t => t.order))
                {
                    yield return VFXSubgraphUtility.GetPropertyFromInputParameter(param);
                }
            }
        }
        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                foreach (var param in GetParameters(t => VFXSubgraphUtility.OutputPredicate(t)).OrderBy(t => t.order))
                {
                    if (!string.IsNullOrEmpty(param.tooltip))
                        yield return new VFXPropertyWithValue(new VFXProperty(param.type, param.exposedName, new VFXPropertyAttribute(VFXPropertyAttribute.Type.kTooltip, param.tooltip)));
                    else
                        yield return new VFXPropertyWithValue(new VFXProperty(param.type, param.exposedName));
                }
            }
        }
        public override void GetImportDependentAssets(HashSet<int> dependencies)
        {
            base.GetImportDependentAssets(dependencies);
            if (!object.ReferenceEquals(m_Subgraph, null))
                dependencies.Add(m_Subgraph.GetInstanceID());
        }

        protected internal override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            if (cause == InvalidationCause.kSettingChanged)
            {
                var graph = GetGraph();

                if (graph != null && m_Subgraph != null && m_Subgraph.GetResource() != null)
                {
                    var otherGraph = m_Subgraph.GetResource().GetOrCreateGraph();
                    if (otherGraph == graph || otherGraph.subgraphDependencies.Contains(graph.GetResource().visualEffectObject))
                        m_Subgraph = null; // prevent cyclic dependencies.
                    if (graph.GetResource().isSubgraph) // BuildSubgraphDependenciesis called for vfx by recompilation, but in subgraph we must call it explicitely
                        graph.BuildSubgraphDependencies();
                }
            }

            base.Invalidate(model, cause);
        }

        IEnumerable<VFXParameter> GetParameters(Func<VFXParameter, bool> predicate)
        {
            if (m_Subgraph == null && !object.ReferenceEquals(m_Subgraph, null))
                m_Subgraph = EditorUtility.InstanceIDToObject(m_Subgraph.GetInstanceID()) as VisualEffectSubgraphOperator;
            if (m_Subgraph == null)
                return Enumerable.Empty<VFXParameter>();
            VFXGraph graph = subgraph.GetResource().GetOrCreateGraph();
            return VFXSubgraphUtility.GetParameters(graph.children, predicate);
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
            // If the graph is reimported it can be because one of its depedency such as the subgraphs, has been changed.

            ResyncSlots(true);
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            if (subgraph == null)
                return new VFXExpression[0];
            VFXGraph graph = m_Subgraph.GetResource().GetOrCreateGraph();

            // Change all the inputExpressions of the parameters.
            var parameters = GetParameters(t => VFXSubgraphUtility.InputPredicate(t)).OrderBy(t => t.order);

            var backedUpExpressions = new List<VFXExpression>();

            VFXSubgraphUtility.TransferExpressionToParameters(inputExpression, parameters, backedUpExpressions);

            List<VFXExpression> outputExpressions = new List<VFXExpression>();
            foreach (var param in GetParameters(t => VFXSubgraphUtility.OutputPredicate(t)))
            {
                outputExpressions.AddRange(param.inputSlots[0].GetExpressionSlots().Select(t => t.GetExpression()));
            }

            foreach (var param in parameters)
            {
                param.ResetOutputValueExpression();
            }

            VFXSubgraphUtility.TransferExpressionToParameters(backedUpExpressions, parameters);

            return outputExpressions.ToArray();
        }
    }
}
