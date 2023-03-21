#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Setup a specific render pipeline on scene loading.
    /// This need to be used with caution as it will change project configuration.
    /// </summary>
#if UNITY_EDITOR
    [ExecuteAlways]
#endif
    public class SceneRenderPipeline : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] bool firstTimeCreated = true;
    
        /// <summary>
        /// Scriptable Render Pipeline Asset to setup on scene load.
        /// </summary>
        public RenderPipelineAsset renderPipelineAsset;

        void Awake()
        {
            if (firstTimeCreated)
            {
                renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
                firstTimeCreated = false;
            }

            //Send analytics each time to find usage in content dl on the asset store too
            SceneRenderPipelineAnalytic.Send(this);
        }

        void OnEnable()
        {
            GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
        }

        static class SceneRenderPipelineAnalytic
        {
            const int k_MaxEventsPerHour = 100;
            const int k_MaxNumberOfElements = 1000;
            const string k_VendorKey = "unity.srp";

            [System.Diagnostics.DebuggerDisplay("{scene_guid}")]
            internal struct Data
            {
                internal const string k_EventName = "sceneRenderPipelineAssignment";

                // Naming convention for analytics data
                public string scene_guid;
            };

            internal static void Send(SceneRenderPipeline sender)
            {
                if (!EditorAnalytics.enabled || EditorAnalytics.RegisterEventWithLimit(Data.k_EventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey) != UnityEngine.Analytics.AnalyticsResult.Ok)
                    return;

                var data = new Data() { scene_guid = sender.gameObject.scene.GetGUID() };
                EditorAnalytics.SendEventWithLimit(Data.k_EventName, data);
            }
        }
#endif
    }
}
