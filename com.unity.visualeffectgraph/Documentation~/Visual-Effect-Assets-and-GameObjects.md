The new Visual Effect pipeline relies on templates stored in the Asset Database and used by Scene Components. Every template can be used in any number of instances and have its configuration customized per-instance.

## Visual Effect Graph (Asset)

Visual Effect Graphs can be created using the Create Asset Menu, under Visual Effects Category.

![](Images/create-asset.gif)

These assets contains all the necessary data (Graph and Shaders) to run an effect completely. These assets are compiled by the Content pipeline to generate necessary nested shaders and compute shaders.

To Open a Visual Effect Graph, you can:

* Double Click the Visual Effect icon of the asset in Project view
* Select a Visual Effect Graph in project view and click the Open Button in inspector
* Click the Edit button next to the template field on an already configured Visual Effect GameObject

You can preview a Visual Effect in its default state by using the Inspector preview.

### Visual Effect Graph Options

#### Rendering Options

* Cast Shadows
* Motion Vectors
* Culling
  * Cull Simulation
  * Cull Simulation and Bounds
  * Cull None

#### Debug Shader View

Shaders can be debug by clicking the show button next to its name in the Asset Inspector

## Visual Effect (GameObject and Component)

The Visual Effect Graphs can be played in Scenes by Visual Effect Components, they come pre-configured on GameObjects that can be created using the `Create > Visual Effects > Visual Effect` menu. 

![](Images/create-go.gif)

> If an effect is selected in the project view during the creation process, It will be automatically assigned to the component template. 

You can also Drag and Drop a Visual Effect from the Project View, directly into the scene to create a GameObject with a Visual Effect Component that references this Asset.

#### Component Overview

The Visual Effect Component contains basic controls to configure an instance of a Visual Effect in scene. As other components, It can be disabled and and enabled. Disabling a visual effect will destroy any living particles and stop any simulation and rendering.



![](Images/component.png)

* **Asset Template** : the Visual effect graph asset used as template for this instance. Use the Edit button to open this template in the Visual Effect Graph window.
* **Random Seed** : A fixed random seed so the effect random becomes unique to this seed.
* **Reseed on Play** : A boolean that will recompute a seed for this instance every time the `Play event` is sent to the component
* **Show Parameters** : A button that will enable the user to access parameter's widgets in order to configure certain types (Such as sphere) directly in the scene, instead of typing numeric values in the inspector.
* **Parameters:** The list of all parameters exposed into the Asset Blackboard. These parameters can be ordered using categories. A checkbox at the left of the label tells the user if the value is the default one or overridden (when checked) for this instance.

