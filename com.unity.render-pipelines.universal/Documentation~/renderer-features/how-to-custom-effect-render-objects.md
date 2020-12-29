# How to create a custom rendering effect using the Render Objects Renderer Feature

This example shows how to use the Render Objects Renderer Feature to create a custom rendering effect.

## The final effect in this example

The effect that this example shows is:

* There is a character in the Scene.

    ![Character](../Images/renderer-features/character.png)

* When the character goes behind objects, Unity draws the character silhouette with a different Material.

    ![Character goes behind objects](../Images/renderer-features/charecter-goes-behind-object.gif)

## Prerequisites

This example requires the following:

* The project has the URP package installed.

* The **Scriptable Render Pipeline Settings** property refers to a URP asset (**Project Settings** > **Graphics** > **Scriptable Render Pipeline Settings**).

## Example Scene and GameObjects

To follow the steps in this example, create a new Scene. Add the following GameObjects:

1. Create a Cube. Set its Scale values so that it looks like a wall.

    ![Cube that represents a wall](../Images/renderer-features/rendobj-cube-wall.png)

2. Create a basic character. In this example, the character consists of three capsules: the big capsule in the center stands for the body, and the two smaller capsules stand for the hands.

    ![The character consisting of three capsules](../Images/renderer-features/character-views-side-top-persp.png)

    To make it easier to manipulate the character in the Scene, put the three Capsules as child GameObjects under the Character GameObject.

    ![Character objects in Hierarchy](../Images/renderer-features/character-in-hierarchy.png)


Now you have the objects necessary to follow the steps in this example.

## Example implementation

The following steps let you create this effect using the Render Objects Renderer Feature.

1. Create a new Scene and add the following GameObjects to it:

    * A Cube 
