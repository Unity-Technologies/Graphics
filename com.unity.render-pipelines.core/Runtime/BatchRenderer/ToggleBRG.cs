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
        }

        private void Update()
        {
            var brg = GetComponent<RenderBRG>();
            if (brg == null)
                return;

            if (Input.GetKeyDown(KeyCode.F))
                brg.enabled = !brg.enabled;
        }
    }
}
