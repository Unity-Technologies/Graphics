# Light Batching Debugger

Light Batching Debugger allows for visualization of how Lights and Shadow Casters are batched according to their associated Sorting Layers in the scene. The tool compares adjacent batches and highlights which Lights or Shadow Casters would be needed to be added or removed in order to be batched together.

## How to use
To open the debugger, go to **Window > 2D > Light Batching Debugger**.
![Light Batching Debugger window](Images/2D/light-batching-debugger-0.png)

</br>
Light Batching Debugger updates in real time if the Game View is opened.
![Light Batching Debugger window](Images/2D/light-batching-debugger-1.png)

</br>
Select on a batch to view Lights and Shadow Casters in the current batch.
![Light Batching Debugger window](Images/2D/light-batching-debugger-2.png)

</br>
Individual batches are color coded differently, to indicate they are not batched together. </br>
![Colorcoded](Images/2D/light-batching-debugger-color-1.png)

Sorting Layers that are batched together have the same color. </br>
![Colorcoded](Images/2D/light-batching-debugger-color-2.png)

</br>
Select on adjacent batches to compare the differences between them. Lights and Shadow Casters are displayed in their own panel. The game objects highlighted here only exist in one batch, and are potentially batchable if targeted to both Sorting Layers.
![Light Batching Debugger window](Images/2D/light-batching-debugger-3.png)

## Examples

In order for Sorting Layers to be batched, they need to satisfy the following conditions:
- share the same sets of Lights
- share the same sets of Shadow Casters

</br>
Here are some examples of how Lights and Shadow Casters are batched:

Example scene has 2 Sorting Layers, **BG** and **Default**.

**Batch Case 1**
| ![Batch Case 1](Images/2D/light-batching-debugger-4.png) | ![Batch Case 1](Images/2D/light-batching-debugger-5.png) |
| :-: | :-: |
| Light A and B targeting **BG** and **Default** </br>Shadows disabled | Does batch |
**Batch Case 2**
| ![Batch Case 2](Images/2D/light-batching-debugger-6.png) | ![Batch Case 2](Images/2D/light-batching-debugger-7.png) |
| :-: | :-: |
| Light A targeting **BG** and Light B targeting **Default** </br>Shadows disabled | Does not batch |
**Batch Case 3**
| ![Batch Case 3](Images/2D/light-batching-debugger-8.png) | ![Batch Case 3](Images/2D/light-batching-debugger-9.png) |
| :-: | :-: |
| Light A and B targeting **BG** and **Default** </br>Shadows enabled for both Lights </br>Shadow Caster targeting **BG** and **Default** | Does batch |
**Batch Case 4**
| ![Batch Case 4](Images/2D/light-batching-debugger-10.png) | ![Batch Case 4](Images/2D/light-batching-debugger-11.png) |
| :-: | :-: |
| Light A and B targeting **BG** and **Default** </br>Shadows only enabled for Light A </br>Shadow Caster targeting **BG** and **Default** | Does batch |
**Batch Case 5**
| ![Batch Case 5](Images/2D/light-batching-debugger-12.png) | ![Batch Case 5](Images/2D/light-batching-debugger-13.png) |
| :-: | :-: |
| Light A and B targeting **BG** and **Default** </br>Shadows enabled for both Lights </br>Shadow Caster targeting only **BG** | Does not batch |
