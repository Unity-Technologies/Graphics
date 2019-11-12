# SRP Asset

The SRP Asset contains the interface that you can use to configure a render pipeline. When Unity performs rendering for the first time, it calls `InternalCreatePipeline` on the Asset and the Asset must return a usable rendering instance. 

The SRP Asset itself is a [ScriptableObject](https://docs.unity3d.com/Manual/class-ScriptableObject.html), which means that it can be a Project Asset and you can save it in your Project and version control works with it correctly. If you want to save a configuration for others to use, you need to create an SRP Asset in your Project. You can create an SRP just like any other ScriptableObject via Script and then save it via the Asset Database API. 

To make Unity use an SRP Asset in your Project, you need to set the Asset via GraphicsSettings. When you set the Asset reference here, Unity uses SRP rendering in your Project and diverts rendering from standard Unity rendering to the configuration the SRP Asset provides.

In addition to returning an instance and holding configuration data, you can also use the SRP Asset to provide a number of helper functions for things like:

- Default Material to use when creating 3d GameObjects.
- Default Material to use when creating 2d GameObjects.
- Default Material to use when creating Particle Systems.
- Default Material to use when creating Terrain.

This is essentially providing hook points to ensure that the end to end editor experience is correct. If you construct a pipeline and would like it to mimic the editor behaviour of the existing Unity pipelines, these steps are necessary.

## An SRP Asset example
The Asset contains rendering properties and returns an instance of a pipeline that Unity can use to render your Scene. If a setting on the Asset changes, Unity destroys all current instances and creates a new instance with the new settings to use for the next frame.

The example below shows an SRP Asset class. It contains a color that the [SRP Instance](SRP-Instance.md) uses to clear the screen. There is also some editor only code that assists the user in creating an SRP Asset in the Project. This is important as you need to set this Asset in the graphics settings window.

```C#
[ExecuteInEditMode]
public class BasicAssetPipe : RenderPipelineAsset
{
    public Color clearColor = Color.green;

#if UNITY_EDITOR
    // Call to create a simple pipeline
    [UnityEditor.MenuItem("SRP-Demo/01 - Create Basic Asset Pipeline")]
    static void CreateBasicAssetPipeline()
    {
        var instance = ScriptableObject.CreateInstance<BasicAssetPipe>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/BasicAssetPipe.asset");
    }
#endif

    // Function to return an instance of this pipeline
    protected override IRenderPipeline InternalCreatePipeline()
    {
        return new BasicPipeInstance(clearColor);
    }
}
```
