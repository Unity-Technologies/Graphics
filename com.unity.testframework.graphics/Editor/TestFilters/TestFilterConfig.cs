using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

[System.Serializable]
public class TestFilterConfig
{
    public SceneAsset FilteredScene;
    public ColorSpace ColorSpace = ColorSpace.Uninitialized;
    public BuildTarget BuildPlatform = BuildTarget.NoTarget;
    public GraphicsDeviceType GraphicsDevice = GraphicsDeviceType.Null;
    public string Reason;
}
