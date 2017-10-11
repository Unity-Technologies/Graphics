using System;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.MaterialGraph;
using UnityEngine;

namespace UnityEditor.MaterialGraph.Drawing
{
    class Vector1ControlPresenter : GraphControlPresenter
    {
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var tNode = node as Vector1Node;
            if (tNode == null)
                return;


            tNode.value = EditorGUILayout.FloatField("Value:", tNode.value);
            /*
            tNode.floatType = (FloatPropertyChunk.FloatType)EditorGUILayout.EnumPopup("Float", tNode.floatType);


            Vector3 ranges = tNode.rangeValues;

            switch (tNode.floatType)
            {
                case FloatPropertyChunk.FloatType.Float:
                    tNode.floatType = FloatPropertyChunk.FloatType.Float;
                    tNode.value = EditorGUILayout.FloatField("Value:", tNode.value);
                    break;
                case FloatPropertyChunk.FloatType.Range:
                    tNode.floatType = FloatPropertyChunk.FloatType.Range;
                    tNode.value = EditorGUILayout.Slider(tNode.value, tNode.rangeValues.x, tNode.rangeValues.y);
                    EditorGUILayout.BeginHorizontal();
                    ranges.x = EditorGUILayout.FloatField(ranges.x);
                    //very dirty...
                    for (int i = 0; i < 15; i++)
                    {
                        EditorGUILayout.Space();
                    }
                    ranges.y = EditorGUILayout.FloatField(ranges.y);
                    EditorGUILayout.EndHorizontal();
                    tNode.rangeValues = ranges;
                    break;
                case FloatPropertyChunk.FloatType.PowerSlider:
                    tNode.floatType = FloatPropertyChunk.FloatType.PowerSlider;
                    tNode.value = EditorGUILayout.Slider(tNode.value, tNode.rangeValues.x, tNode.rangeValues.y);
                    EditorGUILayout.BeginHorizontal();
                    ranges.x = EditorGUILayout.FloatField(ranges.x);
                    ranges.y = EditorGUILayout.FloatField(ranges.y);
                    //power needs to be name
                    ranges.z = EditorGUILayout.FloatField(ranges.z);
                    EditorGUILayout.EndHorizontal();
                    tNode.rangeValues = ranges;
                    break;
                case FloatPropertyChunk.FloatType.Toggle:
                    bool toggleState = tNode.value == 0f ? false : true;
                    tNode.floatType = FloatPropertyChunk.FloatType.Toggle;
                    toggleState = EditorGUILayout.Toggle(toggleState);
                    tNode.value = toggleState == true ? 1f : 0f;
                    break;
            }*/
        }

        public override float GetHeight()
        {
            return (EditorGUIUtility.singleLineHeight + 16 * EditorGUIUtility.standardVerticalSpacing) + EditorGUIUtility.standardVerticalSpacing;
        }
    }

#if WITH_PRESENTER
    [Serializable]
    public class Vector1NodePresenter : PropertyNodePresenter
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = CreateInstance<Vector1ControlPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter>(base.GetControlData()) { instance };
        }
    }
#endif

    public class Vector1NodeView : PropertyNodeView
    {
        protected override IEnumerable<GraphControlPresenter> GetControlData()
        {
            var instance = ScriptableObject.CreateInstance<Vector1ControlPresenter>();
            instance.Initialize(node);
            return new List<GraphControlPresenter>(base.GetControlData()) { instance };
        }
    }

}
