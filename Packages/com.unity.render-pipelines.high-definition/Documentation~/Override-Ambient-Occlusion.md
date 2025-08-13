# Screen space ambient occlusion (SSAO)

The Screen Space Ambient Occlusion (SSAO) volume override simulates [ambient occlusion](ambient-occlusion-introduction.md) in real-time.

![A single-channel screen space ambient occlusion texture of a gothic corridor. The scene is white with shades of grey representing corners and crevices.](Images/RayTracedAmbientOcclusion1.png)<br/>
A single-channel screen space ambient occlusion texture of a gothic corridor. The scene is white with shades of grey representing corners and crevices.

For each frame, SSAO creates a texture containing occluded areas in the camera view, which HDRP uses to reduce indirect lighting in those areas.

SSAO doesn't affect direct lighting, or the indirect light from Reflection Probes.

A screen-space effect only processes what's on-screen, so objects outside the camera view don't occlude objects in the camera view. You can sometimes see this at the edges of the screen. To include off-screen objects for better results, enable [Ray-traced ambient occlusion](Ray-Traced-Ambient-Occlusion.md) instead.

## Enable screen space ambient occlusion

Follow these steps:

1. Enable screen space ambient occlusion in your project.

   [!include[](snippets/Volume-Override-Enable-Override.md)]

   * To enable SSAO in your HDRP Asset, go to **Lighting** > **Screen Space Ambient Occlusion**.
   * To enable SSAO in your Frame Settings, go to **Edit** > **Project Settings** > **Graphics** > **Pipeline Specific Settings** > **HDRP** > **Frame Settings (Default Values)** > **Camera** > **Lighting** > **Screen Space Ambient Occlusion**.

2. [Add a volume component](set-up-a-volume.md#add-a-volume) to any GameObject in your scene.

3. Select the GameObject, then in the **Inspector** window select **Add Override** > **Lighting** > **Ambient Occlusion**.

   HDRP now applies screen space ambient occlusion to any camera this volume affects.

To access and control the volume override at runtime, refer to [Volume scripting API](Volumes-API.md#changing-volume-profile-properties).

## Additional resources

- [Assign an ambient occlusion texture](Ambient-Occlusion.md)
- [Ray-traced ambient occlusion](Ray-Traced-Ambient-Occlusion.md)
