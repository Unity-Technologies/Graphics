using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;
using System;
using System.Reflection;

namespace UnityEditor.Rendering.HighDefinition
{
	/// <summary>
	/// Custom UI class for custom passes
	/// </summary>
	[CustomPassDrawerAttribute(typeof(CustomPass))]
    public class CustomPassDrawer
    {
	    class Styles
	    {
		    public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            public static float reorderableListHandleIndentWidth = 12;
		    public static GUIContent callback = new GUIContent("Event", "Chose the Callback position for this render pass object.");
			public static GUIContent enabled = new GUIContent("Enabled", "Enable or Disable the custom pass");
			public static GUIContent targetDepthBuffer = new GUIContent("Target Depth Buffer");
			public static GUIContent targetColorBuffer = new GUIContent("Target Color Buffer");
			public static GUIContent clearFlags = new GUIContent("Clear Flags", "Clear Flags used when the render targets will are bound, before the pass renders.");
	    }

		[Flags]
		/// <summary>
		/// List of the elements you can show/hide in the default custom pass UI.
		/// </summary>
		public enum PassUIFlag
		{
			None				= 0x00,
			Name				= 0x01,
			TargetColorBuffer	= 0x02,
			TargetDepthBuffer	= 0x04,
			ClearFlags			= 0x08,
			All					= ~0,
		}

		/// <summary>
		/// Controls which field of the common pass UI is displayed.
		/// </summary>
		protected virtual PassUIFlag commonPassUIFlags => PassUIFlag.All;

	    bool firstTime = true;

	    // Serialized Properties
		SerializedProperty      	m_Name;
		SerializedProperty      	m_Enabled;
		SerializedProperty      	m_TargetColorBuffer;
		SerializedProperty      	m_TargetDepthBuffer;
		SerializedProperty      	m_ClearFlags;
		SerializedProperty      	m_PassFoldout;
		List<SerializedProperty>	m_CustomPassUserProperties = new List<SerializedProperty>();
		Type						m_PassType;

		void FetchProperties(SerializedProperty property)
		{
			m_Name = property.FindPropertyRelative("name");
			m_Enabled = property.FindPropertyRelative("enabled");
			m_TargetColorBuffer = property.FindPropertyRelative("targetColorBuffer");
			m_TargetDepthBuffer = property.FindPropertyRelative("targetDepthBuffer");
			m_ClearFlags = property.FindPropertyRelative("clearFlags");
			m_PassFoldout = property.FindPropertyRelative("passFoldout");
		}

		void LoadUserProperties(SerializedProperty customPass)
		{
			// Store all fields in CustomPass so we can exclude them when retrieving the user custom pass type
			var customPassFields = typeof(CustomPass).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			foreach (var field in m_PassType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				var serializeField = field.GetCustomAttribute<SerializeField>();
				var hideInInspector = field.GetCustomAttribute<HideInInspector>();
				var nonSerialized = field.GetCustomAttribute<NonSerializedAttribute>();

				if (customPassFields.Any(f => f.Name == field.Name))
					continue;

				if (nonSerialized != null || hideInInspector != null)
					continue;

				if (!field.IsPublic && serializeField == null)
					continue;
				
				var prop = customPass.FindPropertyRelative(field.Name);
				if (prop != null)
					m_CustomPassUserProperties.Add(prop);
			}
		}

	    void InitInternal(SerializedProperty customPass)
	    {
			FetchProperties(customPass);
			Initialize(customPass);
			LoadUserProperties(customPass);
		    firstTime = false;
	    }

		protected virtual void Initialize(SerializedProperty customPass) {}

		internal void SetPassType(Type passType) => m_PassType = passType;

	    internal void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
	    {
			rect.height = EditorGUIUtility.singleLineHeight;
			EditorGUI.BeginChangeCheck();

			if (firstTime)
			    InitInternal(property);

			DoHeaderGUI(ref rect);

			if (m_PassFoldout.boolValue)
			{
				EditorGUI.EndChangeCheck();
				return;
			}

			EditorGUI.BeginDisabledGroup(!m_Enabled.boolValue);
			{
				DoCommonSettingsGUI(ref rect);

				DoPassGUI(property, rect);
			}
			EditorGUI.EndDisabledGroup();

			if (EditorGUI.EndChangeCheck())
				property.serializedObject.ApplyModifiedProperties();
	    }

