# Upgrading HDRP from Unity 2020.1 to Unity 2020.2

In the High Definition Render Pipeline (HDRP), some features work differently between major versions of Unity. This document helps you upgrade HDRP from Unity 2020.1 to 2020.2.

## Shadows

From Unity 2020.2, it is not necessary to change the [HDRP Config package](HDRP-Config-Package.html) in order to set the [Shadows filtering quality](HDRP-Asset.html#FilteringQualities) for Deferred rendering. Instead the filtering quality can be simply set on the [HDRP Asset](HDRP-Asset.html#FilteringQualities) similarly to what was previously setting only the quality for Forward. Note that if previously the Shadow filtering quality wasn't setup on medium on the HDRP Asset you will experience a change of shadow quality as now it will be taken into account.

Starting from 2020.2, HDRP now stores OnEnable and OnDemand shadows in a separate atlas and more API is available to handle them. For more information, see [Shadows in HDRP](Shadows-in-HDRP.md).

From Unity 2020.2, the shader function `SampleShadow_PCSS` now requires you to pass in an additional float2 parameter which contains the shadow atlas resolution in x and the inverse of the atlas resolution in y.