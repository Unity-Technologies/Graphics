# Understand multiframe rendering and accumulation

Some rendering techniques, such as [path tracing](Ray-Tracing-Path-Tracing.md) and accumulation motion blur, combine information from multiple intermediate sub-frames to create a final "converged" frame. Each intermediate sub-frame can correspond to a slightly different point in time, effectively computing physically based accumulation motion blur, which properly takes into account object rotations, deformations, material or lighting changes.

The High Definition Render Pipeline (HDRP) provides a scripting API that allows you to control the creation of sub-frames and the convergence of multi-frame rendering effects. In particular, the API allows you to control the number of intermediate sub-frames (samples) and the points in time that correspond to each one of them. Furthermore, you can use a shutter profile to control the weights of each sub-frame. A shutter profile describes how fast the physical camera opens and closes its shutter.

This API is particularly useful when recording path-traced movies. Normally, when editing a Scene, the convergence of path tracing restarts every time the Scene changes, to provide artists an interactive editing workflow that allows them to quickly visualize their changes. However such behavior isn't desirable during recording.

The following image shows a rotating GameObject with path tracing and accumulation motion blur, recorded using the [multi-frame recording API](rendering-multiframe-recording-API.md)

[](Images/path_tracing_recording.png)