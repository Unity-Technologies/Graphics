using System;
using System.Collections.Generic;
using UnityEditor.Rendering.Converter;
using UnityEngine;
using UnityEngine.Categorization;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [Serializable]
    [PipelineConverter("Universal Render Pipeline (Universal Renderer)", "Universal Render Pipeline (2D Renderer)")]
    [ElementInfo(Name = "Material Reference Converter",
                 Order = 100,
                 Description = "Converts references to URP Lit and Simple Lit readonly material to Mesh 2D Lit material.")]
    internal class URP3DToURP2DReadonlyMaterialConverter : ReadonlyMaterialConverter
    {
        private List<(string query, string description)> m_ContextSearchQueriesAndIds;
        private Dictionary<string, Func<Material>> m_MaterialMappings;

        public override bool isEnabled
        {
            get
            {
                if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urpAsset)
                    return false;
                return urpAsset.scriptableRenderer is Renderer2D;
            }
        }

        public override string isDisabledMessage => "Converter requires URP with a Renderer 2D. Convert your project to URP to use this converter.";

        private bool m_Initialized = false;

        private void InitializeIfNeeded()
        {
            if (m_Initialized)
                return;

            m_Initialized = true;

            var urpMaterials = GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>();
            var lit = urpMaterials.defaultMaterial;
            var simpleLit = urpMaterials.simpleLitMaterial;

            // -t:RenderPipelineGlobalSettings is to avoid adding the Global Settings as they reference the URP Lit and Simple Lit materials
            // We do not want to modify them as part of this conversion, because they must keep referencing the URP 3D materials.
            const string formattedId = "p: ref=<$object:{0},UnityEngine.Object$> -t:RenderPipelineGlobalSettings";

            m_ContextSearchQueriesAndIds = new List<(string query, string description)>()
            {
                (string.Format(formattedId, GlobalObjectId.GetGlobalObjectIdSlow(lit)), "URP Lit is being referenced"),
                (string.Format(formattedId, GlobalObjectId.GetGlobalObjectIdSlow(simpleLit)), "URP Simple Lit is being referenced")
            };

            Func<Material> mesh2DLitMaterial = () => GraphicsSettings.GetRenderPipelineSettings<Renderer2DResources>().defaultMesh2DLitMaterial;

            m_MaterialMappings = new Dictionary<string, Func<Material>>()
            {
                ["Lit"] = mesh2DLitMaterial,
                ["SimpleLit"] = mesh2DLitMaterial
            };
        }

        protected override List<(string query, string description)> contextSearchQueriesAndIds
        {
            get
            {
                InitializeIfNeeded();
                return m_ContextSearchQueriesAndIds;
            }
        }

        protected override Dictionary<string, Func<Material>> materialMappings
        {
            get
            {
                InitializeIfNeeded();
                return m_MaterialMappings;
            }
        }
    }
}
