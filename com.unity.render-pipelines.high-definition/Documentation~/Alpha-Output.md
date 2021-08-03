# Alpha Output

To maximize performance and minimize bandwidth usage, HDRP by default renders image frames in the **R11G11B10** format. However, this format does not include an alpha channel, which might be required for applications that want to composite HDRP's output over other images.

To configure HDRP to output an alpha channel, open your [HDRP Asset](HDRP-Asset.md) (menu: **Edit > Project Settings > Graphics > Scriptable Render Pipeline Asset**)), go to the **Rendering** section, and set the **Color Buffer Format** to **R16G16B16A16**. However, note that enabling this option incurs a performance overhead. In HDRP, opaque materials always output 1 in the alpha channel, unless you enable [Alpha Clipping](Alpha-Clipping.md). If you want to export the alpha of an opaque material, one solution is to enable **Alpha Clipping** and set the Threshold to 0.

Furthermore, when post-processing is enabled, the *Buffer Format* for post-processing operations should also be set to *R16G16B16A16* in order to apply post-processing operation in the alpha channel. This can be selected from the post-processing section of the HDRP asset. If the post-processing format is set to **R11G11B10**, then HDRP will output a copy of the alpha channel without any post-processing on it.

The following table summarizes the behavior of HDRP regarding the alpha channel of the output frames.

Rendering Buffer Format | Post-processing Buffer Format | Alpha Output
---|---|---
**R11G11B10** | **R11G11B10** | No alpha output
**R16G16B16A16** | **R11G11B10** | Alpha channel without post-processing (AlphaCopy)
**R16G16B16A16** | **R16G16B16A16** | Alpha channel with post-processing

Note that alpha output is also supported in [Path Tracing](Ray-Tracing-Path-Tracing.md).

## DoF and Alpha Output
Another case which might require post-processing of the alpha channel is for scenes that use Depth Of Field. In this case, if the alpha is not processed, compositing will result in a sharp cut-off of an object that should appear blurred. This is better is illustrated in the images below:

![](Images/DoFAlpha.png)

An out-of-focus sphere composited over a solid blue background using a *R16G16B16A16* buffer format for both rendering and post-processing. In this case, DoF is applied in the alpha channel, resulting in a proper composition (the output alpha used in the composition is shown in the image inset).

![](Images/DoFAlphaCopy.png)

An out-of-focus sphere composited over a solid blue background using *AlphaCopy*. In this case, DoF is NOT applied in the alpha channel, resulting in a sharp outline around the composited sphere (the output alpha used in the composition is shown in the image inset). 

## Temporal Anti-Aliasing and Alpha Output
When Temporal Anti-Aliasing (TAA) is enabled it is highly recommended to enable post-processing for the alpha channel (*R16G16B16A16* format for both rendering and post-processing). If the alpha channel is not post-processed, then the alpha mask will be jittered, as shown in the images below:

![](Images/TAA_AlphaCopy.gif)

A sphere rendered with TAA using *AlphaCopy*, composited over a solid blue background using the alpha channel. The alpha channel is not temporally stabilized by TAA, resulting in jittering on the final image.


![](Images/TAA_Alpha.gif)

A sphere rendered with TAA (*R16G16B16A16* for both rendering and post-processing), composited over a solid blue background using the alpha channel. TAA is also applied in the alpha channel, resulting in a stable composition.

