using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;
using static UnityEditor.VFX.VfxTemplateBindings;

namespace UnityEditor.VFX
{
    static class FillIndirectTaskBuilder
    {
        const string k_TemplateName = "FillIndirect";

        public static VfxTemplatedTask Build(VFXAbstractParticleOutput outputContext)
        {
            var bindings = new List<TemplatedTaskBinding>();

            var particleSystemBinding = new TemplatedTaskBinding<ParticleData>(ParticleDataBindingKey);
            particleSystemBinding.Add(AttributeDataPath, VFXAttribute.Alive, AttributeUsage.Read);
            bindings.Add(particleSystemBinding);

            var contextDataBinding = new TemplatedTaskBinding<ValueData>(ContextDataKey);
            contextDataBinding.ReadPathSet.Add(MaxParticleCountPath);
            bindings.Add(contextDataBinding);

            var renderingDataBinding = new TemplatedTaskBinding<RenderingData>(RenderingDataBindingKey);
            renderingDataBinding.WritePathSet.Add(DataPath.Root + RenderingData.IndirectBuffer);
            bindings.Add(renderingDataBinding);

            var args = new VfxTemplatedTask.Args
            {
                Stage = VfxTaskStage.Update,
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

            return new VfxTemplatedTask(k_TemplateName, args, outputContext.GetEntityId());
        }
    }
}
