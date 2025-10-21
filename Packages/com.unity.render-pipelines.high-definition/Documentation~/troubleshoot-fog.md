# Troubleshoot fog

This document explains how to fix some common visual artifacts that can appear when you use a fog volume.

## Slice artifacts

A slice artifact is the appearance of thin layers around the edge of the volumetric fog. To fix this issue, go to the Local Volumetric Fog component and adjust the following settings:

- **Blend Distance**: Set this value above 0 to improve visual transitions at the volume’s border and reduce slice artifacts.
- **Slice Distribution Uniformity**: Increase this value to make the edge of the fog more detailed as it gets closer to the camera.

## Sharp edges 

To remove the appearance of sharp areas or corners in a volumetric fog, open the [Global Fog](create-a-global-fog-effect.md) component and set the **Denoising Mode** property to one of the following modes: 
- **None**.
- **Reprojection**:  Disables denoising for fog with sharp edges or hard corners. This option maintains detail, but can introduce ghosting artifacts, and doubles the amount of memory volumetric fog uses.
- **Gaussian**: Creates soft, blurry fog with undefined shapes. This mode smooths out noise but can blur out sharp features.
- **Both**: Applies both Reprojection and Gaussian techniques. This setting significantly increases the volumetric fog's memory usage and GPU time.

For more physically accurate blending between the fog and the environment, set **Falloff Mode** to **Exponential**.

## Light flickering

To stop light from flickering in a fog volume, change the following properties: 

- **Slice Distribution Uniformity**: Set this property to a low value to make the slice artifacts appear the same near and far away from the camera. A value between 0.5 and 0.9 fixes light flickering in most situations. 
- **Volume Slice Count**: Increase this value to preserve volumetric fog detail over long distances. Set this property to a high value and set the **Slice Distribution Uniformity** to a low value to fix light flickering in distant areas of the fog volume.
- **Volumetric Fog Distance**: Set this value between the **Distance Fade Start** and **Distance Fade End** values set in the Local Volumetric Fog component. This makes the fog fade out at the same point as its maximum distance, which reduces light flickering in the distant areas of the fog volume.
- **Radius**: Increase this value in the [Light](Light-Component.md) component. This lowers the intensity of the light source which makes flickering artifacts less visible.

## Optimize fog performance

How to optimize volumetric fog depends on the content of your scene and the quality you want. For more information about fog properties, refer to [Fog Volume Override reference](fog-volume-override-reference.md) and [Local Volumetric Fog Volume reference](local-volumetric-fog-volume-reference.md).

To make sure all parameters are visible in the **Inspector** window, follow these steps in each Local Volumetric Fog Volume:

1. Set **Quality** to **Custom**.
2. Set **Fog Control Mode** to **Manual**.
3. At the top of the **Fog** component, open the **More** (⋮) menu and enable **Advanced Properties**.

Try adjusting the following properties:

- Decrease the **Distance** parameter to reduce the number of fog slices HDRP renders. For long-distance fog, adjust the **Slice Distribution Uniformity** and **Slice Count** instead.
- Decrease **Volume Slice Count** to reduce the number of fog slices along the camera's forward axis, to reduce GPU and memory usage. Or increase **Volume Slice Count** and reduce **Slice Distribution Uniformity** to focus more slices further from the camera.
- Set the near and far clipping planes of the camera, to avoid fog rendering too close to the camera. This is particularly important in scenarios such as top-down views, where there may be a significant distance between the camera and the first visible fog or objects.
- Set **Denoising Mode** to **Gaussian** instead of **Reprojection**, so the fog uses less memory.
- Reduce the **Screen Resolution Percentage** value to a low value, to limit how much memory and computation fog uses, especially if you have highly diffuse fog.
- Enable **Directional Lights Only**, especially if you have a large or outdoor scene where only sunlight should contribute to volumetric effects.

### Skip lighting calculations in areas with low-density fog

To skip lighting calculations for areas where the fog density is below a certain threshold, use the **Volumetric Lighting Density Cutoff** property.

This property can improve performance if you use [Local Volumetric Fog Volumes](local-volumetric-fog-volume-reference.md). It doesn't affect scenes with constant or height-based fog because all areas have the same density.

Follow these steps:

1. To remove global fog from the scene, set a very high **Fog Attenuation Distance**.
2. Add Local Volumetric Fog Volumes where you want fog to appear.
3. Increase the **Volumetric Lighting Density Cutoff** gradually, until important fog features begin to disappear.

**Note**: This property doesn't take into account changes applied by the **Anisotropy** property. The visibility of fog might not correspond directly to the **Volumetric Lighting Density Cutoff** value in scenes with a lot of anisotropic scattering.
