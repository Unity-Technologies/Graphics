using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace UnityEngine.Rendering
{
    // This file can't be in the editor assembly as we need to access it in runtime-editor-specific
    // places like OnGizmo etc and we don't want to add the editor assembly as a dependency of the
    // runtime one

    // The UI layout/styling in this panel is broken and can't match the one from built-ins
    // preference panels as everything needed is internal/private (at the time of writing this
    // comment)

#if UNITY_EDITOR
    using UnityEditor;

    [InitializeOnLoad]
    public static class CoreRenderPipelinePreferences
    {
        // We do not want that GC frees the preferences that have been added, used to store their references
        static readonly ConcurrentStack<object> s_ColorPref = new ConcurrentStack<object>();

        #region Volumes Gizmo Color

        static Color s_VolumeGizmoColorDefault = new Color(0.2f, 0.8f, 0.1f, 0.5f);
        private static Func<Color> GetColorPrefVolumeGizmoColor;

        public static Color volumeGizmoColor => GetColorPrefVolumeGizmoColor();

        #endregion

        #region Preview Camera Background Color

        static readonly Color kPreviewCameraBackgroundColorDefault = new Color(82f / 255.0f, 82f / 255.0f, 82.0f / 255.0f, 0.0f);
        public static Color previewBackgroundColor => kPreviewCameraBackgroundColorDefault;

        #endregion

        static CoreRenderPipelinePreferences()
        {
            GetColorPrefVolumeGizmoColor = RegisterPreferenceColor("Scene/Volume Gizmo", s_VolumeGizmoColorDefault);
        }

        /// <summary>
        /// This function provides a way of adding a <see cref="PrefColor"/> into the panel 'Preferences\Colors' />
        /// </summary>
        /// <param name="name">The name that the color will have on the panel, in form of 'group/name'</param>
        /// <param name="defaultColor">Used for first time, and to restore the default value</param>
        public static Func<Color> RegisterPreferenceColor(string name, Color defaultColor)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("You must give a valid name for the color property", nameof(name));

            // PrefColor is the type to use to have a Color that is customizable inside the Preference/Colors panel.
            // Sadly it is internal so we must create it and grab color from it by reflection.
            Type prefColorType = typeof(Editor).Assembly.GetType("UnityEditor.PrefColor");
            PropertyInfo colorInfo = prefColorType.GetProperty("Color");

            var colorPref = Activator.CreateInstance(prefColorType, name, defaultColor.r, defaultColor.g, defaultColor.b, defaultColor.a);
            s_ColorPref.Push(colorPref);

            MemberExpression colorProperty = Expression.Property(Expression.Constant(colorPref, prefColorType), colorInfo);

            // Make sure that the new preference color is being loaded into the Preference/Colors panel
            MethodInfo loadMethod = prefColorType.GetMethod("Load");
            loadMethod.Invoke(colorPref, null);

            // Return the method to obtain the color
            return Expression.Lambda<Func<Color>>(colorProperty).Compile();
        }
    }
#endif
}
