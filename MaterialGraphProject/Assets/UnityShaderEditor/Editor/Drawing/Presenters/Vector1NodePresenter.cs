using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine.MaterialGraph;
using UnityEngine;

namespace UnityEditor.MaterialGraph.Drawing
{
    class Vector1ControlPresenter : GraphControlPresenter
    {
		[SerializeField]
		private int dynamicHeight;

        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var tNode = node as Vector1Node;
            if (tNode == null)
                return;

			tNode.floatType = (FloatPropertyChunk.FloatType)EditorGUILayout.EnumPopup ("Float", tNode.floatType);

			Vector3 ranges = tNode.rangeValues;

			switch(tNode.floatType){
			case FloatPropertyChunk.FloatType.Float:
				dynamicHeight = 8;
				tNode.floatType = FloatPropertyChunk.FloatType.Float;
				tNode.value = EditorGUILayout.FloatField ("Value:", tNode.value);
				break;
			case FloatPropertyChunk.FloatType.Range:
				dynamicHeight = 16;
				tNode.floatType = FloatPropertyChunk.FloatType.Range;
				tNode.value = EditorGUILayout.Slider (tNode.value, tNode.rangeValues.x, tNode.rangeValues.y);
				EditorGUILayout.BeginHorizontal ();
				ranges.x = EditorGUILayout.FloatField (ranges.x);
				//very dirty...
				for (int i = 0; i < 15; i++) {
					EditorGUILayout.Space ();
				}
				ranges.y = EditorGUILayout.FloatField (ranges.y);
				EditorGUILayout.EndHorizontal ();
				tNode.rangeValues = ranges;
				break;
			case FloatPropertyChunk.FloatType.PowerSlider:
				dynamicHeight = 16;
				tNode.floatType = FloatPropertyChunk.FloatType.PowerSlider;
				tNode.value = EditorGUILayout.Slider (tNode.value, tNode.rangeValues.x, tNode.rangeValues.y);
				EditorGUILayout.BeginHorizontal ();
				ranges.x = EditorGUILayout.FloatField (ranges.x);
				//very dirty...
				for (int i = 0; i < 15; i++) {
					EditorGUILayout.Space ();
				}
				ranges.y = EditorGUILayout.FloatField (ranges.y);
				EditorGUILayout.EndHorizontal ();
				ranges.z = EditorGUILayout.FloatField ("Power", ranges.z);
				tNode.rangeValues = ranges;
				break;
			case FloatPropertyChunk.FloatType.Toggle:
				dynamicHeight = 8;
				bool toggleState = tNode.value == 0f ? false : true;
				tNode.floatType = FloatPropertyChunk.FloatType.Toggle;
				toggleState = EditorGUILayout.Toggle (toggleState);
				tNode.value = toggleState == true ? 1f : 0f;
				break;
			}

            

        }

        public override float GetHeight()
        {
			return (EditorGUIUtility.singleLineHeight + dynamicHeight * EditorGUIUtility.standardVerticalSpacing) + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class Vector1NodePresenter : PropertyNodePresenter
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<Vector1ControlPresenter>();
            instance.Initialize(node);
            return new List<GraphElementPresenter>(base.GetControlData()) { instance };
        }
    }
}
