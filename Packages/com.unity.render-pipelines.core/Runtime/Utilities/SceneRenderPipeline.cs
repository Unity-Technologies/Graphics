#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using UnityEngine.Analytics;

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
                renderPipelineAsset = GraphicsSettings.defaultRenderPipeline;
                firstTimeCreated = false;
            }

            //Send analytics each time to find usage in content dl on the asset store too
            SceneRenderPipelineAnalytic.SendAnalytic(this);
        }

        void OnEnable()
        {
            GraphicsSettings.defaultRenderPipeline = renderPipelineAsset;
        }


        [AnalyticInfo(eventName: "sceneRenderPipelineAssignment", vendorKey: "unity.srp", maxEventsPerHour: 10, maxNumberOfElements: 1000)]
        class SceneRenderPipelineAnalytic : IAnalytic
        {

            public SceneRenderPipelineAnalytic(string guid)
            {
                m_Data = new Data
                {
                    scene_guid = guid
                };
            }

            [System.Diagnostics.DebuggerDisplay("{scene_guid}")]
            [Serializable]
            internal struct Data : IAnalytic.IData
            {
                // Naming convention for analytics data
                public string scene_guid;
            };

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }

            static public void SendAnalytic(SceneRenderPipeline sender)
            {
                SceneRenderPipelineAnalytic analytic = new SceneRenderPipelineAnalytic(sender.gameObject.scene.GetGUID());
                EditorAnalytics.SendAnalytic(analytic);
            }

            Data m_Data;
        }


#endif
    }
}
