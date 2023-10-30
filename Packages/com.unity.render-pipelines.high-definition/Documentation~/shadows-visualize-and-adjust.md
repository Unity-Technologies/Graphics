## Visualize and adjust shadows

You can use the Shadows override to visualize the cascade sizes in the Inspector, and the boundaries of the cascades as they appear inside your Scene in real time.

In the Inspector, use the **Cascade Splits** bar to see the size of each cascade relative to one another. You can also use the bar to:

- Move the position of each cascade split. To do so, click on the tab above a split and drag it to adjust the position of that cascade split.
- Move the position of each border. To do so, click on the tab below the split and drag it to adjust the position of the border

In the Scene view and the Game view, the cascade visualization feature allows you to see the boundaries of each cascade in your Scene. Each color represents a separate cascade, and the colors match those in the **Cascade Splits** bar. This allows you to see which colored area matches which cascade.

![](/Images/Override-Shadows3.png)

To enable the cascade visualization feature, select **Show Cascades** at the top of the list of **Shadows** properties. You can now see the shadow maps in the Scene view and the Game view.

- You can use the Scene view Camera to move around your Scene and quickly visualize the shadow maps of different areas.
- You can use the Game view Camera to visualize the shadow maps from the point of view of the end user. You can use the **Show Cascades** feature while in Play Mode, which is useful if you have some method of controlling the Camera’s position and rotation and want to see the shadow maps from different points of view in your Project.