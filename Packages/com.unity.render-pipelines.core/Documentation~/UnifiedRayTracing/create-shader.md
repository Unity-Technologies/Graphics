# Create a unified ray tracing shader

Depending on the backend you choose in the [`RayTracingContext`](xref:UnityEngine.Rendering.UnifiedRayTracing.RayTracingContext), write your ray tracing code in either a `.raytrace` or a `.compute` shader.

To create both a `.raytrace` shader and a `.compute` shader, write your shader code in a unified ray tracing shader (`.urtshader`).

To create a shader, follow these steps:

1. Create the shader asset file.
2. Load the shader.

## Create the shader asset file
To create a unified ray tracing shader:

1. Launch the Unity Editor.

1. In the **Project** window, open the **Assets** folder.

1. Open or create the folder in which you want to create your shader.

1. Open the context menu (right-click) and select **Create** &gt; **Shader** &gt; **Unified Ray Tracing Shader**.

1. Enter a name for the shader.
 
Ray tracing occurs in the `RayGenExecute` function. You can edit the example code snippet.

For more information about writing a ray tracing shader, refer to [Write your shader code](write-shader.md).

## Load the shader
To load the `.urtshader` file in the Editor, use the following code snippet:
```C# 
IRayTracingShader shader = rtContext.LoadRayTracingShader("Assets/yourShader.urtshader");
```

To load the shader in the Player, add the `.urtshader` shader to an <xref:UnityEngine.AssetBundle> then load it with the following:
```C# 
// Load the AssetBundle 
var asssetBundle = AssetBundle.LoadFromFile("Assets/pathToYourBuiltAssetBundles/yourAssetBundle");

// Load the shader
IRayTracingShader shader = rtContext.LoadRayTracingShaderFromAssetBundle(asssetBundle, "Assets/yourShader.urtshader");
```

To have more control, load the underlying Compute or Ray Tracing shader asset yourself and pass it to [`RayTracingContext.CreateRayTracingShader`](xref:UnityEngine.Rendering.UnifiedRayTracing.RayTracingContext.CreateRayTracingShader(UnityEngine.Object)). 

[`RayTracingContext.LoadRayTracingShaderFromAssetBundle`](xref:UnityEngine.Rendering.UnifiedRayTracing.RayTracingContext.LoadRayTracingShaderFromAssetBundle(UnityEngine.AssetBundle,System.String)) is a convenience function that performs the following operations:
```C# 
public IRayTracingShader LoadRayTracingShaderFromAssetBundle(AssetBundle assetBundle, string name)
{
    Object asset = assetBundle.LoadAsset(name, BackendHelpers.GetTypeOfShader(BackendType));
    return CreateRayTracingShader(asset);
}
```
