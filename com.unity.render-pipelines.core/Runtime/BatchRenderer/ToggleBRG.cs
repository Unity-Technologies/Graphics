namespace UnityEngine.Rendering
{
    public class ToggleBRG : MonoBehaviour
    {
            void OnGUI()
            {
                var brg = GetComponent<RenderBRG>();
                if (brg == null)
                    return;

                GUI.contentColor = Color.black;
                var state = brg.enabled ? "enabled" : "disabled";
                GUILayout.Label( $"Toggle BRG (F). Current State: {state}");
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
