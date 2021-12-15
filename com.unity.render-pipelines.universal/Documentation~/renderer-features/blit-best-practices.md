# URP Blit best practices

This page discusses different ways of performing Blit operations in URP and best practices to follow when writing custom passes.

## Overview

Copying a source texture to a destination one is what is commonly referred as 'blit' in Unity.

In URP, there are different ways to perform this operation. This document summarizes the different approaches and defines what are the best practices to follow when
writing your custom passes.
Following these best practices on existing projects should also make the process of upgrading to the next URP releases easier.

## CommandBuffer.Blit

[CommandBuffer.Blit](https://docs.unity3d.com/2022.1/Documentation/ScriptReference/Rendering.CommandBuffer.Blit.html) is
the legacy way to blit in Unity and should not be used.

It implicitly runs hidden extra operations, in terms of changing states, binding textues and setting render targets.
All of these operations are happening under the hood, so not transparently from an SRP point of view, which is not ideal when used in an SRP context.

Using this API is not compatible with NativeRenderPass and RenderGraph, so any pass using cmd.Blit will not be able to take advantage of these.
Using cmd.Blit() has also compatibility issues with the URP XR integration. Using `cmd.Blit` might implicitly enable or disable XR shader keywords,
which breaks XR SPI rendering.

The same considerations apply to any utilities or wrappers relying on cmd.Blit() internally, RenderingUtils.Blit() is an example.

## SRP Blitter API

The [Blitter API](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@13.1/api/UnityEngine.Rendering.Blitter.html) is implemented directly in SRP and doesn't
rely on any built-in logic.
This is the recommended API to use in URP and is guaranteed to be compatible with XR, Native Render Passes and other SRP APIs.

## Custom Fullscreen Blit Sample

The [How to perform a full screen blit in URP](renderer-features/how-to-fullscreen-blit-in-xr-spi.md) example shows how to implement custom blit functionality
which works correctly in XR and is compatible with SRP APIs.
