using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    // Utility class that serializes volume component data to/from EditorGUIUtility.systemCopyBuffer.
    internal static class VolumeComponentCopyPaste
    {
        static string GetTypeName(VolumeComponent component) => component.GetType().AssemblyQualifiedName;

        static void WriteCopyBuffer(StringWriter writer, VolumeComponent targetComponent)
        {
            string typeName = GetTypeName(targetComponent);
            string typeData = JsonUtility.ToJson(targetComponent);
            writer.WriteLine($"{typeName}|{typeData}");
        }

        static bool TryReadCopyBuffer(StringReader reader, out string[] typeAndValue)
        {
            string line = reader.ReadLine();
            typeAndValue = line?.Split('|');
            return typeAndValue != null;
        }

        public static bool CanPaste(List<VolumeComponent> targetComponents)
        {
            if (targetComponents == null)
                return false;

            // Allow paste if a single matching component is found
            foreach (var component in targetComponents)
                if (CanPaste(component))
                    return true;

            return false;
        }

        public static bool CanPaste(VolumeComponent targetComponent)
        {
            if (targetComponent == null)
                return false;
            if (string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer))
                return false;

            using var reader = new StringReader(EditorGUIUtility.systemCopyBuffer);
            string targetTypeName = GetTypeName(targetComponent);
            while (TryReadCopyBuffer(reader, out var typeAndValue))
            {
                if (targetTypeName == typeAndValue[0])
                    return true;
            }
            return false;
        }

        public static void CopySettings(VolumeComponent targetComponent)
        {
            using var writer = new StringWriter();
            WriteCopyBuffer(writer, targetComponent);
            EditorGUIUtility.systemCopyBuffer = writer.ToString();
        }

        public static void PasteSettings(VolumeComponent targetComponent)
        {
            if (targetComponent == null)
                return;

            Undo.RecordObject(targetComponent, "Paste Settings");

            using var reader = new StringReader(EditorGUIUtility.systemCopyBuffer);
            if (TryReadCopyBuffer(reader, out var typeAndValue))
                JsonUtility.FromJsonOverwrite(typeAndValue[1], targetComponent);
        }

        public static void CopySettings(List<VolumeComponent> targetComponents)
        {
            using var writer = new StringWriter();
            var targetComponentsInOrder = new List<VolumeComponent>(targetComponents);
            targetComponentsInOrder.Sort(
                (l, r) => string.CompareOrdinal(GetTypeName(l), GetTypeName(r)));
            foreach (var targetComponent in targetComponentsInOrder)
            {
                WriteCopyBuffer(writer, targetComponent);
            }
            EditorGUIUtility.systemCopyBuffer = writer.ToString();
        }

        public static void PasteSettings(List<VolumeComponent> targetComponents)
        {
            if (targetComponents == null || targetComponents.Count == 0)
                return;

            Undo.RecordObjects(targetComponents.ToArray(), "Paste Settings");

            using var reader = new StringReader(EditorGUIUtility.systemCopyBuffer);

            while (TryReadCopyBuffer(reader, out var typeAndValue))
            {
                VolumeComponent targetComponent = null;
                foreach (var x in targetComponents)
                {
                    if (GetTypeName(x) == typeAndValue[0])
                    {
                        targetComponent = x;
                        break;
                    }
                }

                if (targetComponent != null)
                {
                    JsonUtility.FromJsonOverwrite(typeAndValue[1], targetComponent);
                }
            }
        }
    }
}
