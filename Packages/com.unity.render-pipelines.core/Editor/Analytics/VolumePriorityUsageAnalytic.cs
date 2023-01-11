using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Scene = UnityEditor.SearchService.SceneSearch;

namespace UnityEditor.Rendering.Analytics
{
    // schema = com.unity3d.data.schemas.editor.analytics.uVolumePriorityUsageAnalyticData_v2
    // taxonomy = editor.analytics.uVolumePriorityUsageAnalyticData.v2
    internal class VolumePriorityUsageAnalytic
    {
        [System.Diagnostics.DebuggerDisplay("{volume_name} - {scene_name} - {priority}")]
        class Data
        {
            internal const string k_EventName = "uVolumePriorityUsageAnalyticData";
            internal const int k_Version = 2;

            // Naming convention for analytics data
            public string volume_name;
            public string scene_name;
            public float priority;
        }

        public static void Send(Volume volume)
        {
            if (volume == null || !AnalyticsUtils.TryRegisterEvent(Data.k_EventName, Data.k_Version))
                return;

            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
                return;

            using (GenericPool<Data>.Get(out var data))
            {
                data.volume_name = Hash128.Compute(volume.name).ToString();
                data.scene_name = AssetDatabase.AssetPathToGUID(scene.path);
                data.priority = volume.priority;
                AnalyticsUtils.SendData<Data>(data, Data.k_EventName, Data.k_Version);
            }
        }
    }
}
