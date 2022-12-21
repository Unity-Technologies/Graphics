using UnityEngine.Analytics;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal.Analytics
{
    // schema = com.unity3d.data.schemas.editor.analytics.uAssetReimporterAnalytic_v2
    // taxonomy = editor.analytics.uAssetReimporterAnalytic.v2
    internal class AssetReimporterAnalytic
    {
        const int k_MaxEventsPerHour = 100;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.srp";

        [System.Diagnostics.DebuggerDisplay("{duration} - {asset_type} - {num_assets}")]
        class Data
        {
            internal const string k_EventName = "uAssetReimporterAnalytic";
            internal const int k_Version = 2;

            // Naming convention for analytics data
            public uint num_assets;
            public double duration;
            public string asset_type;
        }

        public static void Send<T>(double duration, uint numberOfAssets)
        {
            if (!EditorAnalytics.enabled || EditorAnalytics.RegisterEventWithLimit(Data.k_EventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey, Data.k_Version) != AnalyticsResult.Ok)
                return;

            using (GenericPool<Data>.Get(out var data))
            {
                data.duration = duration;
                data.num_assets = numberOfAssets;
                data.asset_type = typeof(T).ToString();
                EditorAnalytics.SendEventWithLimit(Data.k_EventName, data, Data.k_Version);
            }
        }
    }
}
