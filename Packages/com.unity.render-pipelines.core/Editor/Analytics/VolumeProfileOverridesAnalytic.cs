using System.Collections.Generic;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Analytics;
using System;

namespace UnityEditor.Rendering.Analytics
{
    // schema = com.unity3d.data.schemas.editor.analytics.uVolumeProfileOverridesAnalytic_v2
    // taxonomy = editor.analytics.uVolumeProfileOverridesAnalytic.v2
    internal class VolumeProfileOverridesAnalytic : IPostprocessBuildWithReport
    {
        public int callbackOrder => int.MaxValue;


        [AnalyticInfo(eventName: "uVolumeProfileOverridesAnalytic", version: 2, maxEventsPerHour:1000, vendorKey: "unity.srp")]
        public class Analytic : IAnalytic
        {
            public Analytic(string asset_guid, string type, string[] p)
            {
                m_Data = new Data
                {
                    volume_profile_asset_guid = asset_guid,
                    component_type = type,
                    overrided_parameters = p
                };
            }

            [System.Diagnostics.DebuggerDisplay("{volume_profile_asset_guid} - {component_type} - {overrided_parameters.Length}")]
            [Serializable]
            struct Data : IAnalytic.IData
            {
                // Naming convention for analytics data
                public string volume_profile_asset_guid;
                public string component_type;
                public string[] overrided_parameters;
            }
            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }
            Data m_Data;
        }

        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport _)
        {
            SendAnalytic();
        }

        private static readonly string[] k_SearchFolders = new[] { "Assets" };

        [MustUseReturnValue]
        static bool TryGatherData([NotNullWhen(true)] out List<IAnalytic> datas, [NotNullWhen(false)] out string warning)
        {
            warning = string.Empty;

            datas = new List<IAnalytic>();

            var volumeProfileGUIDs = AssetDatabase.FindAssets($"t:{nameof(VolumeProfile)} glob:\"**/*.asset\"", k_SearchFolders);
            foreach (var guid in volumeProfileGUIDs)
            {
                var volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(AssetDatabase.GUIDToAssetPath(guid));
                if (volumeProfile == null)
                    continue;

                foreach (var volumeComponent in volumeProfile.components)
                {
                    var volumeComponentType = volumeComponent.GetType();
                    var defaultVolumeComponent = (VolumeComponent) ScriptableObject.CreateInstance(volumeComponentType);
                    var overrideParameters = volumeComponent.ToNestedColumnWithDefault(defaultVolumeComponent, true);
                    if (overrideParameters.Length == 0)
                        continue;
                    datas.Add(new Analytic(guid, volumeComponent.GetType().Name, overrideParameters));
                }
            }
            return true;
        }

        [MenuItem("internal:Edit/Rendering/Analytics/Send VolumeProfileOverridesAnalytic ", priority = 1)]
        static void SendAnalytic()
        {
            if (!TryGatherData(out var data, out var warning))
                Debug.Log(warning);

            data.ForEach(d => AnalyticsUtils.SendData(d));
        }
    }

}
