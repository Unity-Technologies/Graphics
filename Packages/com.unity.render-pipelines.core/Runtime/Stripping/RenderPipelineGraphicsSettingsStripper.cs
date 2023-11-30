#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace UnityEngine.Rendering
{
    internal static partial class RenderPipelineGraphicsSettingsStripper
    {
        private static bool CanRemoveSettings(this List<IStripper> strippers, [DisallowNull] Type settingsType, [DisallowNull] IRenderPipelineGraphicsSettings settings)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            var canRemoveSettings = true;

            object[] methodArgs = { Convert.ChangeType(settings, settingsType) };

            foreach (var stripperInstance in strippers)
            {
                var methodInfo = stripperInstance.GetType().GetMethod($"{nameof(IRenderPipelineGraphicsSettingsStripper<IRenderPipelineGraphicsSettings>.CanRemoveSettings)}", flags);
                if (methodInfo != null)
                    canRemoveSettings &= (bool)methodInfo.Invoke(stripperInstance, methodArgs);
            }

            return canRemoveSettings;
        }

        private static bool CanTransferSettingsToPlayer(
            [DisallowNull] Dictionary<Type, List<IStripper>> strippersMap,
            [DisallowNull] IRenderPipelineGraphicsSettings settings,
            out bool isAvailableOnPlayerBuild,
            out bool strippersDefined)
        {
            isAvailableOnPlayerBuild = false;
            strippersDefined = false;

            var settingsType = settings.GetType();

            if (strippersMap.TryGetValue(settingsType, out var strippers))
            {
                if (!strippers.CanRemoveSettings(settingsType, settings))
                    isAvailableOnPlayerBuild = true;

                strippersDefined = true;
            }
            else
            {
                if (settings.isAvailableInPlayerBuild)
                    isAvailableOnPlayerBuild = true;
            }

            return isAvailableOnPlayerBuild;
        }

        public static void PerformStripping(
            List<IRenderPipelineGraphicsSettings> settingsList,
            List<IRenderPipelineGraphicsSettings> runtimeSettingsList)
        {
            if (settingsList == null)
                throw new ArgumentNullException(nameof(settingsList));

            if (runtimeSettingsList == null)
                throw new ArgumentNullException(nameof(runtimeSettingsList));

            using (var report = new Report())
            {
                runtimeSettingsList.Clear();

                var strippersMap = Fetcher.ComputeStrippersMap();
                for (int i = 0; i < settingsList.Count; ++i)
                {
                    var settings = settingsList[i];
                    if (settings == null)
                        continue;

                    if (CanTransferSettingsToPlayer(strippersMap, settings, out var isAvailableOnPlayerBuild, out var strippersDefined))
                        runtimeSettingsList.Add(settings);

                    report.AddStrippedSetting(settings.GetType(), isAvailableOnPlayerBuild, strippersDefined);
                }
            }
            
        }
    }
}
#endif
