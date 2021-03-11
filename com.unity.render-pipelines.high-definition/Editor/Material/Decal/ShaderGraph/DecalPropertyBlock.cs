using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.DecalSurfaceOptionsUIBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class DecalPropertyBlock : SubTargetPropertyBlock
    {
        DecalData decalData;

        protected override string title => "Surface Options";
        protected override int foldoutIndex => 4;

        public DecalPropertyBlock(DecalData decalData) => this.decalData = decalData;

        protected override void CreatePropertyGUI()
        {
            AddProperty(affectAlbedoText, () => decalData.affectsAlbedo, (newValue) => decalData.affectsAlbedo = newValue);
            AddProperty(affectNormalText, () => decalData.affectsNormal, (newValue) => decalData.affectsNormal = newValue);
            AddProperty(affectMetalText, () => decalData.affectsMetal, (newValue) => decalData.affectsMetal = newValue);
            AddProperty(affectAmbientOcclusionText, () => decalData.affectsAO, (newValue) => decalData.affectsAO = newValue);
            AddProperty(affectSmoothnessText, () => decalData.affectsSmoothness, (newValue) => decalData.affectsSmoothness = newValue);
            AddProperty(affectEmissionText, () => decalData.affectsEmission, (newValue) => decalData.affectsEmission = newValue);
            AddProperty(supportLodCrossFadeText, () => decalData.supportLodCrossFade, (newValue) => decalData.supportLodCrossFade = newValue);
        }
    }
}
