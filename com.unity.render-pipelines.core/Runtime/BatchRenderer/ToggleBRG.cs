namespace UnityEngine.Rendering
{
    public class ToggleBRG : MonoBehaviour
    {
        void OnGUI()
        {
            var brg = GetComponent<RenderBRG>();
            if (brg == null)
                return;

            var state = brg.enabled ? "enabled" : "disabled";
            if (GUILayout.Button($"Toggle BRG (F). Current State: {state}"))
                brg.enabled = !brg.enabled;

            GUILayout.Label($"Toggle Occlusion (1 = {OcclusionCullingMode.Disabled}, 2 = {OcclusionCullingMode.CubePrimitive}, 3 = {OcclusionCullingMode.ProceduralCube}, 4 = {OcclusionCullingMode.ProceduralCubeWithVSCulling}). Current state: {brg.OcclusionCullingMode}");
        }

        private void Update()
        {
            var brg = GetComponent<RenderBRG>();
            if (brg == null)
                return;

            if (Input.GetKeyDown(KeyCode.F))
                brg.enabled = !brg.enabled;

            if (Input.GetKeyDown(KeyCode.Alpha1))
                brg.OcclusionCullingMode = OcclusionCullingMode.Disabled;
            if (Input.GetKeyDown(KeyCode.Alpha2))
                brg.OcclusionCullingMode = OcclusionCullingMode.CubePrimitive;
            if (Input.GetKeyDown(KeyCode.Alpha3))
                brg.OcclusionCullingMode = OcclusionCullingMode.ProceduralCube;
            if (Input.GetKeyDown(KeyCode.Alpha4))
                brg.OcclusionCullingMode = OcclusionCullingMode.ProceduralCubeWithVSCulling;
        }
    }
}
