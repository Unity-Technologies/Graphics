using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.VFX
{
    [Serializable]
    sealed class ParticleShadingShaderGraph : ParticleShading, IVFXShaderGraphOutput
    {
        public static readonly  string kErrorOldSG = "The VFXTarget ShaderGraph integration isn't maintained anymore, use the toggle Support VFXGraph instead";

        [VFXSetting, SerializeField]
        ShaderGraphVfxAsset shaderGraph;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.None)]
        ShaderGraphVfxAsset currentShaderGraph; //No serialized transient reference (in case of fallback)

        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), SerializeField]
        VFXMaterialSerializedSettings materialSettings = new();

        ShaderGraphVfxAsset GetOrRefreshShaderGraphObject(List<(string error, VFXErrorType errorType, string desc)> errors = null)
        {
            currentShaderGraph = shaderGraph;

            //This is the only place where shaderGraph property is updated or read
            if (currentShaderGraph == null && !object.ReferenceEquals(currentShaderGraph, null))
            {
                var assetPath = AssetDatabase.GetAssetPath(currentShaderGraph.GetInstanceID());
                var newShaderGraph = AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(assetPath);
                if (newShaderGraph == null)
                {
                    if (errors != null)
                    {
                        var message = VFXShaderGraphHelpers.GetMissingShaderGraphErrorMessage(currentShaderGraph);
                        errors.Add(("ErrorMissingShaderGraph", VFXErrorType.Error, "The VFX Graph" + message));
                    }
                }
                else
                {
                    currentShaderGraph = newShaderGraph;
                    shaderGraph = newShaderGraph;
                }
            }

            if (currentShaderGraph != null)
            {
                if (!currentShaderGraph.generatesWithShaderGraph)
                {
                    if (errors != null)
                        errors.Add(("DeprecatedOldShaderGraph", VFXErrorType.Error, ParticleShadingShaderGraph.kErrorOldSG));

                    currentShaderGraph = VFXResources.errorFallbackShaderGraph;
                } 
                else if (VFXLibrary.currentSRPBinder != null && !VFXLibrary.currentSRPBinder.IsShaderVFXCompatible(currentShaderGraph))
                {
                    if (errors != null)
                        errors.Add(("NoSRPCompatibleSG", VFXErrorType.Error, "The current Shader Graph doesn't support the current SRP or VFX Support is not enabled."));

                    currentShaderGraph = VFXResources.errorFallbackShaderGraph;
                }
            }

            if (currentShaderGraph == null)
            {
                if (errors?.Count == 0)
                    errors.Add(("NoShaderGraph", VFXErrorType.Error, "Please assign a compatible ShaderGraph Asset."));

                currentShaderGraph = VFXResources.errorFallbackShaderGraph;
            }

            return currentShaderGraph;
        }

        static readonly string[] kAlwaysFilteredOutSettings = new[]
        {
            "blendMode",
            "cullMode",
            "zWriteMode",
            "zTestMode",
            "excludeFromTAA",
            "preserveSpecularLighting",
            "doubleSided",
            "onlyAmbientLighting",
            "useExposureWeight",
            "alphaThreshold",
            "normalBending",
            "colorMapping",
            "useAlphaClipping",
            "useSoftParticle"
        };

        public override TraitDescription GetDescription(VFXAbstractComposedParticleOutput parent)
        {
            var desc = base.GetDescription(parent);
            var actualShaderGraph = GetOrRefreshShaderGraphObject(desc.errors);
            desc.name = "ShaderGraph";
            desc.hiddenSettings = new(kAlwaysFilteredOutSettings);
            var srpBinder = VFXLibrary.currentSRPBinder;

            if (actualShaderGraph != null)
            {
                desc.name = string.Empty;
                if (srpBinder != null)
                {
                    desc.name = shaderGraph != null ? srpBinder.GetShaderName(actualShaderGraph) : string.Empty;
                    if (srpBinder.TryGetCastShadowFromMaterial(actualShaderGraph, materialSettings, out var hasShadowCasting))
                    {
                        desc.hasShadowCasting = hasShadowCasting;
                        desc.hiddenSettings.Add("castShadows");
                    }

                    if (srpBinder.TryGetQueueOffset(actualShaderGraph, materialSettings, out var sortingPriority))
                    {
                        desc.sortingPriority = sortingPriority;
                        desc.hiddenSettings.Add("sortingPriority");
                    }
                    desc.supportMotionVectorPerVertex = srpBinder.GetSupportsMotionVectorPerVertex(actualShaderGraph, materialSettings);
                    desc.blendMode = srpBinder.GetBlendModeFromMaterial(actualShaderGraph, materialSettings);
                }
                desc.hasAlphaClipping = actualShaderGraph.alphaClipping;

                //Retrieve properties from shaderGraph (and not actualShaderGraph) to prevent slot removal when listing is available for another SRP
                var shaderGraphProperties = VFXShaderGraphHelpers.GetProperties(shaderGraph);
                foreach (var sgProperty in shaderGraphProperties)
                {
                    desc.properties.Add(sgProperty.property);

                    if (sgProperty.keywordsMapping == null)
                    {
                        desc.propertiesGpuExpressions.Add((ExpressionFromSlot)sgProperty.property.property.name);
                    }
                    else
                    {
                        if (sgProperty.property.property.type == typeof(bool))
                        {
                            var keyword = sgProperty.keywordsMapping[0];
                            desc.propertiesCpuExpressions.Add(new ShaderKeywordExpressionFromSlot(sgProperty.property.property.name, keyword));
                        }
                        if (sgProperty.property.property.type == typeof(uint))
                        {
                            for (uint index = 0; index < sgProperty.keywordsMapping.Length; ++index)
                            {
                                var keyword = sgProperty.keywordsMapping[index];
                                desc.propertiesCpuExpressions.Add(new ShaderKeywordExpressionFromSlot(sgProperty.property.property.name, keyword, index));
                            }
                        }

                        if (!sgProperty.multiCompile)
                        {
                            VFXSlot matchingSlot = null;
                            foreach (var slot in parent.inputSlots)
                            {
                                if (sgProperty.property.property.name == slot.property.name)
                                {
                                    matchingSlot = slot;
                                    break;
                                }
                            }

                            if (matchingSlot != null)
                            {
                                var exp = matchingSlot.GetExpression();
                                var context = new VFXExpression.Context(VFXExpressionContextOption.ConstantFolding);
                                context.RegisterExpression(exp);
                                context.Compile();
                                exp = context.GetReduced(exp);

                                if (!exp.Is(VFXExpression.Flags.Constant))
                                {
                                    desc.errors.Add(new("ErrorShaderFeatureNonConstant", VFXErrorType.Error,
                                        $"The ShaderGraph uses shader_feature for the exposed property '{sgProperty.property.property.name}'." +
                                        $"\nSwitch to multi_compile for any keyword dynamically computed."));
                                }
                            }
                        }
                    }
                }

                foreach (var texture in VFXShaderGraphHelpers.GetTextureConstant(actualShaderGraph))
                    desc.propertiesGpuExpressions.Add((ExpressionConstant)texture);
            }

            return desc;
        }

        public override void GetImportDependentAssets(HashSet<int> dependencies)
        {
            base.GetImportDependentAssets(dependencies);
            if (!ReferenceEquals(shaderGraph, null))
            {
                dependencies.Add(shaderGraph.GetInstanceID());
            }
        }

        public override bool CanBeCompiled()
        {
            var actualShaderGraph = GetOrRefreshShaderGraphObject();
            return actualShaderGraph != null && actualShaderGraph.generatesWithShaderGraph;
        }

        #region Inspector
        MaterialEditor m_MaterialEditor;
        bool m_MaterialEditorDirty;
        List<string> m_OverridenPropertiesCache;
        static SerializedProperty GetWatchedProperties(SerializedObject material)
        {
            var propertyBase = material.FindProperty("m_SavedProperties");
            propertyBase = propertyBase.FindPropertyRelative("m_Floats");
            return propertyBase;
        }

        static string GetPropertyName(SerializedProperty properties, int index)
        {
            var currentEntry = properties.GetArrayElementAtIndex(index);
            return currentEntry.FindPropertyRelative("first").stringValue;
        }

        static bool HasOverridenPropertiesChanged(SerializedObject material, List<string> overridenProperties)
        {
            var properties = GetWatchedProperties(material);
            if (properties.arraySize != overridenProperties.Count)
                return true;

            for (int index = 0; index < properties.arraySize; ++index)
            {
                var name = GetPropertyName(properties, index);
                if (overridenProperties[index] != name)
                    return true;
            }
            return false;
        }

        private static void GetOverridenProperties(SerializedObject material, List<string> overridenProperties)
        {
            overridenProperties.Clear();
            var properties = GetWatchedProperties(material);
            for (int index = 0; index < properties.arraySize; ++index)
            {
                var name = GetPropertyName(properties, index);
                overridenProperties.Add(name);
            }
        }

        public override void SetupMaterial(VFXAbstractComposedParticleOutput parent, Material material)
        {
            var sg = GetOrRefreshShaderGraphObject();
            if (sg != null)
            {
                materialSettings.ApplyToMaterial(material);
                VFXLibrary.currentSRPBinder.SetupMaterial(material, parent.hasMotionVector, parent.hasShadowCasting, sg);
                m_MaterialEditorDirty = true;
            }
        }

        public override void InitInspectorGUI(VFXAbstractComposedParticleOutput parent)
        {
            base.InitInspectorGUI(parent);
            var material = parent.FindMaterial();
            if (material)
            {
                m_MaterialEditor = (MaterialEditor)Editor.CreateEditor(material);
                m_MaterialEditor.firstInspectedEditor = true;
            }
        }

        public override void DoInspectorGUI(VFXAbstractComposedParticleOutput parent, out VFXModel.InvalidationCause? invalidationCause)
        {
            base.DoInspectorGUI(parent, out invalidationCause);

            if (m_MaterialEditorDirty)
            {
                ReleaseInspectorGUI(parent);
                InitInspectorGUI(parent);
                m_MaterialEditorDirty = false;
            }

            if (m_MaterialEditor == null)
                return;

            if (m_MaterialEditor.target == null || (m_MaterialEditor.target as Material)?.shader == null)
                return; //Material has been destroyed

            var isVariant = ((Material)m_MaterialEditor.target).isVariant;
            if (!isVariant)
            {
                EditorGUILayout.HelpBox("Unable to modify VFX Material during Runtime Mode to prevent loss of material override settings.\nTo adjust material settings, please toggle off Runtime Mode in Compile dropdown and return to Edit mode.", MessageType.Warning);
            }

            var actualShaderGraph = GetOrRefreshShaderGraphObject();
            var previousBlendMode = VFXLibrary.currentSRPBinder.GetBlendModeFromMaterial(actualShaderGraph, materialSettings);
            var materialChanged = false;
            using (new EditorGUI.DisabledScope(!isVariant))
            {
                EditorGUI.BeginChangeCheck();
                m_MaterialEditor.OnInspectorGUI();
                materialChanged = EditorGUI.EndChangeCheck();

                if (isVariant)
                {
                    //Automatically detect Revert Override which can be applied outside inspector callback
                    if (m_OverridenPropertiesCache == null)
                    {
                        m_OverridenPropertiesCache = new List<string>();
                        GetOverridenProperties(m_MaterialEditor.serializedObject, m_OverridenPropertiesCache);
                    }

                    if (!materialChanged && HasOverridenPropertiesChanged(m_MaterialEditor.serializedObject, m_OverridenPropertiesCache))
                    {
                        GetOverridenProperties(m_MaterialEditor.serializedObject, m_OverridenPropertiesCache);
                        materialChanged = true;
                    }
                }
            }

            var currentBlendMode = VFXLibrary.currentSRPBinder.GetBlendModeFromMaterial(actualShaderGraph, materialSettings);
            if (materialChanged)
            {
                var material = parent.FindMaterial();
                if (material != null)
                    materialSettings.SyncFromMaterial(material);

                // If the blend mode is changed to one that may require sorting (Auto), we require a full recompilation.
                if (previousBlendMode != currentBlendMode)
                    invalidationCause = VFXModel.InvalidationCause.kSettingChanged;
                else
                    invalidationCause = VFXModel.InvalidationCause.kMaterialChanged;
            }

            //Warning & Info display
            var materialShadowOverride = VFXLibrary.currentSRPBinder.TryGetCastShadowFromMaterial(actualShaderGraph, materialSettings, out var castShadow);
            var materialSortingPriorityOverride = VFXLibrary.currentSRPBinder.TryGetQueueOffset(actualShaderGraph, materialSettings, out var queueOffset) && parent.subOutput.supportsSortingPriority;

            // Indicate material override from shaderGraph which could be hiding output properties.
            if (materialShadowOverride || materialSortingPriorityOverride)
            {
                var msg = new StringBuilder("The ShaderGraph material is overriding some settings:");
                if (materialShadowOverride)
                    msg.AppendFormat("\n - Cast Shadow = {0}", castShadow ? "true" : "false");
                if (materialSortingPriorityOverride)
                    msg.AppendFormat("\n - Sorting Priority = {0}", queueOffset);
                EditorGUILayout.HelpBox(msg.ToString(), MessageType.Info);
            }

            // Indicate caution to the user if transparent motion vectors are disabled and motion vectors are enabled.
            if (parent.hasMotionVector &&
                (currentBlendMode != VFXAbstractRenderedOutput.BlendMode.Opaque &&
                 !VFXLibrary.currentSRPBinder.TransparentMotionVectorEnabled(m_MaterialEditor.target as Material)))
            {
                EditorGUILayout.HelpBox("Transparent Motion Vectors pass is disabled. Consider disabling Generate Motion Vector to improve performance.", MessageType.Warning);
            }

            if (!VFXLibrary.currentSRPBinder.AllowMaterialOverride(shaderGraph))
            {
                EditorGUILayout.HelpBox("Surface Option overriding is disabled. For shaders that allow this behavior, go to the ShaderGraph Graph Settings and select Allow Material Override.", MessageType.Warning);
            }

        }

        public override void ReleaseInspectorGUI(VFXAbstractComposedParticleOutput parent)
        {
            base.ReleaseInspectorGUI(parent);
            UnityEngine.Object.DestroyImmediate(m_MaterialEditor);
            m_OverridenPropertiesCache = null;
        }
        #endregion

        public ShaderGraphVfxAsset GetShaderGraph()
        {
            var actualShaderGraph = GetOrRefreshShaderGraphObject();
            return actualShaderGraph;
        }
    }
}
