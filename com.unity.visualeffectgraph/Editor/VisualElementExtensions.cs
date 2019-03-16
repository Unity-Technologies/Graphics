using System.Linq;
using System.Reflection;

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;

static class VisualElementExtensions
{
    static MethodInfo m_ValidateLayoutMethod;
    public static void InternalValidateLayout(this IPanel panel)
    {
        if (m_ValidateLayoutMethod == null)
            m_ValidateLayoutMethod = panel.GetType().GetMethod("ValidateLayout", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public);

        m_ValidateLayoutMethod.Invoke(panel, new object[] {});
    }

    static PropertyInfo m_OwnerPropertyInfo;

    public static GUIView  InternalGetGUIView(this IPanel panel)
    {
        if (m_OwnerPropertyInfo == null)
            m_OwnerPropertyInfo = panel.GetType().GetProperty("ownerObject", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public);


        return (GUIView)m_OwnerPropertyInfo.GetValue(panel, new object[] {});
    }

    public static bool HasFocus(this VisualElement visualElement)
    {
        if (visualElement.panel == null) return false;

        return visualElement.panel.focusController.focusedElement == visualElement;
    }

    public static void AddStyleSheetPath(this VisualElement visualElement, string path)
    {
        var sheet = Resources.Load<StyleSheet>(path);
        if (sheet != null)
            visualElement.styleSheets.Add(sheet);
    }

    public static void AddStyleSheetPathWithSkinVariant(this VisualElement visualElement, string path)
    {
        visualElement.AddStyleSheetPath(path);
        //if (true)
        {
            visualElement.AddStyleSheetPath(path + "Dark");
        }
        /*else
        {
            visualElement.AddStyleSheetPath(path + "Light");
        }*/
    }

    public static void ResetPositionProperties(this VisualElement visualElement)
    {
        var style = visualElement.style;
        style.position = StyleKeyword.Null;
        style.marginLeft = StyleKeyword.Null;
        style.marginRight = StyleKeyword.Null;
        style.marginBottom = StyleKeyword.Null;
        style.marginTop = StyleKeyword.Null;
        style.left = StyleKeyword.Null;
        style.top = StyleKeyword.Null;
        style.right = StyleKeyword.Null;
        style.bottom = StyleKeyword.Null;
        style.width = StyleKeyword.Null;
        style.height = StyleKeyword.Null;

    }

    public static Vector2 GlobalToBound(this VisualElement visualElement, Vector2 position)
    {
        return visualElement.worldTransform.inverse.MultiplyPoint3x4(position);
    }

    public static Vector2 BoundToGlobal(this VisualElement visualElement, Vector2 position)
    {
        position = visualElement.worldTransform.MultiplyPoint3x4(position);

        return position;
    }
}
