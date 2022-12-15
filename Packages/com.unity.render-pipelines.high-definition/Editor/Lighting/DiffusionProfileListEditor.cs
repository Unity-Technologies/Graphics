using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(DiffusionProfileList))]
    sealed class DiffusionProfileListEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_DiffusionProfiles;

        DiffusionProfileSettingsListUI listUI = new DiffusionProfileSettingsListUI();

        static GUIContent m_DiffusionProfileLabel = new GUIContent("Diffusion Profile List", "Diffusion Profile List from current HDRenderPipeline Asset");

        public override void OnEnable()
        {
            var o = new PropertyFetcher<DiffusionProfileList>(serializedObject);
            m_DiffusionProfiles = Unpack(o.Find(x => x.diffusionProfiles));
        }

        public override void OnInspectorGUI()
        {
            listUI.drawElement = DrawDiffusionProfileElement;

            listUI.OnGUI(m_DiffusionProfiles.value);

            // If the volume is null it means that we're editing the component from the asset
            // So we can't access the bounds of the volume to fill diffusion profiles used in the volume
            if (volume != null && !volume.isGlobal)
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
            if (volume.isGlobal)
                return;

            var volumeCollider = volume.GetComponent<Collider>();

            // Get all mesh renderers that are within the current volume
            var diffusionProfiles = new List<DiffusionProfileSettings>();
            foreach (var meshRenderer in Object.FindObjectsOfType<MeshRenderer>())
            {
                var colliders = Physics.OverlapBox(meshRenderer.bounds.center, meshRenderer.bounds.size / 2);
                if (colliders.Contains(volumeCollider))
                {
                    foreach (var mat in meshRenderer.sharedMaterials)
                    {
                        foreach (var nameID in HDMaterial.GetShaderDiffusionProfileProperties(mat.shader))
                        {
                            if (profiles.Count == DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1)
                                break;

                            var profile = HDMaterial.GetDiffusionProfileAsset(mat, nameID);
                            if (profile != null)
                                profiles.Add(profile);
                        }
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
    }
}
