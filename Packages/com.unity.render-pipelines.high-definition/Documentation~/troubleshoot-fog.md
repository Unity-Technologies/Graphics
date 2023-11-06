# Troubleshoot fog

This document explains how to fix some common visual artifacts that can appear when you use a fog volume.

## Slice artifacts

A slice artifact is the appearance of thin layers around the edge of the volumetric fog. To fix this issue, go to the Local Volumetric Fog component and adjust the following settings:

- **Blend Distance**: Set this value above 0.
- **Slice Distribution Uniformity**: Increase this value to make the edge of the fog more detailed as it gets closer to the camera.
- **Volumetric Fog Distance**: Set this property between 20 and 100 to reduce the appearance of artifacts.

## Sharp edges 

To remove the appearance of sharp areas or corners in a volumetric fog, open the [Global Fog](create-a-global-fog-effect.md) component and set the **Denoising Mode** property to one of the following modes: 
- **None**.
- **Reprojection**: This mode can cause edges to have a blurry artifact (ghosting).
- **Gaussian**: Use this setting for blurry fog with no hard shapes.
- **Both**: Applies both Reprojection and Gaussian techniques. This setting significantly increases the volumetric fog's memory usage and GPU time.

## Light flickering

To stop light from flickering in a fog volume, change the following properties: 

- **Slice Distribution Uniformity**: Set this property to a low value to make the slice artifacts appear the same near and far away from the camera. A value between 0.5 and 0.9 fixes light flickering in most situations. 
- **Volume Slice Count**: Increase this value to preserve volumetric fog detail over long distances. Set this property to a high value and set the **Slice Distribution Uniformity** to a low value to fix light flickering in distant areas of the fog volume.
- **Volumetric Fog Distance**: Set this value between the **Distance Fade Start** and **Distance Fade End** values set in the Local Volumetric Fog component. This makes the fog fade out at the same point as its maximum distance, which reduces light flickering in the distant areas of the fog volume.
- **Radius**: Increase this value in the [Light](Light-Component.md) component. This lowers the intensity of the light source which makes flickering artifacts less visible.

