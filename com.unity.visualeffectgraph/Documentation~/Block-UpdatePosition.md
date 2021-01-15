# Integration : Update Position

Menu Path : **Implicit > Integration : Update Position**

The **Integration : Update Position** Block updates particle positions based on their velocity. If the system uses the velocity attribute and you enable **Update Position** in the Update Context's Inspector, Unity implicitly adds this Block to the Context and hides it.

![](Images/Block-UpdatePositionInspector.png)

This Block adds the particle velocity multiplied by deltaTime to the current particle position:

`position += velocity * deltaTime;`

If you disable **Update Position** in the Update Context's Inspector, the system does not change the particle **position** based on the particle's velocity attribute.

You can also add the **Integration : Update Position** Block to the Update Context manually and enable/disable it to specify when the System updates the particle position based on its velocity.

![](Images/Block-UpdatePositionBlockInContext.png)

## Block compatibility

This Block is compatible with the following Contexts:

- [Update](Context-Update.md)