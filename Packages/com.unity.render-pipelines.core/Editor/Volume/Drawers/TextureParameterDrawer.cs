using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering
{
    internal static class TextureParameterHelper
    {
        static readonly Type k_ObjectFieldValidator = Type.GetType("UnityEditor.EditorGUI+ObjectFieldValidator,UnityEditor");

        internal static Func<Object[], Type, SerializedProperty, int, Object> ValidateObjectFieldAssignment;
        static Func<Rect, Rect, int, Object, Object, Type, Type, SerializedProperty, Delegate, bool, GUIStyle, Object> k_DoObjectField;

        internal delegate Object ObjectFieldValidator(Object[] references, Type objType, SerializedProperty property, int options);

        static TextureParameterHelper()
        {
            Type ObjectFieldValidatorOptions_Type = Type.GetType("UnityEditor.EditorGUI+ObjectFieldValidatorOptions,UnityEditor");

            var typeParameter = Expression.Parameter(typeof(Type), "type");
            var propertyParameter = Expression.Parameter(typeof(SerializedProperty), "property");
            var intParameter = Expression.Parameter(typeof(int), "int");

            // ValidateObjectFieldAssignment
            MethodInfo validateObjectFieldAssignment_Info = Type.GetType("UnityEditor.EditorGUI,UnityEditor").GetMethod("ValidateObjectFieldAssignment", BindingFlags.Static | BindingFlags.NonPublic);

            var referencesParameter = Expression.Parameter(typeof(Object[]), "references");
            var optionsVariable = Expression.Parameter(ObjectFieldValidatorOptions_Type, "options");

            var validateObjectFieldAssignment_Block = Expression.Block(
                new[] { optionsVariable },
                Expression.Assign(optionsVariable, Expression.Convert(intParameter, ObjectFieldValidatorOptions_Type)),
                Expression.Call(validateObjectFieldAssignment_Info, referencesParameter, typeParameter, propertyParameter, optionsVariable)
            );

            var validateObjectFieldAssignment_Lambda = Expression.Lambda<Func<Object[], Type, SerializedProperty, int, Object>>(
                validateObjectFieldAssignment_Block, referencesParameter, typeParameter, propertyParameter, intParameter);
            ValidateObjectFieldAssignment = validateObjectFieldAssignment_Lambda.Compile();

            // ObjectField
            MethodInfo doObjectField_Info = Type.GetType("UnityEditor.EditorGUI,UnityEditor").GetMethod("DoObjectField", BindingFlags.Static | BindingFlags.NonPublic, null,
                new Type[] { typeof(Rect), typeof(Rect), typeof(int), typeof(Object), typeof(Object), typeof(Type), typeof(Type), typeof(SerializedProperty), k_ObjectFieldValidator, typeof(bool), typeof(GUIStyle) }, null);

            var positionParameter = Expression.Parameter(typeof(Rect), "position");
            var rectParameter = Expression.Parameter(typeof(Rect), "rect");
            var objectParameter = Expression.Parameter(typeof(Object), "object");
            var unusedParameter = Expression.Parameter(typeof(Object), "unused");
            var type2Parameter = Expression.Parameter(typeof(Type), "type2");
            var boolParameter = Expression.Parameter(typeof(bool), "allowSceneObject");
            var styleParameter = Expression.Parameter(typeof(GUIStyle), "style");
            var validatorParameter = Expression.Parameter(typeof(Delegate), "validator");
            var validatorVariable = Expression.Parameter(k_ObjectFieldValidator, "validator");

            var doObjectField_Block = Expression.Block(
                new[] { validatorVariable },
                Expression.Assign(validatorVariable, Expression.Convert(validatorParameter, k_ObjectFieldValidator)),
                Expression.Call(doObjectField_Info, positionParameter, rectParameter, intParameter, objectParameter, unusedParameter, typeParameter, type2Parameter, propertyParameter, validatorVariable, boolParameter, styleParameter)
            );

            var doObjectField_Lambda = Expression.Lambda<Func<Rect, Rect, int, Object, Object, Type, Type, SerializedProperty, Delegate, bool, GUIStyle, Object>>(
                doObjectField_Block, positionParameter, rectParameter, intParameter, objectParameter, unusedParameter, typeParameter, type2Parameter, propertyParameter, validatorParameter, boolParameter, styleParameter);
            k_DoObjectField = doObjectField_Lambda.Compile();
        }

        internal static Delegate CastValidator(ObjectFieldValidator validator)
        {
            return DelegateUtility.Cast(validator, k_ObjectFieldValidator);
        }

        internal static void DoObjectField(SerializedProperty property, GUIContent label, Type type1, Type type2, Delegate validator)
        {
            Rect r = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            DoObjectField(r, property, label, type1, type2, validator);
        }

        internal static void DoObjectField(Rect r, SerializedProperty property, GUIContent label, Type type1, Type type2, Delegate validator)
        {
            int id = GUIUtility.GetControlID(FocusType.Keyboard, r);
            r = EditorGUI.PrefixLabel(r, id, label);

            k_DoObjectField(r, r, id, null, null, type1, type2, property, validator, false, EditorStyles.objectField);
        }
    }

    [VolumeParameterDrawer(typeof(TextureParameter))]
    sealed class TextureParameterDrawer : VolumeParameterDrawer
    {
        internal static bool HasGenuineNullSelection(Object[] references)
        {
            bool genuineNullSelection = true;
            for (int i = 0; i < references.Length; ++i)
            {
                genuineNullSelection &= (references[i] == null);
            }
            return genuineNullSelection;
        }

        static Delegate validator = TextureParameterHelper.CastValidator((Object[] references, Type objType, SerializedProperty property, int options) =>
        {
            if (HasGenuineNullSelection(references)) return null;

            // Accept RenderTextures of correct dimensions
            Texture validated = (RenderTexture)TextureParameterHelper.ValidateObjectFieldAssignment(references, typeof(RenderTexture), property, options);
            if (validated != null)
            {
                if (objType == typeof(Texture2D) && validated.dimension != TextureDimension.Tex2D) validated = null;
                if (objType == typeof(Texture3D) && validated.dimension != TextureDimension.Tex3D) validated = null;
                if (objType == typeof(Cubemap)   && validated.dimension != TextureDimension.Cube)  validated = null;
            }
            // Accept textures of correct type
            if (validated == null)
                validated = (Texture)TextureParameterHelper.ValidateObjectFieldAssignment(references, objType, property, options);
            return validated ? validated : property.objectReferenceValue;
        });

        internal static bool TextureField(SerializedProperty property, GUIContent title, TextureDimension dimension)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
                return false;

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, title, property);
            switch (dimension)
            {
                case TextureDimension.Tex2D:
                    TextureParameterHelper.DoObjectField(rect, property, title, typeof(Texture2D), typeof(RenderTexture), validator);
                    break;
                case TextureDimension.Tex3D:
                    TextureParameterHelper.DoObjectField(rect, property, title, typeof(Texture3D), typeof(RenderTexture), validator);
                    break;
                case TextureDimension.Cube:
                    TextureParameterHelper.DoObjectField(rect, property, title, typeof(Cubemap), typeof(RenderTexture), validator);
                    break;
                default:
                    EditorGUILayout.PropertyField(property, title);
                    break;
            }
            EditorGUI.EndProperty();

            return true;
        }

        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;
            var dimension = parameter.GetObjectRef<TextureParameter>().dimension;
            return TextureField(value, title, dimension);
        }
    }

    [VolumeParameterDrawer(typeof(Texture2DParameter))]
    sealed class Texture2DParameterDrawer : VolumeParameterDrawer
    {
        /// <summary>Draws the parameter in the editor.</summary>
        /// <param name="parameter">The parameter to draw.</param>
        /// <param name="title">The label and tooltip of the parameter.</param>
        /// <returns><c>true</c> if the input parameter is valid, <c>false</c> otherwise</returns>
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            return TextureParameterDrawer.TextureField(parameter.value, title, TextureDimension.Tex2D);
        }
    }

    [VolumeParameterDrawer(typeof(Texture3DParameter))]
    sealed class Texture3DParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            return TextureParameterDrawer.TextureField(parameter.value, title, TextureDimension.Tex3D);
        }
    }

    [VolumeParameterDrawer(typeof(CubemapParameter))]
    sealed class CubemapParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            return TextureParameterDrawer.TextureField(parameter.value, title, TextureDimension.Cube);
        }
    }

    [VolumeParameterDrawer(typeof(NoInterpCubemapParameter))]
    sealed class NoInterpCubemapParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            return TextureParameterDrawer.TextureField(parameter.value, title, TextureDimension.Cube);
        }
    }
}
