# Rendering Debug Panel

The Rendering Debug Panel contains tools for various HDRP rendering features.

| **Debug Item**                | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **Fullscreen Debug Mode**     | Shows an overlay of various rendering features.              |
| **- Motion Vectors**          | Displays motion vectors on the screen.                       |
| **- NaN tracker**             | Displays an overlay where NaN values are highlighted.        |
| **MipMaps**                   | Displays texture mipmap state. |
| **- MipRatio**                | Displays heat map of pixel to texel ratio. Blue tint for too little texture detail (texture too small). Red tint for too much detail (texture too large for screen area). Original colour for correct match. |
| **- MipCount**                | Displays mip count as gray scale from black to white as the number of mips increases (for up to 14 mips, or 16K size). Red warns of textures with more than 14 mips. Magenta indicates 0 mips or no shader support for mip count.|
| **- MipCountReduction**       | Displays difference between current mip count and original mip count as a green scale. Brighter green for a larger reduction (i.e. more texture memory saved by mip streaming). Magenta if original mip count is unknown. |
| **- StreamingMipBudget**      | Displays mip status due to streaming budget. Green where memory savings made by streaming. Red where mip levels are lower than desired, due to full texture memory budget. White for textures where no savings made. |
| **- StreamingMip**            | Displays same information as StreamingMipBudget, with the colors applied to the original textures. |
| **-- Terrain Texture**        | Allows user to select the terrain texture used with the mip map debugging. | 
| **Color Picker**              | Allows user to check values of specific pixels with the mouse. |
| **- Debug Mode**              | Allows users to select the format of the color picker display (Byte, Byte4, float or float4). |
| **- Font Color**              | Allows users to select the color of the font used for color picker display. |
| **False Color Mode**          | Allows users to define intensity ranges for showing a color temperature gradient for the currently displayed frame. |
| **MSAA Samples**              | Allows users to select the number of samples used by MSAA.   |
| **Freeze Camera for Culling** | Freezes the camera for culling. This feature is useful to check if the culling is working correctly by freezing it and moving around occluders with the camera. |

<u>Note on color picker:</u>
The color picker will work with whichever debug mode is displayed at the time. It means that users can actually see values of various component of the rendering like Albedo, Diffuse Lighting, etc. By default it will display the value of the main HDR color buffer.

