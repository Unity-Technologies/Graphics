# 2D Pixel Perfect

The __2D Pixel Perfect__ package contains the __Pixel Perfect Camera__ component, which ensures your pixel art remains crisp and clear at different resolutions, and stable in motion.

It is a single component that makes all the calculations Unity needs to scale the viewport with resolution changes, so that you don’t need to do it manually. You can use the component settings to adjust the definition of the rendered pixel art within the camera viewport, and you can use the __Run in Edit Mode__ feature to preview any changes immediately in the Game view.

![Pixel Perfect Camera gizmo](Images/2D/2D_Pix_image_0.png)

Attach the __Pixel Perfect Camera__ component to the main Camera GameObject in the Scene, it is represented by two green bounding boxes centered on the __Camera__ gizmo in the Scene view. The solid green bounding box shows the visible area in Game view, while the dotted bounding box shows the __Reference Resolution.__

The __Reference Resolution__ is the original resolution your Assets are designed for, its effect on the component's functions is detailed further in the documentation.

Before using the component, first ensure your Sprites are prepared correctly for best results with the the following steps.

## Preparing Your Sprites

1. After importing your textures into the project as Sprites, set all Sprites to the same __Pixels Per Unit__ value.

    ![Setting PPU value](Images/2D/2D_Pix_image_1.png)

2. In the Sprites' Inspector window, set their __Filter Mode__ to ‘Point’.

    ![Set 'Point' mode](Images/2D/2D_Pix_image_2.png)

3. Set their __Compression__ to 'None'.

    ![Set 'None' compression](Images/2D/2D_Pix_image_3.png)

4. Follow the steps below to correctly set the pivot for a Sprite

    1. Open the __Sprite Editor__ for the selected Sprite.

    2. If __Sprite Mode __is set to ‘Multiple’ and there are multiple __Sprite__ elements,  then you need to set a pivot point for each individual Sprite element.

    3. Under the Sprite settings, set __Pivot__ to ‘Custom’, then set __Pivot Unit Mode__ to ‘Pixels’. This allows you to set the pivot point's coordinates in pixels, or drag the pivot point around freely in the __Sprite Editor__ and have it automatically snap to pixel corners.

    4. Repeat for each __Sprite__ element as necessary.

    ![Setting the Sprite’s Pivot](Images/2D/2D_Pix_image_4.png)

## Snap Settings

To ensure the pixelated movement of Sprites are consistent with each other, follow the below steps to set the proper snap settings for your project.

![Snap Setting window](Images/2D/2D_Pix_image_5.png)

1. To open the __Snap settings__, go to __Edit__ > __Snap Settings.__

2. Set the __Move X/Y/Z__ properties to 1 divided by the Pixel Perfect Camera’s __Asset Pixels Per Unit (PPU)__ value. For example, if the Asset __PPU__ is 100, you should set the __Move X/Y/Z__ properties to 0.01 (1 / 100 = 0.01).

3. Unity does not apply Snap settings retroactively. If there are any pre-existing GameObjects in the Scene, select each of them and select __Snap All Axes__ to apply the Snap settings.

## Properties

![Property table](Images/2D/2D_Pix_image_6.png)
The component's Inspector window

|__Property__|__Function__|
| --- | --- |
|__Asset Pixels Per Unit__|This is the amount of pixels that make up one unit of the Scene. Match this value to the __Pixels Per Unit__ values of all Sprites in the Scene.|
|__Reference Resolution__|This is the original resolution your Assets are designed for.|
|__Upscale Render Texture__|Enable this property to create a temporary rendered texture of the Scene close-to or at the Reference Resolution, which is then upscaled.|
|__Pixel Snapping (only available when ‘Upscale Render Texture’ is disabled)__|Enable this feature to snap __Sprite Renderers__ to a grid in world space at render-time. The grid size is based on the Assets’ __Pixels Per Unit__ value.|
|__Crop Frame__|Crops the viewport with black bars to match the Reference Resolution along the checked axis. Check X to add horizontal black bars, and Y to add vertical black bars. For more information and a visual example, refer to the Property Details below.|
|__Stretch Fill (available when both X and Y are checked)__|Enable to expand the viewport to fit the screen resolution while maintaining the viewport's aspect ratio.|
|__Run In Edit Mode__| Enable this checkbox to preview Camera setting changes in Edit Mode. This causes constant changes to the Scene while active. |
|__Current Pixel Ratio (available when ‘Run In Edit Mode’ is enabled)__|Shows the size ratio of the rendered Sprites compared to their original size.|

## Additional Property Details

### Reference Resolution

This is the original resolution your Assets are designed for. Scaling up Scenes and Assets from this resolution preserves your pixel art cleanly at higher resolutions.

### Upscale Render Texture

By default, the Scene is rendered at the pixel perfect resolution closest to the full screen resolution.

Enable this option to have the Scene rendered to a temporary texture set as close as possible to the __Reference Resolution__, while maintaining the full screen aspect ratio. This temporary texture is then upscaled to fit the entire screen.

![Box examples](Images/2D/2D_Pix_image_7.png)

The result is unaliased and unrotated pixels, which may be a desirable visual style for certain game projects.

### Pixel Snapping

Enable this feature to snap Sprite Renderers to a grid in world space at render-time. The grid size is based on the __Assets Pixels Per Unit__ value.

__Pixel Snapping__ prevents subpixel movement and make Sprites appear to move in pixel-by-pixel increments. This does not affect any GameObjects' Transform positions.

### Crop Frame

Crops the viewport along the checked axis with black bars to match the __Reference Resolution__. Black bars are added to make the Game view fit the full screen resolution.

| ![Uncropped cat](Images/2D/2D_Pix_image_8.png) | ![Cropped cat](Images/2D/2D_Pix_image_9.png) |
| :--------------------------------------------: | :------------------------------------------: |
|                   Uncropped                    |                   Cropped                    |
