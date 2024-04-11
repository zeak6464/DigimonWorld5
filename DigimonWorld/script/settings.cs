using System; //Namespace containing fundamental classes (mathematical operations, arrays,...)
using Yukar.Engine; //Namespace containing basic classes of RPG Dev Bakin
using System.IO; //Namespace containing classes for reading and writing files.


namespace Bakin
{
    public class settings : BakinObject
    {
        LayoutManager m_menu; //The 'm_menu' variable will store the reference of our menu.

        int[] c_setting = { 1, 3, 1, 1 }; //c_setting is a variable of array type with the current setting of 'bloom', 'shadow quality' 'ssao' and 'dof' effect.       

        string c_path = System.AppDomain.CurrentDomain.BaseDirectory + "rendersetting.dat";
        //c_path is the path of the file where the extra setting will be saved. 'System.AppDomain.CurrentDomain.BaseDirectory' is the current directory.

        string setting; //'setting' variable will sotre the data contained in the extra setting file.
        bool loaded = false; //loaded will be true if the file is loaded
        bool adjusted = false; //adjusted will be true if the setting has been set.

        string c_map; //The 'c_map' variable will store the name of current map, so we can know if the player switches the map.

        public override void Update()
        {
            // キャラクターが生存している間、
            // 毎フレームこのキャラクターのアップデート前にこのメソッドがコールされます。
            // This method is called every frame before this character updates while the character is alive.

            //If the current map is different from the one stored in the 'c_map' variable...
            if (c_map != mapScene.map.name)
            {
                //...the 'loaded' variable will be false and then the 'c_map' variable will be set to the map name.
                loaded = false;
                c_map = mapScene.map.name;
            }

            //At the beginning, 'loaded variable will be false. At that time, the script will check if there is a setting file and will read it.
            if (loaded == false)
            {
                if (!File.Exists(c_path)) write_setting(); //If the setting file doesn't exist, the script will create it.

                using (var fs = new StreamReader(c_path))
                {
                    setting = fs.ReadToEnd(); //We will read the setting file and store it in the variable 'setting'.

                    //The setting file has a string consisting of 4 numbers. We will store each number as an integer value in the 'c_setting' array.
                    for (int i = 0; i < 4; i++) c_setting[i] = Convert.ToInt32(setting.Substring(i, 1));

                    update_render(); //We will update the rendersetting parameters using our function update_render().
                    loaded = true; //The reading has finished, so, the 'loaded' variable will be true.
                }
            }

            //We need to set the 'm_menu' variable depending on displayed main menu
            if (mapScene.map.name == "Menu") m_menu = mapScene.GetMenuController().title; //If the name of the map is 'Menu', the 'm_menu' variable will be the title menu.
            else m_menu = mapScene.GetMenuController().mainMenu; //If the name of the map is not 'Menu', the 'm_menu' variable will be the main menu.

            //In the menu, if any layout is open, we must get its reference and store it in the 'm_menu' variable
            while (m_menu.NextlayoutManager != null) m_menu = m_menu.NextlayoutManager;

            //The options displayed in the user interface should be updated only when the 'MainConfig' layout is open.
            if (m_menu.LayoutNode.Name == "MainConfig")
            {
                //We must access to the 'Bloom', 'Shadows', 'SSAO' and 'DoF' options in the 'MainConfig' layout.
                //We will use 'LayoutDrawer.renderobjects' to access to the list of objects that are rendered in that layout.
                //'ParseNodes().Find' searches the object that meets the condition specified in brackets.
                //Furthermore, we will indicate the type of object that we obtained: TextPanel, SliderPanel or SpinPanel. In this case, all objects are 'SpinPanel'
                var bloom = m_menu.LayoutDrawer.renderObjects[0].ParseNodes().Find(x => x.MenuItem.name == "Bloom") as SpinPanel;
                var shadows = m_menu.LayoutDrawer.renderObjects[0].ParseNodes().Find(x => x.MenuItem.name == "Shadow") as SpinPanel;
                var ssao = m_menu.LayoutDrawer.renderObjects[0].ParseNodes().Find(x => x.MenuItem.name == "SSAO") as SpinPanel;
                var dof = m_menu.LayoutDrawer.renderObjects[0].ParseNodes().Find(x => x.MenuItem.name == "DoF") as SpinPanel;

                //We must set the SpinPanel values 'bloom', 'shadows', 'ssao' and 'dof' using the setting stored in the variable 'c_setting'.
                //We will set the values only at the precise moment when the layout is shown.
                //But first, we must check if 'bloom', 'shadows', 'ssao' and 'dof' variables are not null.
                if (bloom != null && shadows != null && ssao != null && dof != null)
                {
                    //If the setting hasn't been adjusted yet, the 'adjusted' variable will be false.
                    if (adjusted == false)
                    {
                        //We will set the 'bloom', 'shadows', 'ssao' and 'dof' SpinPanels with the 'spinRender.SetValue' function.
                        //'bloom', 'shadows' and 'dof' have two options (0 or 1). These options are in the 1st, 3rd and 4th items of the 'c_setting' array.
                        bloom.spinRender.SetValue(c_setting[0]);
                        ssao.spinRender.SetValue(c_setting[2]);
                        dof.spinRender.SetValue(c_setting[3]);

                        //'shadow' has 4 options (from 0 to 3). We can find it in the 2nd item of the 'c_setting' variable.
                        shadows.spinRender.SetValue(c_setting[1]);

                        //The setting has been adjusted, so the adjusted variable must be true
                        adjusted = true;
                    }

                    //Once the SpinPanel values have been set, we must store any changes made by user.
                    //It will only be possible to store new data if the 'adjusted' variable is not false.
                    else
                    {
                        //Each item from the 'c_setting' array will be set with the corresponding SpinPanel value.
                        //We will use 'spinRender.value' to know the value of the SpinPanel.
                        c_setting[0] = bloom.spinRender.Value;
                        c_setting[1] = shadows.spinRender.Value;
                        c_setting[2] = ssao.spinRender.Value;
                        c_setting[3] = dof.spinRender.Value;

                        update_render(); //If you want, you can update the rendersetting parameters using our update_render() function.
                    }
                }
            }

            //We must only write the setting file, if 'MainConfig' Layout is closed. That is, when 'm_menu.LayoutNode.Name' is not 'MainConfig'
            else
            {
                //We will use the 'adjusted' variable to avoid a writing loop.
                if(adjusted == true)
                {
                    write_setting();
                    loaded = false;
                    adjusted = false;
                }
            }
        }

