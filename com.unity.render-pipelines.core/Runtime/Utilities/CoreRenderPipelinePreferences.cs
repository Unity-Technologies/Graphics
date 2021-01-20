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

    public static class CoreRenderPipelinePreferences
    {
        #region Volumes Gizmo Color

        static Color s_VolumeGizmoColorDefault = new Color(0.2f, 0.8f, 0.1f, 0.5f);
        static Func<Color> GetColorPrefVolumeGizmoColor;

        public static Color volumeGizmoColor => GetColorPrefVolumeGizmoColor();

        #endregion

        #region Preview Camera Background Color

        static readonly Color kPreviewCameraBackgroundColorDefault = new Color(82f / 255.0f, 82f / 255.0f, 82.0f / 255.0f, 0.0f);
        public static Color previewBackgroundColor => kPreviewCameraBackgroundColorDefault;

        #endregion

        static CoreRenderPipelinePreferences()
        {
            GetColorPrefVolumeGizmoColor = RegisterSceneColor("Volume Gizmo", s_VolumeGizmoColorDefault);
        }

        static readonly ConcurrentStack<object> s_ColorPref = new ConcurrentStack<object>();
        internal static Func<Color> RegisterSceneColor(string name, Color defaultColor)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("You must give a valid name for the color property", nameof(name));

            // PrefColor is the type to use to have a Color that is customizable inside the Preference/Colors panel.
            // Sadly it is internal so we must create it and grab color from it by reflection.
            Type prefColorType = typeof(Editor).Assembly.GetType("UnityEditor.PrefColor");

            var colorPref = Activator.CreateInstance(prefColorType, $"Scene/{name}", defaultColor.r, defaultColor.g, defaultColor.b, defaultColor.a);

            PropertyInfo colorInfo = prefColorType.GetProperty("Color");
            if (colorInfo == null)
            {
                Debug.LogError("Unable to find property `Color` on object `UnityEditor.PrefColor`");
                return () => defaultColor;
            }

            s_ColorPref.Push(colorPref);

            MemberExpression colorProperty = Expression.Property(Expression.Constant(colorPref, prefColorType), colorInfo);

            return Expression.Lambda<Func<Color>>(colorProperty).Compile();
        }
    }
#endif
}
