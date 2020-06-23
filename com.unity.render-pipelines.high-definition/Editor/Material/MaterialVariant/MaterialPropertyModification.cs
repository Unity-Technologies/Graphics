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
        [Serializable] enum SerializedType
        {
            Scalar,
            Vector, // will be decomposed as r g b a scalars
            Texture // will be decomposed as m_Texture ObjectReference and m_Scale and m_Offset Vector2s, and each vector2 will be split into x and y
        }

        // Property path of the property being modified (Matches as SerializedProperty.propertyPath)
        [SerializeField] internal string propertyPath;
        // The value being applied
        [SerializeField] float value;
        // The value being applied when it is a object reference (which can not be represented as a string)
        [SerializeField] public Object objectReference;

        private MaterialPropertyModification() { }

        public static System.Collections.Generic.IEnumerable<MaterialPropertyModification> CreateMaterialPropertyModifications(SerializedProperty property)
        {
            SerializedType type = ResolveType(property);
            switch (type)
            {
                case SerializedType.Scalar:
                    return new[] { CreateOneMaterialPropertyModification(property) };
                case SerializedType.Vector:
                    return new[]
                    {
                        CreateOneMaterialPropertyModification(property.FindPropertyRelative("r")),
                        CreateOneMaterialPropertyModification(property.FindPropertyRelative("g")),
                        CreateOneMaterialPropertyModification(property.FindPropertyRelative("b")),
                        CreateOneMaterialPropertyModification(property.FindPropertyRelative("a"))
                    };
                case SerializedType.Texture:
                    return new[]
                    {
                        CreateOneMaterialPropertyModification(property.FindPropertyRelative("m_Texture")),
                        CreateOneMaterialPropertyModification(property.FindPropertyRelative("m_Scale.x")),
                        CreateOneMaterialPropertyModification(property.FindPropertyRelative("m_Scale.y")),
                        CreateOneMaterialPropertyModification(property.FindPropertyRelative("m_Offset.x")),
                        CreateOneMaterialPropertyModification(property.FindPropertyRelative("m_Offset.y"))
                    };
                default:
                    throw new Exception("Unhandled type in Material");
            }
        }

        static MaterialPropertyModification CreateOneMaterialPropertyModification(SerializedProperty property)
            => new MaterialPropertyModification()
            {
                propertyPath = property.propertyPath,
                value = property.floatValue,
                objectReference = property.objectReferenceValue
            };


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

            property.floatValue = propertyModificaton.value;
            property.objectReferenceValue = propertyModificaton.objectReference;
        }
        
        static SerializedType ResolveType(SerializedProperty value)
        {
            if (value.propertyType == SerializedPropertyType.Boolean
                || value.propertyType == SerializedPropertyType.Integer
                || value.propertyType == SerializedPropertyType.Enum
                || value.propertyType == SerializedPropertyType.Float)
                return SerializedType.Scalar;
            else if (value.propertyType == SerializedPropertyType.Color
                || value.propertyType == SerializedPropertyType.Vector2
                || value.propertyType == SerializedPropertyType.Vector2Int
                || value.propertyType == SerializedPropertyType.Vector3
                || value.propertyType == SerializedPropertyType.Vector3Int
                || value.propertyType == SerializedPropertyType.Vector4)
                return SerializedType.Vector;
            else if (value.propertyType == SerializedPropertyType.ObjectReference
                && value.objectReferenceValue is Texture)
                return SerializedType.Texture;
            else
                throw new ArgumentException("Parameter type not allowed in material", "value");
        }
        
        static (SerializedType type, string[] pathParts) RecreateType(MaterialPropertyModification propertyModification)
        {
            string[] parts = propertyModification.propertyPath.Split(new[] { '.' });
            if (parts.Length == 1)
                return (SerializedType.Scalar, parts);

            if (propertyModification.objectReference != null)
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
