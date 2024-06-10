# Weather
This sample comes with a full set of weather-related subgraphs (rain and snow) that can be mixed and matched depending on the requirements of the object type they’re applied to.
### Rain
There are several subgraphs that generate rain effects. Each has a different subset of the available rain effects. Applying all of the effects at once is a bit expensive on performance, so it’s best to choose the option with just the effects you need for the specific type of object/surface. 

#### Rain
The Rain subgraph combines all of the rain effect - drops, drips, puddles, wetness - to create a really nice rain weather effect - but it’s the most expensive on performance. Puddles are a bit expensive to generate as are drips, so this version should only be used on objects that will have both flat horizontal surfaces as well as vertical surfaces. 
#### Rain Floor
The Rain Floor subgraph creates puddle and drop effects, but it does not have the drip effects that would run down vertical surfaces. This subgraph is best used for flat, horizontal surfaces.
#### Rain Props
The Rain Props subgraph has the drop and drip effects but does not include the puddles.  It’s best for small prop objects.
#### Rain Rocks
The Rain Rocks subgraph has been specifically tuned for use on rocks. It includes drips and drops, but not puddles. It also includes the LOD0 parameter that is meant to turn off close-up features on LODs other than the first one.
#### Components
##### Puddles
The puddles subgraph creates procedurally-generated puddles on flat, up-facing surfaces. It outputs a mask that controls where the puddles appear and normals from the puddles. It uses the PuddleWindRipples and RainRipples subgraph to generate both wind and rain ripples in the puddles.
##### PuddleWindRipples
The PuddleWindRipples subgraph creates puddle wind ripples by scrolling two normal map textures.  It’s used by the Puddles subgraph.
##### Rain_Drips
This subgraph creates drips that drip down the sides of an object. The drips are projected in world space, so they work well for static objects but are not meant for moving objects. The speed of the drips is controlled by the permeability of the material. Smooth, impermeable surfaces have fast moving drips while permeable surfaces have slow-moving drips.
##### Rain_DripsOnTheLens
This subgraph is very similar to the Rain_Drips subgraph, but it’s adapted to work correctly for the RainOnTheLens post-process shader.
##### Rain_Drops
This subgraph applies animated rain drops to objects.  The drops are projected in world space from the top down. Because of the world space projection, these rain drops are not designed to be added to objects that move in the scene but instead should be used for static objects. The IsRaining input port turns the effect on and off (when the input value is 1 and 0).
##### Rain_Parameters
A common set of rain parameters used by most of the rain subgraphs. Setting parameters once in this subgraph means you don’t have to set them all over in multiple places.
##### RainDropsOnTheLens
This subgraph is very similar to the RainDrops subgraph, but it’s adapted to work correctly for the RainOnTheLens post-process shader.
##### RainRipple
Creates an animated circular ripple pattern. This subgraph is used multiple times in the RainRipples subgraph to create a really nice-looking pattern of multiple overlapping ripples.
##### RainRipples
The RainRipples subgraph creates ripples from rain drops in a puddle or pool of water. It combines four instances of the RainRipple subgraph (each with its own scale, position, and timing offset) to create the chaotic appearance of multiple ripples all happening at once. It’s used by the Puddles subgraph to add rain ripples to the puddles.
##### Wet
This subgraph makes surfaces look wet by darkening and saturating their base color and by increasing their smoothness. The effect is different depending on how permeable the surface is.
### Snow
The Snow subgraph creates a snow effect and applies it to the tops of objects. The snow material includes color, smoothness, normal, metallic, and emissive - where the emissive is used to apply sparkles to the snow.