
using System;
using UnityEngine;
using SerializedMesh = UnityEditor.ShaderGraph.Utils.SerializableMesh;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Serializable]
    public class SGMainPreviewModel
    {
        string m_GraphModelGuid;
        string ScaleUserPrefKey => m_GraphModelGuid + "." + ChangePreviewZoomCommand.UserPrefsKey;
        string RotationUserPrefKey => m_GraphModelGuid + "." + ChangePreviewRotationCommand.UserPrefsKey;
        string MeshUserPrefKey => m_GraphModelGuid + "." + ChangePreviewMeshCommand.UserPrefsKey;

        // We don't serialize these fields, we just set them for easy access by other systems...
        [NonSerialized] public Vector2 mainPreviewSize = new(200, 200);
        [NonSerialized] public bool lockMainPreviewRotation = false;

        public SGMainPreviewModel(string graphAssetGuid)
        {
            // Get graph asset guid so we can search for user prefs attached to this asset (if any)
            m_GraphModelGuid = graphAssetGuid;

            // Get scale from prefs if present
            scale = EditorPrefs.GetFloat(ScaleUserPrefKey, 1.0f);

            // Get rotation from prefs if present
            var rotationJson = EditorPrefs.GetString(RotationUserPrefKey, string.Empty);
            if (rotationJson != string.Empty)
                m_Rotation = StringToQuaternion(rotationJson);

            // Get mesh from prefs if present
            var meshJson = EditorPrefs.GetString(MeshUserPrefKey, string.Empty);
            if (meshJson != string.Empty)
                EditorJsonUtility.FromJsonOverwrite(meshJson, serializedMesh);
        }

        public static Quaternion StringToQuaternion(string sQuaternion)
        {
            // Remove the parentheses
            if (sQuaternion.StartsWith("(") && sQuaternion.EndsWith(")"))
            {
                sQuaternion = sQuaternion.Substring(1, sQuaternion.Length - 2);
            }

            // split the items
            string[] sArray = sQuaternion.Split(',');

            // store as a Vector3
            Quaternion result = new Quaternion(
                float.Parse(sArray[0]),
                float.Parse(sArray[1]),
                float.Parse(sArray[2]),
                float.Parse(sArray[3]));

            return result;
        }

        [SerializeField]
        private SerializedMesh serializedMesh = new();

        [NonSerialized]
        Quaternion m_Rotation = Quaternion.identity;

        public Quaternion rotation
        {
            get => m_Rotation;
            set
            {
                m_Rotation = value;
                EditorPrefs.SetString(RotationUserPrefKey, rotation.ToString());
            }
        }


        [NonSerialized] float m_Scale = 1.0f;

        public float scale
        {
            get => m_Scale;
            set
            {
                m_Scale = value;
                EditorPrefs.SetFloat(ScaleUserPrefKey, m_Scale);
            }
        }

        public Mesh mesh
        {
            get => serializedMesh.mesh;
            set
            {
                serializedMesh.mesh = value;
                EditorPrefs.SetString(MeshUserPrefKey, EditorJsonUtility.ToJson(serializedMesh));
            }
        }
    }
}
