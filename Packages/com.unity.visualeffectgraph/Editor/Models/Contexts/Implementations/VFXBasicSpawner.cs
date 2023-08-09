using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [CustomEditor(typeof(VFXBasicSpawner))]
    [CanEditMultipleObjects]
    class VFXBasicSpawnerEditor : Editor
    {
        class Styles
        {
            public static GUIContent delayMode = new GUIContent("Delay Mode", "Activate Delay before or after loop");
            public static GUIContent delayRandom = new GUIContent("Delay Random", "Enable or disable random mode for delay.");
            public static GUIContent delayBeforeRandom = new GUIContent("Delay Before Random", "Enable or disable random mode for delay before loop.");
            public static GUIContent delayAfterRandom = new GUIContent("Delay After Random", "Enable or disable random mode for delay after loop.");
        }

        SerializedProperty m_LoopDurationProperty;
        SerializedProperty m_LoopCountProperty;
        SerializedProperty m_DelayBeforeLoopProperty;
        SerializedProperty m_DelayAfterLoopProperty;

        private void OnEnable()
        {
            m_LoopDurationProperty = serializedObject.FindProperty("loopDuration");
            m_LoopCountProperty = serializedObject.FindProperty("loopCount");
            m_DelayBeforeLoopProperty = serializedObject.FindProperty("delayBeforeLoop");
            m_DelayAfterLoopProperty = serializedObject.FindProperty("delayAfterLoop");
        }

        enum DisplayedDelayMode
        {
            None,
            AfterLoop,
            BeforeLoop,
            BeforeAndAfterLoop
        }

        struct DelaySettings
        {
            public VFXBasicSpawner.DelayMode delayBeforeLoop;
            public VFXBasicSpawner.DelayMode delayAfterLoop;
        }

        struct DisplayedDelaySettings
        {
            public DisplayedDelayMode mode;
            public bool? randomBefore;
            public bool? randomAfter;
        }

        private static readonly KeyValuePair<DelaySettings, DisplayedDelaySettings>[] s_Modes =
        {
            new KeyValuePair<DelaySettings, DisplayedDelaySettings>(new DelaySettings(){ delayBeforeLoop = VFXBasicSpawner.DelayMode.None,      delayAfterLoop = VFXBasicSpawner.DelayMode.None     }, new DisplayedDelaySettings() { mode = DisplayedDelayMode.None,                randomBefore = null,  randomAfter = null  }),
            new KeyValuePair<DelaySettings, DisplayedDelaySettings>(new DelaySettings(){ delayBeforeLoop = VFXBasicSpawner.DelayMode.None,      delayAfterLoop = VFXBasicSpawner.DelayMode.Constant }, new DisplayedDelaySettings() { mode = DisplayedDelayMode.AfterLoop,           randomBefore = null,  randomAfter = false }),
            new KeyValuePair<DelaySettings, DisplayedDelaySettings>(new DelaySettings(){ delayBeforeLoop = VFXBasicSpawner.DelayMode.None,      delayAfterLoop = VFXBasicSpawner.DelayMode.Random   }, new DisplayedDelaySettings() { mode = DisplayedDelayMode.AfterLoop,           randomBefore = null,  randomAfter = true  }),
            new KeyValuePair<DelaySettings, DisplayedDelaySettings>(new DelaySettings(){ delayBeforeLoop = VFXBasicSpawner.DelayMode.Constant,  delayAfterLoop = VFXBasicSpawner.DelayMode.None     }, new DisplayedDelaySettings() { mode = DisplayedDelayMode.BeforeLoop,          randomBefore = false, randomAfter = null  }),
            new KeyValuePair<DelaySettings, DisplayedDelaySettings>(new DelaySettings(){ delayBeforeLoop = VFXBasicSpawner.DelayMode.Constant,  delayAfterLoop = VFXBasicSpawner.DelayMode.Constant }, new DisplayedDelaySettings() { mode = DisplayedDelayMode.BeforeAndAfterLoop,  randomBefore = false, randomAfter = false }),
            new KeyValuePair<DelaySettings, DisplayedDelaySettings>(new DelaySettings(){ delayBeforeLoop = VFXBasicSpawner.DelayMode.Constant,  delayAfterLoop = VFXBasicSpawner.DelayMode.Random   }, new DisplayedDelaySettings() { mode = DisplayedDelayMode.BeforeAndAfterLoop,  randomBefore = false, randomAfter = true  }),
            new KeyValuePair<DelaySettings, DisplayedDelaySettings>(new DelaySettings(){ delayBeforeLoop = VFXBasicSpawner.DelayMode.Random,    delayAfterLoop = VFXBasicSpawner.DelayMode.None     }, new DisplayedDelaySettings() { mode = DisplayedDelayMode.BeforeLoop,          randomBefore = true,  randomAfter = null  }),
            new KeyValuePair<DelaySettings, DisplayedDelaySettings>(new DelaySettings(){ delayBeforeLoop = VFXBasicSpawner.DelayMode.Random,    delayAfterLoop = VFXBasicSpawner.DelayMode.Constant }, new DisplayedDelaySettings() { mode = DisplayedDelayMode.BeforeAndAfterLoop,  randomBefore = true,  randomAfter = false }),
            new KeyValuePair<DelaySettings, DisplayedDelaySettings>(new DelaySettings(){ delayBeforeLoop = VFXBasicSpawner.DelayMode.Random,    delayAfterLoop = VFXBasicSpawner.DelayMode.Random   }, new DisplayedDelaySettings() { mode = DisplayedDelayMode.BeforeAndAfterLoop,  randomBefore = true,  randomAfter = true  }),
        };

        struct RandomAvailable
        {
            public bool before;
            public bool after;
        }
        private static readonly KeyValuePair<DisplayedDelayMode, RandomAvailable>[] s_RandomAvailable = s_Modes.GroupBy(o => o.Value.mode).Select(o =>
        {
            return new KeyValuePair<DisplayedDelayMode, RandomAvailable>(o.Key, new RandomAvailable()
            {
                before = o.Any(k => k.Value.randomBefore != null),
                after = o.Any(k => k.Value.randomAfter != null)
            });
        }).ToArray();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var referenceContext = serializedObject.targetObject as VFXContext;
            var resource = referenceContext.GetResource();
            GUI.enabled = resource != null ? resource.IsAssetEditable() : true;

            DisplayName(referenceContext);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_LoopDurationProperty);
            EditorGUILayout.PropertyField(m_LoopCountProperty);

            bool applyModifiedProperty = false;

            if (m_DelayBeforeLoopProperty.hasMultipleDifferentValues || m_DelayAfterLoopProperty.hasMultipleDifferentValues)
            {
                //Default GUI when multi editing
                EditorGUILayout.PropertyField(m_DelayBeforeLoopProperty);
                EditorGUILayout.PropertyField(m_DelayAfterLoopProperty);
                applyModifiedProperty = EditorGUI.EndChangeCheck();
            }
            else
            {
                //Smarter way to display these option : allowing an efficient resync slot
                var delayBefore = (VFXBasicSpawner.DelayMode)m_DelayBeforeLoopProperty.enumValueIndex;
                var delayAfter = (VFXBasicSpawner.DelayMode)m_DelayAfterLoopProperty.enumValueIndex;

                var currentState = s_Modes.First(o => o.Key.delayBeforeLoop == delayBefore && o.Key.delayAfterLoop == delayAfter).Value;

                currentState.mode = (DisplayedDelayMode)EditorGUILayout.EnumPopup(Styles.delayMode, currentState.mode);
                bool shouldIndicateWhichRandom = currentState.randomBefore != null && currentState.randomAfter != null;
                if (currentState.randomBefore.HasValue)
                    currentState.randomBefore = EditorGUILayout.Toggle(shouldIndicateWhichRandom ? Styles.delayBeforeRandom : Styles.delayRandom, (bool)currentState.randomBefore);
                if (currentState.randomAfter.HasValue)
                    currentState.randomAfter = EditorGUILayout.Toggle(shouldIndicateWhichRandom ? Styles.delayAfterRandom : Styles.delayRandom, (bool)currentState.randomAfter);

                if (EditorGUI.EndChangeCheck())
                {
                    //Copy random setting if other random hasn't a value
                    if (currentState.randomBefore.HasValue && !currentState.randomAfter.HasValue)
                        currentState.randomAfter = currentState.randomBefore;
                    if (!currentState.randomBefore.HasValue && currentState.randomAfter.HasValue)
                        currentState.randomBefore = currentState.randomAfter;

                    //Cancel useless random setting or reset to false if random is used by null is provided
                    var randomAvailable = s_RandomAvailable.First(o => o.Key == currentState.mode);
                    if (!randomAvailable.Value.before)
                        currentState.randomBefore = null;
                    else if (!currentState.randomBefore.HasValue)
                        currentState.randomBefore = false;

                    if (!randomAvailable.Value.after)
                        currentState.randomAfter = null;
                    else if (!currentState.randomAfter.HasValue)
                        currentState.randomAfter = false;

                    var actualSetting = s_Modes.First(o =>
                        o.Value.mode == currentState.mode
                        && (o.Value.randomBefore == currentState.randomBefore)
                        && (o.Value.randomAfter == currentState.randomAfter)).Key;

                    m_DelayBeforeLoopProperty.enumValueIndex = (int)actualSetting.delayBeforeLoop;
                    m_DelayAfterLoopProperty.enumValueIndex = (int)actualSetting.delayAfterLoop;
                    applyModifiedProperty = true;
                }
            }

            if (applyModifiedProperty)
            {
                if (serializedObject.ApplyModifiedProperties())
                {
                    foreach (var context in targets.OfType<VFXModel>())
                    {
                        // notify that something changed.
                        context.Invalidate(VFXModel.InvalidationCause.kSettingChanged);
                    }
                }
            }
        }

        private void DisplayName(VFXContext context)
        {
            var label = string.IsNullOrEmpty(context.label) ? context.letter.ToString() : context.label;
            GUILayout.Label(label, VFXSlotContainerEditor.Styles.spawnStyle);
        }
    }

    [VFXHelpURL("Context-Spawn")]
    [VFXInfo]
    class VFXBasicSpawner : VFXContext
    {
        public enum DelayMode
        {
            None,
            Constant,
            Random
        }

        public enum LoopMode
        {
            Infinite,
            Constant,
            Random
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        private LoopMode loopDuration = LoopMode.Infinite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        private LoopMode loopCount = LoopMode.Infinite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        private DelayMode delayBeforeLoop = DelayMode.None;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        private DelayMode delayAfterLoop = DelayMode.None;

        public VFXBasicSpawner() : base(VFXContextType.Spawner, VFXDataType.SpawnEvent, VFXDataType.SpawnEvent) { }
        public override string name { get { return "Spawn"; } }

        protected override int inputFlowCount
        {
            get
            {
                return 2;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var property in base.inputProperties)
                    yield return property;

                if (loopDuration == LoopMode.Constant)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "LoopDuration"), 0.1f);
                else if (loopDuration == LoopMode.Random)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "LoopDuration"), new Vector2(1.0f, 3.0f));

                if (loopCount == LoopMode.Constant)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(int), "LoopCount"), 1);
                else if (loopCount == LoopMode.Random)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "LoopCount"), new Vector2(1.0f, 3.0f)); //Int2 isn't supported yet

                if (delayBeforeLoop == DelayMode.Constant)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "DelayBeforeLoop"), 0.1f);
                else if (delayBeforeLoop == DelayMode.Random)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "DelayBeforeLoop"), new Vector2(0.1f, 0.3f));

                if (delayAfterLoop == DelayMode.Constant)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "DelayAfterLoop"), 0.1f);
                else if (delayAfterLoop == DelayMode.Random)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "DelayAfterLoop"), new Vector2(0.1f, 0.3f));
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.SpawnCount, VFXAttributeMode.ReadWrite);
                foreach (var attribute in base.attributes)
                    yield return attribute;
            }
        }

        static VFXExpression RandomFromVector2(VFXExpression input, RandId randId)
        {
            return VFXOperatorUtility.Lerp(input.x, input.y, new VFXExpressionRandom(false, randId));
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            if (target == VFXDeviceTarget.CPU)
            {
                var mapper = VFXExpressionMapper.FromBlocks(activeFlattenedChildrenWithImplicit);

                var mapperFromContext = new VFXExpressionMapper();
                mapperFromContext.AddExpressionsFromSlotContainer(this, -1);

                if (loopDuration != LoopMode.Infinite)
                {
                    var expression = mapperFromContext.FromNameAndId("LoopDuration", -1);
                    if (loopDuration == LoopMode.Random)
                        expression = RandomFromVector2(expression, new RandId(this, 0));
                    mapper.AddExpression(expression, "LoopDuration", -1);
                }

                if (loopCount != LoopMode.Infinite)
                {
                    var expression = mapperFromContext.FromNameAndId("LoopCount", -1);
                    if (loopCount == LoopMode.Random)
                        expression = new VFXExpressionCastFloatToInt(RandomFromVector2(expression, new RandId(this, 1)));
                    mapper.AddExpression(expression, "LoopCount", -1);
                }

                if (delayBeforeLoop != DelayMode.None)
                {
                    var expression = mapperFromContext.FromNameAndId("DelayBeforeLoop", -1);
                    if (delayBeforeLoop == DelayMode.Random)
                        expression = RandomFromVector2(expression, new RandId(this, 2));
                    mapper.AddExpression(expression, "DelayBeforeLoop", -1);
                }

                if (delayAfterLoop != DelayMode.None)
                {
                    var expression = mapperFromContext.FromNameAndId("DelayAfterLoop", -1);
                    if (delayAfterLoop == DelayMode.Random)
                        expression = RandomFromVector2(expression, new RandId(this, 3));
                    mapper.AddExpression(expression, "DelayAfterLoop", -1);
                }
                return mapper;
            }

            return null;
        }

        public override bool CanBeCompiled()
        {
            return outputContexts.Any(c => c.CanBeCompiled());
        }
    }
}
