using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    // This class only purpose is to be used as a sub-asset to a material and store references to other assets.
    // The goal is to be able to export the material as a package and not miss those referenced assets.
    class MaterialExternalReferences : ScriptableObject
    {
        [SerializeField]
        DiffusionProfileSettings[] m_DiffusionProfileReferences = new DiffusionProfileSettings[0];
        [SerializeField]
        Material[] m_MaterialReferences = new Material[0];

        internal Material[] materialReferences => m_MaterialReferences;

        public void SetDiffusionProfileReference(int index, DiffusionProfileSettings profile)
        {
            if (index >= m_DiffusionProfileReferences.Length)
            {
                var newList = new DiffusionProfileSettings[index + 1];
                for (int i = 0; i < m_DiffusionProfileReferences.Length; ++i)
                    newList[i] = m_DiffusionProfileReferences[i];

                m_DiffusionProfileReferences = newList;
            }

            m_DiffusionProfileReferences[index] = profile;
            EditorUtility.SetDirty(this);
        }

        public void SetMaterialReference(int index, Material mat)
        {
            if (index >= m_MaterialReferences.Length)
            {
                var newList = new Material[index + 1];
                for (int i = 0; i < m_MaterialReferences.Length; ++i)
                    newList[i] = m_MaterialReferences[i];

                m_MaterialReferences = newList;
            }

            m_MaterialReferences[index] = mat;
            EditorUtility.SetDirty(this);
        }

        public static MaterialExternalReferences GetMaterialExternalReferences(Material material)
        {
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(material));
            MaterialExternalReferences matExternalRefs = null;
            foreach (var subAsset in subAssets)
            {
                if (subAsset.GetType() == typeof(MaterialExternalReferences))
                {
                    matExternalRefs = subAsset as MaterialExternalReferences;
                    break;
                }
            }

            if (matExternalRefs == null)
            {
                matExternalRefs = CreateInstance<MaterialExternalReferences>();
                matExternalRefs.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;
                AssetDatabase.AddObjectToAsset(matExternalRefs, material);
                EditorUtility.SetDirty(matExternalRefs);
                EditorUtility.SetDirty(material);
            }

            return matExternalRefs;
        }
    }
}
