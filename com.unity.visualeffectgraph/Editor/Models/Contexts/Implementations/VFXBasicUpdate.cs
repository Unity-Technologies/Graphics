using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX.Block;
using System.Reflection;

namespace UnityEditor.VFX
{
    [CustomEditor(typeof(VFXBasicUpdate))]
    [CanEditMultipleObjects]
    class VFXBasicUpdateEditor : VFXContextEditor
    {
        class UpdateStyles
        {
            public static GUIContent header = new GUIContent("Particle Update Options", "Particle Update Options");
            public static GUIContent updatePosition = new GUIContent("Update Position", "When enabled, particle positions are automatically updated using their velocity.");
            public static GUIContent updateRotation = new GUIContent("Update Rotation", "When enabled, particle rotations are automatically updated using their angular velocity.");
            public static GUIContent ageParticles = new GUIContent("Age Particles", "When enabled, the particle age attribute will increase every frame based on deltaTime.");
            public static GUIContent reapParticles = new GUIContent("Reap Particles", "When enabled, particles whose age exceeds their lifetime will be destroyed.");
        }

        SerializedProperty m_IntegrationProperty;
        SerializedProperty m_AngularIntegrationProperty;
        SerializedProperty m_AgeParticlesProperty;
        SerializedProperty m_ReapParticlesProperty;

        protected new void OnEnable()
        {
            base.OnEnable();
            m_IntegrationProperty = serializedObject.FindProperty("integration");
            m_AngularIntegrationProperty = serializedObject.FindProperty("angularIntegration");
            m_AgeParticlesProperty = serializedObject.FindProperty("ageParticles");
            m_ReapParticlesProperty = serializedObject.FindProperty("reapParticles");
        }

        private static Func<VFXBasicUpdate, IEnumerable<string>> s_fnGetFilteredOutSettings = delegate(VFXBasicUpdate context)
        {
            var property = typeof(VFXBasicUpdate).GetProperty("filteredOutSettings", BindingFlags.Instance | BindingFlags.NonPublic);
            return property.GetValue(context) as IEnumerable<string>;
        };

        void DisplayToggle(GUIContent content, SerializedProperty property, bool? input, bool isIntegrationMode)
        {
            EditorGUI.BeginChangeCheck();

            bool actualInput = input.HasValue ? input.Value : false;
            if (!input.HasValue)
            {
                EditorGUI.showMixedValue = true;
            }
            actualInput = EditorGUILayout.Toggle(content, actualInput);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                if (isIntegrationMode)
                    property.enumValueIndex = actualInput ? (int)VFXBasicUpdate.VFXIntegrationMode.Euler : (int)VFXBasicUpdate.VFXIntegrationMode.None;
                else
                    property.boolValue = actualInput;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DisplaySpace();
            DisplayName();

            EditorGUILayout.LabelField(UpdateStyles.header, EditorStyles.boldLabel);

            bool? updatePosition = null;
            bool? updateRotation = null;
            bool? ageParticles = null;
            bool? reapParticles = null;

            if (!m_IntegrationProperty.hasMultipleDifferentValues)
                updatePosition = m_IntegrationProperty.enumValueIndex == (int)VFXBasicUpdate.VFXIntegrationMode.Euler;
            if (!m_AngularIntegrationProperty.hasMultipleDifferentValues)
                updateRotation = m_AngularIntegrationProperty.enumValueIndex == (int)VFXBasicUpdate.VFXIntegrationMode.Euler;
            if (!m_AgeParticlesProperty.hasMultipleDifferentValues)
                ageParticles = m_AgeParticlesProperty.boolValue;
            if (!m_ReapParticlesProperty.hasMultipleDifferentValues)
                reapParticles = m_ReapParticlesProperty.boolValue;

            bool filterOutUpdatePosition = targets.OfType<VFXBasicUpdate>().All(o => s_fnGetFilteredOutSettings(o).Contains("updatePosition"));
            bool filterOutUpdateRotation = targets.OfType<VFXBasicUpdate>().All(o => s_fnGetFilteredOutSettings(o).Contains("updateRotation"));
            bool filterOutAgeParticles = targets.OfType<VFXBasicUpdate>().All(o => s_fnGetFilteredOutSettings(o).Contains("ageParticles"));
            bool filterOutReapParticles = targets.OfType<VFXBasicUpdate>().All(o => s_fnGetFilteredOutSettings(o).Contains("reapParticles"));

            if (!filterOutUpdatePosition)
                DisplayToggle(UpdateStyles.updatePosition, m_IntegrationProperty, updatePosition, true);
            if (!filterOutUpdateRotation)
                DisplayToggle(UpdateStyles.updateRotation, m_AngularIntegrationProperty, updateRotation, true);
            if (!filterOutAgeParticles)
                DisplayToggle(UpdateStyles.ageParticles, m_AgeParticlesProperty, ageParticles, false);
            if (!filterOutReapParticles)
                DisplayToggle(UpdateStyles.reapParticles, m_ReapParticlesProperty, reapParticles, false);

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (var context in targets.OfType<VFXBasicUpdate>())
                {
                    context.Invalidate(VFXModel.InvalidationCause.kSettingChanged);
                }
            }

            DisplaySummary();
        }
    }

    [VFXInfo]
    class VFXBasicUpdate : VFXContext
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

        public VFXBasicUpdate() : base(VFXContextType.Update, VFXDataType.None, VFXDataType.None) {}
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
                var age = data.IsCurrentAttributeRead(VFXAttribute.Age);
                var positionVelocity = data.IsCurrentAttributeWritten(VFXAttribute.Velocity);
                var angularVelocity =   data.IsCurrentAttributeWritten(VFXAttribute.AngularVelocityX) ||
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
                var age = GetData().IsCurrentAttributeRead(VFXAttribute.Age);

                if (age || lifeTime)
                {
                    if (ageParticles)
                        yield return VFXBlock.CreateImplicitBlock<Age>(data);

                    if (lifeTime && reapParticles)
                        yield return VFXBlock.CreateImplicitBlock<Reap>(data);
                }
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                if ((GetData() as VFXDataParticle).NeedsIndirectBuffer())
                    yield return "VFX_HAS_INDIRECT_DRAW";

                if (ownedType == VFXDataType.ParticleStrip)
                    yield return "HAS_STRIPS";
            }
        }
    }
}
