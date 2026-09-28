using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Lowers the templated tasks, binding each concrete buffer under the same name the generated shader
    /// code declares it by, so the code produced by <see cref="VfxTemplateCodeGenerationPass"/> keeps
    /// resolving.
    /// </summary>
    class TemplateToVfxTaskPass : TaskReplacementPass
    {
        readonly DataDescriptionWriterRegistry m_DataWriter;

        public TemplateToVfxTaskPass(DataDescriptionWriterRegistry dataWriter)
        {
            m_DataWriter = dataWriter;
        }

        protected override bool IsTargetTask(ITask task) => task is VfxTemplatedTask;

        protected override IDataKey GetConcreteBindingKey(DataView dataView)
        {
            // The writer is keyed on the root description: it is the composite writer (particle data, event
            // list) that names the sub-buffers, not the sub-view's own writer.
            if (!m_DataWriter.TryGetDataDescriptionWriter(dataView.Root.DataDescription, out var writer))
                return null;

            var bindingName = writer.GetResourceName(dataView);
            return bindingName != null ? new NameDataKey(bindingName) : null;
        }

        protected override void OnBindingLowered(ITask oldTask, VfxTask newTask, IDataKey logicalBindingKey, IDataKey concreteBindingKey)
        {
            if (oldTask is VfxTemplatedTask templatedTask && templatedTask.IsTaskParam(logicalBindingKey))
            {
                newTask.AddParamBinding(concreteBindingKey);
            }
        }

        protected override void OnTaskNodeReplaced(TaskNodeId oldId, TaskNodeId newId, ref CompilationContext context)
        {
            // The generated source is keyed by task node, so it has to follow the replacement.
            var generatedCodeContainer = context.data.Get<GeneratedCodeContainer>();
            generatedCodeContainer.Add(newId, generatedCodeContainer.Find(oldId));
        }

        protected override void PatchSystemContainer(
            Dictionary<TaskNodeId, TaskNodeId> replacements,
            ref CompilationContext context)
        {
            var particleSystemContainer = context.data.Get<ParticleSystemContainer>();
            foreach (var particleSystem in particleSystemContainer)
            {
                for (int i = 0; i < particleSystem.Tasks.Count; i++)
                {
                    var task = particleSystem.Tasks[i];
                    if (replacements.TryGetValue(task.Id, out var replacementId))
                        particleSystem.Tasks[i] = new ParticleSystemContainer.Task(task.Name, replacementId, task.TaskType, task.ModelId);
                }

                // The logical bindings on the system task have served their purpose: the buffers they were
                // lowered to are bound under BufferKeys by SystemTaskBindingsPass.
                context.graph.UnbindData(particleSystem.SystemTask.Id, ParticleSystemTask.ParticleDataBindingKey);
                context.graph.UnbindData(particleSystem.SystemTask.Id, ParticleSystemTask.GraphValuesBindingKey);
                context.graph.UnbindData(particleSystem.SystemTask.Id, ParticleSystemTask.CpuEventListBindingKey);
            }
        }
    }
}
