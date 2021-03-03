using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace UnityEditor.Rendering.Universal.Internal
{
    /// <summary>
    /// Contains a database of built-in resource GUIds. These are used to load built-in resource files.
    /// </summary>
    public static class ResourceGuid
    {
        /// <summary>
        /// GUId for the <c>ScriptableRendererFeature</c> template file.
        /// </summary>
        public static readonly string rendererTemplate = "51493ed8d97d3c24b94c6cffe834630b";
    }
}

namespace UnityEditor.Rendering.Universal
{
    static partial class EditorUtils
    {
        // Each group is separate in the menu by a menu bar
        public const int lwrpAssetCreateMenuPriorityGroup1 = CoreUtils.assetCreateMenuPriority1;
        public const int lwrpAssetCreateMenuPriorityGroup2 = CoreUtils.assetCreateMenuPriority1 + 50;
        public const int lwrpAssetCreateMenuPriorityGroup3 = lwrpAssetCreateMenuPriorityGroup2 + 50;

        internal enum Unit { Metric, Percent }

        internal class Styles
        {
            //Measurements
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        static float[] splitZero = new float[0];
        static float[] splitOne = new float[1];
        static float[] splitTwo = new float[2];
        static float[] splitThree = new float[3];
        static int splitCount;
        public static void DrawCascadeSplitGUI<T>(ref SerializedProperty shadowCascadeSplit, float distance, int cascadeCount, Unit unit)
        {
            if (cascadeCount <= 0)
            {
                throw new ArgumentException($"Cascade value ({cascadeCount}) needs to be positive.");
            }

            splitCount = cascadeCount - 1;
            if (splitCount == 0)
            {
                ShadowCascadeSplitGUI.HandleCascadeSliderGUI(ref splitZero, distance, unit);
                return;
            }

            Type type = typeof(T);
            if (type == typeof(float))
            {
                DrawFloatGUI(ref shadowCascadeSplit, distance, unit);
            }
            else if (type == typeof(Vector2))
            {
                DrawVector2GUI(ref shadowCascadeSplit, distance, unit);
            }
            else if (type == typeof(Vector3))
            {
                DrawVector3GUI(ref shadowCascadeSplit, distance, unit);
            }
        }

        private static void DrawFloatGUI(ref SerializedProperty shadowCascadeSplit, float distance, Unit unit)
        {
            splitOne[0] = shadowCascadeSplit.floatValue;
            var value = shadowCascadeSplit.floatValue;
            float unitValue = 0f;
            EditorGUI.BeginChangeCheck();
            if (unit == Unit.Metric)
            {
                unitValue = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent($"Split {1}", ""), (float)Math.Round(value * distance, 2), 0f, distance, null);
            }
            else if (unit == Unit.Percent)
            {
                var posPerc = Mathf.Clamp(value, 0.01f, distance) * 100f;
                var percValue = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent($"Split {1}", ""), (float)Math.Round(posPerc, 2), 0f, 100, null);
                unitValue = percValue / 100;
            }

            if (EditorGUI.EndChangeCheck())
            {
                float percValue = 0f;
                if (unit == Unit.Metric)
                {
                    var posMeter = Mathf.Clamp(unitValue, 0.01f, distance);
                    percValue = posMeter / distance;
                }
                else if (unit == Unit.Percent)
                {
                    percValue = unitValue;
                }

                shadowCascadeSplit.floatValue = percValue;
            }

            EditorGUI.BeginChangeCheck();
            ShadowCascadeSplitGUI.HandleCascadeSliderGUI(ref splitOne, distance, unit);
            if (EditorGUI.EndChangeCheck())
            {
                shadowCascadeSplit.floatValue = splitOne[0];
            }
        }

