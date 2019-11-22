using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditorForRenderPipeline(typeof(ReflectionProbe), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    sealed partial class HDReflectionProbeEditor : HDProbeEditor<HDProbeSettingsProvider, SerializedHDReflectionProbe>
    {
        #region Context Menu
        [MenuItem("CONTEXT/ReflectionProbe/Remove Component", false, 0)]
        static void RemoveReflectionProbe(MenuCommand menuCommand)
        {
            GameObject go = ((ReflectionProbe)menuCommand.context).gameObject;

            Assert.IsNotNull(go);

            Undo.SetCurrentGroupName("Remove HD Reflection Probe");
            Undo.DestroyObjectImmediate(go.GetComponent<ReflectionProbe>());
            Undo.DestroyObjectImmediate(go.GetComponent<HDAdditionalReflectionData>());
        }

        [MenuItem("CONTEXT/ReflectionProbe/Reset", false, 0)]
        static void ResetReflectionProbe(MenuCommand menuCommand)
        {
            GameObject go = ((ReflectionProbe)menuCommand.context).gameObject;

            Assert.IsNotNull(go);

            ReflectionProbe reflectionProbe = go.GetComponent<ReflectionProbe>();
            HDAdditionalReflectionData reflectionProbeAdditionalData = go.GetComponent<HDAdditionalReflectionData>();

            Assert.IsNotNull(reflectionProbe);
            Assert.IsNotNull(reflectionProbeAdditionalData);

            Undo.SetCurrentGroupName("Reset HD Reflection Probe");
            Undo.RecordObjects(new UnityEngine.Object[] { reflectionProbe, reflectionProbeAdditionalData }, "Reset HD Reflection Probe");
            reflectionProbe.Reset();
            // To avoid duplicating init code we copy default settings to Reset additional data
            // Note: we can't call this code inside the HDAdditionalReflectionData, thus why we don't wrap it in Reset() function
            EditorUtility.CopySerialized(HDUtils.s_DefaultHDAdditionalReflectionData, reflectionProbeAdditionalData);
        }
        #endregion

        protected override void OnEnable()
        {
            base.OnEnable();
            InitializeTargetProbe();
        }

        internal override HDProbe GetTarget(UnityEngine.Object editorTarget)
            => ((ReflectionProbe)editorTarget).GetComponent<HDAdditionalReflectionData>();
        protected override SerializedHDReflectionProbe NewSerializedObject(SerializedObject so)
        {
            var additionalData = CoreEditorUtils.GetAdditionalData<HDAdditionalReflectionData>(so.targetObjects);
            var addSO = new SerializedObject(additionalData);
            return new SerializedHDReflectionProbe(so, addSO);
        }
    }

    struct HDProbeSettingsProvider : HDProbeUI.IProbeUISettingsProvider, InfluenceVolumeUI.IInfluenceUISettingsProvider
    {
        bool InfluenceVolumeUI.IInfluenceUISettingsProvider.drawOffset => true;
        bool InfluenceVolumeUI.IInfluenceUISettingsProvider.drawNormal => true;
        bool InfluenceVolumeUI.IInfluenceUISettingsProvider.drawFace => true;

        ProbeSettingsOverride HDProbeUI.IProbeUISettingsProvider.displayedCaptureSettings => new ProbeSettingsOverride
        {
            probe = ProbeSettingsFields.proxyCapturePositionProxySpace,
            camera = new CameraSettingsOverride
            {
                camera = (CameraSettingsFields)(-1) & ~(
                    CameraSettingsFields.frustumFieldOfView
                    | CameraSettingsFields.frustumAspect
                    | CameraSettingsFields.flipYMode
                    | CameraSettingsFields.cullingInvertFaceCulling
                    | CameraSettingsFields.frustumMode
                    | CameraSettingsFields.frustumProjectionMatrix
                )
            }
        };

        public ProbeSettingsOverride displayedAdvancedCaptureSettings => new ProbeSettingsOverride
        {
            probe = ProbeSettingsFields.lightingRangeCompression
        };

        ProbeSettingsOverride HDProbeUI.IProbeUISettingsProvider.displayedCustomSettings => new ProbeSettingsOverride
        {
            probe = ProbeSettingsFields.lightingLightLayer
                | ProbeSettingsFields.lightingMultiplier
                | ProbeSettingsFields.lightingWeight
                | ProbeSettingsFields.lightingFadeDistance,
            camera = new CameraSettingsOverride
            {
                camera = CameraSettingsFields.none
            }
        };

        Type HDProbeUI.IProbeUISettingsProvider.customTextureType => typeof(Cubemap);
        static readonly HDProbeUI.ToolBar[] k_ToolBars =
        {
            HDProbeUI.ToolBar.InfluenceShape | HDProbeUI.ToolBar.NormalBlend | HDProbeUI.ToolBar.Blend,
            HDProbeUI.ToolBar.CapturePosition,
            HDProbeUI.ToolBar.ShowChromeGizmo
        };
        HDProbeUI.ToolBar[] HDProbeUI.IProbeUISettingsProvider.toolbars => k_ToolBars;

        static Dictionary<KeyCode, HDProbeUI.ToolBar> k_ToolbarShortCutKey = new Dictionary<KeyCode, HDProbeUI.ToolBar>
        {
            { KeyCode.Alpha1, HDProbeUI.ToolBar.InfluenceShape },
            { KeyCode.Alpha2, HDProbeUI.ToolBar.Blend },
            { KeyCode.Alpha3, HDProbeUI.ToolBar.NormalBlend },
            { KeyCode.Alpha4, HDProbeUI.ToolBar.CapturePosition }
        };
        Dictionary<KeyCode, HDProbeUI.ToolBar> HDProbeUI.IProbeUISettingsProvider.shortcuts => k_ToolbarShortCutKey;
    }
}
