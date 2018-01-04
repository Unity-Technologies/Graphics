using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityEditor.Experimental.Rendering
{
    [CustomEditorForRenderPipeline(typeof(ReflectionProbe), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    partial class HDReflectionProbeEditor : Editor
    {
        static Dictionary<ReflectionProbe, HDReflectionProbeEditor> s_ReflectionProbeEditors = new Dictionary<ReflectionProbe, HDReflectionProbeEditor>();

        static HDReflectionProbeEditor GetEditorFor(ReflectionProbe p)
        {
            HDReflectionProbeEditor e;
            if (s_ReflectionProbeEditors.TryGetValue(p, out e) 
                && e != null 
                && !e.Equals(null)
                && ArrayUtility.IndexOf(e.targets, p) != -1)
                return e;

            return null;
        }

        SerializedReflectionProbe m_SerializedReflectionProbe;
        SerializedObject m_AdditionalDataSerializedObject;
        UIState m_UIState = new UIState();

        int m_PositionHash = 0;

        public bool sceneViewEditing
        {
            get { return IsReflectionProbeEditMode(EditMode.editMode) && EditMode.IsOwner(this); }
        }

        void OnEnable()
        {
            var additionalData = CoreEditorUtils.GetAdditionalData<HDAdditionalReflectionData>(targets);
            m_AdditionalDataSerializedObject = new SerializedObject(additionalData);
            m_SerializedReflectionProbe = new SerializedReflectionProbe(serializedObject, m_AdditionalDataSerializedObject);
            m_UIState.Reset(
                this,
                Repaint,
                m_SerializedReflectionProbe);

            foreach (var t in targets)
            {
                var p = (ReflectionProbe)t;
                s_ReflectionProbeEditors[p] = this;
            }

            InitializeAllTargetProbes();
            ChangeVisibilityOfAllTargets(true);
        }

        void OnDisable()
        {
            ChangeVisibilityOfAllTargets(false);
        }

        public override void OnInspectorGUI()
        {
            //InspectColorsGUI();

            serializedObject.Update();
            m_AdditionalDataSerializedObject.Update();

            var s = m_UIState;
            var p = m_SerializedReflectionProbe;
            // Set the legacy blend distance to 0 so the legacy culling system use the probe extent
            p.legacyBlendDistance.floatValue = 0;

            k_PrimarySection.Draw(s, p, this);
            k_InfluenceVolumeSection.Draw(s, p, this);
            //k_InfluenceNormalVolumeSection.Draw(s, p, this);
            k_SeparateProjectionVolumeSection.Draw(s, p, this);
            k_CaptureSection.Draw(s, p, this);
            k_AdditionalSection.Draw(s, p, this);
            k_BakingActions.Draw(s, p, this);

            PerformOperations(s, p, this);

            m_AdditionalDataSerializedObject.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();

            HideAdditionalComponents(false);

            DoShortcutKey(p, this);
        }

        static void PerformOperations(UIState s, SerializedReflectionProbe p, HDReflectionProbeEditor o)
        {
            
        }

        void HideAdditionalComponents(bool visible)
        {
            var adds = CoreEditorUtils.GetAdditionalData<HDAdditionalReflectionData>(targets);
            var flags = visible ? HideFlags.None : HideFlags.HideInInspector;
            for (var i = 0 ; i < targets.Length; ++i)
            {
                var target = targets[i];
                var addData = adds[i];
                var p = (ReflectionProbe)target;
                var meshRenderer = p.GetComponent<MeshRenderer>();
                var meshFilter = p.GetComponent<MeshFilter>();

                addData.hideFlags = flags;
                meshRenderer.hideFlags = flags;
                meshFilter.hideFlags = flags;
            }
        }

        void BakeRealtimeProbeIfPositionChanged(UIState s, SerializedReflectionProbe sp, Editor o)
        {
            if (Application.isPlaying
                || ((ReflectionProbeMode)sp.mode.intValue) != ReflectionProbeMode.Realtime)
            {
                m_PositionHash = 0;
                return;
            }

            var hash = 0;
            for (var i = 0; i < sp.so.targetObjects.Length; i++)
            {
                var p = (ReflectionProbe)sp.so.targetObjects[i];
                var tr = p.GetComponent<Transform>();
                hash ^= tr.position.GetHashCode();
            }

            if (hash != m_PositionHash)
            {
                m_PositionHash = hash;
                for (var i = 0; i < sp.so.targetObjects.Length; i++)
                {
                    var p = (ReflectionProbe)sp.so.targetObjects[i];
                    p.RenderProbe();
                }
            }
        }

        static Matrix4x4 GetLocalSpace(ReflectionProbe probe)
        {
            var t = probe.transform.position;
            return Matrix4x4.TRS(t, GetLocalSpaceRotation(probe), Vector3.one);
        }

        static Quaternion GetLocalSpaceRotation(ReflectionProbe probe)
        {
            var supportsRotation = (SupportedRenderingFeatures.active.reflectionProbeSupportFlags & SupportedRenderingFeatures.ReflectionProbeSupportFlags.Rotation) != 0;
            return supportsRotation 
                ? probe.transform.rotation 
                : Quaternion.identity;
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

        static bool IsReflectionProbeEditMode(EditMode.SceneViewEditMode editMode)
        {
            return editMode == EditMode.SceneViewEditMode.ReflectionProbeBox || editMode == EditMode.SceneViewEditMode.Collider || editMode == EditMode.SceneViewEditMode.GridBox ||
                editMode == EditMode.SceneViewEditMode.ReflectionProbeOrigin;
        }

        static void InspectColorsGUI()
        {
            EditorGUILayout.LabelField("Color Theme", EditorStyles.largeLabel);
            k_GizmoThemeColorExtent = EditorGUILayout.ColorField("Extent", k_GizmoThemeColorExtent);
            k_GizmoThemeColorExtentFace = EditorGUILayout.ColorField("Extent Face", k_GizmoThemeColorExtentFace);
            k_GizmoThemeColorInfluenceBlend = EditorGUILayout.ColorField("Influence Blend", k_GizmoThemeColorInfluenceBlend);
            k_GizmoThemeColorInfluenceBlendFace = EditorGUILayout.ColorField("Influence Blend Face", k_GizmoThemeColorInfluenceBlendFace);
            k_GizmoThemeColorInfluenceNormalBlend = EditorGUILayout.ColorField("Influence Normal Blend", k_GizmoThemeColorInfluenceNormalBlend);
            k_GizmoThemeColorInfluenceNormalBlendFace = EditorGUILayout.ColorField("Influence Normal Blend Face", k_GizmoThemeColorInfluenceNormalBlendFace);
            k_GizmoThemeColorProjection = EditorGUILayout.ColorField("Projection", k_GizmoThemeColorProjection);
            k_GizmoThemeColorProjectionFace = EditorGUILayout.ColorField("Projection Face", k_GizmoThemeColorProjectionFace);
            k_GizmoThemeColorDisabled = EditorGUILayout.ColorField("Disabled", k_GizmoThemeColorDisabled);
            k_GizmoThemeColorDisabledFace = EditorGUILayout.ColorField("Disabled Face", k_GizmoThemeColorDisabledFace);
            EditorGUILayout.Space();
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

        static void ResetProbeSceneTextureInMaterial(ReflectionProbe p)
        {
            var renderer = p.GetComponent<Renderer>();
            renderer.sharedMaterial.SetTexture(_Cubemap, p.texture);
        }

        static void ApplyConstraintsOnTargets(UIState s, SerializedReflectionProbe sp, Editor o)
        {
            switch ((ReflectionInfluenceShape)sp.influenceShape.enumValueIndex)
            {
                case ReflectionInfluenceShape.Box:
                {
                    var maxBlendDistance = CalculateBoxMaxBlendDistance(s, sp, o);
                    sp.targetData.blendDistancePositive = Vector3.Min(sp.targetData.blendDistancePositive, maxBlendDistance);
                    sp.targetData.blendDistanceNegative = Vector3.Min(sp.targetData.blendDistanceNegative, maxBlendDistance);
                    sp.targetData.blendNormalDistancePositive = Vector3.Min(sp.targetData.blendNormalDistancePositive, maxBlendDistance);
                    sp.targetData.blendNormalDistanceNegative = Vector3.Min(sp.targetData.blendNormalDistanceNegative, maxBlendDistance);
                    break;
                }
                case ReflectionInfluenceShape.Sphere:
                {
                    var maxBlendDistance = Vector3.one * CalculateSphereMaxBlendDistance(s, sp, o);
                    sp.targetData.blendDistancePositive = Vector3.Min(sp.targetData.blendDistancePositive, maxBlendDistance);
                    sp.targetData.blendDistanceNegative = Vector3.Min(sp.targetData.blendDistanceNegative, maxBlendDistance);
                    sp.targetData.blendNormalDistancePositive = Vector3.Min(sp.targetData.blendNormalDistancePositive, maxBlendDistance);
                    sp.targetData.blendNormalDistanceNegative = Vector3.Min(sp.targetData.blendNormalDistanceNegative, maxBlendDistance);
                    break;
                }
            }
        }

        static readonly KeyCode[] k_ShortCutKeys =
        {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
        };
        static void DoShortcutKey(SerializedReflectionProbe p, Editor o)
        {
            var evt = Event.current;
            if (evt.type != EventType.KeyDown || !evt.shift)
                return;

            for (var i = 0; i < k_ShortCutKeys.Length; ++i)
            {
                if (evt.keyCode == k_ShortCutKeys[i])
                {
                    var mode = EditMode.editMode == k_Toolbar_SceneViewEditModes[i]
                        ? EditMode.SceneViewEditMode.None
                        : k_Toolbar_SceneViewEditModes[i];
                    EditMode.ChangeEditMode(mode, GetBoundsGetter(p)(), o);
                    evt.Use();
                    break;
                }
            }
        }
    }
}
