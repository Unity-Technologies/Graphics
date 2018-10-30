using UnityEditor;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class LWRPAdditionalLightData : MonoBehaviour
    {
        [Tooltip("Controls the usage of pipeline settings.")]
        [SerializeField] bool m_UsePipelineSettings = true;

        public bool usePipelineSettings
        {
            get { return m_UsePipelineSettings; }
            set { m_UsePipelineSettings = value; }
        }

        [MenuItem("CONTEXT/LWRPAdditionalLightData/Remove Component")]
        static void RemoveComponent(MenuCommand command)
        {
            if (EditorUtility.DisplayDialog("Remove Component?", "Are you sure you want to remove this component? If you do, you will lose some settings.", "Remove", "Cancel"))
            {
                Undo.DestroyObjectImmediate(command.context);
            }
        }
    }
}
