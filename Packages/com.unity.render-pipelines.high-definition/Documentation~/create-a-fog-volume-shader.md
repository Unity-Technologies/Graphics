# Create a Fog Volume shader

Use the Fog Volume [shader graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Getting-Started.html) to create volumetric fog and environmental effects. HDRP includes an example scene that demonstrates different volumetric fog effects in the [Volumetric Sample scene](HDRP-Sample-Content.md#volumetric-samples).

For details on the settings in the Fog Volume shader graph, refer to [Fog Volume Master Stack](fog-volume-master-stack-reference.md)

![](Images/Volumetric-ground-fog.png)

<a name="create-volumetric-shadergraph"></a>

## Create a Fog Volume shader

You can create a Fog Volume shader in [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/First-Shader-Graph.html) in one of the following ways:

- Create a new Fog Volume Shader Graph:

- - Go to **Assets** > **Create** > **Shader Graph** > **HDRP** > **Fog Volume Shader Graph**.

- Change an existing shader graph:

1. In the **Project** window, select a shader graph to open it in the Shader Editor.
2. In **Graph Settings**, select the **HDRP** Target. If there isn't one, go to **Active Targets**, click the **Plus** button, and select **HDRP**.
3. In the **Material** drop-down, select **Fog Volume**.

- Open a Fog Volume shader in the [volumetric sample scene](HDRP-Sample-Content.md#volumetric-samples).

1. Open the volumetric sample scene.
2. In the Project window, go to **Assets** > **Samples** > **High Definition RP** > HDRP version number > **Volumetric samples** > **Fog Volume Shadergraph.**
3. Double-click a shader graph asset to open it in Shader Graph. 

This creates a shader graph that contains the [Fog Volume master stack](fog-volume-master-stack-reference.md). 

Refer to [Create a local fog effect](create-a-local-fog-effect.md) for more information about applying a Fog Volume shader.

<a name="setup-fog-volume-shadergraph"></a>

## Set up a Fog Volume in Shader Graph

Volumetric effects in Shader Graph require a 3D input. To get this 3D input, use one or both of the following nodes: 

- [UV](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/UV-Node.html): Use this node to sample along the local, normalized position inside the volume along the X,Y and Z axes.
- [Position](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Position-Node.html): Use this node to sample along the Z and X axes starting at a value of 0 in the center of the fog volume.

You can use these nodes as inputs to the [Sample Texture 3D](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/Sample-Texture-3D-Node.html) node. 

To find an example of a shader graph that uses these nodes, open the volumetric sample scene.

**Note**: To create a fog effect in world space that isn’t affected by the volume’s position, set the Position node’s **Space** property to **Absolute World**. This is because HDRP uses [Camera Relative Rendering](Camera-Relative-Rendering.md).

## Optimize performance

This section explains how to improve how a Fog Volume shader graph looks and performs. To fix artefacts in a Fog Volume shader, refer to [Fix issues with Local Volumetric Fog](troubleshoot-fog.md).

- Keep the number of nodes in a volumetric fog shader graph to a minimum. This is because a high number of Shader graph calculations limit the performance of a volumetric fog shader on the GPU. 
- HDRP invokes a pixel for every voxel it executes in a local volumetric fog. To lower the amount of resources this uses, reduce the size of the volumetric fog volume.
- Enable mipmaps in the [Texture Importer](https://docs.unity3d.com/Manual/class-TextureImporter.html) for any textures you sample in Shader Graph. This reduces the time HDRP takes to fetch textures because the resolution of the V-buffer is often lower than the screen resolution.
- Keep the screen space size of the fog volume as small as possible. To check how much screen space the fog volume takes up, go to **Window** > **Rendering Debugger** > **Rendering** > **Fullscreen Debug Mode** and use the debug mode **LocalVolumetricFogOverdraw.**
- If the **Fog Volume Mesh Voxelization** step is slower than the **Volumetric Lighting** step in the [GPU profiler](https://docs.unity3d.com/Manual/ProfilerGPU.html), this tells you that your scene uses too many fog volumes or that the fog volumes in your scene are too large. To fix this, reduce the number of volumes in your scene or lower the quality of the volumetric fog.
-  In the [Fog volume override](fog-volume-override-reference.md), set the **Screen Resolution Percentage** to a low value to improve performance. Low values work best with diffuse fog. Values over 30 use a high amount of memory on the GPu.
