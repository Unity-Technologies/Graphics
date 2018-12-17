The visual effects system relies on a modular flow design where processing is described through states. **Systems** are composed by chaining **Contexts** which contains dedicated behavior **Blocks**. Every context type has its own specific properties and using a block in one state or another can be used to achieve different results.

## Systems

Systems are generally composed of a **top-to-bottom chain of contexts** that follows this order : 

![](https://raw.githubusercontent.com/wiki/Unity-Technologies/ScriptableRenderPipeline/Pages/VFXEditor/img/system-contexts.png)

## Contexts

Contexts represent states of processing through Visual Effect Systems, they have their own behavior depending on their type, and most of them can hold one or many blocks.

![](https://raw.githubusercontent.com/wiki/Unity-Technologies/ScriptableRenderPipeline/Pages/VFXEditor/img/contexts.png)

They are connected vertically by flow connections, from top to bottom. Here is a list of commonly used contexts, for more information see the [Contexts]() section.

| Context Name | Description                                                  | Input      | Output     |
| ------------ | ------------------------------------------------------------ | ---------- | ---------- |
| Event        | Event exposed to the Component, can be triggered by C#       |            | SpawnEvent |
| GPU Event    | Event caught by other systems                                |            | SpawnEvent |
| Spawn        | Toggleable machine that outputs SpawnEvents over time using specific behavior. | SpawnEvent | SpawnEvent |
| Initialize   | Generate N new particles with attributes from a SpawnEvent (CPU or GPU) and injects them into simulation | SpawnEvent | Particles  |
| Update       | Update loop of a particle simulation, takes input of new generated particles. | Particles  | Particles  |
| Output       | Renders particles with a specific renderer and shader.       | Particles  |            |
| Static Mesh  | Renders a static mesh with a shader and transform, with shader properties exposed to the expression Graph |            |            |

## Blocks

Blocks are one of the modular processing power of the visual effect editor : they are small chunks of features that can be added to contexts to process a specific task. They are held in a library that can be extended by writing new blocks.

#### Manipulating Blocks

![](https://raw.githubusercontent.com/wiki/Unity-Technologies/ScriptableRenderPipeline/Pages/VFXEditor/img/blocks-create-move-delete.gif)

* Blocks can be **added** to a context by right clicking the context then selecting `Create Block` from the menu or pressing the Spacebar while having the cursor over the context.
* The block **creation** menu can be navigated through categories and/or filtered using the search field.
* Blocks can be **dragged** and **reordered** among one context, or **dragged to another compatible context**.
* Blocks can be **duplicated**, **copied**, **pasted** and **deleted**.

#### Block overview

Blocks are composed of a **Header**, a **Settings** area and a **property** area.

![](https://raw.githubusercontent.com/wiki/Unity-Technologies/ScriptableRenderPipeline/Pages/VFXEditor/img/block.png)

* **Header** is the area where you can click to drag around the block. It contains the following:
  * Block title (corresponding to the current configuration)
  * Toggle checkbox to disable the block.
  * UI Collapse button
* **Settings** area is where you can staticly configure the block. *(please note that not all settings are necessarily in this area, some can be accessible in the inspector)*
* **Properties** area is where you can enter values or connect operators to configure the block's values.

There is a large number of blocks you can add and a even larger amount of combinations you can achieve using these. For more details about blocks and the library contents : see the [Blocks]() section of the help.