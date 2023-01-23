using System.Collections.Generic;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Analytics
{
    // schema = com.unity3d.data.schemas.editor.analytics.uVolumeProfileOverridesAnalytic_v2
    // taxonomy = editor.analytics.uVolumeProfileOverridesAnalytic.v2
    internal class VolumeProfileOverridesAnalytic : IPostprocessBuildWithReport
    {
        public int callbackOrder => int.MaxValue;

        [System.Diagnostics.DebuggerDisplay("{volume_profile_asset_guid} - {component_type} - {overrided_parameters.Length}")]
        struct Data
        {
            internal const string k_EventName = "uVolumeProfileOverridesAnalytic";
            internal const int k_Version = 2;

            // Naming convention for analytics data
            public string volume_profile_asset_guid;
            public string component_type;
            public string[] overrided_parameters;
        }

        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport _)
        {
            SendAnalytic();
        }

        private static readonly string[] k_SearchFolders = new[] { "Assets" };

        [MustUseReturnValue]
        static bool TryGatherData([NotNullWhen(true)] out List<Data> datas, [NotNullWhen(false)] out string warning)
        {
            warning = string.Empty;

            datas = new List<Data>();

            var volumeProfileGUIDs = AssetDatabase.FindAssets($"t:{nameof(VolumeProfile)} glob:\"**/*.asset\"", k_SearchFolders);
            foreach (var guid in volumeProfileGUIDs)
            {
                var volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(AssetDatabase.GUIDToAssetPath(guid));
                if (volumeProfile == null)
                    continue;

                foreach (var volumeComponent in volumeProfile.components)
                {
                    var volumeComponentType = volumeComponent.GetType();
                    var overrideParameters =
                        volumeComponent.ToNestedColumnWithDefault(VolumeManager.instance.GetDefaultVolumeComponent(volumeComponentType),
                            true);
                    if (overrideParameters.Length == 0)
                        continue;
                    datas.Add(new Data()
                    {
                        volume_profile_asset_guid = guid,
                        component_type = volumeComponent.GetType().Name,
                        overrided_parameters = overrideParameters
                    });
                }
            }

            return true;
        }

        [MenuItem("internal:Edit/Rendering/Analytics/Send VolumeProfileOverridesAnalytic ", priority = 1)]
        static void SendAnalytic()
        {
            if(!AnalyticsUtils.TryRegisterEvent(Data.k_EventName, Data.k_Version, maxEventPerHour: 1000))
                return;

            if (!TryGatherData(out var data, out var warning))
                Debug.Log(warning);

            data.ForEach(d => AnalyticsUtils.SendData(d, Data.k_EventName, Data.k_Version));
        }
    }

}
