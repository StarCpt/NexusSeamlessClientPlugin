using System;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage;

namespace SeamlessClientPlugin.Utilities
{
	public class OnlinePlayersComms
	{
		private static readonly Harmony Patcher = new Harmony("OnlinePlayersCommsPatcher");
		private static MyGuiControlListbox.Item m_globalItem;
		private static MyGuiControlListbox.Item m_chatBotItem;
		private static Type MyTerminalChatControllerType = null;
		private static MyGuiControlListbox playerList;

		
		private static StringBuilder m_tempStringBuilder = new StringBuilder();
		
		private static bool AllowPlayerDrivenChat => MyMultiplayer.Static?.IsTextChatAvailable != false;


		public static void Patch()
		{
			MyTerminalChatControllerType =
				typeof(MyTerminalControls).Assembly.GetType("Sandbox.Game.Gui.MyTerminalChatController");
			
			if (MyTerminalChatControllerType == null)
				return;

			var RefreshPlayerListMethod = 
				MyTerminalChatControllerType.GetMethod("RefreshPlayerList", BindingFlags.Instance | BindingFlags.NonPublic);

			Patcher.Patch(RefreshPlayerListMethod, prefix: new HarmonyMethod(GetPatchMethod(nameof(RefreshPlayers))));
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
				playerList.Add(m_globalItem);
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
				foreach (var player in MySession.Static.Players.GetAllPlayers())
				{
					var playerIdentity =
						MySession.Static.Players.TryGetIdentity(MySession.Static.Players.TryGetIdentityId(player.SteamId, player.SerialId));

					if (playerIdentity != null && playerIdentity.IdentityId != MySession.Static.LocalPlayerId && player.SerialId == 0)
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
		
		private static MethodInfo GetPatchMethod(string v) =>
			typeof(OnlinePlayersComms).GetMethod(v,
				BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}
}