# What's new in HDRP version 12 / Unity 2021.2

This page contains an overview of new features, improvements, and issues resolved in version 12 of the High Definition Render Pipeline (HDRP), embedded in Unity 2021.2.

## Features

The following is a list of features Unity added to version 12 of the High Definition Render Pipeline, embedded in Unity 2021.2. Each entry includes a summary of the feature and a link to any relevant documentation.

### Render the Emissive contribution of a Lit Deferred Material in a separate forward pass.

From HDRP 12.0, you can render the Emissive contribution of a Lit Material in a separate pass when the Lit Shader Mode is set to Both or Deferred in the HDRP settings. You can do this instead of using the GBuffer pass.
This can be used to fix artefacts when using [Screen Space Global Illumination](Override-Screen-Space-GI.md) - With or without Raytracing enabled - and Emissive Material with Lit Shader Mode setup as Both or Deferred. Previously it Emissive contributoin was drop, now it is keep. Same usage for the new Adaptive probe volumes.
Limitation: When Unity performs a separate pass for the Emissive contribution, it also performs an additional DrawCall. This means it uses more resources on your CPU.
Group of Materials / GameObject can be setup to use Force Emissive forward with the script "Edit/Render Pipeline/HD Render Pipeline/Force Forward Emissive on Material/Enable In Selection".


## Improvements

The following is a list of improvements Unity made to the High Definition Render Pipeline in version 12. Each entry includes a summary of the improvement and, if relevant, a link to any documentation.





## Issues resolved

For information on issues resolved in version 12 of HDRP, see the [changelog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@12.0/changelog/CHANGELOG.html).
