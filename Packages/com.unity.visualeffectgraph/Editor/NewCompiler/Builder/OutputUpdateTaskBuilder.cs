using System.Collections.Generic;
using System.Linq;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;
using UnityEngine.VFX;
using static UnityEditor.VFX.VfxTemplateBindings;

namespace UnityEditor.VFX
{
    class OutputUpdateTaskBuilder
    {
        const string k_TemplateName = "OutputUpdate";
        const string k_SortKeyName = "sortKey";

        readonly SubtaskBuilder m_SubtaskBuilder;

        public OutputUpdateTaskBuilder(SubtaskBuilder subtaskBuilder)
        {
            m_SubtaskBuilder = subtaskBuilder;
        }

        public VfxTemplatedTask Build(VFXAbstractParticleOutput outputContext, VFXSpace space,
            VFXExpressionGraph expressionGraph, VFXCompilationMode compilationMode,
            out ContextExpressions expressions)
        {
            expressions = ContextExpressions.ForDerivedTask(expressionGraph.BuildGPUMapper(outputContext),
                expressionGraph.GetBufferUsage(outputContext),
                CollectOwnParameters(outputContext, space, compilationMode));

            var bindings = new List<TemplatedTaskBinding>();

            // Reads only, like the output it derives from: its attribute writes never reach the store.
            var particleSystemBinding = new TemplatedTaskBinding<ParticleData>(ParticleDataBindingKey);
            particleSystemBinding.AddAsRead(AttributeDataPath, outputContext.attributes);
            particleSystemBinding.Add(AttributeDataPath, VFXAttribute.Alive, AttributeUsage.Read);
            bindings.Add(particleSystemBinding);

            var contextDataBinding = new TemplatedTaskBinding<ValueData>(ContextDataKey);
            contextDataBinding.ReadPathSet.Add(MaxParticleCountPath);
            contextDataBinding.ReadPathSet.Add(SystemSeedPath);
            bindings.Add(contextDataBinding);

            var renderingDataBinding = new TemplatedTaskBinding<RenderingData>(RenderingDataBindingKey);
            renderingDataBinding.WritePathSet.Add(DataPath.Root + RenderingData.SortingData);
            bindings.Add(renderingDataBinding);

            var args = new VfxTemplatedTask.Args
            {
                Stage = VfxTaskStage.Update,
                IsPerCamera = IsPerCamera(outputContext),
                BaseArgs = new TemplatedTask.Args
                {
                    Subtasks = new(),
                    Bindings = bindings,
                },
                ContextParameters = new(),
                AttributeKeyMappings = new()
                {
                    [IntermediateGraphBuilder.DefaultAttributeKey] =
                        new(ParticleDataBindingKey, AttributeDataPath)
                }
            };

            m_SubtaskBuilder.GenerateSubtasks(outputContext, expressions, args);

            foreach (var subtask in args.BaseArgs.Subtasks)
            {
                if (subtask.Task is TemplateSubtask templateSubtask)
                {
                    foreach (var attributeSet in templateSubtask.AttributeSets.Values)
                        attributeSet.ClearWrite();
                }
            }

            return new VfxTemplatedTask(k_TemplateName, args, outputContext.GetEntityId());
        }

        public static bool IsPerCamera(VFXAbstractParticleOutput outputContext)
            => VFXSortingUtility.IsPerCamera(outputContext.GetSortCriterion());

        static IEnumerable<VFXNamedExpression> CollectOwnParameters(VFXAbstractParticleOutput outputContext,
            VFXSpace space, VFXCompilationMode compilationMode)
        {
            var sortKey = BuildSortKey(outputContext, space);
            if (sortKey == null)
                yield break;

            // Reduced here: no context reports this expression, so the expression graph never saw it.
            yield return new VFXNamedExpression(VfxExpressionReducer.ReduceForGpu(sortKey, compilationMode), k_SortKeyName);
        }

        static VFXExpression BuildSortKey(VFXAbstractParticleOutput outputContext, VFXSpace space)
        {
            VFXExpression key;
            var sign = outputContext.revertSorting ? -1f : 1f;

            switch (outputContext.GetSortCriterion())
            {
                case VFXSortingUtility.SortCriteria.Custom:
                {
                    var sortKeySlot = outputContext.inputSlots.FirstOrDefault(slot => slot.name == k_SortKeyName);
                    if (sortKeySlot == null)
                    {
                        Debug.LogWarning($"{outputContext.name} sorts on a custom key, but has no {k_SortKeyName} slot to sort on.");
                        return null;
                    }
                    key = sortKeySlot.GetExpression();
                    sign = -sign;
                    break;
                }

                case VFXSortingUtility.SortCriteria.YoungestInFront:
                    key = new VFXAttributeExpression(VFXAttribute.Age);
                    break;

                case VFXSortingUtility.SortCriteria.DistanceToCamera:
                    key = VFXOperatorUtility.SqrDistance(CameraRelativePositionExpression(space), new VfxCurrentViewPositionExpression());
                    break;

                case VFXSortingUtility.SortCriteria.CameraDepth:
                    var viewForward = new VFXExpressionTransformDirection(new VfxCurrentViewToWorldMatrixExpression(),
                        VFXValue.Constant(new Vector3(0f, 0f, -1f)));
                    key = VFXOperatorUtility.Dot(CameraRelativePositionExpression(space), viewForward);
                    break;

                default:
                    Debug.LogWarning($"{outputContext.name} sorts on a criterion the new compiler does not know: {outputContext.GetSortCriterion()}.");
                    return null;
            }

            return sign == 1f ? key : VFXOperatorUtility.Negate(key);
        }

        static VFXExpression CameraRelativePositionExpression(VFXSpace space)
        {
            VFXExpression position = new VFXAttributeExpression(VFXAttribute.Position);
            if (space == VFXSpace.Local)
                position = new VFXExpressionTransformPosition(VFXBuiltInExpression.LocalToWorld, position);
            return new VfxCameraRelativePositionExpression(position);
        }
    }
}
