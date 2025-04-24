# Unlit material

The Unlit Shader and the Unlit Shader Graph let you create Materials that are not affected by lighting. They include options for the Surface Type, Emissive Color, and GPU Instancing. For more information about Materials, Shaders and Textures, see the [Unity User Manual](https://docs.unity3d.com/Manual/Shaders.html).

![Two glowing strip lights in a very dark industrial environment.](Images/HDRPFeatures-UnlitShader.png)

## Creating an Unlit Material

New Materials in HDRP use the [Lit Shader](lit-material.md) by default. To create an Unlit Material, you need to create a new Material then make it use the Unlit Shader. To do this:

1. In the Unity Editor, navigate to your Project's Asset window.

2. Right-click the Asset Window and select __Create > Material__. This adds a new Material to your Unity Project’s Asset folder.

3. Click the __Shader__ drop-down at the top of the Material Inspector, and select __HDRP > Unlit__.

Refer to [Unlit Material Inspector reference](unlit-material-inspector-reference.md) for more information.

## Creating an Unlit Shader Graph

To create an Unlit material in Shader Graph, you can either:

* Modify an existing Shader Graph.

    1. Open the Shader Graph in the Shader Editor.
    2. In **Graph Settings**, select the **HDRP** Target. If there isn't one, go to **Active Targets,** click the **Plus** button and select **HDRP**.
    3. In the **Material** drop-down, select **Unlit**.

* Create a new Shader Graph. Go to **Assets** > **Create** > **Shader Graph** > **HDRP** and select **Unlit Shader Graph**.

Refer to [Unlit Master Stack reference](unlit-master-stack-reference.md) for more information.