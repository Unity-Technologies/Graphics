# Requirements and setup

Install the following Editor and package versions to work with the 2D Renderer:

- __Unity 2019.2.0b1__ or later

- __Universal Render Pipeline__ version 6.7 or higher (available via the Package Manager)

## 2D Renderer Setup
![](Images/2D/New_Project_With_Template.png)

1. Create a new project using the 2D template

    ![](Images/2D/image_2.png)

2. Create a new __Pipeline Asset__ by going to the __Assets__ menu and selecting __Create > Rendering > Universal Render Pipeline > Pipeline Asset__ and name it.

3. Create a new __2D Renderer__ by going to __Assets > Create > Rendering > Universal Render Pipeline > 2D Renderer__. Give it a name when prompted.

   ![](Images/2D/image_3.png)

4. Drag the __2D Renderer__ Asset onto the __Renderer List__, or select the circle icon to open the __Select Object__ window and then select the __2D Renderer__ Asset from the list.

6. Follow __ONE__ of the following options:
   
   ![](Images/2D/image_4.png)

   __(Option 1)__ For a single setting across all platforms, go to __Edit > Project Settings__, and select the __Graphics__ category. Set the __Scriptable Render Pipeline Settings__ to your created __Pipeline Asset__ by dragging the Asset directly onto the box, or select the circle open to the right of the box to open the __Select Object__ window and then selecting the Asset from the list.   

    ![](Images/2D/Quality_Settings.png) 

    __(Option 2)__ For settings per quality level Go to __Edit > Project Settings__, and select the __Quality__ category. Set the __Scriptable Render Pipeline Settings__ to your created __Pipeline Asset__ by dragging the Asset directly onto the box, or select the circle open to the right of the box to open the __Select Object__ window and then selecting the Asset from the list. You will need to set an asset per quality level that will be used.  



The __2D Renderer__ is now setup for your Project.

__Note:__ If you have the experimental 2D Renderer enabled, some of the options related to 3D rendering in the Universal Render Pipeline Asset will not have any impact on your final app or game.



