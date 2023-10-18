using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Analytics
{
    // schema = com.unity3d.data.schemas.editor.analytics.uVolumeProfileUsageAnalytic_v4
    // taxonomy = editor.analytics.uVolumeProfileUsageAnalytic.v4
    internal class VolumeProfileUsageAnalytic
    {
        [AnalyticInfo(eventName: "uVolumeProfileUsageAnalytic", version: 4, vendorKey: "unity.srp" )]
        class Analytic : IAnalytic
        {
            public Analytic(Volume volume, VolumeProfile volumeProfile)
            {
                using (GenericPool<Data>.Get(out var data))
                {
                    data.volume_name = Hash128.Compute(volume.name).ToString();
                    data.scene_name = EditorSceneManager.GetActiveScene().GetGUID();
                    data.volume_profile_asset_guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(volumeProfile.GetInstanceID()));
                    m_Data = data;
                }
            }

            [System.Diagnostics.DebuggerDisplay("{volume_name} - {scene_name}- {volume_profile_asset_guid}")]
            [Serializable]
            class Data : IAnalytic.IData
            {
                // Naming convention for analytics data
                public string volume_name;
                public string scene_name;
                public string volume_profile_asset_guid;
            }
            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }
            Data m_Data;
        };

        public static void Send(Volume volume, VolumeProfile volumeProfile)
        {
            if (volume == null || volumeProfile == null)
                return;

            Analytic analytic = new Analytic(volume, volumeProfile);

            AnalyticsUtils.SendData(analytic);
        }
    }
}
