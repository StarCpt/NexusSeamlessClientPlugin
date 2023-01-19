using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SeamlessClientPlugin.Messages;
using VRage;

namespace SeamlessClientPlugin.Utilities
{
	public class DirectMessagePatch
	{
		private static readonly Harmony Patcher = new Harmony("PlayerFactionCommunicationPatcher");
		private static MyGuiControlListbox.Item m_globalItem;
		private static MyGuiControlListbox.Item m_chatBotItem;
		private static Type MyTerminalChatControllerType = null;
		private static Type MyGuiScreenChatType = null;
		private static MyGuiControlListbox playerList;
		private static MethodInfo guiScreenSendChatMessageMethod = null;

		
		private static StringBuilder m_tempStringBuilder = new StringBuilder();
		
		private static bool AllowPlayerDrivenChat => MyMultiplayer.Static?.IsTextChatAvailable != false;


		public static void Patch()
		{
			MyTerminalChatControllerType =
				typeof(MyTerminalControls).Assembly.GetType("Sandbox.Game.Gui.MyTerminalChatController");
			
			MyGuiScreenChatType = typeof(MyGuiScreenBoard).Assembly.GetType("Sandbox.Game.Gui.MyGuiScreenChat");
			
			if (MyTerminalChatControllerType == null)
				return;

			var refreshPlayerListMethod = 
				MyTerminalChatControllerType.GetMethod("RefreshPlayerList", BindingFlags.Instance | BindingFlags.NonPublic);
			var closeMethod = MyTerminalChatControllerType.GetMethod("Close", BindingFlags.Public | BindingFlags.Instance);
			var commandChannelWhisperMethod =
				typeof(Sandbox.Game.GameSystems.Chat.MyChatCommands).GetMethod("CommandChannelWhisper", BindingFlags.Static | BindingFlags.NonPublic);
			
			guiScreenSendChatMessageMethod = MyGuiScreenChatType.GetMethod("SendChatMessage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
			
			Patcher.Patch(refreshPlayerListMethod, prefix: new HarmonyMethod(GetPatchMethod(nameof(RefreshPlayers))));
			Patcher.Patch(closeMethod, prefix: new HarmonyMethod(GetPatchMethod(nameof(OnCloseScreen))));
			Patcher.Patch(commandChannelWhisperMethod, prefix: new HarmonyMethod(GetPatchMethod(nameof(CommandChannelWhisperNexus))));
			
		}

		public static bool RefreshPlayers(Type __instance)
		{
			if (playerList == null)
			{

				var field = MyTerminalChatControllerType.GetField("m_playerList",
						BindingFlags.Instance | BindingFlags.NonPublic);

				playerList = field?.GetValue(__instance) as MyGuiControlListbox;
				
			}

			if (m_globalItem == null)
			{
				m_globalItem = new MyGuiControlListbox.Item(MyTexts.Get(MySpaceTexts.TerminalTab_Chat_ChatHistory), toolTip: MyTexts.GetString(MySpaceTexts.TerminalTab_Chat_ChatHistory));
			}
			

			if (AllowPlayerDrivenChat)
			{
				playerList?.Add(m_globalItem);
			}

			//Comms broadcast history
			m_tempStringBuilder.Clear();
			m_tempStringBuilder.Append(MyTexts.Get(MySpaceTexts.TerminalTab_Chat_GlobalChat));

			m_tempStringBuilder.Clear();
			m_tempStringBuilder.Append("-");
			m_tempStringBuilder.Append(MyTexts.Get(MySpaceTexts.ChatBotName));
			m_tempStringBuilder.Append("-");

			m_chatBotItem = new MyGuiControlListbox.Item(m_tempStringBuilder, toolTip: m_tempStringBuilder.ToString());
			playerList.Add(m_chatBotItem);

			if (AllowPlayerDrivenChat)
			{
				var playersOrganised = new List<OnlinePlayer>();
				foreach (var server in OnlinePlayers.AllServers)
				{
					foreach (var player in server.Players)
					{
						playersOrganised.Add(player);
					}
				}
				
				playersOrganised.Sort((x, y) => string.Compare(x.PlayerName, y.PlayerName, StringComparison.Ordinal));

				foreach (var player in playersOrganised)
				{
					var playerIdentity =
						MySession.Static.Players.TryGetIdentity(player.IdentityID);

					if (playerIdentity != null && playerIdentity.IdentityId != MySession.Static.LocalPlayerId)
					{
						m_tempStringBuilder.Clear();
						m_tempStringBuilder.Append(playerIdentity.DisplayName);

						var item = new MyGuiControlListbox.Item(text: m_tempStringBuilder, userData: playerIdentity,
							toolTip: m_tempStringBuilder.ToString());
						playerList.Add(item);
					}
				}
			}
			else
			{
				playerList.Add(m_globalItem);
			}

			return false;
		}

		public static bool CommandChannelWhisperNexus(string[] args )
		{
			string name = string.Empty;
            string msg = string.Empty;

            if (args == null || args.Length < 1)
            {
                MyHud.Chat.ShowMessage(MyTexts.GetString(MyCommonTexts.ChatCommand_Texts_Author), MyTexts.GetString(MyCommonTexts.ChatCommand_Texts_WhisperChatTarget));
                return false;
            }

            if(args[0].Length > 0 && args[0][0] == '\"')
            {
                // compound name - '/w "name with spaces" message with spaces' should send "message with spaces" to player 'name with spaces', if you know how to do it through regex or have time to cleane it, you are welcome to do so, we are out of time.
                int index = 0;
                bool found = false;
                while(index < args.Length)
                {
                    if(args[index][args[index].Length-1] == '"')
                    {
                        found = true;
                        break;
                    }
                    index++;
                }
                if(found)
                {
                    if(index == 0)
                    {
                        name = (args[0].Length > 2)?args[0].Substring(1,args[0].Length-2):string.Empty;

                        if (index < args.Length - 1)
                        {
                            string text = args[1];
                            int i = 2;
                            while (i < args.Length)
                            {
                                text += " " + args[i];
                                i++;
                            }
                            msg = text;
                        }
                    }
                    else
                    {
                        string textN = args[0];
                        int i = 1;
                        while (i <= index)
                        {
                            textN += " " + args[i];
                            i++;
                        }
                        name = (textN.Length > 2)?textN.Substring(1,textN.Length-2):string.Empty;

                        if (index < args.Length - 1)
                        {
                            string text = args[index+1];
                            i = index+2;
                            while (i < args.Length)
                            {
                                text += " " + args[i];
                                i++;
                            }
                            msg = text;
                        }
                    }
                }
                else
                {
                    name = args[0];
                    if (args.Length > 1)
                    {
                        string text = args[1];
                        int i = 2;
                        while (i < args.Length)
                        {
                            text += " " + args[i];
                            i++;
                        }
                        msg = text;
                    }
                }
            }
            else
            {
                // simple name

                name = args[0];

                if(args.Length > 1 )
                {
                    string text = args[1];
                    int i = 2;
                    while(i < args.Length)
                    {
                        text += " " + args[i];
                        i++;
                    }

                    if (string.IsNullOrEmpty(text))
                        return false;
                    msg = text;
                }
            }
            
            var server = OnlinePlayers.AllServers.FirstOrDefault(x => x.Players.Any(y => y.PlayerName == name));
            
            if (server == null)
			{
				MyHud.Chat.ShowMessage(MyTexts.GetString(MyCommonTexts.ChatCommand_Texts_Author), MyTexts.GetString(MyCommonTexts.ChatCommand_Texts_WhisperChatTarget));
				return false;
			}
            
            var player = server.Players.FirstOrDefault(x => x.PlayerName == name);


            MySession.Static.ChatSystem.ChangeChatChannel_Whisper(player.IdentityID);

            if(!string.IsNullOrEmpty(msg))
                guiScreenSendChatMessageMethod.Invoke(null, new object[] { msg });
			return false;
		}

		public static void OnCloseScreen()
		{
			m_globalItem = null;
			playerList = null;
		}
		
		private static MethodInfo GetPatchMethod(string v) =>
			typeof(DirectMessagePatch).GetMethod(v,
				BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}
}