# Requirements and setup

Install the following Editor and package versions to begin working with the __2D Renderer__:

- __Unity 2019.2.0b1__ or later

- __Universal Render Pipeline__ version 6.7 or higher (available via the Package Manager)

## 2D Renderer Setup
1. Create a new Project using the [2D template](https://docs.unity3d.com/Manual/ProjectTemplates.html).![](Images/2D/New_Project_With_Template.png)


2. Create a new __Pipeline Asset__ by going to the __Assets__ menu and selecting __Create > Rendering > Universal Render Pipeline > Pipeline Asset__ and then name the Asset.![](Images/2D/image_2.png)


3. Create a new __2D Renderer__ by going to __Assets > Create > Rendering > Universal Render Pipeline > 2D Renderer__. Give it a name when prompted.

   ![](Images/2D/image_3.png)


4. Assign the __2D Renderer__ as the default Renderer for the Render Pipeline Asset. Drag the __2D Renderer__ Asset onto the __Renderer List__, or select the circle icon to open the __Select Object__ window and then select the __2D Renderer__ Asset from the list.


5. Set the graphics quality settings:

   __Option 1: For a single setting across all platforms__

   ![](Images/2D/image_4.png)

   1. Go to __Edit > Project Settings__ and select the __Graphics__ category.
   2. Drag the __Pipeline Asset__ created earlier to the __Scriptable Render Pipeline Settings__ box, or select the circle icon to the right of the box to open the __Select Object__ window and then select the Asset from the list.

   __Option 2: For settings per quality level__![](Images/2D/Quality_Settings.png)

   1. Go to __Edit > Project Settings__ and select the [Quality](https://docs.unity3d.com/Manual/class-QualitySettings.html) category.
   2. Select a quality level to be included in your Project.
   3. Drag the __Pipeline Asset__ created earlier to the __Rendering__ box, or select the circle open to the right of the box to open the __Select Object__ window and then select the Asset from the list.
   4. Repeat steps 2-3 for each quality level and platform included in your  Project.

The __2D Renderer__ is now set up for your Project.

__Note:__ If you use the __2D Renderer__ in your Project, some of the options related to 3D rendering in the __Universal Render Pipeline Asset__ will not have any impact on your final app or game.
