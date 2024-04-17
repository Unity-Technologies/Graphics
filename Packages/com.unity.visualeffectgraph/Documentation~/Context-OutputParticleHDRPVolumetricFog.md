# Output Particle HDRP Volumetric Fog

The **Output Particle HDRP Volumetric Fog** Context allows for the injection of fog directly into HDRP's volumetric lighting system, resulting in dynamic fog effects created with the VFX graph simulation.

Prior to using this output, ensure that volumetric fog is enabled within your project. Refer to the [HDRP Volumetric Lighting](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/Volumetric-Lighting.html) documentation page for more information on this.

The **Output Particle HDRP Volumetric Fog** Context converts all the particles of the system into volumetric fog for HDRP. To do this, it converts each particle into a solid sphere. It sets the sphere's center as the particle's center, and determines the sphere's size based on the particle's size.

The particle color controls the color of the fog, and the particle alpha controls the density of the fog, making color and alpha the two primary inputs for this Context.

Menu Path : **Context > Output Particle HDRP Volumetric Fog**

# Context Settings

| **Input**                    | **Type** | **Description**                                                                                                                                                                                                                                                                               |
|------------------------------|----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Fog Blend Mode** | Enum     | Determines how the particle's fog is blended with the existing fog in the scene. This mode works well with the [Renderer priority](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest?subfolder=/manual/VisualEffectComponent.html#rendering-properties) as it allows to control the order of fog blending when commutative blending modes are used. This property is similar to the **priority** in the [Local Volumetric Fog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/Local-Volumetric-Fog.html#properties) component. |
| **Falloff Mode**     | Enum     | Determines the type of falloff to apply on the **Fade Radius**, similar to the "Falloff Mode" in the [Local Volumetric Fog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/Local-Volumetric-Fog.html#properties) component. |
| **Use Mask Map**        | Bool     | Exposes a 3D texture on the output Context, sampled in the particle's volume. Note that because the particle shape is a sphere, 3D texture corners are never sampled. |
| **Use Distance Fading**    | Bool     | Exposes two ports to control the particle fading. Distance fade consists of a Start and End distance, where the particle alpha is interpolated to 0 (that is, when the particle center distance from the camera equals **Distance Fade Start** the alpha is multiplied by 1, and when the distance reaches **Distance Fade End** it is multiplied by 0). Distance Fading can help prevent small fog particles from flickering. |


# Context Properties

| **Input**             | **Type** | **Description**   |
|-----------------------|----------|-------------------|
| **Fade Radius**       | Float    | A value between 0 and 1 indicating the level of softness for the sphere border. Lower values create harder edges and more visible aliasing, so it is recommended to keep this value fairly high when fog particles are small. |
| **Density**        | Float  | Determines the density of fog inside the particle. Higher density results in more opaque fog. |
| **Mask**      | Texture3D    | A 3D texture that adds detail inside fog particles. The RGB channels of the texture are multiplied by the particle color to create the final fog color. The alpha channel of the texture is multiplied by the density and the particle alpha to create the final fog density. |
| **UV Scale** | Vector3    | Allows scaling of the UVs of the 3D texture. |
| **UV Bias** | Vector3    | Allows adding an offset in the UVs of the texture. The offset is applied after scaling. |
| **Distance Fade Start** | Float    | Determines the distance from the camera to the particle center where the particle begins to fade. |
| **Distance Fade End** | Float    | Determines the distance from the camera to the particle center where the particle ends fading. |


# Limitations

- This Output does not support [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest).
- For this Context to work, enable Volumetric Fog in the HDRP Asset and in the HDRP Global Settings.
- The quality of the fog is determined by the settings of the [Fog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/Override-Fog.html) volume component in your scene, so make sure to adjust these values for better visuals.
- Only spheres are supported as the primitive to output fog.
