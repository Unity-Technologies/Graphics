using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Analytics
{
    // schema = com.unity3d.data.schemas.editor.analytics.uVolumeProfileUsageAnalytic_v4
    // taxonomy = editor.analytics.uVolumeProfileUsageAnalytic.v4
    internal class VolumeProfileUsageAnalytic
    {
        [System.Diagnostics.DebuggerDisplay("{volume_name} - {scene_name}- {volume_profile_asset_guid}")]
        class Data
        {
            internal const string k_EventName = "uVolumeProfileUsageAnalytic";
            internal const int k_Version = 4;

            // Naming convention for analytics data
            public string volume_name;
            public string scene_name;
            public string volume_profile_asset_guid;
        }

        public static void Send(Volume volume, VolumeProfile volumeProfile)
        {
            if (volume == null || volumeProfile == null || !AnalyticsUtils.TryRegisterEvent(Data.k_EventName, Data.k_Version))
                return;

            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
                return;
            
            using (GenericPool<Data>.Get(out var data))
            {
                data.volume_name = Hash128.Compute(volume.name).ToString();
                data.scene_name = AssetDatabase.AssetPathToGUID(scene.path);
                data.volume_profile_asset_guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(volumeProfile.GetInstanceID()));
                AnalyticsUtils.SendData<Data>(data, Data.k_EventName, Data.k_Version);
            }
        }
    }
}
