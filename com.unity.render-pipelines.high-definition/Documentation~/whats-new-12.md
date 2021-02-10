# What's new in HDRP version 12 / Unity 2021.2

This page contains an overview of new features, improvements, and issues resolved in version 12 of the High Definition Render Pipeline (HDRP), embedded in Unity 2021.2.

## Features

The following is a list of features Unity added to version 12 of the High Definition Render Pipeline, embedded in Unity 2021.2. Each entry includes a summary of the feature and a link to any relevant documentation.

### Render Emissive contibution of a Lit Deferred Material in a separate forward pass.

From HDRP 12.0, it is now possible to render the Emissive contribution of a Lit Material when using the the Lit Shader Mode as Both or Deferred in HDRP settings in a separate pass instead of the GBuffer pass.
This can be used to fix artefacts when using [Screen Space Global Illumination](Override-Screen-Space-GI.md) - With or without Raytracing enabled - and Emissive Material with Lit Shader Mode setup as Both or Deferred. Previously it Emissive contributoin was drop, now it is keep. Same usage for the new Adaptive probe volumes.
Limitation: Doing a separate pass for Emissive contribution can have an additional CPU cost as it require additional DrawCall.


## Improvements

The following is a list of improvements Unity made to the High Definition Render Pipeline in version 12. Each entry includes a summary of the improvement and, if relevant, a link to any documentation.





## Issues resolved

For information on issues resolved in version 12 of HDRP, see the [changelog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@12.0/changelog/CHANGELOG.html).
