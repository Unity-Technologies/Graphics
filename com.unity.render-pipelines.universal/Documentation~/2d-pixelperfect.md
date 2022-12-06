# 2D Pixel Perfect

The **2D Pixel Perfect** package contains the **Pixel Perfect Camera** component, which ensures your pixel art remains crisp and clear at different resolutions, and stable in motion.

It is a single component that makes all the calculations Unity needs to scale the viewport with resolution changes, so that you don’t need to do it manually. You can use the component settings to adjust the definition of the rendered pixel art within the camera viewport, and preview any changes immediately in the Game view.

![Pixel Perfect Camera gizmo](Images/2D/2D_Pix_image_0.png)

Attach the **Pixel Perfect Camera** component to the main Camera GameObject in the Scene, it is represented by two green bounding boxes centered on the **Camera** gizmo in the Scene view. The solid green bounding box shows the visible area in Game view, while the dotted bounding box shows the **Reference Resolution.**

The **Reference Resolution** is the original resolution your Assets are designed for, its effect on the component's functions is detailed further in the documentation.

Before using the component, first ensure your Sprites are prepared correctly for best results with the the following steps.

## Preparing Your Sprites

1. After importing your textures into the project as Sprites, set all Sprites to the same **Pixels Per Unit** value.

    ![Setting PPU value](Images/2D/2D_Pix_image_1.png)

2. In the Sprites' Inspector window, set their **Filter Mode** to ‘Point’.

    ![Set 'Point' mode](Images/2D/2D_Pix_image_2.png)

3. Set their **Compression** to 'None'.

    ![Set 'None' compression](Images/2D/2D_Pix_image_3.png)

4. Follow the steps below to correctly set the pivot for a Sprite

    1. Open the **Sprite Editor** for the selected Sprite.

    2. If **Sprite Mode** is set to ‘Multiple’ and there are multiple **Sprite** elements,  then you need to set a pivot point for each individual Sprite element.

    3. Under the Sprite settings, set **Pivot** to ‘Custom’, then set **Pivot Unit Mode** to ‘Pixels’. This allows you to set the pivot point's coordinates in pixels, or drag the pivot point around freely in the **Sprite Editor** and have it automatically snap to pixel corners.

    4. Repeat for each **Sprite** element as necessary.

    ![Setting the Sprite’s Pivot](Images/2D/2D_Pix_image_4.png)

## Snap Settings

To ensure the pixelated movement of Sprites are consistent with each other, follow the below steps to set the proper snap settings for your project.

![Snap Setting window](Images/2D/2D_Pix_image_5.png)

1. To open the **Increment Snapping** settings, go to **Grid and Snap Overlay** in the Scene view.

2. Set the **Move X/Y/Z** properties to 1 divided by the Pixel Perfect Camera’s **Asset Pixels Per Unit (PPU)** value. For example, if the Asset **PPU** is 100, you should set the **Move X/Y/Z** properties to 0.01 (1 / 100 = 0.01).

![Grid Snap Setting window](Images/2D/2D_Pix_image_6.png)

1. Unity does not apply Snap settings retroactively. Select the **Grid Snapping** icon to enable it (highlighted in blue).

2. If there are any pre-existing GameObjects in the Scene, select each of them and select **All Axes** to apply the Snap settings.

## Properties

![Property table](Images/2D/2D_Pix_image_7.png)
The component's Inspector window

|**Property**|**Function**|
| --- | --- |
|**Asset Pixels Per Unit**|This is the amount of pixels that make up one unit of the Scene. Match this value to the **Pixels Per Unit** values of all Sprites in the Scene.|
|**Reference Resolution**|This is the original resolution your Assets are designed for.|
|**Crop Frame**| Describes what to do when there is a difference in aspect ratio.
|**Grid Snapping**| Describes how to handle snapping.
|**Filter Mode** (available if Stretch Fill option is enabled)| Describes how the final image is upscaled.
|**Current Pixel Ratio**|Shows the size ratio of the rendered Sprites compared to their original size.|

## Additional Property Details

### Reference Resolution

This is the original resolution your Assets are designed for. Scaling up Scenes and Assets from this resolution preserves your pixel art cleanly at higher resolutions.


### Grid Snapping
#### Upscale Render Texture

By default, the Scene is rendered at the pixel perfect resolution closest to the full screen resolution.

Enable this option to have the Scene rendered to a temporary texture set as close as possible to the **Reference Resolution**, while maintaining the full screen aspect ratio. This temporary texture is then upscaled to fit the entire screen.

![Box examples](Images/2D/2D_Pix_image_8.png)

The result is unaliased and unrotated pixels, which may be a desirable visual style for certain game projects.

#### Pixel Snapping

Enable this feature to snap Sprite Renderers to a grid in world space at render-time. The grid size is based on the **Assets Pixels Per Unit** value.

**Pixel Snapping** prevents subpixel movement and make Sprites appear to move in pixel-by-pixel increments. This does not affect any GameObjects' Transform positions.

### Crop Frame

Crops the viewport based on the option selected, adding black bars to match the **Reference Resolution**. Black bars are added to make the Game view fit the full screen resolution.

| ![Uncropped cat](Images/2D/2D_Pix_image_9.png) | ![Cropped cat](Images/2D/2D_Pix_image_10.png) |
| :--------------------------------------------: | :------------------------------------------: |
|                   Uncropped                    |                   Cropped                    |

### Filter Mode

**Filter Mode** is only usable when Stretch Fill option is enabled.

Defaults to **Retro AA** upscale filtering, where the image is upscaled as close as possible to the screen resolution as a multiple of the **Reference resolution**, followed by a bilinear filtering to upscale to the target screen resolution.

**Point** filtering is also available for user preference. If you upscale the image this way, it can suffer from bad pixel placement, thus losing pixel perfectness.


| ![Uncropped cat](Images/2D/2D_Pix_image_11.png) | ![Cropped cat](Images/2D/2D_Pix_image_12.png) |
| :--------------------------------------------: | :------------------------------------------: |
|                   Point                        |                   Retro AA                   |
| ![Uncropped cat](Images/2D/2D_Pix_image_13.png) | ![Cropped cat](Images/2D/2D_Pix_image_14.png) |
|         Upscale Render Texture + Point         |      Upscale Render Texture + Retro AA       |
