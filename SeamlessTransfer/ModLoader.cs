using Sandbox.Definitions;
using Sandbox.Engine.Networking;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.GUI;

namespace SeamlessClientPlugin.SeamlessTransfer
{
    public static class ModLoader
    {
        /*  Mod loader should download and load missing mods for target server, and unload ones that arent needed
         * 
         * Sandbox.Game.World.MyScriptManager.LoadData() is where modded scripts get loaded and added
         * Sandbox.Game.World.MySession() calls MyDefinitionManager.Static.LoadData(mods); which loads mod data files
         * 
         * 
         *  Need to be called in the following order: 
         *  ScriptManager.Init(checkpoint.ScriptManagerData);
	     *  MyDefinitionManager.Static.LoadData(checkpoint.Mods);
	     *  PreloadModels(sector);
         * 
         * 
         */

        //Mods that are currently loaded in this instance.
        private static List<MyObjectBuilder_Checkpoint.ModItem> _currentLoadedMods =
            new List<MyObjectBuilder_Checkpoint.ModItem>();

        //Mods that we need to Load
        private static readonly List<MyObjectBuilder_Checkpoint.ModItem> TargetLoadMods =
            new List<MyObjectBuilder_Checkpoint.ModItem>();

        //Mods that we need to UnLoad
        private static readonly List<MyObjectBuilder_Checkpoint.ModItem> TargetUnLoadMods =
            new List<MyObjectBuilder_Checkpoint.ModItem>();


        private static bool _finishedDownloadingMods;
        private static bool _downloadSuccess;

        private static DateTime _downloadTimeout;

        private static readonly MethodInfo PrepareBaseSession =
            typeof(MySession).GetMethod("PreloadModels", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly FieldInfo ScriptManager =
            typeof(MySession).GetField("ScriptManager", BindingFlags.Instance | BindingFlags.Public);

        public static void DownloadNewMods(List<MyObjectBuilder_Checkpoint.ModItem> Target)
        {
            _currentLoadedMods = MySession.Static.Mods;

            //Loop through our current mods
            foreach (var mod in _currentLoadedMods.Where(mod => !Target.Contains(mod))) TargetUnLoadMods.Add(mod);

            //Loop through our TargetMods
            foreach (var mod in Target.Where(mod => !_currentLoadedMods.Contains(mod))) TargetLoadMods.Add(mod);

            _downloadTimeout = DateTime.Now + TimeSpan.FromMinutes(5);
            SeamlessClient.TryShow("Downloading New Mods");
            MyWorkshop.DownloadModsAsync(TargetLoadMods, ModDownloadingFinished);
        }

        private static void ModDownloadingFinished(bool Success)
        {
            if (Success)
            {
                SeamlessClient.TryShow("Mod Downloading Finished!");
                _finishedDownloadingMods = true;
                _downloadSuccess = true;
                //May need to wait seamless loading if mods have yet to finish downloading
            }
            else
            {
                _downloadSuccess = false;
                _finishedDownloadingMods = true;
            }
        }

        public static void ReadyModSwitch(MyObjectBuilder_Checkpoint checkpoint, MyObjectBuilder_Sector sector)
        {
            while (!_finishedDownloadingMods)
            {
                //Break out of loop
                if (_downloadTimeout < DateTime.Now)
                    break;

                Thread.Sleep(20);
            }

            _finishedDownloadingMods = false;

            //Skip mod switch
            if (!_downloadSuccess)
                return;

            //Create new script manager?
            ScriptManager.SetValue(MySession.Static, new MyScriptManager());

            MyGuiTextures.Static.Unload();
            MySession.Static.ScriptManager.Init(checkpoint.ScriptManagerData);
            //MyDefinitionManager.Static.LoadData(TargetServerMods);
            PrepareBaseSession.Invoke(null, new object[] { sector });

            MyLocalCache.PreloadLocalInventoryConfig();

            //SeamlessClient.TryShow("Finished transfering!");

            // PrepareBaseSession.Invoke(MySession.Static, new object[] { TargetServerMods, null });
        }

        private static void UnloadOldScripts()
        {
            //	MySandboxGame.Log.WriteLine(string.Format("Script loaded: {0}", value.FullName));
            var amount = 0;
            foreach (var mod in TargetUnLoadMods)
            {
                var val = MySession.Static.ScriptManager.Scripts.FirstOrDefault(x =>
                    x.Value.FullName.Contains(mod.PublishedFileId.ToString()));
                MySession.Static.ScriptManager.Scripts.Remove(val.Key);

                amount++;
            }

            SeamlessClient.TryShow($"Removed {amount} old scripts!");
        }
    }
}