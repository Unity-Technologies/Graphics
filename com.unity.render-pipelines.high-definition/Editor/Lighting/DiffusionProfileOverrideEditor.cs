using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Linq;
using System;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [VolumeComponentEditor(typeof(DiffusionProfileOverride))]
    sealed class DiffusionProfileOverrideEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_DiffusionProfiles;
        Volume                  m_Volume;

        DiffusionProfileSettingsListUI      listUI = new DiffusionProfileSettingsListUI();

        static GUIContent m_DiffusionProfileLabel = new GUIContent("Diffusion Profile List", "Diffusion Profile List from current HDRenderPipeline Asset");

        public override void OnEnable()
        {
            var o = new PropertyFetcher<DiffusionProfileOverride>(serializedObject);

            m_Volume = (m_Inspector.target as Volume);
            m_DiffusionProfiles = Unpack(o.Find(x => x.diffusionProfiles));
            var hdAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
        }

        public override void OnInspectorGUI()
        {
            listUI.drawElement = DrawDiffusionProfileElement;

            listUI.OnGUI(m_DiffusionProfiles.value);

            // If the volume is null it means that we're editing the component from the asset
            // So we can't access the bounds of the volume to fill diffusion profiles used in the volume
            if (m_Volume != null && !m_Volume.isGlobal)
            {
                if (GUILayout.Button("Fill Profile List With Scene Materials"))
                    FillProfileListWithScene();
            }
        }

        void DrawDiffusionProfileElement(SerializedProperty element, Rect rect, int index)
        {
            EditorGUI.BeginDisabledGroup(!m_DiffusionProfiles.overrideState.boolValue);
            EditorGUI.ObjectField(rect, element, new GUIContent("Profile " + index));
            EditorGUI.EndDisabledGroup();
        }

        void FillProfileListWithScene()
        {
            var profiles = new HashSet<DiffusionProfileSettings>();
            if (m_Volume.isGlobal)
                return;

            var volumeCollider = m_Volume.GetComponent<Collider>();

            // Get all mesh renderers that are within the current volume
            var diffusionProfiles = new List<DiffusionProfileSettings>();
            foreach (var meshRenderer in Object.FindObjectsOfType<MeshRenderer>())
            {
                var colliders = Physics.OverlapBox(meshRenderer.bounds.center, meshRenderer.bounds.size / 2);
                if (colliders.Contains(volumeCollider))
                {
                    foreach (var mat in meshRenderer.sharedMaterials)
                    {
                        var profile = GetMaterialDiffusionProfile(mat);
                        
                        if (profiles.Count == DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1)
                            break ;

                        if (profile != null)
                            profiles.Add(profile);
                    }
                }
            }

            m_DiffusionProfiles.value.arraySize = profiles.Count;
            int i = 0;
            foreach (var profile in profiles)
            {
                m_DiffusionProfiles.value.GetArrayElementAtIndex(i).objectReferenceValue = profile;
                i++;
            }
        }

        DiffusionProfileSettings GetMaterialDiffusionProfile(Material mat)
        {
            if (!mat.HasProperty(HDShaderIDs._DiffusionProfileAsset))
                return null;
            
            string guid = HDUtils.ConvertVector4ToGUID(mat.GetVector(HDShaderIDs._DiffusionProfileAsset));
            
            if (String.IsNullOrEmpty(guid))
                return null;
            
            return AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}
