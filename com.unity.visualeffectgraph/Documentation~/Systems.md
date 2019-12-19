<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Draft:</b> The content on this page is complete, but it has not been reviewed yet.</div>
# Systems

Systems are compounds of one or many  [Contexts](Contexts.md) that define a standalone part of a Visual Effect. A system can be a Particle System, a Particle Strip System, a Mesh, or a Spawn machine.

<u>Systems can interact between themselves among a Visual Effect Graph :</u> 

* A **Spawn** System can **spawn particles** in one or many Particle or Systems : This is the main method of spawning particles.

* **Particle Systems** can **spawn particles** in **other particle systems** through GPU Events : This alternate method can spawn particles from other particles based on simulation events (for example : death of a particle).

* A **Spawn** System can **Turn on/off** other **Spawn Systems** : This enables synchronizing emission by using a master Spawn System to orchestrate other Spawn Systems.

  

## Creating System from Templates

Visual Effect Graph comes with Built-in templates that you can add to your graph using the following:

1.  Right Click in an empty space of your workspace, then select Create Node
2. In The Node Creation Menu, Select **System** Category
3. Select a template from the system list to add a template system.

![](Images/SystemAddTemplate.png)

## System Spaces

Some systems embed a Space property that will define the reference space that will be used to simulate its contents:

* Local Space will simulate locally to the Game Object that holds the  [Visual Effect Component](VisualEffectComponent.md) 
* World space will simulate independently of the Game Object that holds the [Visual Effect Component](VisualEffectComponent.md) 

> Regardless of the System's Simulation Space you can use [Spaceable Properties](Properties.md#spaceable-properties) in order to access Local or World Values.

### Setting a System Space

In order to know the Space of a System, you can look at the Top-Right corner of its contexts and look for the **System Space Identifier**. If the Context does not compute anything regarding to space, its top-right corner will not display any System Space identifier.

![](Images/SystemSpaceIdentifier.png)

In order to change the System space, just click the System Space Identifier to cycle through the compatible spaces.

![](Images/SystemSpaceLocalWorld.png)

### System Space Identifiers in Properties

Some [Spaceable Properties](Properties.md) display a smaller version of the System Space Identifier. This doesn't change the system simulation space but instead enable expressing a value in a space tha't s different from the System simulation Space. (For instance, a local position while system is simulating in world space).

![](Images/SystemSpaceLocalWorldSmall.png)