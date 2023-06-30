using System;
using UnityEngine.Analytics;
using UnityEngine.Rendering;
using static UnityEditor.VFX.VFXAnalytics;

namespace UnityEditor.Rendering.HighDefinition.Analytics
{
    // schema = com.unity3d.data.schemas.editor.analytics.uAssetReimporterAnalytic_v2
    // taxonomy = editor.analytics.uAssetReimporterAnalytic.v2
    internal class AssetReimporterAnalytic
    {
        const int k_MaxEventsPerHour = 100;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.srp";


        [System.Diagnostics.DebuggerDisplay("{duration} - {asset_type} - {num_assets}")]
        [Serializable]
        internal class Data : IAnalytic.IData
        {
            internal const string k_EventName = "uAssetReimporterAnalytic";
            internal const int k_Version = 2;

            // Naming convention for analytics data
            public uint num_assets;
            public double duration;
            public string asset_type;
        }

        [AnalyticInfo(eventName: "uAssetReimporterAnalytic", vendorKey: "unity.srp", maxEventsPerHour: 100, maxNumberOfElements: 1000, version:2)]
        internal class Analytic : IAnalytic
        {
            public Analytic(Data data) { m_Data = data; }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }

            Data m_Data;
        };
        public static void Send<T>(double duration, uint numberOfAssets)
        {
            if (!EditorAnalytics.enabled)
                return;

            using (GenericPool<Data>.Get(out var data))
            {
                data.duration = duration;
                data.num_assets = numberOfAssets;
                data.asset_type = typeof(T).ToString();
                Analytic analytic = new Analytic(data);
                EditorAnalytics.SendAnalytic(analytic);
            }
        }
    }
}
