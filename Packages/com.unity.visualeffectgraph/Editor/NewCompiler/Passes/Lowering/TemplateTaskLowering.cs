using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Lowers the templated tasks, binding each concrete buffer under the same name the generated shader
    /// code declares it by, so the code produced by <see cref="VfxTemplateCodeGenerationPass"/> keeps
    /// resolving.
    /// </summary>
    class TemplateTaskLowering : TaskLoweringPass.TaskLowering<VfxTemplatedTask>
    {
        readonly DataDescriptionWriterRegistry m_DataWriter;

        public TemplateTaskLowering(DataDescriptionWriterRegistry dataWriter)
        {
            m_DataWriter = dataWriter;
        }

        public override IDataKey GetConcreteBindingKey(VfxTemplatedTask task, IDataKey logicalBindingKey, DataView dataView)
        {
            // The writer is keyed on the root description: it is the composite writer (particle data, event
            // list) that names the sub-buffers, not the sub-view's own writer.
            if (!m_DataWriter.TryGetDataDescriptionWriter(dataView.Root.DataDescription, out var writer))
                return null;

            var bindingName = writer.GetResourceName(dataView);
            return bindingName != null ? new NameDataKey(bindingName) : null;
        }

        public override void ConfigureTask(VfxTemplatedTask oldTask, VfxTask newTask)
        {
            newTask.TaskType = ToRuntimeTaskType(oldTask);
            newTask.SourceModelId = oldTask.SourceModelId;

            foreach (var (key, value) in oldTask.ParameterValues)
                newTask.AddParameterValue(key, value);
        }

        static UnityEngine.VFX.VFXTaskType ToRuntimeTaskType(VfxTemplatedTask task)
        {
            switch (task.Stage)
            {
                case VfxTaskStage.Initialize:
                    return UnityEngine.VFX.VFXTaskType.Initialize;
                case VfxTaskStage.Update:
                    return task.IsPerCamera ? UnityEngine.VFX.VFXTaskType.PerCameraUpdate : UnityEngine.VFX.VFXTaskType.Update;
                case VfxTaskStage.Output:
                    return task.Topology switch
                    {
                        VfxOutputTopology.Point => UnityEngine.VFX.VFXTaskType.ParticlePointOutput,
                        VfxOutputTopology.Line => UnityEngine.VFX.VFXTaskType.ParticleLineOutput,
                        VfxOutputTopology.Triangle => UnityEngine.VFX.VFXTaskType.ParticleTriangleOutput,
                        VfxOutputTopology.Quad => UnityEngine.VFX.VFXTaskType.ParticleQuadOutput,
                        VfxOutputTopology.Octagon => UnityEngine.VFX.VFXTaskType.ParticleOctagonOutput,
                        VfxOutputTopology.Hexahedron => UnityEngine.VFX.VFXTaskType.ParticleHexahedronOutput,
                        VfxOutputTopology.Mesh => UnityEngine.VFX.VFXTaskType.ParticleMeshOutput,
                        _ => throw new System.ArgumentOutOfRangeException(nameof(task.Topology), task.Topology, null),
                    };
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(task.Stage), task.Stage, null);
            }
        }

        public override void OnBindingLowered(VfxTemplatedTask oldTask, VfxTask newTask,
            IDataKey logicalBindingKey, IDataKey concreteBindingKey)
        {
            if (oldTask.IsTaskParameter(logicalBindingKey))
            {
                newTask.AddParameterBinding(concreteBindingKey);
            }
        }

        public override void OnTaskLowered(TaskNodeId oldId, TaskNodeId newId, ref CompilationContext context)
        {
            // The generated source is keyed by task node, so it has to follow the replacement.
            var generatedCodeContainer = context.data.Get<GeneratedCodeContainer>();
            generatedCodeContainer.Add(newId, generatedCodeContainer.Find(oldId));
        }

        public override void OnLoweringComplete(ref CompilationContext context)
        {
            // The templated tasks were the readers of the system task's logical bindings, and they now
            // read the buffers those were lowered to, bound under BufferKeys by SystemTaskBindingsPass.
            // Retiring them any earlier would take with them the parents the tasks are rebound onto.
            var particleSystemContainer = context.data.Get<ParticleSystemContainer>();
            foreach (var particleSystem in particleSystemContainer)
            {
                context.graph.UnbindData(particleSystem.SystemTask.Id, ParticleSystemTask.ParticleDataBindingKey);
                context.graph.UnbindData(particleSystem.SystemTask.Id, ParticleSystemTask.GraphValuesBindingKey);
                context.graph.UnbindData(particleSystem.SystemTask.Id, ParticleSystemTask.CpuEventListBindingKey);
            }
        }
    }
}