        //We will need a writing function.
        private void write_setting()
        {
            //We will create the 'fs' variable in roder to be able to write in a file.
            //We will also create a file in the path specified by 'c_path' variable.
            using(var fs = File.CreateText(c_path))
            {
                //We will convert the values of c_setting array into text and store them in a single line.
                fs.WriteLine(c_setting[0].ToString() + c_setting[1].ToString() + c_setting[2].ToString() + c_setting[3].ToString());
            }
        }

        //We will use a function that updates rendering parameters
        private void update_render()
        {
            var render = mapScene.mapDrawer.renderSettings;
            //mapScene is a variable of the MapScene class of Yukar.engine library that stores data from the scene.
            //mapDrawer is a variable that allows to get and control drawing options, add or remove lights, set the terrain height, ...
            //renderSettings is a varaible that contains the rendering parameters of the scene.

            render.useBloom = Convert.ToBoolean(c_setting[0]);
            //useBloom allows setting the bloom effect to true or false

            render.UseSSAO = Convert.ToBoolean(c_setting[2]);
            //UseSSAO allows setting the ambient occlusion effect to true or false.

            render.useDof = Convert.ToBoolean(c_setting[3]);
            //useDof allows setting the depth of field effect to true or false.

            render.ShadowCascadeCount = c_setting[1];
            //shadowCascadeCount can be defined as the quality of the shadows (from 1 to 4)

            if (c_setting[1] == 0) render.ShadowDistance = 0; //If the shadows option is set to 'No Shadows', the shadow distance is 0.
            else render.ShadowDistance = 25; //you can use any value that suits your needs.
;
        }
    }
}