		void DoCommonSettingsGUI(ref Rect rect)
		{
			if ((commonPassUIFlags & PassUIFlag.Name) != 0)
			{
				EditorGUI.PropertyField(rect, m_Name);
				rect.y += Styles.defaultLineSpace;
			}

#if true
			if ((commonPassUIFlags & PassUIFlag.TargetColorBuffer) != 0)
            {
                m_TargetColorBuffer.intValue = (int)(CustomPass.TargetBuffer)EditorGUI.EnumPopup(rect, Styles.targetColorBuffer, (CustomPass.TargetBuffer)m_TargetColorBuffer.intValue);
                rect.y += Styles.defaultLineSpace;
            }

            if ((commonPassUIFlags & PassUIFlag.TargetDepthBuffer) != 0)
            {
                m_TargetDepthBuffer.intValue = (int)(CustomPass.TargetBuffer)EditorGUI.EnumPopup(rect, Styles.targetDepthBuffer, (CustomPass.TargetBuffer)m_TargetDepthBuffer.intValue);
                rect.y += Styles.defaultLineSpace;
            }

            if ((commonPassUIFlags & PassUIFlag.ClearFlags) != 0)
            {
                m_ClearFlags.intValue = (int)(ClearFlag)EditorGUI.EnumPopup(rect, Styles.clearFlags, (ClearFlag)m_ClearFlags.intValue);
                rect.y += Styles.defaultLineSpace;
			}
			
#else		// TODO: remove all this code when the fix for SerializedReference lands
			
			EditorGUI.PropertyField(rect, m_TargetColorBuffer, Styles.targetColorBuffer);
			rect.y += Styles.defaultLineSpace;

			EditorGUI.PropertyField(rect, m_TargetDepthBuffer, Styles.targetDepthBuffer);
			rect.y += Styles.defaultLineSpace;
			
			EditorGUI.PropertyField(rect, m_ClearFlags, Styles.clearFlags);
			rect.y += Styles.defaultLineSpace;
#endif
		}

		protected virtual void DoPassGUI(SerializedProperty customPass, Rect rect)
		{
			foreach (var prop in m_CustomPassUserProperties)
			{
				EditorGUI.PropertyField(rect, prop);
				rect.y += Styles.defaultLineSpace;
			}
		}

		void DoHeaderGUI(ref Rect rect)
		{
			var enabledSize = EditorStyles.boldLabel.CalcSize(Styles.enabled) + new Vector2(Styles.reorderableListHandleIndentWidth, 0);
			var headerRect = new Rect(rect.x + Styles.reorderableListHandleIndentWidth,
							rect.y + EditorGUIUtility.standardVerticalSpacing,
							rect.width - Styles.reorderableListHandleIndentWidth - enabledSize.x,
							EditorGUIUtility.singleLineHeight);
			rect.y += Styles.defaultLineSpace;
			var enabledRect = headerRect;
			enabledRect.x = rect.xMax - enabledSize.x;
			enabledRect.width = enabledSize.x;

			m_PassFoldout.boolValue = EditorGUI.Foldout(headerRect, m_PassFoldout.boolValue, $"{m_Name.stringValue} ({m_PassType.Name})", true, EditorStyles.boldLabel);
			EditorGUIUtility.labelWidth = enabledRect.width - 14;
			m_Enabled.boolValue = EditorGUI.Toggle(enabledRect, Styles.enabled, m_Enabled.boolValue);
			EditorGUIUtility.labelWidth = 0;
		}

		protected virtual float GetPassHeight(SerializedProperty customPass)
		{
			float height = 0;

			foreach (var prop in m_CustomPassUserProperties)
			{
				height += EditorGUI.GetPropertyHeight(prop);
				height += EditorGUIUtility.standardVerticalSpacing;
			}

			return height;
		}

	    internal float GetPropertyHeight(SerializedProperty property, GUIContent label)
	    {
		    float height = Styles.defaultLineSpace;

			if (firstTime)
				InitInternal(property);

			if (m_PassFoldout.boolValue)
				return height;

		    if (!firstTime)
		    {
				int lines = 4; // name + target buffers + clearFlags

				if (commonPassUIFlags != PassUIFlag.All)
				{
					lines = 0;
					for (int i = 0; i < 32; i++)
						lines += (((int)commonPassUIFlags & (1 << i)) != 0) ? 1 : 0;
				}

				height += Styles.defaultLineSpace * lines;
		    }

		    return height + GetPassHeight(property);
	    }
    }
}