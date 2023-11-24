using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    /// <summary>
    /// Camera parameter binding helper class.
    /// </summary>
    [VFXBinder("HDRP/HDRP Camera")]
    public class HDRPCameraBinder : VFXBinderBase
    {
        /// <summary>
        /// Camera HDRP additional data.
        /// </summary>
        public HDAdditionalCameraData AdditionalData;
        Camera m_Camera;

        [VFXPropertyBinding("UnityEditor.VFX.CameraType"), SerializeField]
        ExposedProperty CameraProperty = "Camera";

        RTHandle m_Texture;

        ExposedProperty m_Position;
        ExposedProperty m_Angles;
        ExposedProperty m_Scale;
        ExposedProperty m_FieldOfView;
        ExposedProperty m_NearPlane;
        ExposedProperty m_FarPlane;
        ExposedProperty m_AspectRatio;
        ExposedProperty m_Dimensions;
        ExposedProperty m_ScaledDimensions;
        ExposedProperty m_DepthBuffer;
        ExposedProperty m_ColorBuffer;
        ExposedProperty m_Orthographic;
        ExposedProperty m_OrthographicSize;
        ExposedProperty m_LensShift;

        /// <summary>
        /// Set a camera property.
        /// </summary>
        /// <param name="name">Property name.</param>
        public void SetCameraProperty(string name)
        {
            CameraProperty = name;
            UpdateSubProperties();
        }

        void UpdateSubProperties()
        {
            // Get Camera component from HDRP additional data
            if (AdditionalData != null)
            {
                m_Camera = AdditionalData.GetComponent<Camera>();
            }

            // Update VFX Sub Properties
            m_Position = CameraProperty + "_transform_position";
            m_Angles = CameraProperty + "_transform_angles";
            m_Scale = CameraProperty + "_transform_scale";
            m_Orthographic = CameraProperty + "_orthographic";
            m_FieldOfView = CameraProperty + "_fieldOfView";
            m_NearPlane = CameraProperty + "_nearPlane";
            m_FarPlane = CameraProperty + "_farPlane";
            m_OrthographicSize = CameraProperty + "_orthographicSize";
            m_AspectRatio = CameraProperty + "_aspectRatio";
            m_Dimensions = CameraProperty + "_pixelDimensions";
            m_LensShift = CameraProperty + "_lensShift";
            m_DepthBuffer = CameraProperty + "_depthBuffer";
            m_ColorBuffer = CameraProperty + "_colorBuffer";
            m_ScaledDimensions = CameraProperty + "_scaledPixelDimensions";
        }

        void RequestHDRPBuffersAccess(ref HDAdditionalCameraData.BufferAccess access)
        {
            access.RequestAccess(HDAdditionalCameraData.BufferAccessType.Color);
            access.RequestAccess(HDAdditionalCameraData.BufferAccessType.Depth);
        }

        /// <summary>
        /// OnEnable implementation.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            if (AdditionalData != null)
                AdditionalData.requestGraphicsBuffer += RequestHDRPBuffersAccess;

            UpdateSubProperties();
        }

        /// <summary>
        /// OnDisable implementation.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            if (AdditionalData != null)
                AdditionalData.requestGraphicsBuffer -= RequestHDRPBuffersAccess;
        }

        private void OnValidate()
        {
            UpdateSubProperties();

            if (AdditionalData != null)
                AdditionalData.requestGraphicsBuffer += RequestHDRPBuffersAccess;
        }

        /// <summary>
        /// Returns true if the Visual Effect and the configuration of the binder are valid to perform the binding.
        /// </summary>
        /// <param name="component">Component to be tested.</param>
        /// <returns>True if the Visual Effect and the configuration of the binder are valid to perform the binding.</returns>
        public override bool IsValid(VisualEffect component)
        {
            return AdditionalData != null
                && m_Camera != null
                && component.HasVector3(m_Position)
                && component.HasVector3(m_Angles)
                && component.HasVector3(m_Scale)
                && component.HasBool(m_Orthographic)
                && component.HasFloat(m_FieldOfView)
                && component.HasFloat(m_NearPlane)
                && component.HasFloat(m_FarPlane)
                && component.HasFloat(m_OrthographicSize)
                && component.HasFloat(m_AspectRatio)
                && component.HasVector2(m_Dimensions)
                && component.HasVector2(m_LensShift)
                && component.HasTexture(m_DepthBuffer)
                && component.HasTexture(m_ColorBuffer)
                && component.HasVector2(m_ScaledDimensions);
        }

        /// <summary>
        /// Update bindings for a visual effect.
        /// </summary>
        /// <param name="component">Component to update.</param>
        public override void UpdateBinding(VisualEffect component)
        {
            var targetSpace = component.visualEffectAsset.GetExposedSpace(m_Position);
            Matrix4x4 readTransform;
            if (targetSpace == VFXSpace.Local)
            {
                readTransform = component.transform.worldToLocalMatrix * AdditionalData.transform.localToWorldMatrix;
            }
            else
            {
                readTransform = AdditionalData.transform.localToWorldMatrix;
            }

            component.SetVector3(m_Position, readTransform.GetPosition());
            component.SetVector3(m_Angles, readTransform.rotation.eulerAngles);
            component.SetVector3(m_Scale, readTransform.lossyScale);

            component.SetBool(m_Orthographic, m_Camera.orthographic);
            component.SetFloat(m_OrthographicSize, m_Camera.orthographicSize);
            // While field of View is set in degrees for the camera, it is expected in radians in VFX
            component.SetFloat(m_FieldOfView, Mathf.Deg2Rad * m_Camera.fieldOfView);
            component.SetFloat(m_NearPlane, m_Camera.nearClipPlane);
            component.SetFloat(m_FarPlane, m_Camera.farClipPlane);
            component.SetVector2(m_LensShift, m_Camera.lensShift);
            component.SetFloat(m_AspectRatio, m_Camera.aspect);

            component.SetVector2(m_Dimensions, new Vector2(m_Camera.pixelWidth, m_Camera.pixelHeight));
            DynamicResolutionHandler.UpdateAndUseCamera(m_Camera);
            Vector2 scaledSize = DynamicResolutionHandler.instance.GetScaledSize(new Vector2Int(m_Camera.pixelWidth, m_Camera.pixelHeight));
            DynamicResolutionHandler.ClearSelectedCamera();
            component.SetVector2(m_ScaledDimensions, scaledSize);

            var depth = AdditionalData.GetGraphicsBuffer(HDAdditionalCameraData.BufferAccessType.Depth);
            var color = AdditionalData.GetGraphicsBuffer(HDAdditionalCameraData.BufferAccessType.Color);

            if (depth != null)
                component.SetTexture(m_DepthBuffer, depth.rt);

            if (color != null)
                component.SetTexture(m_ColorBuffer, color.rt);
        }

        /// <summary>
        /// To string implementation.
        /// </summary>
        /// <returns>String containing the binder information.</returns>
        public override string ToString()
        {
            return string.Format($"HDRP Camera : '{(AdditionalData == null ? "null" : AdditionalData.gameObject.name)}' -> {CameraProperty}");
        }
    }
}
