using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Rendering
{
    [CustomPropertyDrawer(typeof(SRPLensFlareDataElement))]
    public class SRPLensFlareElementEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            const float indent = 35.0f;
            float originY = position.y;
            float offsetHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            Rect rect = new Rect(position.x + indent, position.y, position.width - indent, GUIStyle.none.lineHeight);

            EditorGUI.BeginProperty(new Rect(rect.x, rect.y, rect.width, 2.0f * rect.height), label, property);

            SerializedProperty intensityProp = property.FindPropertyRelative("localIntensity");
            SerializedProperty positionProp = property.FindPropertyRelative("position");
            SerializedProperty lensFlareProp = property.FindPropertyRelative("lensFlareTexture");
            SerializedProperty tintProp = property.FindPropertyRelative("tint");
            SerializedProperty blendModeProp = property.FindPropertyRelative("blendMode");
            SerializedProperty sizeProp = property.FindPropertyRelative("size");
            SerializedProperty aspectRatioProp = property.FindPropertyRelative("aspectRatio");
            SerializedProperty rotationProp = property.FindPropertyRelative("rotation");
            SerializedProperty speedProp = property.FindPropertyRelative("speed");
            SerializedProperty autoRotateProp = property.FindPropertyRelative("autoRotate");
            SerializedProperty modulateByLightColor = property.FindPropertyRelative("modulateByLightColor");

            float tmp;
            if ((tmp = EditorGUI.FloatField(rect, EditorGUIUtility.TrTextContent("Intensity"), intensityProp.floatValue)) != intensityProp.floatValue)
                intensityProp.floatValue = Mathf.Max(tmp, 0.0f);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if ((tmp = EditorGUI.FloatField(rect, EditorGUIUtility.TrTextContent("Position"), positionProp.floatValue)) != positionProp.floatValue)
                positionProp.floatValue = tmp;
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            Texture tmpTex;
            if ((tmpTex = (EditorGUI.ObjectField(rect, EditorGUIUtility.TrTextContent("Flare Texture"), lensFlareProp.objectReferenceValue, typeof(Texture), false) as Texture)) != (lensFlareProp.objectReferenceValue as Texture))
            {
                lensFlareProp.objectReferenceValue = tmpTex;
                aspectRatioProp.floatValue = ((float)tmpTex.width) / ((float)tmpTex.height);
                aspectRatioProp.serializedObject.ApplyModifiedProperties();
            }
            if (lensFlareProp.objectReferenceValue != null)
            {
                Rect imgRect = new Rect(rect.x - indent - offsetHeight, originY + offsetHeight, indent + offsetHeight * 0.75f, indent + offsetHeight * 0.75f);
                EditorGUI.DrawTextureTransparent(imgRect, lensFlareProp.objectReferenceValue as Texture, ScaleMode.ScaleToFit, 1.0f / aspectRatioProp.floatValue);
            }
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            SRPLensFlareBlendMode newBlendMode;
            SRPLensFlareBlendMode blendModeValue = (UnityEngine.SRPLensFlareBlendMode)blendModeProp.enumValueIndex;
            if ((newBlendMode = ((SRPLensFlareBlendMode)(EditorGUI.EnumPopup(rect, "Blend Mode", blendModeValue)))) != blendModeValue)
                blendModeProp.enumValueIndex = (int)newBlendMode;
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if ((tmp = EditorGUI.FloatField(rect, EditorGUIUtility.TrTextContent("Size"), sizeProp.floatValue)) != sizeProp.floatValue)
                sizeProp.floatValue = Mathf.Max(tmp, 1e-5f);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if ((tmp = EditorGUI.FloatField(rect, EditorGUIUtility.TrTextContent("Aspect Ratio"), aspectRatioProp.floatValue)) != aspectRatioProp.floatValue)
                aspectRatioProp.floatValue = Mathf.Max(tmp, 1e-5f);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            Color tmpCol;
            if ((tmpCol = EditorGUI.ColorField(rect, EditorGUIUtility.TrTextContent("Tint"), tintProp.colorValue)) != tintProp.colorValue)
                tintProp.colorValue = tmpCol;
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if ((tmp = EditorGUI.FloatField(rect, EditorGUIUtility.TrTextContent("Rotation"), rotationProp.floatValue)) != rotationProp.floatValue)
                rotationProp.floatValue = Mathf.Max(tmp, 0.0f);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if ((tmp = EditorGUI.FloatField(rect, EditorGUIUtility.TrTextContent("Speed"), speedProp.floatValue)) != speedProp.floatValue)
                speedProp.floatValue = tmp;
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            bool tmpBool;
            if ((tmpBool = EditorGUI.Toggle(rect, EditorGUIUtility.TrTextContent("Auto Rotate"), autoRotateProp.boolValue)) != autoRotateProp.boolValue)
                autoRotateProp.boolValue = tmpBool;
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if ((tmpBool = EditorGUI.Toggle(rect, EditorGUIUtility.TrTextContent("Modulate By Light Color"), modulateByLightColor.boolValue)) != modulateByLightColor.boolValue)
                modulateByLightColor.boolValue = tmpBool;
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            rect.height = 1;
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin
                ? new Color32(26, 26, 26, 255)
                : new Color32(127, 127, 127, 255));
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 11.5f * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }
    }
}
