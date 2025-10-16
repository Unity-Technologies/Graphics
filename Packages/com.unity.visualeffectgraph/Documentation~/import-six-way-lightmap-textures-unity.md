# Import six-way lightmap textures into Unity

Import and configure six-way lightmap textures for use in Visual Effect Graph.

To import and set up six-way lightmap textures, follow these steps:

1. In the **Project** window, go to your **Assets** folder.

1. Import the two six-way lightmap texture files into the folder.

1. For each imported texture, set the following:
    - Set **Max Size** to match the texture resolution (for example, 4096 for 4K textures).
    - Set **Compression** to balance quality and memory usage (for example, **High Quality (BC7)**).
    - Set **sRGB (Color Texture)** to match your export settings:
        - If you exported the texture in gamma space, enable **sRGB (Color Texture)**.
        - If you exported the texture in linear space, disable **sRGB (Color Texture)**.

1. Select **Apply** to save the import settings.

After you complete these steps, the six-way lightmap textures are ready for use in Visual Effect Graph.
