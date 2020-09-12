using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.DecalSurfaceInputsUIBlock.Styles;
using static UnityEditor.Rendering.HighDefinition.SurfaceOptionUIBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class DecalPropertyBlock : SubTargetPropertyBlock
    {
        class Styles
        {
            public static GUIContent normalModeText = new GUIContent("Affect Normal", "TODO");
            public static GUIContent affectEmissionText = new GUIContent("Affect Emission", "TODO");
        }

        DecalData decalData;

        protected override string title => "Surface Settings";
        protected override int foldoutIndex => 4;

        public DecalPropertyBlock(DecalData decalData) => this.decalData = decalData;

        protected override void CreatePropertyGUI()
        {
            AddProperty(albedoModeText, () => decalData.affectsAlbedo, (newValue) => decalData.affectsAlbedo = newValue);
            AddProperty(Styles.normalModeText, () => decalData.affectsNormal, (newValue) => decalData.affectsNormal = newValue);
            AddProperty(affectMetalText, () => decalData.affectsMetal, (newValue) => decalData.affectsMetal = newValue);
            AddProperty(affectAmbientOcclusionText, () => decalData.affectsAO, (newValue) => decalData.affectsAO = newValue);
            AddProperty(affectSmoothnessText, () => decalData.affectsSmoothness, (newValue) => decalData.affectsSmoothness = newValue);
            AddProperty(Styles.affectEmissionText, () => decalData.affectsEmission, (newValue) => decalData.affectsEmission = newValue);
        }
    }
}
