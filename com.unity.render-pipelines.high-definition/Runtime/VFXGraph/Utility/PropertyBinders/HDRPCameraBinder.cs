using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    /// <summary>
    /// Camera parameter binding helper class.
    /// </summary>
    [VFXBinder("HDRP/HDRP Camera")]
    public class HDRPCameraBinder : VFXBinderBase, IVersionable<HDRPCameraBinder.Version>
    {
        #region migration
        enum Version
        {
            Initial,
            ChangeSerializationToSaveCamera,
        }
        [SerializeField] Version m_Version = MigrationDescription.LastVersion<Version>();

        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        static readonly MigrationDescription<Version, HDRPCameraBinder> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.ChangeSerializationToSaveCamera, (HDRPCameraBinder data) =>
            {
#pragma warning disable CS0618 // Type or member is obsolete
                data.cameraBinded = data.m_AdditionalData?.GetComponent<Camera>();
                data.m_AdditionalData = null;
#pragma warning restore CS0618 // Type or member is obsolete
            })
        );

        //formerly serialized data. should not be used
        [Obsolete("Keeped for migration only")]
        [SerializeField, HideInInspector, FormerlySerializedAs("AdditionalData")]
#pragma warning disable 649 // Field never assigned
        HDAdditionalCameraData m_AdditionalData;
#pragma warning restore 649 // Field never assigned

        protected override void Awake()
        {
            k_Migration.Migrate(this);
            base.Awake();
        }
        #endregion

        /// <summary>
        /// Camera HDRP.
        /// </summary>
        public Camera cameraBinded;
        HDCameraExtension m_Extension;

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
        ExposedProperty m_DepthBuffer;
        ExposedProperty m_ColorBuffer;

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
            if (cameraBinded != null)
            {
                if (cameraBinded.extension is HDCameraExtension extension)
                    m_Extension = extension;
                else
                {
                    if (!cameraBinded.HasExtension<HDCameraExtension>())
                        cameraBinded.CreateExtension<HDCameraExtension>();
                    m_Extension = cameraBinded.SwitchActiveExtensionTo<HDCameraExtension>();
                }
            }

            // Update VFX Sub Properties
            m_Position = CameraProperty + "_transform_position";
            m_Angles = CameraProperty + "_transform_angles";
            m_Scale = CameraProperty + "_transform_scale";
            m_FieldOfView = CameraProperty + "_fieldOfView";
            m_NearPlane = CameraProperty + "_nearPlane";
            m_FarPlane = CameraProperty + "_farPlane";
            m_AspectRatio = CameraProperty + "_aspectRatio";
            m_Dimensions = CameraProperty + "_pixelDimensions";
            m_DepthBuffer = CameraProperty + "_depthBuffer";
            m_ColorBuffer = CameraProperty + "_colorBuffer";
        }

        void RequestHDRPBuffersAccess(ref HDCameraExtension.BufferAccess access)
        {
            access.RequestAccess(HDCameraExtension.BufferAccess.BufferAccessType.Color);
            access.RequestAccess(HDCameraExtension.BufferAccess.BufferAccessType.Depth);
        }

        /// <summary>
        /// OnEnable implementation.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_Extension != null)
                m_Extension.requestGraphicsBuffer += RequestHDRPBuffersAccess;

            UpdateSubProperties();
        }

        /// <summary>
        /// OnDisable implementation.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            if (m_Extension != null)
                m_Extension.requestGraphicsBuffer -= RequestHDRPBuffersAccess;
        }

        private void OnValidate()
        {
            UpdateSubProperties();

            if (m_Extension != null)
                m_Extension.requestGraphicsBuffer += RequestHDRPBuffersAccess;
        }

        /// <summary>
        /// Returns true if the Visual Effect and the configuration of the binder are valid to perform the binding.
        /// </summary>
        /// <param name="component">Component to be tested.</param>
        /// <returns>True if the Visual Effect and the configuration of the binder are valid to perform the binding.</returns>
        public override bool IsValid(VisualEffect component)
        {
            return cameraBinded != null
                && m_Extension != null
                && component.HasVector3(m_Position)
                && component.HasVector3(m_Angles)
                && component.HasVector3(m_Scale)
                && component.HasFloat(m_FieldOfView)
                && component.HasFloat(m_NearPlane)
                && component.HasFloat(m_FarPlane)
                && component.HasFloat(m_AspectRatio)
                && component.HasVector2(m_Dimensions)
                && component.HasTexture(m_DepthBuffer)
                && component.HasTexture(m_ColorBuffer);
        }

        /// <summary>
        /// Update bindings for a visual effect.
        /// </summary>
        /// <param name="component">Component to update.</param>
        public override void UpdateBinding(VisualEffect component)
        {
            var depth = m_Extension.GetGraphicsBuffer(HDCameraExtension.BufferAccess.BufferAccessType.Depth);
            var color = m_Extension.GetGraphicsBuffer(HDCameraExtension.BufferAccess.BufferAccessType.Color);

            if (depth == null && color == null)
                return;

            component.SetVector3(m_Position, cameraBinded.transform.position);
            component.SetVector3(m_Angles, cameraBinded.transform.eulerAngles);
            component.SetVector3(m_Scale, cameraBinded.transform.lossyScale);

            // While field of View is set in degrees for the camera, it is expected in radians in VFX
            component.SetFloat(m_FieldOfView, Mathf.Deg2Rad * cameraBinded.fieldOfView);
            component.SetFloat(m_NearPlane, cameraBinded.nearClipPlane);
            component.SetFloat(m_FarPlane, cameraBinded.farClipPlane);

            component.SetFloat(m_AspectRatio, cameraBinded.aspect);
            component.SetVector2(m_Dimensions, new Vector2(cameraBinded.pixelWidth * depth.rtHandleProperties.rtHandleScale.x, cameraBinded.pixelHeight * depth.rtHandleProperties.rtHandleScale.y));

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
            return string.Format($"HDRP Camera : '{(cameraBinded == null? "null" : cameraBinded.gameObject.name)}' -> {CameraProperty}");
        }
    }

}
