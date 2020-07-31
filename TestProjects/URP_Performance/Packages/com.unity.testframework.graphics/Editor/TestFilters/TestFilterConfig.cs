using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Collections.Generic;

[System.Serializable]
public class TestFilterConfig
{
    public SceneAsset FilteredScene;
    public SceneAsset[] FilteredScenes;
    public ColorSpace ColorSpace = ColorSpace.Uninitialized;
    public BuildTarget BuildPlatform = BuildTarget.NoTarget;
    public GraphicsDeviceType GraphicsDevice = GraphicsDeviceType.Null;
    public string XrSdk;
    public StereoRenderingModeFlags StereoModes;
    public string Reason;
}

public enum StereoRenderingModeFlags
{
    None = 0,
    MultiPass = 1,
    SinglePass = 2,
    Instancing = 4
}