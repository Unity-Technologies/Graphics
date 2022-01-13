using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX.Block;
using System.Reflection;

namespace UnityEditor.VFX
{
    [VFXInfoGTF]
    class VFXBasicUpdateGTF : VFXContext
    {
        public enum VFXIntegrationMode
        {
            Euler,
            None
        }

        [Header("Particle Update Options")]
        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("When enabled, particle positions are automatically updated using their velocity.")]
        private VFXIntegrationMode integration = VFXIntegrationMode.Euler;

        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("When enabled, particle rotations are automatically updated using their angular velocity.")]
        private VFXIntegrationMode angularIntegration = VFXIntegrationMode.Euler;

        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("When enabled, the particle age attribute will increase every frame based on deltaTime.")]
        private bool ageParticles = true;

        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("When enabled, particles whose age exceeds their lifetime will be destroyed.")]
        private bool reapParticles = true;

        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("When enabled, filters out block execution if deltaTime is equal to 0.")]
        private bool skipZeroDeltaUpdate = false;

        public VFXBasicUpdateGTF() : base(VFXContextType.Update, VFXDataType.None, VFXDataType.None) { }
        public override string name { get { return "Update " + ObjectNames.NicifyVariableName(ownedType.ToString()); } }
        public override string codeGeneratorTemplate { get { return VisualEffectGraphPackageInfo.assetPackagePath + "/Shaders/VFXUpdate"; } }
        public override bool codeGeneratorCompute { get { return true; } }
        public override VFXTaskType taskType { get { return VFXTaskType.Update; } }
        public override VFXDataType inputType { get { return GetData() == null ? VFXDataType.Particle : GetData().type; } }
        public override VFXDataType outputType { get { return GetData() == null ? VFXDataType.Particle : GetData().type; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                if (GetData().IsCurrentAttributeRead(VFXAttribute.OldPosition))
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.OldPosition, VFXAttributeMode.Write);
                }

                if (GetData().IsCurrentAttributeWritten(VFXAttribute.Alive) && GetData().dependenciesOut.Any(d => ((VFXDataParticle)d).hasStrip))
                    yield return new VFXAttributeInfo(VFXAttribute.StripAlive, VFXAttributeMode.ReadWrite);

                VFXDataParticle particleData = GetData() as VFXDataParticle;
                if (particleData && particleData.NeedsComputeBounds())
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.PivotX, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.PivotZ, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.ScaleZ, VFXAttributeMode.Read);
                }
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;


                var data = GetData();
                var lifeTime = data.IsCurrentAttributeWritten(VFXAttribute.Lifetime);
                var age = data.IsCurrentAttributeUsed(VFXAttribute.Age);
                var positionVelocity = data.IsCurrentAttributeWritten(VFXAttribute.Velocity);
                var angularVelocity = data.IsCurrentAttributeWritten(VFXAttribute.AngularVelocityX) ||
                    data.IsCurrentAttributeWritten(VFXAttribute.AngularVelocityY) ||
                    data.IsCurrentAttributeWritten(VFXAttribute.AngularVelocityZ);

                if (!age && !lifeTime)
                    yield return "ageParticles";

                if (!lifeTime)
                    yield return "reapParticles";

                if (!positionVelocity)
                    yield return "updatePosition";

                if (!angularVelocity)
                    yield return "updateRotation";
            }
        }

        protected override IEnumerable<VFXBlock> implicitPreBlock
        {
            get
            {
                var data = GetData();
                if (data.IsCurrentAttributeUsed(VFXAttribute.OldPosition))
                {
                    yield return VFXBlock.CreateImplicitBlock<BackupOldPosition>(data);
                }
            }
        }

        protected override IEnumerable<VFXBlock> implicitPostBlock
        {
            get
            {
                var data = GetData();

                if (integration == VFXIntegrationMode.Euler && data.IsCurrentAttributeWritten(VFXAttribute.Velocity))
                    yield return VFXBlock.CreateImplicitBlock<EulerIntegration>(data);

                if (angularIntegration == VFXIntegrationMode.Euler &&
                    (
                        data.IsCurrentAttributeWritten(VFXAttribute.AngularVelocityX) ||
                        data.IsCurrentAttributeWritten(VFXAttribute.AngularVelocityY) ||
                        data.IsCurrentAttributeWritten(VFXAttribute.AngularVelocityZ))
                )
                    yield return VFXBlock.CreateImplicitBlock<AngularEulerIntegration>(data);

                var lifeTime = GetData().IsCurrentAttributeWritten(VFXAttribute.Lifetime);
                var age = GetData().IsCurrentAttributeUsed(VFXAttribute.Age);

                if (age || lifeTime)
                {
                    if (ageParticles)
                        yield return VFXBlock.CreateImplicitBlock<Age>(data);

                    if (lifeTime && reapParticles)
                        yield return VFXBlock.CreateImplicitBlock<Reap>(data);
                }
            }
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            var mapper = base.GetExpressionMapper(target);
            if (target == VFXDeviceTarget.GPU && skipZeroDeltaUpdate)
                mapper.AddExpression(VFXBuiltInExpression.DeltaTime, "deltaTime", -1);
            var dataParticle = GetData() as VFXDataParticle;

            if (target == VFXDeviceTarget.GPU && dataParticle && dataParticle.NeedsComputeBounds() && space == VFXCoordinateSpace.World)
            {
                mapper.AddExpression(VFXBuiltInExpression.WorldToLocal, "worldToLocal", -1);
            }
            return mapper;
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                if ((GetData() as VFXDataParticle).NeedsGlobalIndirectBuffer())
                    yield return "VFX_HAS_INDIRECT_DRAW";

                if (ownedType == VFXDataType.ParticleStrip)
                    yield return "HAS_STRIPS";

                if (skipZeroDeltaUpdate)
                    yield return "VFX_UPDATE_SKIP_ZERO_DELTA_TIME";
                if ((GetData() as VFXDataParticle).NeedsComputeBounds())
                    yield return "VFX_COMPUTE_BOUNDS";
            }
        }
    }
}
