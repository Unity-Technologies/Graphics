using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Rendering.Universal.ShaderUtils;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// URP-specific column providers for the Lighting Search window.
    /// </summary>
    static class UniversalLightingSearchColumnProviders
    {
        internal const string k_MaterialGlobalIlluminationUniversalPath = "Material/MaterialGlobalIlluminationURP";

        // The order of the labels must match the order of the flags below.
        static readonly string[] k_EmissionOptions =
        {
            "Realtime Direct Emission",
            "Realtime Indirect Emission",
            "Baked Emission",
        };

        static readonly MaterialGlobalIlluminationFlags[] k_EmissionFlags =
        {
            MaterialGlobalIlluminationFlags.RealtimeDirectEmission,
            MaterialGlobalIlluminationFlags.RealtimeIndirectEmission,
            MaterialGlobalIlluminationFlags.BakedEmission,
        };

        static MaterialGlobalIlluminationFlags EmissionModeMask =>
            MaterialGlobalIlluminationFlags.RealtimeDirectEmission
            | MaterialGlobalIlluminationFlags.RealtimeIndirectEmission
            | MaterialGlobalIlluminationFlags.BakedEmission;

        // Emission flags -> MaskField bits (bit i == flag i).
        internal static int ToMaskValue(MaterialGlobalIlluminationFlags flags)
        {
            int mask = 0;
            for (int i = 0; i < k_EmissionFlags.Length; i++)
            {
                if ((flags & k_EmissionFlags[i]) != 0)
                    mask |= 1 << i;
            }
            return mask;
        }

        // MaskField bits -> emission flags, preserving unrelated bits (e.g. EmissiveIsBlack).
        internal static MaterialGlobalIlluminationFlags FromMaskValue(MaterialGlobalIlluminationFlags current, int mask)
        {
            var flags = current & ~EmissionModeMask;
            for (int i = 0; i < k_EmissionFlags.Length; i++)
            {
                if ((mask & (1 << i)) != 0)
                    flags |= k_EmissionFlags[i];
            }
            return flags;
        }

        [SearchColumnProvider(k_MaterialGlobalIlluminationUniversalPath)]
        public static void MaterialGlobalIlluminationUniversalSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var material = args.item.data as Material ?? args.item.ToObject<Material>();
                if (material == null)
                    return null;

                return ToMaskValue(material.globalIlluminationFlags);
            };
            column.setter = args =>
            {
                if (args.value == null || !(args.value is int mask))
                    return;

                var material = args.item.data as Material ?? args.item.ToObject<Material>();
                if (material == null)
                    return;

                var newFlags = FromMaskValue(material.globalIlluminationFlags, mask);
                if (newFlags == material.globalIlluminationFlags)
                    return;

                Undo.RecordObject(material, $"Modify Emission Flags of {material.name}");
                material.globalIlluminationFlags = newFlags;
                EditorUtility.SetDirty(material);

                // Refresh shader keywords (e.g. _EMISSION) to match the changed flags,
                UpdateMaterial(material, MaterialUpdateType.ModifiedMaterial);
            };
            column.cellCreator = _ => new MaskField(new List<string>(k_EmissionOptions), 0)
            {
                style = { flexGrow = 1 }
            };
            column.binder = (args, ve) =>
            {
                if (args.value is int mask && ve is MaskField field)
                {
                    field.visible = true;
                    field.SetValueWithoutNotify(mask);
                }
                else
                {
                    ve.visible = false;
                }
            };
        }
    }
}
