using System;
using System.Collections.Generic;
using UnityEngine.Analytics;
#if ENABLE_UPSCALER_FRAMEWORK
using UnityEngine.Rendering;
#endif

namespace UnityEditor.Rendering.Analytics
{
    /// <summary>
    /// Represents quality information for upscaler analytics, supporting both string and float values.
    /// </summary>
    struct UpscalerQualityInfo
    {
        private readonly string m_Value;

        private UpscalerQualityInfo(string value)
        {
            m_Value = value;
        }

        /// <summary>
        /// Creates a quality info from a string value (e.g., "Quality", "Performance").
        /// </summary>
        internal static UpscalerQualityInfo FromString(string value) =>
            new UpscalerQualityInfo(value ?? string.Empty);

        /// <summary>
        /// Creates a quality info from a float value (e.g., render scale, resolution percentage).
        /// </summary>
        internal static UpscalerQualityInfo FromFloat(float value) =>
            new UpscalerQualityInfo(value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

        /// <summary>
        /// Returns the formatted quality info string.
        /// </summary>
        internal string Format() => m_Value;
    }

    internal class UpscalerBuildConfiguredAnalytic
    {
        [AnalyticInfo(eventName: "upscaler_BuildConfigured", version: 2, vendorKey: "unity.srp", maxEventsPerHour: 1000, maxNumberOfElements: 1000)]
        internal class Analytic : IAnalytic
        {
            IAnalytic.IData m_Data;

            public Analytic(IAnalytic.IData data)
            {
                m_Data = data;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return data != null;
            }
        }

        [Serializable]
        internal struct UpscalerConfiguredData : IAnalytic.IData
        {
            public string build_target;
            public string renderPipeline;
            public string[] upscalerPriorityList;
            public string build_guid;
        }

        public static void SendAnalytic(UpscalerConfiguredData data)
        {
            if (!EditorAnalytics.enabled)
                return;

            Analytic analytic = new Analytic(data);
            EditorAnalytics.SendAnalytic(analytic);
        }

        /// <summary>
        /// Formats a list of (upscalerName, qualityInfo) pairs into the analytics string format.
        /// </summary>
        /// <param name="upscalerInfoPairs">List of tuples containing upscaler name and quality info.</param>
        /// <returns>Array of formatted strings in "upscalerName:qualityInfo" format.</returns>
        public static string[] FormatUpscalerPriorityList(List<(string name, UpscalerQualityInfo qualityInfo)> upscalerInfoPairs)
        {
            if (upscalerInfoPairs == null || upscalerInfoPairs.Count == 0)
                return Array.Empty<string>();

            var formattedList = new string[upscalerInfoPairs.Count];
            for (int i = 0; i < upscalerInfoPairs.Count; i++)
            {
                var (name, qualityInfo) = upscalerInfoPairs[i];
                formattedList[i] = $"{name}:{qualityInfo.Format()}";
            }

            return formattedList;
        }

#if ENABLE_UPSCALER_FRAMEWORK
        /// <summary>
        /// Tries to get the quality info for known upscaler options (DLSS, FSR2).
        /// </summary>
        /// <param name="options">The upscaler options to inspect.</param>
        /// <param name="qualityInfo">The resulting quality info if found.</param>
        /// <returns>True if quality info was found for a known upscaler in QualityMode, false otherwise.</returns>
        public static bool TryGetQualityInfo(UpscalerOptions options, out UpscalerQualityInfo qualityInfo)
        {
            if (options == null || options.resolutionMode != UpscalerResolutionMode.QualityMode)
            {
                qualityInfo = default;
                return false;
            }

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            if (options is DLSSOptions dlss)
            {
                qualityInfo = UpscalerQualityInfo.FromString(dlss.dlssQualityMode.ToString());
                return true;
            }
#endif

#if ENABLE_AMD && ENABLE_AMD_MODULE
            if (options is FSR2Options fsr2)
            {
                qualityInfo = UpscalerQualityInfo.FromString(fsr2.fsr2QualityMode.ToString());
                return true;
            }
#endif

            qualityInfo = default;
            return false;
        }
#endif

        /// <summary>
        /// Collects and sends upscaler analytics for all render pipeline assets in the build.
        /// </summary>
        /// <typeparam name="TAsset">The render pipeline asset type.</typeparam>
        /// <param name="buildGuid">The build GUID from BuildReport.</param>
        /// <param name="renderPipelineAssets">List of render pipeline assets to collect analytics from.</param>
        /// <param name="createAnalyticsData">Function to create analytics data for each asset.</param>
        public static void SendEvent<TAsset>(
            string buildGuid,
            List<TAsset> renderPipelineAssets,
            Func<string, TAsset, UpscalerConfiguredData> createAnalyticsData)
            where TAsset : UnityEngine.Rendering.RenderPipelineAsset
        {
            if (renderPipelineAssets == null)
                return;

            foreach (var asset in renderPipelineAssets)
            {
                var data = createAnalyticsData(buildGuid, asset);
                if (!string.IsNullOrEmpty(data.renderPipeline))
                {
                    SendAnalytic(data);
                }
            }
        }
    }
}
