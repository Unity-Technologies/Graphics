# Contexts

Contexts are the logic containers of the systems, each context handles a pass of processing happening at a different stage of the simulation. Here is a summary of all current contexts.

![](Images/contexts.png)

## Event

Event contexts are flow inputs that will turn on and off the spawn of particles, These contexts are simple and only contain a string that will define their names. The `OnPlay` and `OnStop` events are dedicated event names that correspond to the `Play()` and `Stop()` methods of the component, which can be considered as an **intent of start spawning particles**, and another intent of **stop spawning particles**.

Events can also have any custom name defined as a string, and thus can be invoked by the `SendEvent()` method of the Visual Effects component.

![](Images/events.PNG)

## GPU Event

GPU Events are triggered by systems upon certain conditions and can be caught by other systems to spawn new particles. 

A system that triggers a GPU Event will output a GPU Event data that can be connected to the GPU Event Context.

![](Images/gpu-events.PNG)

Event data can be accessed in the child system by reading **Source Attributes** or using the **Inherit Source (attribute)** node

## Spawn

Spawn contexts are triggered by **SpawnEvent** data types and can be chained to synchronize themselves. **SpawnEvent**s can be considered as messages containing a **spawn order** with a **spawn count** , and a **state payload**.

Spawn Contexts have two inputs : **Start** and **Stop**. These are implicitly bound to the `OnPlay` and `OnStop` events, which means that the spawning machine will start spawning when some SpawnEvent hits the start flow input, and shutdown when another SpawnEvent hits the stop flow Input.

![](Images/implicit-events-spawner.PNG)

Every time the Start input is hit by a SpawnEvent, the Spawn context internal time resets to zero, and spawn resets. So if a Single burst happens at T=0s it will be triggered every frame a spawn event hits the start input.

#### Spawn and Event State

Event state is conveyed through the SpawnEvent flow, from one Spawn context to another, and overwritten into the spawn context every time a SpawnEvent hits the start flow input. 

> **Special Case :** If two SpawnEvents hits the input at the same frame, only the last event hitting the spawn context will store its state palyoad into it.
>
> Also, the spawn context reset will happen twice but the execution only once, by default. As the time will reset to zero once (with the first event), then another time (with the second event), then spawn blocks will be executed.
>
> If you need to accumulate spawn orders happening at the same frame, see the [Custom Spawners]() section.

## Initialize

Initialize contexts are the makers of new particles and are executed when a SpawnEvent hits the input. 

![](Images/context-initialize.png)

#### Behavior

The context will create an amount of particles equal to the **spawnCount** of the SpawnEvent data payload. For each new particle, the context blocks will be executed, then the particles inserted into the simulation.

#### Properties/Settings

- (*Setting*) **Capacity** : the allocation count for particles, It should reflect the expected amount of particles you need for this system. If initialize does not find any room for all new particles in the simulation pool, some or all new particles could be discarded.
- **Bounds** : The bounding box corresponding to the extents of the system. As this is a property, It can be computed using operators.

## Update

Update contexts updates particles attributes every frame and update their state along time.

![](Images/context-update.png)

#### Behavior

Update context processes every particle in the simulation if it is alive, every frame. It can also handle automatically some tasks. such as particle aging and reaping, and the integration of the velocity to the position. These automatic settings can be disabled if you need to perform this tasks in a more custom way.

Update contexts can be skipped if no update is necessary. Connecting an initialize to an output will perform a update-less simulation with particles initialized at start and rendered following their initial state.

#### Properties/Settings

- (*Setting*) **Integration** : (Euler or None) If this option is set to Euler, the positions will be integrated from velocity every frame following the euler model.
- (*Setting*) **Age Particles** : if Age attribute is set, every particle will age accordingly to the current deltaTime
- (*Setting*) **Reap Particles** : if Age and Lifetime attribute are set, every particle which age goes beyond Lifetime will be killed.

## Output

Output contexts are executed every frame and will give shape to the simulated particles for every living particle. Many output contexts exist and have their own specificities.

