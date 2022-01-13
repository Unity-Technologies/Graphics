using UnityEngine;
using UnityEngine.Rendering;
using UnityEditorInternal;

namespace UnityEditor.Rendering
{
    [VolumeParameterDrawer(typeof(MinIntParameter))]
    sealed class MinIntParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Integer)
                return false;

            var o = parameter.GetObjectRef<MinIntParameter>();
            int v = EditorGUILayout.IntField(title, value.intValue);
            value.intValue = Mathf.Max(v, o.min);
            return true;
        }
    }

    [VolumeParameterDrawer(typeof(NoInterpMinIntParameter))]
    sealed class NoInterpMinIntParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Integer)
                return false;

            var o = parameter.GetObjectRef<NoInterpMinIntParameter>();
            int v = EditorGUILayout.IntField(title, value.intValue);
            value.intValue = Mathf.Max(v, o.min);
            return true;
        }
    }

    [VolumeParameterDrawer(typeof(MaxIntParameter))]
    sealed class MaxIntParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Integer)
                return false;

            var o = parameter.GetObjectRef<MaxIntParameter>();
            int v = EditorGUILayout.IntField(title, value.intValue);
            value.intValue = Mathf.Min(v, o.max);
            return true;
        }
    }

    [VolumeParameterDrawer(typeof(NoInterpMaxIntParameter))]
    sealed class NoInterpMaxIntParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Integer)
                return false;

            var o = parameter.GetObjectRef<NoInterpMaxIntParameter>();
            int v = EditorGUILayout.IntField(title, value.intValue);
            value.intValue = Mathf.Min(v, o.max);
            return true;
        }
    }

    [VolumeParameterDrawer(typeof(ClampedIntParameter))]
    sealed class ClampedIntParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Integer)
                return false;

            var o = parameter.GetObjectRef<ClampedIntParameter>();
            EditorGUILayout.IntSlider(value, o.min, o.max, title);
            value.intValue = Mathf.Clamp(value.intValue, o.min, o.max);
            return true;
        }
    }

    [VolumeParameterDrawer(typeof(NoInterpClampedIntParameter))]
    sealed class NoInterpClampedIntParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Integer)
                return false;

            var o = parameter.GetObjectRef<NoInterpClampedIntParameter>();
            EditorGUILayout.IntSlider(value, o.min, o.max, title);
            value.intValue = Mathf.Clamp(value.intValue, o.min, o.max);
            return true;
        }
    }

    [VolumeParameterDrawer(typeof(LayerMaskParameter))]
    sealed class LayerMaskParameterDrawer : VolumeParameterDrawer
    {
        private static int FieldToLayerMask(int field)
        {
            int mask = 0;
            var layers = InternalEditorUtility.layers;
            bool everything = true;
            for (int c = 0; c < layers.Length; c++)
            {
                if ((field & (1 << c)) != 0)
                    mask |= 1 << LayerMask.NameToLayer(layers[c]);
                else
                {
                    mask &= ~(1 << LayerMask.NameToLayer(layers[c]));
                    everything = false;
                }
            }

            return everything ? -1 : mask;
        }

        private static int LayerMaskToField(int mask)
        {
            int field = 0;
            var layers = InternalEditorUtility.layers;
            bool everything = true;
            for (int c = 0; c < layers.Length; c++)
            {
                if ((mask & (1 << LayerMask.NameToLayer(layers[c]))) != 0)
                    field |= 1 << c;
                else
                    everything = false;
            }

            return everything ? -1 : field;
        }

        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.LayerMask)
                return false;

            value.intValue = FieldToLayerMask(
                EditorGUILayout.MaskField(title, LayerMaskToField(value.intValue), InternalEditorUtility.layers));

            return true;
        }
    }
}
