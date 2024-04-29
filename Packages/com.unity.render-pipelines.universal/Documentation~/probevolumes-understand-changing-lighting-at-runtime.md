# Choose how to change lighting at runtime

You can change how objects use the baked data in Adaptive Probe Volumes, to create lighting that changes at runtime. For example, you can turn the lights on and off in a scene, or change the time of day.

You can use one of the following processes:

- [Bake different lighting setups with Lighting Scenarios](probevolumes-bakedifferentlightingsetups.md), for example you can bake a Lighting Scenario for each stage in a day-night cycle.
- [Update light from the sky at runtime with sky occlusion](probevolumes-skyocclusion.md).

Lighting Scenarios have the following advantages:

- Lighting Scenarios are more accurate. Lighting Scenarios don't approximate the light from the sky, or the color of objects that light bounces off. 
- Lighting Scenarios store all the lighting in a scene, so you can update light from both the sky and scene lights.

Sky occlusion has the following advantages:

- Easier to set up. For example, you only need to bake once to set up the data you need for a day-night cycle.
- Better performance.
- Faster and smoother transitions, because sky occlusion doesn't have to blend between different sets of data.

You can use sky occlusion and Lighting Scenarios together. For example, you can use sky occlusion to update the light from the sky, and Lighting Scenarios to update the position of the sun or the state of an interior lamp.