![](Images/context-output.png)

#### Behavior

Output contexts take, every frame, the simulation data and will render it according to the context configuration. Also, blocks can be used to perform computation before rendering. However, **the output context does not modify the simulated data**, so block operations made in this context can be considered as pre-render operations. 

An Initialize or update block **can be connected to multiple outputs at once**. The simulation data will be shared across these output contexts.

#### Common Settings

Depending on the output context you use, settings are subject to change. See the following for more specific information. Here is a list of commonly used settings (not necessarily used by all output contexts):

| Name (Type)              | Description                                                  |
| ------------------------ | ------------------------------------------------------------ |
| Blend Mode (Enum)        | <u>How the particle will render:</u> <br />**- Opaque** is non-blended and will ignore the alpha channel<br />**- Masked** is non-blended and will use a Alpha threshold setting to perform pixel culling<br />**- Alpha** uses standard Alpha blending<br />**- Additive** uses additive blending, (alpha is multiplied by color before blending)<br />**- AlphaPremultiplied** uses Pre-Multiplied alpha blending so both additive and alpha blending can be achieved, |
| UV Mode (Enum)           | <u>How the UVs will be processed:</u><br />**- Simple**  does no modification to UVs<br />**- Flipbook** uses a uint2 *FlipbookSize* property to define rows and columns, and uses the texIndex attribute to select a cell into the flipbook<br />**- FlipbookBlend** is the same as Flipbook but instead blends between two cells depending on the fractional part of the texIndex attribute<br />**- ScaleAndBias** uses two input properties *UVScale* and *UVBias* to perform tiling and offset to UVs (useful to scroll patterns) |
| Use Soft Particle (Bool) | Enables testing particle depth vs scene depth for blended particles : adds a *SoftParticleFadeDistance* input property to control the fade distance to the next opaque pixel. |
| Cull Mode (Enum)         | Selects the Cull Mode render state : Default, Front, Back, Off (Two-sided) |
| Z Write Mode (Enum)      | Selects whether particles write their own depth : Default, Off, On |
| Z Test Mode (Enum)       | Select whether particles will clip depending on scene depth: Default, Less, LEqual, Greater, GEqual, Equal, NotEqual, Always<br />As default particles will clip if their depth is Less or equal, using modes Greater or GEqual permits rendering particles only behind opaque geometry, or Always to never clip the particles behind opaque geometry |
| Sort Priority (int)      | Orders this output compared to other outputs in the system   |
| Sort (Enum)              | Performs per-particle Sorting (Auto detects if blend mode requires sorting) Sorting |
| Indirect Draw (Bool)     | Performs an indirect draw call                               |
| Cast Shadows (Bool)      | Whether the particles will cast shadows                      |
| Pre-Refraction (Bool)    | Whether the particles will be rendered into the Pre-Refraction render queue of the HD Render Pipeline (useful to refract particles behind glass) |



### Quad Output

Outputs a textured quad per-particle. By default, the quad is not aligned to anything and will require the **Orient** block to perform camera facing (or any other sort of alignment: velocity, axis rotation, fixed, etc.)

These particles are textured and output color with the following formula : `tex2D(_MainTexture,uv) * float4(color,alpha)` 

### Mesh Output

Outputs a textured mesh per-particle. These particles are textured and output color with the following formula : `tex2D(_MainTexture,uv) * float4(color,alpha)` 

### Sphere Output

Ouputs a billboarded Sphere with corrected depth per-particle. These particles are not textured but insteads uses a color to render.

### Cube Output

Outputs a textured cube per particle : Cubes are textured  and output color with the following formula : `tex2D(_MainTexture,uv) * float4(color,alpha)` 

### Point Output

Ouputs a point primitive (1px) per-particle

### Line Output

Outputs a line per-particle, the line can be defined either using angle, axises or using targetPosition

## Static Mesh

Static mesh output renders a mesh using a shader. Every shader property becomes an inputProperty that can be manipulated by the expression graph.

![](Images/context-staticmesh.png)

