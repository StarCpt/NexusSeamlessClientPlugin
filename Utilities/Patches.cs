using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.Entity;
using VRage.GameServices;
using VRage.Network;
using VRage.Utils;
using VRageMath;

namespace SeamlessClientPlugin.SeamlessTransfer
{
    public static class Patches
    {
        /* All internal classes Types */
        public static readonly Type ClientType =
            Type.GetType("Sandbox.Engine.Multiplayer.MyMultiplayerClient, Sandbox.Game");

        public static readonly Type SyncLayerType = Type.GetType("Sandbox.Game.Multiplayer.MySyncLayer, Sandbox.Game");

        public static readonly Type MyTransportLayerType =
            Type.GetType("Sandbox.Engine.Multiplayer.MyTransportLayer, Sandbox.Game");

        public static readonly Type MySessionType = Type.GetType("Sandbox.Game.World.MySession, Sandbox.Game");

        public static readonly Type VirtualClientsType =
            Type.GetType("Sandbox.Engine.Multiplayer.MyVirtualClients, Sandbox.Game");

        public static readonly Type GUIScreenChat = Type.GetType("Sandbox.Game.Gui.MyGuiScreenChat, Sandbox.Game");

        public static readonly Type MyMultiplayerClientBase =
            Type.GetType("Sandbox.Engine.Multiplayer.MyMultiplayerClientBase, Sandbox.Game");

        public static readonly Type MySteamServerDiscovery =
            Type.GetType("VRage.Steam.MySteamServerDiscovery, Vrage.Steam");
        
        public static readonly Type MyEntitiesType =
            Type.GetType("Sandbox.Game.Entities.MyEntities, Sandbox.Game");
        
        public static readonly Type MySlimBlockType =
            Type.GetType("Sandbox.Game.Entities.Cube.MySlimBlock, Sandbox.Game");

        /* Harmony Patcher */
        private static readonly Harmony Patcher = new Harmony("SeamlessClientPatcher");


        /* Static Contructors */
        public static ConstructorInfo ClientConstructor { get; private set; }
        public static ConstructorInfo SyncLayerConstructor { get; private set; }
        public static ConstructorInfo TransportLayerConstructor { get; private set; }
        public static ConstructorInfo MySessionConstructor { get; private set; }
        public static ConstructorInfo MyMultiplayerClientBaseConstructor { get; private set; }


        /* Static FieldInfos and PropertyInfos */
        public static PropertyInfo MySessionLayer { get; private set; }
        public static FieldInfo VirtualClients { get; private set; }
        public static FieldInfo AdminSettings { get; private set; }
        public static FieldInfo RemoteAdminSettings { get; private set; }
        public static FieldInfo MPlayerGpsCollection { get; private set; }


        /* Static MethodInfos */
        public static MethodInfo InitVirtualClients { get; private set; }
        public static MethodInfo LoadPlayerInternal { get; private set; }
        public static MethodInfo LoadMembersFromWorld { get; private set; }
        public static MethodInfo LoadMultiplayer { get; private set; }
        public static MethodInfo GpsRegisterChat { get; private set; }

        public static MethodInfo SendPlayerData;


        public static event EventHandler<JoinResultMsg> OnJoinEvent;


        /* WorldGenerator */
        public static MethodInfo UnloadProceduralWorldGenerator;


        private static FieldInfo _mBuffer;

        public static void GetPatches()
        {
            //Get reflected values and store them


            /* Get Constructors */
            ClientConstructor = GetConstructor(ClientType, BindingFlags.Instance | BindingFlags.NonPublic,
                new[] { typeof(MyGameServerItem), SyncLayerType });
            SyncLayerConstructor = GetConstructor(SyncLayerType, BindingFlags.Instance | BindingFlags.NonPublic,
                new[] { MyTransportLayerType });
            TransportLayerConstructor = GetConstructor(MyTransportLayerType,
                BindingFlags.Instance | BindingFlags.Public, new[] { typeof(int) });
            MySessionConstructor = GetConstructor(MySessionType, BindingFlags.Instance | BindingFlags.NonPublic,
                new[] { typeof(MySyncLayer), typeof(bool) });
            MyMultiplayerClientBaseConstructor = GetConstructor(MyMultiplayerClientBase,
                BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(MySyncLayer) });


