using System;
using System.Linq;
using UnityEngine;
namespace UnityEditor.Experimental.Rendering
{
    static class LightweightRenderPipelineEditorUtils
    {
        public static void DrawCascadeSplitGUI<T>(ref SerializedProperty shadowCascadeSplit, float distance)
        {
            float[] cascadePartitionSizes = null;
            Type type = typeof(T);
            if (type == typeof(float))
            {
                cascadePartitionSizes = new float[] { shadowCascadeSplit.floatValue };
            }
            else if (type == typeof(Vector3))
            {
                Vector3 splits = shadowCascadeSplit.vector3Value;
                cascadePartitionSizes = new float[]
                {
                    Mathf.Clamp(splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[1] - splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[2] - splits[1], 0.0f, 1.0f)
                };
            }
            if (cascadePartitionSizes != null)
            {
                EditorGUI.BeginChangeCheck();
                ShadowCascadeSplitGUI.HandleCascadeSliderGUI(ref cascadePartitionSizes);

                // Checking changes to update slider and fields accordingly
                if (EditorGUI.EndChangeCheck())
                {
                    if (type == typeof(float))
                        shadowCascadeSplit.floatValue = cascadePartitionSizes[0];
                    else
                    {
                        Vector3 updatedValue = new Vector3();
                        updatedValue[0] = cascadePartitionSizes[0];
                        updatedValue[1] = updatedValue[0] + cascadePartitionSizes[1];
                        updatedValue[2] = updatedValue[1] + cascadePartitionSizes[2];
                        shadowCascadeSplit.vector3Value = updatedValue;
                    }
                }

                // Float fields for adding values
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Splits");

                if (type == typeof(float))
                {
                    var value = shadowCascadeSplit.floatValue;
                    EditorGUI.BeginChangeCheck();
                    var meterValue = EditorGUILayout.DelayedFloatField((float)Math.Round(value * distance, 2), GUILayout.Width(70f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        var posMeter = Mathf.Clamp(meterValue, 0.01f, distance);
                        float percValue = posMeter / distance;
                        shadowCascadeSplit.floatValue = percValue;
                    }
                }
                else if (type == typeof(Vector3))
                {
                    for (int i = 0; i < cascadePartitionSizes.Length; ++i)
                    {
                        var vec3value = shadowCascadeSplit.vector3Value;
                        var threshold = 0.1f/distance;
                        if (i != 0)
                        {
                            GUILayout.FlexibleSpace();
                        }

                        EditorGUI.BeginChangeCheck();
                        var meterValue = EditorGUILayout.DelayedFloatField((float)Math.Round(vec3value[i] * distance, 2), GUILayout.Width(70f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            var posMeter = Mathf.Clamp(meterValue, 0.01f, distance);
                            float percValue = posMeter / distance;
                            if (i < cascadePartitionSizes.Length-1)
                            {
                                percValue = Math.Min((percValue), (vec3value[i+1]-threshold) );
                            }

                            if (i != 0)
                            {
                                percValue = Math.Max((percValue), (vec3value[i-1]+threshold) );
                            }

                            vec3value[i] = percValue;
                            shadowCascadeSplit.vector3Value = vec3value;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
