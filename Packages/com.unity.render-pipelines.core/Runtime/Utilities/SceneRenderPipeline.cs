#if UNITY_EDITOR
using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;
using UnityEngine.SceneManagement;
#endif //UNITY_EDITOR

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Setup a specific render pipeline on scene loading.
    /// This need to be used with caution as it will change project configuration.
    /// </summary>
#if UNITY_EDITOR
    [ExecuteAlways]
#endif //UNITY_EDITOR
    public class SceneRenderPipeline : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] bool firstTimeCreated = true;
    
        /// <summary>
        /// Scriptable Render Pipeline Asset to setup on scene load.
        /// </summary>
        public RenderPipelineAsset renderPipelineAsset;

        void Awake()
        {
            if (firstTimeCreated)
            {
                renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
                firstTimeCreated = false;
            }
            
#if ENABLE_CLOUD_SERVICES_ANALYTICS
            //Send analytics each time to find usage in content dl on the asset store too
            SceneRenderPipelineAnalytic.Send(this);
#endif
        }

        void OnEnable()
        {
            GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
        }

#if ENABLE_CLOUD_SERVICES_ANALYTICS
        static class SceneRenderPipelineAnalytic
        {
            const int k_MaxEventsPerHour = 100;
            const int k_MaxNumberOfElements = 1000;
            const string k_VendorKey = "unity.srp";

            [System.Diagnostics.DebuggerDisplay("{scene_guid}")]
            internal struct Data
            {
                internal const string k_EventName = "sceneRenderPipelineAssignment";

                // Naming convention for analytics data
                public string scene_guid;
            };

            internal static void Send(SceneRenderPipeline sender)
            {
                if (!EditorAnalytics.enabled || !EditorAnalyticsExtensions.RegisterEventWithLimit(Data.k_EventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey))
                    return;

                var data = new Data() { scene_guid = SceneExtensions.GetGUID(sender.gameObject.scene) };
                EditorAnalyticsExtensions.SendEventWithLimit(Data.k_EventName, data);
            }

            //bellow is missing API specific for 2022.2
            internal static class SceneExtensions
            {
                static PropertyInfo s_SceneGUID = typeof(Scene).GetProperty("guid", BindingFlags.NonPublic | BindingFlags.Instance);
                public static string GetGUID(Scene scene)
                {
                    Debug.Assert(s_SceneGUID != null, "Reflection for scene GUID failed");
                    return (string)s_SceneGUID.GetValue(scene);
                }
            }
            internal static class EditorAnalyticsExtensions
            {
                //All of this is just to bypass UnityEngine.Analytics module not being
                //loaded in some test project in 2022. It is loaded in 2023.1+ though.
                //EditorAnalytics.RegisterEventWithLimit method is thus ill-defined on those project as
                //it returns the missing type. Lets just produce a casted to int similar method.
                static Func<string, int, int, string, int> s_RegisterEventWithLimit;
                static Func<string, object, int> s_SendEventWithLimit;
                
                public static bool RegisterEventWithLimit(string eventName, int maxEventPerHour, int maxItems, string vendorKey) 
                    => s_RegisterEventWithLimit?.Invoke(eventName, maxEventPerHour, maxItems, vendorKey) == 0 /*UnityEngine.Analytics.AnalyticsResult.Ok*/;

                public static bool SendEventWithLimit(string eventName, object parameters) 
                    => s_SendEventWithLimit?.Invoke(eventName, parameters) == 0 /*UnityEngine.Analytics.AnalyticsResult.Ok*/;

                static EditorAnalyticsExtensions()
                {
                    Type unityEditorType = typeof(UnityEditor.EditorAnalytics);
                    MethodInfo registerEventWithLimitMethodInfo = unityEditorType.GetMethod(
                        "RegisterEventWithLimit", 
                        BindingFlags.Static | BindingFlags.Public, 
                        null,
                        new Type[] { typeof(string), typeof(int), typeof(int), typeof(string) }, 
                        null);
                    MethodInfo sendEventWithLimitMethodInfo = unityEditorType.GetMethod(
                        "SendEventWithLimit", 
                        BindingFlags.Static | BindingFlags.Public, 
                        null,
                        new Type[] { typeof(string), typeof(object) }, 
                        null);

                    var eventNameParameter = Expression.Parameter(typeof(string), "eventName");
                    var maxEventPerHourParameter = Expression.Parameter(typeof(int), "maxEventPerHour");
                    var maxItemsParameter = Expression.Parameter(typeof(int), "maxItems");
                    var vendorKeyParameter = Expression.Parameter(typeof(string), "vendorKey");
                    var parametersParameter = Expression.Parameter(typeof(object), "parameters");

                    var registerEventWithLimitLambda = Expression.Lambda<Func<string, int, int, string, int>>(
                        Expression.Convert(
                            Expression.Call(registerEventWithLimitMethodInfo, eventNameParameter, maxEventPerHourParameter, maxItemsParameter, vendorKeyParameter),
                            typeof(int)), 
                        eventNameParameter, maxEventPerHourParameter, maxItemsParameter, vendorKeyParameter);
                    var sendEventWithLimitLambda = Expression.Lambda<Func<string, object, int>>(
                        Expression.Convert(
                            Expression.Call(sendEventWithLimitMethodInfo, eventNameParameter, parametersParameter),
                            typeof(int)), 
                        eventNameParameter, parametersParameter);
                    s_RegisterEventWithLimit = registerEventWithLimitLambda.Compile();
                    s_SendEventWithLimit = sendEventWithLimitLambda.Compile();
                }
            }
        }
#endif //ENABLE_CLOUD_SERVICES_ANALYTICS
#endif //UNITY_EDITOR
    }
}
