RPG Developer Bakin
About Battle Plug-in Sample
March 3, 2023 SmileBoom Co.Ltd.
---------------------------------------------
This is a sample project for the "RPG Developer Bakin" battle plug-in.

A plug-in written in C# is stored in the 'battlescript' folder directly under the project folder.

--- Summary ---
What this sample battle plug-in does is roughly the following three things:
- Based on the status of the cast members' "agility," a value (count) is added over time, and the cast member who has reached the specified value can choose their action.
- Stores the accumulated counts in the variable "turnCount", which can be referenced from the Layout Tool.
- New special formats available in the Layout Tool: "battleext" for displaying the order of actions, and "CtbSelected[]", a special coordinate specification tag for indicating the opponent of an attack.


--- Layouts ---
The layout for displaying the order of actions during battle is created in the Layout Tool > Battle Statuses.
Two layouts are currently available.

Type-A
This layout shows the order of actions by the sequence of cast icons.
Text panels that display the special format "battleext" are lined up on the battle status screen.
The "battleext" checks the count of each cast and displays an icon image of the cast (or a graphic for movement if not specified) according to the order of their actions.
To indicate when the enemy selected as the attack target is scheduled to act, rendering containers with the special coordinate specification tag CtbSelected[] are used.

Type-B　
This layout shows the count of each cast in the form of gauges.
Added slider rendering panels (part name "Count Gauge") to the battle status screen to display gauges based on the value of "turnCount".
The layout also includes hidden parts that display an enemy status.  Please refer to it.

- Other battle-related screen layouts are adjusted to match the display of each battle status.
- After starting the sample project, you can talk to the statue at the rear of the map to switch the battle layout to be used.


--- Cameras ---
Two types of preset data were added to the Battle Camera.
Battle Camera E: Side-view type for either layout Type-A or B
Battle Camera F: Back view type suitable for layout Type-B
After selecting the battle camera in the Camera Tool, you can select it from the Load from Presets button. The default battle camera for the project is Battle Camera E.


--- How to Apply This Project's Battle Script to Your Own Project
*Some of the image data uses assets that are included when the amount of assets is set to "Normal" when creating a new project.
If you do not find the images you need in your project after performing the following operations, please use multiple launches of Bakin and copy the resources from the sample game "Orb Stories" or from a new project created with the asset amount set to "Normal".　

1. Copy the 'battlescript' folder directly under the project you want to reflect.
2. Launch Battle Plug-in Sample, open the Layout Tool, and right-click on the preview screen. Save the layouts to a file.
3. Open the project you want to reflect and right-click in the preview window of the Layout Tool. Load the .lyrbr file you just saved.


--- About Plug-ins ---
Please refer to the manual "RPG Developer Bakin" for more information on the plug-in itself, including how to create it.
RPG Developer Bakin Wiki : https://rpgbakin.com/pukiwiki_en/


--- Notice ---
Please note that this sample battle plug-in may not work with updates to Bakin itself.

---------------------------------------------
RPG Developer Bakin
© 2024 SmileBoom Co.Ltd. All Rights Reserved.