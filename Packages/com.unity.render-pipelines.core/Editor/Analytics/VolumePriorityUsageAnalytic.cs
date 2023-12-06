using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering;
using static UnityEngine.Analytics.IAnalytic;
using Scene = UnityEditor.SearchService.SceneSearch;

namespace UnityEditor.Rendering.Analytics
{
    // schema = com.unity3d.data.schemas.editor.analytics.uVolumePriorityUsageAnalyticData_v2
    // taxonomy = editor.analytics.uVolumePriorityUsageAnalyticData.v2
    internal class VolumePriorityUsageAnalytic
    {

        [AnalyticInfo(eventName: "uVolumePriorityUsageAnalyticData", version: 2, vendorKey: "unity.srp")]
        class Analytic : IAnalytic
        {
            public Analytic(Volume volume, string guid)
            {
                using (GenericPool<Data>.Get(out var data))
                {
                    data.volume_name = Hash128.Compute(volume.name).ToString();
                    data.scene_name = guid;
                    data.priority = volume.priority;
                    m_Data = data;
                }
            }

            [System.Diagnostics.DebuggerDisplay("{volume_name} - {scene_name} - {priority}")]
            [Serializable]
            class Data : IAnalytic.IData
            {
                // Naming convention for analytics data
                public string volume_name;
                public string scene_name;
                public float priority;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }

            Data m_Data;
        };

       
        public static void Send(Volume volume)
        {
            if (volume == null)
                return;

            var sceneGUID = EditorSceneManager.GetActiveScene().GetGUID();
            GUID guid = new GUID(sceneGUID);
            if (guid.Empty())
                return;

            Analytic analytic = new Analytic(volume, sceneGUID);
            AnalyticsUtils.SendData(analytic);

        }
    }
}
