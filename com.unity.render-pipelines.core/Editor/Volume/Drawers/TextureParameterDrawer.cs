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

    [VolumeParameterDrawer(typeof(Texture2DParameter))]
    sealed class Texture2DParameterDrawer : VolumeParameterDrawer
    {
        static Delegate validator = TextureParameterHelper.CastValidator((Object[] references, Type objType, SerializedProperty property, int options) =>
        {
            // Accept RenderTextures of dimension 2D
            Texture validated = (RenderTexture)TextureParameterHelper.ValidateObjectFieldAssignment(references, typeof(RenderTexture), property, options);
            if (validated != null && validated.dimension != TextureDimension.Tex2D)
                validated = null;
            // Accept all Texture2D
            if (validated == null)
                validated = (Texture2D)TextureParameterHelper.ValidateObjectFieldAssignment(references, typeof(Texture2D), property, options);
            return validated ? validated : property.objectReferenceValue;
        });

        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;
            if (value.propertyType != SerializedPropertyType.ObjectReference)
                return false;

            TextureParameterHelper.DoObjectField(value, title, typeof(Texture2D), typeof(RenderTexture), validator);
            return true;
        }
    }

    [VolumeParameterDrawer(typeof(Texture3DParameter))]
    sealed class Texture3DParameterDrawer : VolumeParameterDrawer
    {
        static Delegate validator = TextureParameterHelper.CastValidator((Object[] references, Type objType, SerializedProperty property, int options) =>
        {
            // Accept RenderTextures of dimension 3D
            Texture validated = (RenderTexture)TextureParameterHelper.ValidateObjectFieldAssignment(references, typeof(RenderTexture), property, options);
            if (validated != null && validated.dimension != TextureDimension.Tex3D)
                validated = null;
            // Accept all Texture3D
            if (validated == null)
                validated = (Texture3D)TextureParameterHelper.ValidateObjectFieldAssignment(references, typeof(Texture3D), property, options);
            return validated ? validated : property.objectReferenceValue;
        });

        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;
            if (value.propertyType != SerializedPropertyType.ObjectReference)
                return false;

            TextureParameterHelper.DoObjectField(value, title, typeof(Texture3D), typeof(RenderTexture), validator);
            return true;
        }
    }

    [VolumeParameterDrawer(typeof(CubemapParameter))]
    sealed class CubemapParameterDrawer : VolumeParameterDrawer
    {
        static Delegate validator = TextureParameterHelper.CastValidator((Object[] references, Type objType, SerializedProperty property, int options) =>
        {
            // Accept RenderTextures of dimension cube
            Texture validated = (RenderTexture)TextureParameterHelper.ValidateObjectFieldAssignment(references, typeof(RenderTexture), property, options);
            if (validated != null && validated.dimension != TextureDimension.Cube)
                validated = null;
            // Accept all Cubemaps
            if (validated == null)
                validated = (Cubemap)TextureParameterHelper.ValidateObjectFieldAssignment(references, typeof(Cubemap), property, options);
            return validated ? validated : property.objectReferenceValue;
        });

        static internal bool OnGUI(SerializedProperty value, GUIContent title)
        {
            if (value.propertyType != SerializedPropertyType.ObjectReference)
                return false;

            TextureParameterHelper.DoObjectField(value, title, typeof(Cubemap), typeof(RenderTexture), validator);
            return true;
        }

        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            return OnGUI(parameter.value, title);
        }
    }

    [VolumeParameterDrawer(typeof(NoInterpCubemapParameter))]
    sealed class NoInterpCubemapParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            return CubemapParameterDrawer.OnGUI(parameter.value, title);
        }
    }
}
