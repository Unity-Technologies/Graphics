using Unity.Collections;
using UnityEngine.Profiling;
using static UnityEngine.ObjectDispatcher;

namespace UnityEngine.Rendering
{
    internal class MeshRendererDrawer
    {
        private GPUResidentDrawer m_GPUResidentDrawer;
        private ObjectDispatcher m_Dispatcher;

        public MeshRendererDrawer(GPUResidentDrawer gpuResidentDrawer, ObjectDispatcher dispatcher)
        {
            m_GPUResidentDrawer = gpuResidentDrawer;
            m_Dispatcher = dispatcher;

            m_Dispatcher.EnableTypeTracking<MeshRenderer>(TypeTrackingFlags.SceneObjects);
            m_Dispatcher.EnableTransformTracking<MeshRenderer>(TransformTrackingType.GlobalTRS);
        }

        public void Dispose()
        {
            m_Dispatcher.DisableTypeTracking<MeshRenderer>();
            m_Dispatcher.DisableTransformTracking<MeshRenderer>(TransformTrackingType.GlobalTRS);
        }

        public void ProcessDraws()
        {
            Profiler.BeginSample("MeshRendererDrawer.ProcessMeshRenderers");
            var rendererChanges = m_Dispatcher.GetTypeChangesAndClear<MeshRenderer>(Allocator.TempJob, noScriptingArray: true);
            m_GPUResidentDrawer.ProcessRenderers(rendererChanges.changedID);
            m_GPUResidentDrawer.FreeRendererGroupInstances(rendererChanges.destroyedID);
            rendererChanges.Dispose();
            Profiler.EndSample();

            Profiler.BeginSample("MeshRendererDrawer.TransformMeshRenderers");
            var transformChanges = m_Dispatcher.GetTransformChangesAndClear<MeshRenderer>(TransformTrackingType.GlobalTRS, Allocator.TempJob);
            var transformedInstances = new NativeArray<InstanceHandle>(transformChanges.transformedID.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_GPUResidentDrawer.ScheduleQueryRendererGroupInstancesJob(transformChanges.transformedID, transformedInstances).Complete();
            // We can pull localToWorldMatrices directly from the renderers if we are doing update after PostLatUpdate.
            // This will save us transform re computation as matrices are ready inside renderer's TransformInfo.
            m_GPUResidentDrawer.TransformInstances(transformedInstances, transformChanges.localToWorldMatrices);
            transformedInstances.Dispose();
            transformChanges.Dispose();
            Profiler.EndSample();
        }
    }
}
