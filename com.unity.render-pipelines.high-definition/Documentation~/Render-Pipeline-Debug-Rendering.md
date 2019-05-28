# Rendering Debug Panel

The Rendering Debug Panel contains tools for various HDRP rendering features.

| **Debug Item**                | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **Fullscreen Debug Mode**     | Shows an overlay of various rendering features.              |
| **- Motion Vectors**          | Displays motion vectors on the screen.                       |
| **- NaN tracker**             | Displays an overlay where NaN values are highlighted.        |
| **MipMaps**                   | Displays various mipmap streaming properties.                |
| **Color Picker**              | Allows user to check values of specific pixels with the mouse. |
| **- Debug Mode**              | Allows users to select the format of the color picker display (Byte, Byte4, float or float4). |
| **- Font Color**              | Allows users to select the color of the font used for color picker display. |
| **False Color Mode**          | Allows users to define intensity ranges for showing a color temperature gradient for the currently displayed frame. |
| **MSAA Samples**              | Allows users to select the number of samples used by MSAA.   |
| **Freeze Camera for Culling** | Freezes the camera for culling. This feature is useful to check if the culling is working correctly by freezing it and moving around occluders with the camera. |

<u>Note on color picker:</u>
The color picker will work with whichever debug mode is displayed at the time. It means that users can actually see values of various component of the rendering like Albedo, Diffuse Lighting, etc. By default it will display the value of the main HDR color buffer.

