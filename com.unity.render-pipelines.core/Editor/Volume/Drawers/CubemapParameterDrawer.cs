using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [VolumeParameterDrawer(typeof(CubemapParameter))]
    sealed class CubemapParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.ObjectReference)
                return false;

            var o = parameter.GetObjectRef<CubemapParameter>();
            EditorGUI.BeginChangeCheck();
            var newTexture = EditorGUILayout.ObjectField(title, value.objectReferenceValue, typeof(Texture), false) as Texture;
            if (EditorGUI.EndChangeCheck())
            {
                // Texture field can accept any texture (to allow Cube Render Texture) but we still check the dimension to avoid errors
                if (newTexture != null && newTexture.dimension == TextureDimension.Cube)
                    value.objectReferenceValue = newTexture;
                else
                {
                    if (newTexture != null)
                        Debug.LogError($"{newTexture} is not a Cubemap. Only textures of Cubemap dimension can be assigned to this field.");
                    value.objectReferenceValue = null;
                }
            }
            return true;
        }
    }

    [VolumeParameterDrawer(typeof(NoInterpCubemapParameter))]
    sealed class NoInterpCubemapParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.ObjectReference)
                return false;

            var o = parameter.GetObjectRef<NoInterpCubemapParameter>();
            EditorGUI.BeginChangeCheck();
            var newTexture = EditorGUILayout.ObjectField(title, value.objectReferenceValue, typeof(Texture), false) as Texture;
            if (EditorGUI.EndChangeCheck())
            {
                // Texture field can accept any texture (to allow Cube Render Texture) but we still check the dimension to avoid errors
                if (newTexture.dimension == TextureDimension.Cube)
                    value.objectReferenceValue = newTexture;
                else
                {
                    Debug.LogError($"{newTexture} is not a Cubemap. Only textures of Cubemap dimension can be assigned to this field.");
                    value.objectReferenceValue = null;
                }
            }
            return true;
        }
    }
}