            /* Get Fields and Properties */
            MySessionLayer = GetProperty(typeof(MySession), "SyncLayer", BindingFlags.Instance | BindingFlags.Public);
            VirtualClients = GetField(typeof(MySession), "VirtualClients",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            AdminSettings = GetField(typeof(MySession), "m_adminSettings",
                BindingFlags.Instance | BindingFlags.NonPublic);
            RemoteAdminSettings = GetField(typeof(MySession), "m_remoteAdminSettings",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MPlayerGpsCollection = GetField(typeof(MyPlayerCollection), "m_players",
                BindingFlags.Instance | BindingFlags.NonPublic);

            _mBuffer = GetField(MyTransportLayerType, "m_buffer", BindingFlags.Instance | BindingFlags.NonPublic);


            /* Get Methods */
            var onJoin = GetMethod(ClientType, "OnUserJoined", BindingFlags.NonPublic | BindingFlags.Instance);
            var loadingAction = GetMethod(typeof(MySessionLoader), "LoadMultiplayerSession",
                BindingFlags.Public | BindingFlags.Static);
            var setEntityName = GetMethod(MyEntitiesType, "SetEntityName", BindingFlags.Public | BindingFlags.Static);
            var removeName = GetMethod(MyEntitiesType, "RemoveName", BindingFlags.Public | BindingFlags.Static);
            var isNameExists = GetMethod(MyEntitiesType, "IsNameExists", BindingFlags.Public | BindingFlags.Static);
            var unloadData =  GetMethod(MyEntitiesType, "UnloadData", BindingFlags.Public | BindingFlags.Static);
            var updateDeformation = GetMethod(MySlimBlockType, "UpdateMaxDeformation", BindingFlags.Public | BindingFlags.Instance);
            InitVirtualClients = GetMethod(VirtualClientsType, "Init", BindingFlags.Instance | BindingFlags.Public);
            LoadPlayerInternal = GetMethod(typeof(MyPlayerCollection), "LoadPlayerInternal",
                BindingFlags.Instance | BindingFlags.NonPublic);
            LoadMembersFromWorld = GetMethod(typeof(MySession), "LoadMembersFromWorld",
                BindingFlags.NonPublic | BindingFlags.Instance);
            LoadMultiplayer = GetMethod(typeof(MySession), "LoadMultiplayer",
                BindingFlags.Static | BindingFlags.NonPublic);
            SendPlayerData = GetMethod(ClientType, "SendPlayerData", BindingFlags.Instance | BindingFlags.NonPublic);
            UnloadProceduralWorldGenerator = GetMethod(typeof(MyProceduralWorldGenerator), "UnloadData",
                BindingFlags.Instance | BindingFlags.NonPublic);
            GpsRegisterChat = GetMethod(typeof(MyGpsCollection), "RegisterChat",
                BindingFlags.Instance | BindingFlags.NonPublic);


            //MethodInfo ConnectToServer = GetMethod(typeof(MyGameService), "ConnectToServer", BindingFlags.Static | BindingFlags.Public);
            var loadingScreenDraw = GetMethod(typeof(MyGuiScreenLoading), "DrawInternal",
                BindingFlags.Instance | BindingFlags.NonPublic);


            //Test patches
            //MethodInfo SetPlayerDed = GetMethod(typeof(MyPlayerCollection), "SetPlayerDeadInternal", BindingFlags.Instance | BindingFlags.NonPublic);


            Patcher.Patch(loadingScreenDraw, prefix: new HarmonyMethod(GetPatchMethod(nameof(DrawInternal))));
            Patcher.Patch(onJoin, postfix: new HarmonyMethod(GetPatchMethod(nameof(OnUserJoined))));
            Patcher.Patch(loadingAction, prefix: new HarmonyMethod(GetPatchMethod(nameof(LoadMultiplayerSession))));
            Patcher.Patch(setEntityName, prefix: new HarmonyMethod(GetPatchMethod(nameof(SetEntityName))));
            Patcher.Patch(removeName, prefix: new HarmonyMethod(GetPatchMethod(nameof(RemoveName))));
            Patcher.Patch(isNameExists, prefix: new HarmonyMethod(GetPatchMethod(nameof(IsNameExists))));
            Patcher.Patch(unloadData, postfix: new HarmonyMethod(GetPatchMethod(nameof(UnloadData))));
            Patcher.Patch(updateDeformation, prefix: new HarmonyMethod(GetPatchMethod(nameof(UpdateDeformation))));
            //Patcher.Patch(SetPlayerDed, prefix: new HarmonyMethod(GetPatchMethod(nameof(SetPlayerDeadInternal))));
        }


        private static MethodInfo GetPatchMethod(string v)
        {
            return typeof(Patches).GetMethod(v, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        #region EntityNamePatches
        
        private static ConcurrentDictionary<long, string> EntityNameReverseLookup =
            new ConcurrentDictionary<long, string>();
        // reverse dictionary
        private static bool SetEntityName(MyEntity myEntity, bool possibleRename)
        {
            if (string.IsNullOrEmpty(myEntity.Name))
                return false;
            if (possibleRename)
                if (EntityNameReverseLookup.ContainsKey(myEntity.EntityId))
                {
                    var previousName = EntityNameReverseLookup[myEntity.EntityId];
                    if (previousName != myEntity.Name) MyEntities.m_entityNameDictionary.Remove(previousName);
                }

            if (MyEntities.m_entityNameDictionary.TryGetValue(myEntity.Name, out var myEntity1))
            {
                if (myEntity1 == myEntity)
                    return false;
            }
            else
            {
                MyEntities.m_entityNameDictionary[myEntity.Name] = myEntity;
                EntityNameReverseLookup[myEntity.EntityId] = myEntity.Name;
            }

            return false;
        }
        
        private static bool RemoveName(MyEntity entity)
        {
            if (string.IsNullOrEmpty(entity.Name))
                return false;
            MyEntities.m_entityNameDictionary.Remove(entity.Name);
            EntityNameReverseLookup.Remove(entity.EntityId);
            return false;
        }
        
        private static bool IsNameExists(ref bool __result, MyEntity entity, string name)
        {
            if (string.IsNullOrEmpty(entity.Name))
            {
                __result = false;
                return false;
            }

            if (MyEntities.m_entityNameDictionary.ContainsKey(name))
            {
                var ent = MyEntities.m_entityNameDictionary[entity.Name];
                __result = ent != entity;
                return false;
            }

            __result = false;
            return false;
        }
        
        private static void UnloadData()
        {
            EntityNameReverseLookup.Clear();
        }
        
        #endregion
        
        #region SlimBlockPatches
        
        private static bool UpdateDeformation(MySlimBlock __instance)
        {
            return __instance.UsesDeformation;
        }
        #endregion

        #region LoadingScreenPatches

        /* Loading Screen Stuff */

        private static string _loadingScreenTexture = null;
        private static string _serverName;

        private static bool LoadMultiplayerSession(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        {
            //


            MyLog.Default.WriteLine("LoadSession() - Start");
            if (!MyWorkshop.CheckLocalModsAllowed(world.Checkpoint.Mods, allowLocalMods: false))
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error,
                    MyMessageBoxButtonsType.OK, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.DialogTextLocalModsDisabledInMultiplayer)));
                MyLog.Default.WriteLine("LoadSession() - End");
                return false;
            }

            MyLog.Default.WriteLine("Seamless Downloading mods!");


            MyWorkshop.DownloadModsAsync(world.Checkpoint.Mods, delegate(bool success)
            {
                if (success)
                {
                    MyScreenManager.CloseAllScreensNowExcept(null);
                    MyGuiSandbox.Update(16);
                    if (MySession.Static != null)
                    {
                        MySession.Static.Unload();
                        MySession.Static = null;
                    }

                    _serverName = multiplayerSession.HostName;
                    GetCustomLoadingScreenPath(world.Checkpoint.Mods, out _loadingScreenTexture);


                    MySessionLoader.StartLoading(delegate
                    {
                        LoadMultiplayer.Invoke(null, new object[] { world, multiplayerSession });
                        //MySession.LoadMultiplayer(world, multiplayerSession);
                    }, null, null, null);
                }
                else
                {
                    multiplayerSession.Dispose();
                    MySessionLoader.UnloadAndExitToMenu();
                    if (MyGameService.IsOnline)
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error,
                            MyMessageBoxButtonsType.OK,
                            messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                            messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed)));
                    }
                    else
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error,
                            MyMessageBoxButtonsType.OK,
                            messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                            messageText: new StringBuilder(string.Format(
                                MyTexts.GetString(MyCommonTexts.DialogTextDownloadModsFailedSteamOffline),
                                MySession.GameServiceName))));
                    }
                }

                MyLog.Default.WriteLine("LoadSession() - End");
            }, delegate
            {
                multiplayerSession.Dispose();
                MySessionLoader.UnloadAndExitToMenu();
            });

            return false;
        }

        private static bool DrawInternal(MyGuiScreenLoading __instance)
        {
            //If we dont have a custom loading screen texture, do not do the special crap below
            if (string.IsNullOrEmpty(_loadingScreenTexture))
                return true;


            var mTransitionAlpha = (float)typeof(MyGuiScreenBase)
                .GetField("m_transitionAlpha", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            const string mFont = "LoadingScreen";
            //MyGuiControlMultilineText m_multiTextControl = (MyGuiControlMultilineText)typeof(MyGuiScreenLoading).GetField("m_multiTextControl", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);


            var color = new Color(255, 255, 255, 250);
            color.A = (byte)(color.A * mTransitionAlpha);
            var fullscreenRectangle = MyGuiManager.GetFullscreenRectangle();
            MyGuiManager.DrawSpriteBatch("Textures\\GUI\\Blank.dds", fullscreenRectangle, Color.Black, false, true);
            Rectangle outRect;
            MyGuiManager.GetSafeHeightFullScreenPictureSize(MyGuiConstants.LOADING_BACKGROUND_TEXTURE_REAL_SIZE,
                out outRect);
            MyGuiManager.DrawSpriteBatch(_loadingScreenTexture, outRect,
                new Color(new Vector4(1f, 1f, 1f, mTransitionAlpha)), true, true);
            MyGuiManager.DrawSpriteBatch("Textures\\Gui\\Screens\\screen_background_fade.dds", outRect,
                new Color(new Vector4(1f, 1f, 1f, mTransitionAlpha)), true, true);

            //MyGuiSandbox.DrawGameLogoHandler(m_transitionAlpha, MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, 44, 68));

            var loadScreen = $"Loading into {_serverName}! Please wait!";


            MyGuiManager.DrawString(mFont, loadScreen, new Vector2(0.5f, 0.95f),
                MyGuiSandbox.GetDefaultTextScaleWithLanguage() * 1.1f,
                new Color(MyGuiConstants.LOADING_PLEASE_WAIT_COLOR * mTransitionAlpha),
                MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM);

            MyGuiManager.DrawString(mFont, "Nexus & SeamlessClient Made by: Casimir", new Vector2(0.95f, 0.95f),
                MyGuiSandbox.GetDefaultTextScaleWithLanguage() * 1.1f,
                new Color(MyGuiConstants.LOADING_PLEASE_WAIT_COLOR * mTransitionAlpha),
                MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM);

            /*
            if (string.IsNullOrEmpty(m_customTextFromConstructor))
            {
                string font = m_font;
                Vector2 positionAbsoluteBottomLeft = m_multiTextControl.GetPositionAbsoluteBottomLeft();
                Vector2 textSize = m_multiTextControl.TextSize;
                Vector2 normalizedCoord = positionAbsoluteBottomLeft + new Vector2((m_multiTextControl.Size.X - textSize.X) * 0.5f + 0.025f, 0.025f);
                MyGuiManager.DrawString(font, m_authorWithDash.ToString(), normalizedCoord, MyGuiSandbox.GetDefaultTextScaleWithLanguage());
            }
            */


            //m_multiTextControl.Draw(1f, 1f);

            return false;
        }


        private static bool GetCustomLoadingScreenPath(List<MyObjectBuilder_Checkpoint.ModItem> Mods, out string File)
        {
            File = null;
            var workshopDir = MyFileSystem.ModsPath;
            var backgrounds = new List<string>();
            var r = new Random(DateTime.Now.Millisecond);
            SeamlessClient.TryShow(workshopDir);
            try
            {
                SeamlessClient.TryShow("Installed Mods: " + Mods);
                foreach (var mod in Mods)
                {
                    var searchDir = mod.GetPath();

                    if (!Directory.Exists(searchDir))
                        continue;


                    var files = Directory.GetFiles(searchDir, "CustomLoadingBackground*.dds",
                        SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        // Adds all files containing CustomLoadingBackground to a list for later randomisation
                        SeamlessClient.TryShow(mod.FriendlyName + " contains a custom loading background!");
                        backgrounds.Add(file);
                    }
                }

                // Randomly pick a loading screen from the available backgrounds
                var rInt = r.Next(0, backgrounds.Count);
                File = backgrounds[rInt];
                return true;
            }
            catch (Exception ex)
            {
                SeamlessClient.TryShow(ex.ToString());
            }

            SeamlessClient.TryShow("No installed custom loading screen!");
            return false;
        }

        #endregion


        private static void OnUserJoined(ref JoinResultMsg msg)
        {
            if (msg.JoinResult == JoinResult.OK)
            {
                //SeamlessClient.TryShow("User Joined! Result: " + msg.JoinResult.ToString());

                //Invoke the switch event
                OnJoinEvent?.Invoke(null, msg);
            }
        }

        private static bool OnConnectToServer(MyGameServerItem server, Action<JoinResult> onDone) =>
            !SeamlessClient.IsSwitching;

        /* Patch Utils */

        private static MethodInfo GetMethod(IReflect type, string methodName, BindingFlags flags)
        {
            var foundMethod = type.GetMethod(methodName, flags);
            if (foundMethod == null)
                throw new NullReferenceException($"Method for {methodName} is null!");
            return foundMethod;
        }

        private static FieldInfo GetField(IReflect type, string fieldName, BindingFlags flags)
        {
            var foundField = type.GetField(fieldName, flags);
            if (foundField == null)
                throw new NullReferenceException($"Field for {fieldName} is null!");
            return foundField;
        }

        private static PropertyInfo GetProperty(IReflect type, string propertyName, BindingFlags flags)
        {
            var foundProperty = type.GetProperty(propertyName, flags);
            if (foundProperty == null)
                throw new NullReferenceException($"Property for {propertyName} is null!");
            return foundProperty;
        }

        private static ConstructorInfo GetConstructor(Type type, BindingFlags flags, Type[] types)
        {
            var foundConstructor = type.GetConstructor(flags, null, types, null);
            if (foundConstructor == null)
                throw new NullReferenceException($"Contructor for {type.Name} is null!");
            return foundConstructor;
        }
    }
}