using System;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Experimental.Rendering.LWRP;

namespace UnityEditor.Experimental.Rendering.LWRP.Analytics
{
    struct AnalyticsDataTypes
    {
        public const string k_LightDataString = "u2drendererlights";
        public const string k_2DRendererDataString = "u2drendererdata";
    }

    internal interface IAnalyticsData { };

    [Serializable]
    internal struct Light2DData : IAnalyticsData
    {
        public enum EventType
        {
            Created,
            Modified
        }

        public EventType event_type;
        public int instance_id;
        public Light2D.LightType light_type;
    };


    [Serializable]
    internal struct RendererAssetData : IAnalyticsData
    {
        public enum EventType
        {
            Created,
            Modified
        }

        public EventType event_type;
        public int instance_id;
        public int blending_layers_count;
        public int blending_modes_used;
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

            if(data is Light2DData)
            {
                Light2DData lightData = (Light2DData)data;
                if(lightData.event_type == Light2DData.EventType.Created)
                {
                    Debug.Log("Light Created Type = " + lightData.light_type);
                }
                else
                {
                    Debug.Log("Light Modified Type = " + lightData.light_type);
                }
                
            }
            else if(data is RendererAssetData)
            {
                RendererAssetData rendererAssetData = (RendererAssetData)data;
                if (rendererAssetData.event_type == RendererAssetData.EventType.Created)
                {
                    Debug.Log("Renderer Assest Data Created - Blending Layer Count:" + rendererAssetData.blending_layers_count + " Blending Layer Used:" + rendererAssetData.blending_modes_used);
                }
                else
                {
                    Debug.Log("Renderer Assest Data Modified - Blending Layer Count:" + rendererAssetData.blending_layers_count + " Blending Layer Used:" + rendererAssetData.blending_modes_used);
                }
            }

            //return EditorAnalytics.SendEventWithLimit(eventString, data, k_Version);
            
            return AnalyticsResult.Ok;
        }
    }
}
