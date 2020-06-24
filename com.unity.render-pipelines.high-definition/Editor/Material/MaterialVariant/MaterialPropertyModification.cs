using System.Runtime.InteropServices;
using Object = UnityEngine.Object;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEditor;
using System;

namespace Unity.Assets.MaterialVariant.Editor
{
    // Defines a single modified property.
    [System.Serializable]
    public sealed class MaterialPropertyModification
    {
        enum SerializedType
        {
            Scalar,
            Vector, // will be decomposed as r g b a scalars
            Texture // will be decomposed as m_Texture ObjectReference and m_Scale and m_Offset Vector2s, and each vector2 will be split into x and y
        }

        // Property path of the property being modified (Matches as SerializedProperty.propertyPath)
        [SerializeField] string m_PropertyPath;
        // The value being applied
        [SerializeField] float m_Value;
        // The value being applied when it is a object reference (which can not be represented as a string)
        [SerializeField] Object m_ObjectReference;

        internal string propertyPath => m_PropertyPath;

        private MaterialPropertyModification(string propertyPath, float value, Object objectReference)
        {
            m_PropertyPath = propertyPath;
            m_Value = value;
            m_ObjectReference = objectReference;
        }

        public static System.Collections.Generic.IEnumerable<MaterialPropertyModification> CreateMaterialPropertyModifications(MaterialProperty property)
        {
            SerializedType type = ResolveType(property);
            switch (type)
            {
                case SerializedType.Scalar:
                    return new[] { new MaterialPropertyModification(property.name, property.floatValue, null) };
                case SerializedType.Vector:
                    return new[]
                    {
                        new MaterialPropertyModification(property.name + ".r", property.vectorValue.x, null),
                        new MaterialPropertyModification(property.name + ".g", property.vectorValue.y, null),
                        new MaterialPropertyModification(property.name + ".b", property.vectorValue.z, null),
                        new MaterialPropertyModification(property.name + ".a", property.vectorValue.w, null)
                    };
                case SerializedType.Texture:
                    return new[]
                    {
                        new MaterialPropertyModification(property.name + ".m_Texture", 0f, property.textureValue),
                        new MaterialPropertyModification(property.name + ".m_Scale.x", property.textureScaleAndOffset.x, null),
                        new MaterialPropertyModification(property.name + ".m_Scale.y", property.textureScaleAndOffset.y, null),
                        new MaterialPropertyModification(property.name + ".m_Offset.x", property.textureScaleAndOffset.z, null),
                        new MaterialPropertyModification(property.name + ".m_Offset.y", property.textureScaleAndOffset.w, null)
                    };
                default:
                    throw new Exception("Unhandled type in Material");
            }
        }

        public static void ApplyPropertyModificationsToMaterial(Material material, System.Collections.Generic.IEnumerable<MaterialPropertyModification> propertyModifications)
        {
            SerializedObject serializedMaterial = new SerializedObject(material);
            foreach (MaterialPropertyModification propertyModification in propertyModifications)
                ApplyOnePropertyModificationToSerializedObject(serializedMaterial, propertyModification);
            serializedMaterial.ApplyModifiedProperties();
        }

        static void ApplyOnePropertyModificationToSerializedObject(SerializedObject serializedMaterial, MaterialPropertyModification propertyModificaton)
        {
            (SerializedType type, string[] pathParts) = RecreateType(propertyModificaton);
            (SerializedProperty property, int index, SerializedProperty parent) = FindProperty(serializedMaterial, pathParts[0], type);
            for (int i = 1; i < pathParts.Length; ++i)
                property = property.FindPropertyRelative(pathParts[i]);

            property.floatValue = propertyModificaton.m_Value;
            //property.objectReferenceValue = propertyModificaton.m_ObjectReference;
        }
        
        static SerializedType ResolveType(MaterialProperty value)
        {
            switch (value.type)
            {
                case MaterialProperty.PropType.Float:   return SerializedType.Scalar;
                case MaterialProperty.PropType.Range:   return SerializedType.Scalar;
                case MaterialProperty.PropType.Color:   return SerializedType.Vector;
                case MaterialProperty.PropType.Vector:  return SerializedType.Vector;
                case MaterialProperty.PropType.Texture: return SerializedType.Texture;
                default:
                    throw new ArgumentException("Unhandled MaterialProperty Type", "value");
            }
        }
        
        static (SerializedType type, string[] pathParts) RecreateType(MaterialPropertyModification propertyModification)
        {
            string[] parts = propertyModification.m_PropertyPath.Split(new[] { '.' });
            if (parts.Length == 1)
                return (SerializedType.Scalar, parts);

            if (propertyModification.m_ObjectReference != null)
                return (SerializedType.Texture, parts);
            
            if (parts.Length == 2)
                return (SerializedType.Vector, parts);

            return (SerializedType.Texture, parts);  //could be length 2 only if object reference, else length is 3
        }
        
        static SerializedProperty FindBase(SerializedObject material, SerializedType type)
        {
            var propertyBase = material.FindProperty("m_SavedProperties");

            switch (type)
            {
                case SerializedType.Scalar:
                    propertyBase = propertyBase.FindPropertyRelative("m_Floats");
                    break;
                case SerializedType.Vector:
                    propertyBase = propertyBase.FindPropertyRelative("m_Colors");
                    break;
                case SerializedType.Texture:
                    propertyBase = propertyBase.FindPropertyRelative("m_TexEnvs");
                    break;
                default:
                    throw new ArgumentException($"Unknown SerializedType {type}");
            }

            return propertyBase;
        }
        
        static (SerializedProperty property, int index, SerializedProperty parent) FindProperty(SerializedObject material, string propertyName, SerializedType type)
        {
            var propertyBase = FindBase(material, type);

            SerializedProperty property = null;
            int maxSearch = propertyBase.arraySize;
            int indexOf = 0;
            for (; indexOf < maxSearch; ++indexOf)
            {
                property = propertyBase.GetArrayElementAtIndex(indexOf);
                if (property.FindPropertyRelative("first").stringValue == propertyName)
                    break;
            }
            if (indexOf == maxSearch)
                throw new ArgumentException($"Unknown property: {propertyName}");

            property = property.FindPropertyRelative("second");
            return (property, indexOf, propertyBase);
        }
    }
}
