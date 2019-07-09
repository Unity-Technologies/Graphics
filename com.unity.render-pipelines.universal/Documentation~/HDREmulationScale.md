## HDR Emulation Scale

All Lights in the 2D lighting system support HDR. While a typical RGBA32 color channel has the range of zero to one, a HDR channel can go beyond one. 

| ![Light RGB(1,1,1)](images\image_32.png) | ![HDR Light RGB(1,1,1) + Light RGB(2,2,2)](images\image_33.png) |
| ---------------------------------------- | ------------------------------------------------------------ |
| Light RGB(1,1,1)                         | HDR Light RGB(1,1,1) + Light RGB(2,2,2)                      |

However, not all platforms natively support HDR textures. HDR Emulation Scale allows those platforms to use HDR lighting by compressing the number of expressible colors in exchange for extra intensity range. Scale describes this extra intensity range. Increasing this value too high may cause undesirable banding to occur.

Light Intensity scale examples:

| ![HDR Reference](images\image_34.png)           | ![Light Intensity Scale 1 (No HDR)](images\image_35.png) |
| ----------------------------------------------- | -------------------------------------------------------- |
| HDR Reference                                   | Light Intensity Scale 1 (No HDR)                         |
| ![Light Intensity Scale 4](images\image_36.png) | ![Light Intensity Scale 12](images\image_37.png)         |
| Light Intensity Scale 4                         | Light Intensity Scale 12                                 |

When choosing a value for HDR Emulation Scale, the developer should choose the combined maximum brightness for the lights in the scene as the value.

