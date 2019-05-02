using System;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Experimental.Rendering.LWRP;

namespace UnityEditor.Experimental.Rendering.LWRP.Analytics
{
    struct AnalyticsDataTypes
    {
        public const string k_LightCreatedString = "u2drendererlightsadded";
        public const string k_LightModifiedString = "u2drendererlightsmodified";
        public const string k_2DRendererDataCreatedString = "u2drendererdatacreated";
        public const string k_2DRendererDataModifiedString = "u2drendererdatacreated";
        public const string k_LightOperationModifiedString = "u2drendererblendstyleselected";
    }


    internal interface IAnalyticsData { };

    [Serializable]
    internal struct Light2DAddedData : IAnalyticsData
    {
        public int instance_id;
        public Light2D.LightType type;
    };

    [Serializable]
    internal struct Light2DModifiedData : IAnalyticsData
    {
        public int instance_id;
        public Light2D.LightType type;
    }

    [Serializable]
    internal struct RendererDataCreatedData : IAnalyticsData
    {
        public int instance_id;
    }
    
    [Serializable]
    internal struct RendererModifiedData : IAnalyticsData
    {
        public int instance_id;
        public int number_of_blending_layers_enabled;
        //public int 
    }

    interface IAnalytics
    {
        AnalyticsResult SendData(string eventString, IAnalyticsData data);
    }

    [UnityEditor.InitializeOnLoad]
    internal class Analytics : IAnalytics
    {
        const int k_MaxEventsPerHour = 100;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "Unity.RenderPipelines.Lightweight.Editor";
        const int k_Version = 1;
        static Analytics m_Instance = new Analytics();
        public static Analytics instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new Analytics();

                return m_Instance;
            }
        }

        private Analytics()
        {
            //EditorAnalytics.RegisterEventWithLimit(k_LightAddedEventString, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey, k_Version);
        }

        public AnalyticsResult SendData(string eventString, IAnalyticsData data)
        {
            //return Edito1rAnalytics.SendEventWithLimit(eventString, data, k_Version);
            Debug.Log("Light Modified");
            return AnalyticsResult.Ok;
        }
    }
}
