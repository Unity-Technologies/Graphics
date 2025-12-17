using UnityEditor;

namespace UnityEngine.Rendering
{
    //@ This is a simple instance data viewer class used for instance data debugging.
    //@ This could become a user facing tool in the future. Where the user could see the instance data in the GPU.
    //@ It could show layouts, archetypes, components and instance data.
    internal class GPUInstanceDataBufferViewer : EditorWindow
    {
        [SerializeField] private Vector2 m_ScrollPos;

        public static void InitializeWindow()
        {
            GPUInstanceDataBufferViewer wnd = GetWindow<GPUInstanceDataBufferViewer>();
            wnd.titleContent = new GUIContent("GPU Resident Instance Data Viewer");
        }

        public void OnGUI()
        {
            if (!GPUResidentDrawer.IsInitialized())
            {
                EditorGUILayout.LabelField("GPU Resident Drawer is not enabled in Render Pipeline Asset.");
                return;
            }

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            //@ This is usefully for internal GPU Resident Drawer debugging.

            ref GPUArchetypeManager archetypeMgr = ref GPUResidentDrawer.GetGPUArchetypeManager().GetRef();
            ref DefaultGPUComponents defaultGPUComponents = ref GPUResidentDrawer.GetDefaultGPUComponents();
            var buffer = GPUResidentDrawer.GetInstanceDataBuffer();
            var readback = GPUResidentDrawer.ReadbackInstanceDataBuffer<uint>();

            EditorGUILayout.LabelField($"Archetypes Count: {buffer.layout.archetypes.Length}");

            for (int i = 0; i < buffer.layout.archetypes.Length; ++i)
            {
                var archetype = buffer.layout.archetypes[i];
                var archetypeDesc = archetypeMgr.GetArchetypeDesc(archetype);
                EditorGUILayout.LabelField($"Archetype {i}, ID: {archetype.index}");
                EditorGUILayout.LabelField("Components:");
                for (int j = 0; j < archetypeDesc.components.Length; ++j)
                {
                    var component = archetypeDesc.components[j];
                    var componentDesc = archetypeMgr.GetComponentDesc(component);

                    // Replace when Shader.PropertyIDToName API lands
                    //var componentName = Shader.PropertyIDToName(componentDesc.propertyID);
                    string componentName = "<unknown>";
                    EditorGUILayout.LabelField($"Component {j} - Name: {componentName}, ID: {component.index}");
                }
            }

            for (int archIndex = 0; archIndex < buffer.layout.archetypes.Length; ++archIndex)
            {
                var archetype = buffer.layout.archetypes[archIndex];
                var archetypeDesc = archetypeMgr.GetArchetypeDesc(archetype);

                EditorGUILayout.LabelField($"====== Archetype {archetype} Instance Data ======");

                var count = buffer.layout.instancesCount[archIndex];
                for (int instanceIndex = 0; instanceIndex < count; ++instanceIndex)
                {
                    GPUInstanceIndex gpuIndex = buffer.InstanceGPUHandleToGPUIndex(InstanceGPUHandle.Create(archetype, instanceIndex));
                    PackedMatrix objectToWorld = readback.LoadData<PackedMatrix>(defaultGPUComponents.objectToWorld, gpuIndex);
                    EditorGUILayout.LabelField($"Instance {instanceIndex}; GPU Index: {gpuIndex.index}; ObjectToWorld {objectToWorld}");
                    //@ Read and display more instance data here if needed...
                }
            }

            readback.Dispose();

            EditorGUILayout.EndScrollView();
        }
    }
}
