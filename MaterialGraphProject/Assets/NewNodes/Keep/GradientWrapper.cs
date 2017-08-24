using UnityEngine;
using UnityEditor;

using System.Reflection;
using System.Linq;

using Activator = System.Activator;
using Array = System.Array;
using Type = System.Type;

[System.Serializable]
public class GradientWrapper
{
    /// <summary>
    /// Wrapper for <c>GradientColorKey</c>.
    /// </summary>
    public struct ColorKey
    {
        public Color color;
        public float time;

        public ColorKey(Color color, float time)
        {
            this.color = color;
            this.time = time;
        }
    }

    /// <summary>
    /// Wrapper for <c>GradientAlphaKey</c>.
    /// </summary>
    public struct AlphaKey
    {
        public float alpha;
        public float time;

        public AlphaKey(float alpha, float time)
        {
            this.alpha = alpha;
            this.time = time;
        }
    }

    #region Initial Setup

    /// <summary>
    /// Type of gradient.
    /// </summary>
    public static Type s_tyGradient;

#if (UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9)
    private static MethodInfo s_miEvaluate;
    private static PropertyInfo s_piColorKeys;
    private static PropertyInfo s_piAlphaKeys;

    private static Type s_tyGradientColorKey;
    private static Type s_tyGradientAlphaKey;
#endif

    /// <summary>
    /// Perform one-off setup when class is accessed for first time.
    /// </summary>
    static GradientWrapper()
    {
#if (UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9)
        Assembly editorAssembly = typeof(Editor).Assembly;

        s_tyGradientColorKey = editorAssembly.GetType("UnityEditor.GradientColorKey");
        s_tyGradientAlphaKey = editorAssembly.GetType("UnityEditor.GradientAlphaKey");

        // Note that `Gradient` is defined in the editor namespace in Unity 3.5.7!
        s_tyGradient = editorAssembly.GetType("UnityEditor.Gradient");
        s_miEvaluate = s_tyGradient.GetMethod("CalcColor", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(float) }, null);
        s_piColorKeys = s_tyGradient.GetProperty("colorKeys", BindingFlags.Public | BindingFlags.Instance);
        s_piAlphaKeys = s_tyGradient.GetProperty("alphaKeys", BindingFlags.Public | BindingFlags.Instance);
#else
        // In Unity 4 this is easy :)
        s_tyGradient = typeof(Gradient);
#endif
    }

    #endregion

#if (UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9)
    #region Unity 3.5.7 Implementation

    private object _gradient = Activator.CreateInstance(s_tyGradient);

    public object GradientData
    {
        get { return _gradient; }
        set { _gradient = value; }
    }

    public Color Evaluate(float time)
    {
        return (Color)s_miEvaluate.Invoke(_gradient, new object[] { time });
    }

    public void SetKeys(ColorKey[] colorKeys, AlphaKey[] alphaKeys)
    {
        if (colorKeys != null)
        {
            Array colorKeyParam = (Array)Activator.CreateInstance(s_tyGradientColorKey.MakeArrayType(), new object[] { colorKeys.Length });
            for (int i = 0; i < colorKeys.Length; ++i)
                colorKeyParam.SetValue(Activator.CreateInstance(s_tyGradientColorKey, colorKeys[i].color, colorKeys[i].time), i);
            s_piColorKeys.SetValue(_gradient, colorKeyParam, null);
        }
        if (alphaKeys != null)
        {
            Array alphaKeyParam = (Array)Activator.CreateInstance(s_tyGradientAlphaKey.MakeArrayType(), new object[] { alphaKeys.Length });
            for (int i = 0; i < alphaKeys.Length; ++i)
                alphaKeyParam.SetValue(Activator.CreateInstance(s_tyGradientAlphaKey, alphaKeys[i].alpha, alphaKeys[i].time), i);
            s_piAlphaKeys.SetValue(_gradient, alphaKeyParam, null);
        }
    }

    #endregion
#else
    #region Unity 4.x Implementation

    private Gradient _gradient = new Gradient();

    public object GradientData
    {
        get { return _gradient; }
        set { _gradient = value as Gradient; }
    }

    public Color Evaluate(float time)
    {
        return _gradient.Evaluate(time);
    }

    public void SetKeys(ColorKey[] colorKeys, AlphaKey[] alphaKeys)
    {
        GradientColorKey[] actualColorKeys = null;
        GradientAlphaKey[] actualAlphaKeys = null;

        if (colorKeys != null)
            actualColorKeys = colorKeys.Select(key => new GradientColorKey(key.color, key.time)).ToArray();
        if (alphaKeys != null)
            actualAlphaKeys = alphaKeys.Select(key => new GradientAlphaKey(key.alpha, key.time)).ToArray();

        _gradient.SetKeys(actualColorKeys, actualAlphaKeys);
    }

    #endregion
#endif
}
