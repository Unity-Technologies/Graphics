#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine.Rendering
{
    internal static partial class RenderPipelineGraphicsSettingsStripper
    {
        private static bool CanRemoveSettings(this List<IStripper> strippers, Type settingsType, IRenderPipelineGraphicsSettings settings)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            var canRemoveSettings = !settings.isAvailableInPlayerBuild;

            object[] methodArgs = { Convert.ChangeType(settings, settingsType) };

            foreach (var stripperInstance in strippers)
            {
                var methodInfo = stripperInstance.GetType().GetMethod($"{nameof(IRenderPipelineGraphicsSettingsStripper<IRenderPipelineGraphicsSettings>.CanRemoveSettings)}", flags);
                if (methodInfo != null)
                    canRemoveSettings |= (bool)methodInfo.Invoke(stripperInstance, methodArgs);
            }

            return canRemoveSettings;
        }

        public static void PerformStripping(
            List<IRenderPipelineGraphicsSettings> settingsList,
            List<IRenderPipelineGraphicsSettings> runtimeSettingsList)
        {
            if (settingsList == null)
                throw new ArgumentNullException(nameof(settingsList));

            if (runtimeSettingsList == null)
                throw new ArgumentNullException(nameof(runtimeSettingsList));

            runtimeSettingsList.Clear();

            var strippersMap = Fetcher.ComputeStrippersMap();
            foreach (var settings in settingsList)
            {
                var settingsType = settings.GetType();

                if (strippersMap.TryGetValue(settingsType, out var strippers))
                {
                    if (!strippers.CanRemoveSettings(settingsType, settings))
                        runtimeSettingsList.Add(settings);
                }
                else
                {
                    if (settings.isAvailableInPlayerBuild)
                        runtimeSettingsList.Add(settings);
                }
            }
        }
    }
}
#endif
