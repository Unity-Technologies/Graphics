# Use a full-screen shader with a custom post-processing volume

You can use HDRP’s [Fullscreen Master Stack](fullscreen-master-stack-reference.md) to create a full-screen shader that you can use in a custom post-processing effect. This means you don’t need to write any shader code.

To use a full-screen shader with a custom post-processing volume: 

1. In the **Project** window, select the full-screen shader graph to view it in the Inspector. 
2. Find the name and subcategory of the fullscreen shader. For example **`ShaderGraphs/Fullscreen_PostProcess`**
3. In the **Project** window, double-click the custom post-processing volume script to open it in a script editor. 
4. In the custom post-processing volume script, find the following line that defines the name and subcategory of the shader the volume uses: 

```c#
 const string kShaderName = "Hidden/Shader/NewPostProcessVolume";
```

5. Replace `Hidden/Shader/NewPostProcessVolume` with the name and subcategory of the fullscreen shader you want to use in this custom post-processing effect. For example:

```c#
const string kShaderName = "ShaderGraphs/Fullscreen_PostProcess";
```

