# Custom Material Inspector

Custom Material Inspectors enable you to define how Unity displays properties in the Material Inspector for a particular shader. This is useful if a shader includes a lot of properties and you want to organize them in the Inspector. The Universal Render Pipeline (URP) and High Definition Render Pipeline (HDRP) both support custom Material Inspectors, but the method to create them is slightly different.

## Creating a custom Material Inspector

The implementation for custom Material Inspectors differs between URP and HDRP. For example, for compatibility purposes, every custom Material Inspector in HDRP must inherit from `HDShaderGUI` which does not exist in URP. For information on how to create custom Material Inspectors for the respective render pipelines, see:

- **HDRP**: [HDRP custom Material Inspectors](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/custom-material-inspectors.html).
- **URP**: [Unity Custom Shader GUI](https://docs.unity3d.com/Manual/SL-CustomShaderGUI.html).

## Assigning a custom Material Inspector

When you create a shader, either hand-written or using Shader Graph, both URP and HDRP provide a default editor for it to use. To override this default and provide your own custom Material Inspector, the method differs depending on whether you hand-wrote the shader or used Shader Graph.

### Using hand-written shaders

To set a custom Material Inspector for a hand-written shader:

1. Open the shader source file.
2. Assign a string that contains the class name of the custom Material Inspector to the **CustomEditor** shader instruction.

This is the same method as for the Built-in Renderer's [custom shader GUI](<https://docs.unity3d.com/Manual/SL-CustomShaderGUI.html>).

For an example of how to do this, see the following shader code sample. In this example, the name of the custom Material Inspector class is **ExampleCustomMaterialInspector**:

```c#
Shader "Custom/Example"
{
    Properties
    {
        // Shader properties
    }
    SubShader
    {
        // Shader code
    }
    CustomEditor "ExampleCustomMaterialInspector"
}
```


### Using Shader Graph

To set a custom Material Inspector for a Shader Graph shader:

1. Open the Shader Graph.
2. In the [Graph Inspector](<https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/Internal-Inspector.html>), open the Graph Settings tab.
3. If **Active Targets** does not include the render pipeline your project uses, click the **plus** button then, in the drop-down, click the render pipeline.
4. In the render pipeline section (**HDRP** or **URP** depending on the render pipeline your project uses) find the **Custom Editor GUI** property and provide it the name of the custom Material Inspector.
