using System;
using UnityEngine;
using UnityEngine.UIElements;

public class Readme : ScriptableObject
{
    public StyleSheet commonStyle;
    public StyleSheet darkStyle;
    public StyleSheet lightStyle;
    public Texture2D icon;
    public string title;
    public Section[] sections;
    public bool loadedLayout;

    [Serializable]
    public class Section
    {
        public string heading, text, linkText, url;
    }
}
