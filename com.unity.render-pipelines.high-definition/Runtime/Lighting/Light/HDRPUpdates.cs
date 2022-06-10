#if XP_CRAZYHUNTER
namespace UnityEngine.Rendering.HighDefinition
{
    [ExecuteAlways]
    public class HDRPUpdates : MonoBehaviour
    {
        private static HDRPUpdates s_Instance;

        internal static void LazyInit()
        {
            if (s_Instance == null)
            {
                GameObject go = new GameObject("HDRPUpdates");
                DontDestroyOnLoad(go);
                s_Instance = go.AddComponent<HDRPUpdates>();
            }
        }

        private void LateUpdate()
        {
            // Prevent any unwanted sync when not in HDRP (case 1217575)
            if (HDRenderPipeline.currentPipeline == null)
                return;

            var hdAdditionalLightDatas = HDAdditionalLightData.s_InstancesHDAdditionalLightData;
            int dataCount = hdAdditionalLightDatas.Count;

            for (int i = dataCount - 1; i >= 0; i--)
            {
                hdAdditionalLightDatas[i].DoLateUpdate();
            }
        }
    }
}
#endif
