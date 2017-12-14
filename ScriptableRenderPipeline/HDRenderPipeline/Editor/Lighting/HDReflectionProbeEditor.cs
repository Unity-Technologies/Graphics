using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.SceneManagement;

namespace UnityEditor.Experimental.Rendering
{
    [CustomEditorForRenderPipeline(typeof(ReflectionProbe), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    partial class HDReflectionProbeEditor : Editor
    {
        SerializedReflectionProbe m_SerializedReflectionProbe;
        SerializedObject m_AdditionalDataSerializedObject;
        UIState m_UIState = new UIState();

        Matrix4x4 m_OldLocalSpace = Matrix4x4.identity;

        void OnEnable()
        {
            var additionalData = CoreEditorUtils.GetAdditionalData<HDAdditionalReflectionData>(targets);
            m_AdditionalDataSerializedObject = new SerializedObject(additionalData);
            m_SerializedReflectionProbe = new SerializedReflectionProbe(serializedObject, m_AdditionalDataSerializedObject);
            m_UIState.Reset(
                this,
                Repaint,
                m_SerializedReflectionProbe);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_AdditionalDataSerializedObject.Update();

            var s = m_UIState;
            var p = m_SerializedReflectionProbe;

            k_PrimarySection.Draw(s, p, this);
            k_InfluenceVolumeSection.Draw(s, p, this);
            k_SeparateProjectionVolumeSection.Draw(s, p, this);
            k_CaptureSection.Draw(s, p, this);
            k_AdditionalSection.Draw(s, p, this);
            k_BakingActions.Draw(s, p, this);

            PerformOperations(s, p, this);

            m_AdditionalDataSerializedObject.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();

            HideAdditionalComponents(false);
        }

        void PerformOperations(UIState s, SerializedReflectionProbe p, HDReflectionProbeEditor o)
        {
            if (s.HasAndClearOperation(Operation.UpdateOldLocalSpace))
                UpdateOldLocalSpace();
        }

        void HideAdditionalComponents(bool visible)
        {
            var adds = CoreEditorUtils.GetAdditionalData<HDAdditionalReflectionData>(targets);
            for (var i = 0 ; i < targets.Length; ++i)
            {
                var target = targets[i];
                var addData = adds[i];
                var p = (ReflectionProbe)target;
                var meshRenderer = p.GetComponent<MeshRenderer>();
                var meshFilter = p.GetComponent<MeshFilter>();

                addData.hideFlags = visible ? HideFlags.None : HideFlags.HideInInspector;
                meshRenderer.hideFlags = visible ? HideFlags.None : HideFlags.HideInInspector;
                meshFilter.hideFlags = visible ? HideFlags.None : HideFlags.HideInInspector;
            }
        }

        void UpdateOldLocalSpace()
        {
            m_OldLocalSpace = GetLocalSpace((ReflectionProbe)target);
        }

        static Matrix4x4 GetLocalSpace(ReflectionProbe probe)
        {
            Vector3 t = probe.transform.position;
            return Matrix4x4.TRS(t, GetLocalSpaceRotation(probe), Vector3.one);
        }

        static Quaternion GetLocalSpaceRotation(ReflectionProbe probe)
        {
            bool supportsRotation = (SupportedRenderingFeatures.active.reflectionProbeSupportFlags & SupportedRenderingFeatures.ReflectionProbeSupportFlags.Rotation) != 0;
            if (supportsRotation)
                return probe.transform.rotation;
            else
                return Quaternion.identity;
        }

        // Ensures that probe's AABB encapsulates probe's position
        // Returns true, if center or size was modified
        static bool ValidateAABB(ReflectionProbe p, ref Vector3 center, ref Vector3 size)
        {
            var localSpace = GetLocalSpace(p);
            var localTransformPosition = localSpace.inverse.MultiplyPoint3x4(p.transform.position);

            var b = new Bounds(center, size);

            if (b.Contains(localTransformPosition))
                return false;

            b.Encapsulate(localTransformPosition);

            center = b.center;
            size = b.size;
            return true;
        }

        static void BakeCustomReflectionProbe(ReflectionProbe probe, bool usePreviousAssetPath, bool custom)
        {
            if (!custom && probe.bakedTexture != null)
                probe.customBakedTexture = probe.bakedTexture;

            string path = "";
            if (usePreviousAssetPath)
                path = AssetDatabase.GetAssetPath(probe.customBakedTexture);

            string targetExtension = probe.hdr ? "exr" : "png";
            if (string.IsNullOrEmpty(path) || Path.GetExtension(path) != "." + targetExtension)
            {
                // We use the path of the active scene as the target path
                var targetPath = SceneManager.GetActiveScene().path;
                targetPath = Path.Combine(Path.GetDirectoryName(targetPath), Path.GetFileNameWithoutExtension(targetPath));
                if (string.IsNullOrEmpty(targetPath))
                    targetPath = "Assets";
                else if (Directory.Exists(targetPath) == false)
                    Directory.CreateDirectory(targetPath);

                string fileName = probe.name + (probe.hdr ? "-reflectionHDR" : "-reflection") + "." + targetExtension;
                fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GenerateUniqueAssetPath(Path.Combine(targetPath, fileName)));

                path = EditorUtility.SaveFilePanelInProject("Save reflection probe's cubemap.", fileName, targetExtension, "", targetPath);
                if (string.IsNullOrEmpty(path))
                    return;

                ReflectionProbe collidingProbe;
                if (IsCollidingWithOtherProbes(path, probe, out collidingProbe))
                {
                    if (!EditorUtility.DisplayDialog("Cubemap is used by other reflection probe",
                        string.Format("'{0}' path is used by the game object '{1}', do you really want to overwrite it?",
                            path, collidingProbe.name), "Yes", "No"))
                    {
                        return;
                    }
                }
            }

            EditorUtility.DisplayProgressBar("Reflection Probes", "Baking " + path, 0.5f);
            if (!UnityEditor.Lightmapping.BakeReflectionProbe(probe, path))
                Debug.LogError("Failed to bake reflection probe to " + path);
            EditorUtility.ClearProgressBar();
        }

        static bool IsCollidingWithOtherProbes(string targetPath, ReflectionProbe targetProbe, out ReflectionProbe collidingProbe)
        {
            ReflectionProbe[] probes = FindObjectsOfType<ReflectionProbe>().ToArray();
            collidingProbe = null;
            foreach (var probe in probes)
            {
                if (probe == targetProbe || probe.customBakedTexture == null)
                    continue;
                string path = AssetDatabase.GetAssetPath(probe.customBakedTexture);
                if (path == targetPath)
                {
                    collidingProbe = probe;
                    return true;
                }
            }
            return false;
        }

        static MethodInfo k_Lightmapping_BakeReflectionProbeSnapshot = typeof(UnityEditor.Lightmapping).GetMethod("BakeReflectionProbeSnapshot", BindingFlags.Static | BindingFlags.NonPublic);
        static bool BakeReflectionProbeSnapshot(ReflectionProbe probe)
        {
            return (bool)k_Lightmapping_BakeReflectionProbeSnapshot.Invoke(null, new object[] { probe });
        }

        static MethodInfo k_Lightmapping_BakeAllReflectionProbesSnapshots = typeof(UnityEditor.Lightmapping).GetMethod("BakeAllReflectionProbesSnapshots", BindingFlags.Static | BindingFlags.NonPublic);
        static bool BakeAllReflectionProbesSnapshots()
        {
            return (bool)k_Lightmapping_BakeAllReflectionProbesSnapshots.Invoke(null, new object[0]);
        }
    }
}
