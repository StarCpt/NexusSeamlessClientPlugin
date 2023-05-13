using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SeamlessClientPlugin.Messages;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace SeamlessClientPlugin.Utilities
{
    public class OnlinePlayers
    {
        private static readonly Harmony Patcher = new Harmony("OnlinePlayersPatcher");
        public static List<OnlineServer> AllServers = new List<OnlineServer>();
        public static int CurrentServer;
        private static string _currentServerName;

        public static int TotalPlayerCount;
        private static int _currentPlayerCount;

        private static MethodInfo _mUpdateCaption;
        private static MethodInfo _mRefreshMuteIcons;
        private static MethodInfo _mOnToggleMutePressed;
        private static MethodInfo _mAddCaption;

        private static MethodInfo _mProfileButtonButtonClicked;
        private static MethodInfo _mPromoteButtonButtonClicked;
        private static MethodInfo _mDemoteButtonButtonClicked;
        private static MethodInfo _mKickButtonButtonClicked;
        private static MethodInfo _mBanButtonButtonClicked;
        private static MethodInfo _mTradeButtonButtonClicked;
        private static MethodInfo _mInviteButtonButtonClicked;
        private static MethodInfo _mPlayersTableItemSelected;

        private static MethodInfo _mUpdateButtonsEnabledState;

        private static FieldInfo _mPlayersTable;
        private static FieldInfo _mPings;
        private static FieldInfo _mMaxPlayers;
        private static FieldInfo _mWarfareTimeRemaintingLabel;
        private static FieldInfo _mWarfareTimeRemaintingTime;
        private static FieldInfo _mLastSelected;

        /* Buttons */
        private static FieldInfo _mProfileButton;
        private static FieldInfo _mPromoteButton;
        private static FieldInfo _mDemoteButton;
        private static FieldInfo _mKickButton;
        private static FieldInfo _mBanButton;
        private static FieldInfo _mTradeButton;
        private static FieldInfo _mInviteButton;

        private static FieldInfo _mCaption;
        private static FieldInfo _mLobbyTypeCombo;
        private static FieldInfo _mMaxPlayersSlider;

        public static void Patch()
        {
            _mPlayersTable =
                typeof(MyGuiScreenPlayers).GetField("m_playersTable", BindingFlags.Instance | BindingFlags.NonPublic);
            _mPings = typeof(MyGuiScreenPlayers).GetField("pings", BindingFlags.Instance | BindingFlags.NonPublic);
            _mUpdateCaption =
                typeof(MyGuiScreenPlayers).GetMethod("UpdateCaption", BindingFlags.Instance | BindingFlags.NonPublic);
            _mRefreshMuteIcons =
                typeof(MyGuiScreenPlayers).GetMethod("RefreshMuteIcons",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            _mOnToggleMutePressed = typeof(MyGuiScreenPlayers).GetMethod("OnToggleMutePressed",
                BindingFlags.Instance | BindingFlags.NonPublic);


            _mProfileButtonButtonClicked = typeof(MyGuiScreenPlayers).GetMethod("profileButton_ButtonClicked",
                BindingFlags.Instance | BindingFlags.NonPublic);
            _mPromoteButtonButtonClicked = typeof(MyGuiScreenPlayers).GetMethod("promoteButton_ButtonClicked",
                BindingFlags.Instance | BindingFlags.NonPublic);
            _mDemoteButtonButtonClicked = typeof(MyGuiScreenPlayers).GetMethod("demoteButton_ButtonClicked",
                BindingFlags.Instance | BindingFlags.NonPublic);
            _mKickButtonButtonClicked = typeof(MyGuiScreenPlayers).GetMethod("kickButton_ButtonClicked",
                BindingFlags.Instance | BindingFlags.NonPublic);
            _mBanButtonButtonClicked = typeof(MyGuiScreenPlayers).GetMethod("banButton_ButtonClicked",
                BindingFlags.Instance | BindingFlags.NonPublic);
            _mTradeButtonButtonClicked = typeof(MyGuiScreenPlayers).GetMethod("tradeButton_ButtonClicked",
                BindingFlags.Instance | BindingFlags.NonPublic);
            _mInviteButtonButtonClicked = typeof(MyGuiScreenPlayers).GetMethod("inviteButton_ButtonClicked",
                BindingFlags.Instance | BindingFlags.NonPublic);
            _mUpdateButtonsEnabledState = typeof(MyGuiScreenPlayers).GetMethod("UpdateButtonsEnabledState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            _mPlayersTableItemSelected = typeof(MyGuiScreenPlayers).GetMethod("playersTable_ItemSelected",
                BindingFlags.Instance | BindingFlags.NonPublic);

            //m_SetColumnName = typeof(MyGuiScreenPlayers).GetMethod("SetColumnName", BindingFlags.Instance | BindingFlags.se);


            _mCaption = typeof(MyGuiScreenPlayers).GetField("m_caption",
                BindingFlags.Instance | BindingFlags.NonPublic);
            _mMaxPlayers =
                typeof(MyGuiScreenPlayers).GetField("m_maxPlayers", BindingFlags.Instance | BindingFlags.NonPublic);
            _mWarfareTimeRemaintingLabel = typeof(MyGuiScreenPlayers).GetField("m_warfare_timeRemainting_label",
                BindingFlags.Instance | BindingFlags.NonPublic);
            _mWarfareTimeRemaintingTime = typeof(MyGuiScreenPlayers).GetField("m_warfare_timeRemainting_time",
                BindingFlags.Instance | BindingFlags.NonPublic);
            _mLastSelected =
                typeof(MyGuiScreenPlayers).GetField("m_lastSelected", BindingFlags.Instance | BindingFlags.NonPublic);
            _mMaxPlayersSlider =
                typeof(MyGuiScreenPlayers).GetField("m_maxPlayersSlider",
                    BindingFlags.Instance | BindingFlags.NonPublic);


            /* Buttons */
            _mProfileButton =
                typeof(MyGuiScreenPlayers).GetField("m_profileButton", BindingFlags.Instance | BindingFlags.NonPublic);
            _mPromoteButton =
                typeof(MyGuiScreenPlayers).GetField("m_promoteButton", BindingFlags.Instance | BindingFlags.NonPublic);
            _mDemoteButton =
                typeof(MyGuiScreenPlayers).GetField("m_demoteButton", BindingFlags.Instance | BindingFlags.NonPublic);
            _mKickButton =
                typeof(MyGuiScreenPlayers).GetField("m_kickButton", BindingFlags.Instance | BindingFlags.NonPublic);
            _mBanButton =
                typeof(MyGuiScreenPlayers).GetField("m_banButton", BindingFlags.Instance | BindingFlags.NonPublic);
            _mTradeButton =
                typeof(MyGuiScreenPlayers).GetField("m_tradeButton", BindingFlags.Instance | BindingFlags.NonPublic);
            _mInviteButton =
                typeof(MyGuiScreenPlayers).GetField("m_inviteButton", BindingFlags.Instance | BindingFlags.NonPublic);
            _mLobbyTypeCombo =
                typeof(MyGuiScreenPlayers).GetField("m_lobbyTypeCombo", BindingFlags.Instance | BindingFlags.NonPublic);
            _mAddCaption = typeof(MyGuiScreenPlayers).GetMethod("AddCaption",
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(MyStringId), typeof(Vector4?), typeof(Vector2?), typeof(float) }, null);

            var recreateControls =
                typeof(MyGuiScreenPlayers).GetMethod("RecreateControls", BindingFlags.Instance | BindingFlags.Public);
            var updateCaption =
                typeof(MyGuiScreenPlayers).GetMethod("UpdateCaption", BindingFlags.Instance | BindingFlags.NonPublic);

            Patcher.Patch(recreateControls, prefix: new HarmonyMethod(GetPatchMethod(nameof(RecreateControlsPrefix))));
            Patcher.Patch(updateCaption, prefix: new HarmonyMethod(GetPatchMethod(nameof(UpdateCaption))));
            //Patcher.Patch(recreateControls, postfix: new HarmonyMethod(GetPatchMethod(nameof(RecreateControlsSuffix))));
        }

        // ReSharper disable once InconsistentNaming
        public static bool RecreateControlsPrefix(MyGuiScreenPlayers __instance, bool constructor)
        {
            if (MyMultiplayer.Static != null && MyMultiplayer.Static.IsLobby) return true;

            try
            {
                __instance.Controls.Clear();
                __instance.Elements.Clear();
                //__instance.Elements.Add(m_cl);
                __instance.FocusedControl = null;
                //__instance.m_firstUpdateServed = false;
                //__instance.m_screenCreation = DateTime.UtcNow;
                //__instance.m_gamepadHelpInitialized = false;
                //__instance.m_gamepadHelpLabel = null;

                //SeamlessClient.TryShow("A");


                //__instance.RecreateControls(constructor);
                __instance.Size = new Vector2(0.937f, 0.913f);
                __instance.CloseButtonEnabled = true;


                //SeamlessClient.TryShow("A2");
                //MyCommonTexts.ScreenCaptionPlayers

                //MyStringId ID = MyStringId.GetOrCompute("Test Caption");
                _mCaption.SetValue(__instance,
                    _mAddCaption.Invoke(__instance,
                        new object[] { MyCommonTexts.ScreenCaptionPlayers, null, new Vector2(0f, 0.003f), 0.8f }));


                const float startX = -0.435f;
                const float startY = -0.36f;

                var myGuiControlSeparatorList = new MyGuiControlSeparatorList();
                myGuiControlSeparatorList.AddHorizontal(new Vector2(startX, startY), .83f);

                var start = new Vector2(startX, 0.358f);
                myGuiControlSeparatorList.AddHorizontal(start, 0.728f);
                myGuiControlSeparatorList.AddHorizontal(new Vector2(startX, 0.05f), 0.17f);
                __instance.Controls.Add(myGuiControlSeparatorList);


                var spacing = new Vector2(0f, 0.057f);
                var vector3 = new Vector2(startX, startY + 0.035f);

                //SeamlessClient.TryShow("B");

                var mProfileButton = new MyGuiControlButton(vector3, MyGuiControlButtonStyleEnum.Default, null, null,
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, null,
                    MyTexts.Get(MyCommonTexts.ScreenPlayers_Profile));
                mProfileButton.ButtonClicked += delegate(MyGuiControlButton obj)
                {
                    _mProfileButtonButtonClicked.Invoke(__instance, new object[] { obj });
                };
                __instance.Controls.Add(mProfileButton);
                vector3 += spacing;
                _mProfileButton.SetValue(__instance, mProfileButton);


                var mPromoteButton = new MyGuiControlButton(vector3, MyGuiControlButtonStyleEnum.Default, null, null,
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, null,
                    MyTexts.Get(MyCommonTexts.ScreenPlayers_Promote));
                mPromoteButton.ButtonClicked += delegate(MyGuiControlButton obj)
                {
                    _mPromoteButtonButtonClicked.Invoke(__instance, new object[] { obj });
                };
                __instance.Controls.Add(mPromoteButton);
                vector3 += spacing;
                _mPromoteButton.SetValue(__instance, mPromoteButton);

                var mDemoteButton = new MyGuiControlButton(vector3, MyGuiControlButtonStyleEnum.Default, null, null,
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, null,
                    MyTexts.Get(MyCommonTexts.ScreenPlayers_Demote));
                mDemoteButton.ButtonClicked += delegate(MyGuiControlButton obj)
                {
                    _mDemoteButtonButtonClicked.Invoke(__instance, new object[] { obj });
                };
                __instance.Controls.Add(mDemoteButton);
                vector3 += spacing;
                _mDemoteButton.SetValue(__instance, mDemoteButton);


                var mKickButton = new MyGuiControlButton(vector3, MyGuiControlButtonStyleEnum.Default, null, null,
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, null,
                    MyTexts.Get(MyCommonTexts.ScreenPlayers_Kick));
                mKickButton.ButtonClicked += delegate(MyGuiControlButton obj)
                {
                    _mKickButtonButtonClicked.Invoke(__instance, new object[] { obj });
                };
                __instance.Controls.Add(mKickButton);
                vector3 += spacing;
                _mKickButton.SetValue(__instance, mKickButton);

                var mBanButton = new MyGuiControlButton(vector3, MyGuiControlButtonStyleEnum.Default, null, null,
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, null,
                    MyTexts.Get(MyCommonTexts.ScreenPlayers_Ban));
                mBanButton.ButtonClicked += delegate(MyGuiControlButton obj)
                {
                    _mBanButtonButtonClicked.Invoke(__instance, new object[] { obj });
                };
                __instance.Controls.Add(mBanButton);
                vector3 += spacing;
                _mBanButton.SetValue(__instance, mBanButton);


                var mTradeButton = new MyGuiControlButton(vector3, MyGuiControlButtonStyleEnum.Default, null, null,
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, null,
                    MyTexts.Get(MySpaceTexts.PlayersScreen_TradeBtn));
                mTradeButton.SetTooltip(MyTexts.GetString(MySpaceTexts.PlayersScreen_TradeBtn_TTP));
                mTradeButton.ButtonClicked += delegate(MyGuiControlButton obj)
                {
                    _mTradeButtonButtonClicked.Invoke(__instance, new object[] { obj });
                };
                __instance.Controls.Add(mTradeButton);
                _mTradeButton.SetValue(__instance, mTradeButton);


                //SeamlessClient.TryShow("C");

                var vector4 = vector3 + new Vector2(-0.002f, mTradeButton.Size.Y + 0.03f);
                var mLobbyTypeCombo = new MyGuiControlCombobox(vector4, null, null, null, 3);
                _mLobbyTypeCombo.SetValue(__instance, mLobbyTypeCombo);

                var vector5 = vector4 + new Vector2(0f, 0.05f);
                vector5 += new Vector2(0f, 0.03f);
                var mMaxPlayers = (Sync.IsServer ? MyMultiplayerLobby.MAX_PLAYERS : 16);
                _mMaxPlayers.SetValue(__instance, mMaxPlayers);
                var mMaxPlayersSlider = new MyGuiControlSlider(vector5, 2f, Math.Max(mMaxPlayers, 3),
                    0.177f, Sync.IsServer ? MySession.Static.MaxPlayers : MyMultiplayer.Static.MemberLimit, null, null,
                    1, 0.8f, 0f, "White", null, MyGuiControlSliderStyleEnum.Default,
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, intValue: true);
                _mMaxPlayersSlider.SetValue(__instance, mMaxPlayersSlider);


                var mInviteButton = new MyGuiControlButton(new Vector2(startX, 0.25000026f),
                    MyGuiControlButtonStyleEnum.Default, null, null,
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, null,
                    MyTexts.Get(MyCommonTexts.ScreenPlayers_Invite));
                mInviteButton.ButtonClicked += delegate(MyGuiControlButton obj)
                {
                    _mInviteButtonButtonClicked.Invoke(__instance, new object[] { obj });
                };
                __instance.Controls.Add(mInviteButton);
                _mInviteButton.SetValue(__instance, mInviteButton);

                var vector6 = new Vector2(-startX - 0.034f, startY + 0.033f);
                var size = new Vector2(0.66f, 1.2f);
                var num2 = 18;
                const float num3 = 0f;


                //SeamlessClient.TryShow("D");
                var component = MySession.Static.GetComponent<MySessionComponentMatch>();
                if (component.IsEnabled)
                {
                    var vector7 = __instance.GetPositionAbsolute() + vector6 + new Vector2(0f - size.X, 0f);
                    var mWarfareTimeRemaintingLabel = new MyGuiControlLabel(vector6 - new Vector2(size.X, 0f))
                    {
                        Text = $"{MyTexts.GetString(MySpaceTexts.WarfareCounter_TimeRemaining)}: "
                    };
                    __instance.Controls.Add(mWarfareTimeRemaintingLabel);
                    _mWarfareTimeRemaintingLabel.SetValue(__instance, mWarfareTimeRemaintingLabel);


                    var timeSpan = TimeSpan.FromMinutes(component.RemainingMinutes);
                    var mWarfareTimeRemainingTime = new MyGuiControlLabel(vector6 - new Vector2(size.X, 0f) +
                                                                          new Vector2(
                                                                              mWarfareTimeRemaintingLabel.Size.X, 0f))
                    {
                        Text = timeSpan.ToString(timeSpan.TotalHours >= 1.0 ? "hh\\:mm\\:ss" : "mm\\:ss")
                    };
                    __instance.Controls.Add(mWarfareTimeRemainingTime);
                    _mWarfareTimeRemaintingTime.SetValue(__instance, mWarfareTimeRemaintingLabel);

                    const float num4 = 0.09f;
                    var num5 = size.X / 3f - 2f * num3;
                    var num6 = 0;
                    var allFactions = MySession.Static.Factions.GetAllFactions();
                    foreach (var myFaction in allFactions)
                    {
                        if ((!myFaction.Name.StartsWith("Red") && !myFaction.Name.StartsWith("Green") &&
                             !myFaction.Name.StartsWith("Blue")) || !myFaction.Name.EndsWith("Faction")) continue;
                        __instance.Controls.Add(new MyGuiScreenPlayersWarfareTeamScoreTable(
                            vector7 + new Vector2(num6 * (num5 + num3),
                                mWarfareTimeRemaintingLabel.Size.Y + num3), num5, num4, myFaction.Name,
                            myFaction.FactionIcon.Value.String,
                            MyTexts.GetString(MySpaceTexts.WarfareCounter_EscapePod), myFaction.FactionId,
                            drawOwnBackground: false, drawBorders: true,
                            myFaction.IsMember(MySession.Static.LocalHumanPlayer.Identity.IdentityId)));
                        num6++;
                    }

                    vector6.Y += mWarfareTimeRemaintingLabel.Size.Y + num4 + num3 * 2f;
                    num2 -= 3;
                }
                //SeamlessClient.TryShow("E");

                var mPlayersTable = new MyGuiControlTable
                {
                    Position = vector6,
                    Size = size,
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                    ColumnsCount = 7
                };

                _mPlayersTable.SetValue(__instance, mPlayersTable);

                //SeamlessClient.TryShow("F");

                mPlayersTable.GamepadHelpTextId = MySpaceTexts.PlayersScreen_Help_PlayersList;
                mPlayersTable.VisibleRowsCount = num2;
                const float playerName = 0.2f;
                const float rank = 0.1f;
                const float ping = 0.08f;
                const float muted = 0.1f;
                const float steamIcon = 0.04f;
                const float serverWidth = 0.20f;
                const float factionName = 1f - playerName - rank - muted - ping - steamIcon - serverWidth;

                mPlayersTable.SetCustomColumnWidths(new[]
                {
                    steamIcon,
                    playerName,
                    factionName,
                    rank,
                    ping,
                    muted,
                    serverWidth
                });

                //SeamlessClient.TryShow("G");

                mPlayersTable.SetColumnComparison(1,
                    (a, b) => a.Text.CompareToIgnoreCase(b.Text));
                mPlayersTable.SetColumnName(1, MyTexts.Get(MyCommonTexts.ScreenPlayers_PlayerName));
                mPlayersTable.SetColumnComparison(2,
                    (a, b) => a.Text.CompareToIgnoreCase(b.Text));
                mPlayersTable.SetColumnName(2, MyTexts.Get(MyCommonTexts.ScreenPlayers_FactionName));
                mPlayersTable.SetColumnName(5,
                    new StringBuilder(MyTexts.GetString(MyCommonTexts.ScreenPlayers_Muted)));
                mPlayersTable.SetColumnComparison(3, GameAdminCompare);
                mPlayersTable.SetColumnName(3, MyTexts.Get(MyCommonTexts.ScreenPlayers_Rank));
                mPlayersTable.SetColumnComparison(4, GamePingCompare);
                mPlayersTable.SetColumnName(4, MyTexts.Get(MyCommonTexts.ScreenPlayers_Ping));


                var colName = new StringBuilder("Server");
                mPlayersTable.SetColumnName(6, colName);
                mPlayersTable.SetColumnComparison(6, (a, b) => a.Text.CompareToIgnoreCase(b.Text));

                //SeamlessClient.TryShow("H");


                //m_PlayersTable_ItemSelected
                mPlayersTable.ItemSelected += delegate(MyGuiControlTable i, MyGuiControlTable.EventArgs x)
                {
                    _mPlayersTableItemSelected.Invoke(__instance, new object[] { i, x });
                };
                mPlayersTable.UpdateTableSortHelpText();
                __instance.Controls.Add(mPlayersTable);


                var thisServerName = "thisServer";
                TotalPlayerCount = 0;
                foreach (var server in AllServers)
                {
                    var servername = server.ServerName;
                    if (server.ServerID == CurrentServer)
                    {
                        thisServerName = servername;
                        _currentServerName = servername;
                        continue;
                    }

                    foreach (var player in server.Players)
                    {
                        AddPlayer(__instance, player.SteamID, servername, player.PlayerName, player.IdentityID);
                        TotalPlayerCount++;
                    }
                }


                _currentPlayerCount = 0;
                foreach (var onlinePlayer in Sync.Players.GetOnlinePlayers())
                {
                    if (onlinePlayer.Id.SerialId != 0)
                    {
                        continue;
                    }

                    _currentPlayerCount++;
                    TotalPlayerCount++;
                    AddPlayer(__instance, onlinePlayer.Id.SteamId, thisServerName);
                }

                //SeamlessClient.TryShow("I");

                var mLastSelected = (ulong)_mLastSelected.GetValue(__instance);
                if (mLastSelected != 0L)
                {
                    var row2 = mPlayersTable.Find(r => (ulong)r.UserData == mLastSelected);
                    if (row2 != null) mPlayersTable.SelectedRow = row2;
                }

                _mUpdateButtonsEnabledState.Invoke(__instance, null);
                //UpdateButtonsEnabledState();

                _mUpdateCaption.Invoke(__instance, null);

                var minSizeGui = MyGuiControlButton.GetVisualStyle(MyGuiControlButtonStyleEnum.Default)
                    .NormalTexture.MinSizeGui;
                var myGuiControlLabel =
                    new MyGuiControlLabel(new Vector2(start.X, start.Y + minSizeGui.Y / 2f))
                    {
                        Name = MyGuiScreenBase.GAMEPAD_HELP_LABEL_NAME
                    };
                __instance.Controls.Add(myGuiControlLabel);
                __instance.GamepadHelpTextId = MySpaceTexts.PlayersScreen_Help_Screen;
                __instance.FocusedControl = mPlayersTable;

                //SeamlessClient.TryShow("J");
            }
            catch (Exception ex)
            {
                SeamlessClient.TryShow(ex.ToString());
            }

            return false;
        }

        private static bool AddPlayer(MyGuiScreenPlayers screen, ulong userId, string server,
            string playerName = null, long playerId = 0)
        {
            var table = (MyGuiControlTable)_mPlayersTable.GetValue(screen);
            var pings = (Dictionary<ulong, short>)_mPings.GetValue(screen);

            if (playerName == null)
                playerName = MyMultiplayer.Static.GetMemberName(userId);

            if (string.IsNullOrEmpty(playerName)) return false;

            var row = new MyGuiControlTable.Row(userId);
            var memberServiceName = MyMultiplayer.Static.GetMemberServiceName(userId);
            var text = new StringBuilder();

            MyGuiHighlightTexture? icon = new MyGuiHighlightTexture
            {
                Normal = $"Textures\\GUI\\Icons\\Services\\{memberServiceName}.png",
                Highlight = $"Textures\\GUI\\Icons\\Services\\{memberServiceName}.png",
                SizePx = new Vector2(24f, 24f)
            };
            row.AddCell(new MyGuiControlTable.Cell(text, null, memberServiceName, Color.White, icon,
                MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));
            row.AddCell(new MyGuiControlTable.Cell(new StringBuilder(playerName), playerName));

            if (playerId == 0)
                playerId = Sync.Players.TryGetIdentityId(userId);

            var playerFaction = MySession.Static.Factions.GetPlayerFaction(playerId);
            var text2 = "";
            var stringBuilder = new StringBuilder();
            if (playerFaction != null)
            {
                text2 = $"{playerFaction.Name} | {playerName}";
                foreach (var member in playerFaction.Members)
                {
                    if ((!member.Value.IsLeader && !member.Value.IsFounder) ||
                        !MySession.Static.Players.TryGetPlayerId(member.Value.PlayerId, out var result) ||
                        !MySession.Static.Players.TryGetPlayerById(result, out var player)) continue;
                    text2 = $"{text2} | {player.DisplayName}";
                    break;
                }

                stringBuilder.Append(MyStatControlText.SubstituteTexts(playerFaction.Name));
                if (playerFaction.IsLeader(playerId))
                    stringBuilder.Append(" (").Append(MyTexts.Get(MyCommonTexts.Leader)).Append(")");

                if (!string.IsNullOrEmpty(playerFaction.Tag)) stringBuilder.Insert(0, $"[{playerFaction.Tag}] ");
            }

            row.AddCell(new MyGuiControlTable.Cell(stringBuilder, null, text2));
            var stringBuilder2 = new StringBuilder();
            var userPromoteLevel = MySession.Static.GetUserPromoteLevel(userId);
            for (var i = 0; i < (int)userPromoteLevel; i++) stringBuilder2.Append("*");

            row.AddCell(new MyGuiControlTable.Cell(stringBuilder2));
            row.AddCell(pings.ContainsKey(userId)
                ? new MyGuiControlTable.Cell(new StringBuilder(pings[userId].ToString()))
                : new MyGuiControlTable.Cell(new StringBuilder("----")));

            var cell = new MyGuiControlTable.Cell(new StringBuilder(""));
            row.AddCell(cell);
            if (userId != Sync.MyId)
            {
                var myGuiControlButton = new MyGuiControlButton
                {
                    CustomStyle = MButtonSizeStyleUnMuted,
                    Size = new Vector2(0.03f, 0.04f),
                    CueEnum = GuiSounds.None
                };

                void BtnClicked(MyGuiControlButton b) => _mOnToggleMutePressed.Invoke(screen, new object[] { b });

                myGuiControlButton.ButtonClicked += BtnClicked;
                myGuiControlButton.UserData = userId;
                cell.Control = myGuiControlButton;
                table.Controls.Add(myGuiControlButton);
                _mRefreshMuteIcons.Invoke(screen, null);
                //RefreshMuteIcons();
            }

            table.Add(row);
            _mUpdateCaption.Invoke(screen, null);

            row.AddCell(new MyGuiControlTable.Cell(new StringBuilder(server), "Server Name"));

            return false;
        }

        // ReSharper disable once InconsistentNaming
        private static bool UpdateCaption(MyGuiScreenPlayers __instance)
        {
            if (MyMultiplayer.Static != null && MyMultiplayer.Static.IsLobby)
                return true;

            var mMCaption = (MyGuiControlLabel)_mCaption.GetValue(__instance);
            // var mmPlayersTable = (MyGuiControlTable)m_playersTable.GetValue(__instance);


            //string s = $"{MyTexts.Get(MyCommonTexts.ScreenCaptionServerName).ToString()} - SectorPlayers: ({ mm_playersTable.RowsCount} / {MySession.Static.MaxPlayers}) TotalPlayers: ( {5} / 200 )";

            mMCaption.Text =
                $"Server: {_currentServerName}  -  Instance Players ({_currentPlayerCount} / {MySession.Static.MaxPlayers})    TotalPlayers: ( {TotalPlayerCount} )";


            return false;
        }

        private static int GamePingCompare(MyGuiControlTable.Cell a, MyGuiControlTable.Cell b)
        {
            if (!int.TryParse(a.Text.ToString(), out var result))
            {
                result = -1;
            }

            if (!int.TryParse(b.Text.ToString(), out var result2))
            {
                result2 = -1;
            }

            return result.CompareTo(result2);
        }

        private static int GameAdminCompare(MyGuiControlTable.Cell a, MyGuiControlTable.Cell b)
        {
            var steamId = (ulong)a.Row.UserData;
            var steamId2 = (ulong)b.Row.UserData;
            var userPromoteLevel = (int)MySession.Static.GetUserPromoteLevel(steamId);
            var userPromoteLevel2 = (int)MySession.Static.GetUserPromoteLevel(steamId2);
            return userPromoteLevel.CompareTo(userPromoteLevel2);
        }

        private static readonly MyGuiControlButton.StyleDefinition MButtonSizeStyleUnMuted =
            new MyGuiControlButton.StyleDefinition
            {
                NormalFont = "White",
                HighlightFont = "White",
                NormalTexture = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_NORMAL,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_OUTLINELESS_HIGHLIGHT
            };

        static OnlinePlayers() => TotalPlayerCount = 0;

        private static MethodInfo GetPatchMethod(string v) =>
            typeof(OnlinePlayers).GetMethod(v,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    }
}