        private static void DrawVector2GUI(ref SerializedProperty shadowCascadeSplit, float distance, Unit unit)
        {
            Vector2 splits = shadowCascadeSplit.vector2Value;
            splitTwo[0] = Mathf.Clamp(splits[0], 0.0f, 1.0f);
            splitTwo[1] = Mathf.Clamp(splits[1] - splits[0], 0.0f, 1.0f);

            for (int i = 0; i < splitCount; ++i)
            {
                var vec2value = shadowCascadeSplit.vector2Value;
                var threshold = 0.1f / distance;
                float unitValue = 0f;

                EditorGUI.BeginChangeCheck();
                if (unit == Unit.Metric)
                {
                    unitValue = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent($"Split {i + 1}", ""), (float)Math.Round(vec2value[i] * distance, 2), 0f, distance, null);
                }
                else if (unit == Unit.Percent)
                {
                    var posPerc = Mathf.Clamp(vec2value[i], 0.01f, distance) * 100f;
                    var percValue = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent($"Split {i + 1}", ""), (float)Math.Round(posPerc, 2), 0f, 100, null);
                    unitValue = percValue / 100f;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    float percValue = 0f;
                    if (unit == Unit.Metric)
                    {
                        var posMeter = Mathf.Clamp(unitValue, 0.01f, distance);
                        percValue = posMeter / distance;
                    }
                    else if (unit == Unit.Percent)
                    {
                        percValue = unitValue;
                    }

                    if (i < splitCount - 1)
                    {
                        percValue = Math.Min((percValue), (vec2value[i + 1] - threshold));
                    }

                    if (i != 0)
                    {
                        percValue = Math.Max((percValue), (vec2value[i - 1] + threshold));
                    }

                    vec2value[i] = percValue;
                    shadowCascadeSplit.vector2Value = vec2value;
                }
            }

            EditorGUI.BeginChangeCheck();
            ShadowCascadeSplitGUI.HandleCascadeSliderGUI(ref splitTwo, distance, unit);
            if (EditorGUI.EndChangeCheck())
            {
                Vector2 updatedValue = new Vector2();
                updatedValue[0] = splitTwo[0];
                updatedValue[1] = updatedValue[0] + splitTwo[1];
                shadowCascadeSplit.vector2Value = updatedValue;
            }
        }

        private static void DrawVector3GUI(ref SerializedProperty shadowCascadeSplit, float distance, Unit unit)
        {
            Vector3 splits = shadowCascadeSplit.vector3Value;
            splitThree[0] = Mathf.Clamp(splits[0], 0.0f, 1.0f);
            splitThree[1] = Mathf.Clamp(splits[1] - splits[0], 0.0f, 1.0f);
            splitThree[2] = Mathf.Clamp(splits[2] - splits[1], 0.0f, 1.0f);

            for (int i = 0; i < splitCount; ++i)
            {
                var vec3value = shadowCascadeSplit.vector3Value;
                var threshold = 0.1f / distance;
                float unitValue = 0f;
                EditorGUI.BeginChangeCheck();
                if (unit == Unit.Metric)
                {
                    unitValue = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent($"Split {i + 1}", ""), (float)Math.Round(vec3value[i] * distance, 2), 0f, distance, null);
                }
                else if (unit == Unit.Percent)
                {
                    var posPerc = Mathf.Clamp(vec3value[i], 0.01f, distance) * 100f;
                    var percValue = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent($"Split {i + 1}", ""), (float)Math.Round(posPerc, 2), 0f, 100, null);
                    unitValue = percValue / 100f;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    float percValue = 0f;
                    if (unit == Unit.Metric)
                    {
                        var posMeter = Mathf.Clamp(unitValue, 0.01f, distance);
                        percValue = posMeter / distance;
                    }
                    else if (unit == Unit.Percent)
                    {
                        percValue = unitValue;
                    }

                    if (i < splitCount - 1)
                    {
                        percValue = Math.Min((percValue), (vec3value[i + 1] - threshold));
                    }

                    if (i != 0)
                    {
                        percValue = Math.Max((percValue), (vec3value[i - 1] + threshold));
                    }

                    vec3value[i] = percValue;
                    shadowCascadeSplit.vector3Value = vec3value;
                }
            }

            EditorGUI.BeginChangeCheck();
            ShadowCascadeSplitGUI.HandleCascadeSliderGUI(ref splitThree, distance, unit);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 updatedValue = new Vector3();
                updatedValue[0] = splitThree[0];
                updatedValue[1] = updatedValue[0] + splitThree[1];
                updatedValue[2] = updatedValue[1] + splitThree[2];
                shadowCascadeSplit.vector3Value = updatedValue;
            }
        }
    }
}
