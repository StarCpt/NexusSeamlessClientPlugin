using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
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
			Patcher.Patch(commandChannelWhisperMethod, transpiler: new HarmonyMethod(GetPatchMethod(nameof(Transpiler))));
			
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
		{
			List<CodeInstruction> list = instructions.ToList();
			LocalBuilder local = gen.DeclareLocal(typeof(long));
			List<int> index = new List<int>();
			for (int i = 0; i < list.Count(); i++)
			{
				if (list[i].Calls(AccessTools.Method(typeof(MyPlayerCollection), "GetPlayerByName")))
				{
					list[i - 3].MoveLabelsTo(list[i - 1]);
					list[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DirectMessagePatch), nameof(PlayerByNameNexus)));
					index.Add(i - 2);
					index.Add(i - 3);
				}
				else if (list[i].opcode == OpCodes.Ldloc_2)
				{
					list[i] = new CodeInstruction(OpCodes.Ldloc, local);
				}
				else if (list[i].opcode == OpCodes.Stloc_2)
				{
					list[i] = new CodeInstruction(OpCodes.Stloc, local);
				}
				else if (list[i].Calls(AccessTools.Method(typeof(MyPlayer), "get_Identity")) || list[i].Calls(AccessTools.Method(typeof(MyIdentity), "get_IdentityId")))
				{
					index.Add(i);
				}
			}
			for (int i = 0; i < list.Count; i++)
			{
				if (!index.Contains(i))
					yield return list[i];
			}
		}

		public static long PlayerByNameNexus(string name)
		{
			long id = 0L;
			MyPlayer playerByName = MySession.Static.Players.GetPlayerByName(name);
			if (playerByName != null)
			{
				id = playerByName.Identity.IdentityId;
			}
			if (id == 0 && OnlinePlayers.AllServers.Count > 0)
			{
				foreach (var server in OnlinePlayers.AllServers)
				{
					foreach (OnlinePlayer player in server.Players)
					{
						if (player.PlayerName == name)
						{
							return player.IdentityID;
						}
					}
				}
			}
			return id;
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