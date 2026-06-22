using EasyTextEffects.Editor.MyBoxCopy.Extensions;

using GameNetcodeStuff;

using HarmonyLib;

using Steamworks;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using TMPro;

using UnityEngine;

namespace Patches
{
    [HarmonyWrapSafe]
    public static class FixPlayerName_Patches
    {
        private static Coroutine? _fixCoroutine;
        private static bool _isRegistered;
        private static readonly WaitForSeconds _workInterval = new WaitForSeconds(Plugin.FixPlayerName_WorkInterval.Value);

        [HarmonyPatch]
        [HarmonyWrapSafe]
        public static class UpdateMapTargetPostfixPatch
        {
            static bool Prepare()
            {
                return Plugin.FixPlayerName_Enable.Value;
            }

            static MethodBase TargetMethod()
            {
                var stateMachineType = AccessTools.Inner(typeof(ManualCameraRenderer), "<updateMapTarget>d__74");
                return AccessTools.Method(stateMachineType, "MoveNext");
            }

            private static ManualCameraRenderer manualCamera = null;

            [HarmonyPostfix]
            public static void Postfix(object __instance, ref bool __result)
            {
                if (!__result)
                {
                    if (manualCamera == null)
                    {
                        var thisField = AccessTools.Field(__instance.GetType(), "<>4__this");
                        if (thisField == null)
                        {

                            return;
                        }
                        manualCamera = thisField.GetValue(__instance) as ManualCameraRenderer;
                    }
                    if (manualCamera != null && manualCamera.targetedPlayer != null)
                    {
                        if (StartOfRound.Instance.mapScreenPlayerName.text != manualCamera.targetedPlayer.playerUsername)
                        {
                            StartOfRound.Instance.mapScreenPlayerName.text = manualCamera.targetedPlayer.playerUsername;
                        }
                    }
                }
            }
        }


        [HarmonyPatch(typeof(QuickMenuManager), "OpenQuickMenu")]
        [HarmonyPostfix]
        public static void OpenQuickMenu(QuickMenuManager __instance)
        {
            if (!__instance.NonHostPlayerSlotsEnabled())
            {
                return;
            }
            PlayerName_Refresh();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), "SteamMatchmaking_OnLobbyMemberJoined")]
        public static void SteamMatchmaking_OnLobbyMemberJoinedPostfix()
        {
            PlayerName_Refresh();
        }

        private static bool PlayerName_Refresh()
        {
            if (GameNetworkManager.Instance == null || StartOfRound.Instance == null)
            {
                return false;
            }
            if (GameNetworkManager.Instance.disableSteam)
            {
                return false;
            }
            if (!Plugin.FixPlayerName_Enable.Value)
            {
                return false;
            }
            GameNetworkManager.Instance.StartCoroutine(RefreshPlayerNames());
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "SendNewPlayerValuesClientRpc")]

        public static void SendNewPlayerValuesClientRpc()
        {
            PlayerName_Refresh();
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), "OnPlayerConnectedClientRpc")]

        public static void OnPlayerConnectedClientRpc()
        {
            PlayerName_Refresh();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        public static void ConnectClientToPlayerObjectPostfix(PlayerControllerB __instance)
        {
            if (GameNetworkManager.Instance.disableSteam) return;
            if (!Plugin.FixPlayerName_Enable.Value)
            {
                return;
            }
            StopFixCoroutine(__instance);
            _fixCoroutine = __instance.StartCoroutine(FixPlayerNamesRoutine());
        }



        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), "OnLocalDisconnect")]
        public static void OnLocalDisconnect()
        {
            StopFixCoroutine();
        }

        private static void StopFixCoroutine(MonoBehaviour behaviour = null)
        {
            if (_fixCoroutine == null) return;

            var target = behaviour ?? StartOfRound.Instance?.localPlayerController;
            target?.StopCoroutine(_fixCoroutine);

            if (_isRegistered)
            {
                SteamFriends.OnPersonaStateChange -= OnPersonaStateChange;
                _isRegistered = false;
            }

            _fixCoroutine = null;
        }

        private static IEnumerator FixPlayerNamesRoutine()
        {
            if (!_isRegistered)
            {
                SteamFriends.OnPersonaStateChange += OnPersonaStateChange;
                _isRegistered = true;
            }

            while (StartOfRound.Instance?.localPlayerController != null)
            {
                yield return _workInterval;
                yield return RefreshPlayerNames();
            }

            StopFixCoroutine();
        }

        private static IEnumerator RefreshPlayerNames()
        {
            var instance = StartOfRound.Instance;
            if (instance?.allPlayerScripts == null)
                yield break;
            var quickMenu = UnityEngine.Object.FindObjectOfType<QuickMenuManager>();
            if (quickMenu == null) yield break;

            var players = instance.allPlayerScripts;

            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                var slot = quickMenu.playerListSlots[i];
                if (slot == null || slot.slotContainer == null)
                    continue;
                var container = slot.slotContainer;
                if (!player.isPlayerControlled && !player.isPlayerDead)
                {
                    if (container.activeSelf)
                        container.SetActive(false);
                    continue;
                }
                if (!container.activeSelf)
                    container.SetActive(true);
                yield return UpdatePlayerName(player, slot);
            }
        }

        private static IEnumerator UpdatePlayerName(PlayerControllerB player, PlayerListSlot slot)
        {
            try
            {
                var friend = new Friend(player.playerSteamId);
                string steamName = friend.Name;
                if (steamName == "[unknown]")
                {
                    bool requested = SteamFriends.RequestUserInformation((SteamId)player.playerSteamId, true);
                    yield break;
                }

                if (player.playerUsername != steamName)
                {
                    player.playerUsername = steamName;
                    player.usernameBillboardText.text = steamName;
                }

                if (slot.usernameHeader.text != steamName)
                {
                    slot.usernameHeader.text = steamName;
                    slot.playerSteamId = player.playerSteamId;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[FixPlayerName] UpdatePlayerName:{ex}");
            }
        }

        private static void OnPersonaStateChange(Friend friend)
        {
            try
            {
                var instance = StartOfRound.Instance;
                if (instance?.allPlayerScripts == null) return;
                var quickMenu = UnityEngine.Object.FindObjectOfType<QuickMenuManager>();
                if (quickMenu == null) return;

                var players = instance.allPlayerScripts;

                for (int i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    if (!player.isPlayerControlled && !player.isPlayerDead)
                    {
                        continue;
                    }
                    if (player.playerSteamId == friend.Id.Value)
                    {
                        if (friend.Name != player.playerUsername || player.usernameBillboardText.text != friend.Name)
                        {
                            player.playerUsername = friend.Name;
                            player.usernameBillboardText.text = friend.Name;
                        }
                        var slot = quickMenu.playerListSlots[i];
                        slot.usernameHeader.text = friend.Name;
                        if (!slot.slotContainer.activeSelf)
                            slot.slotContainer.SetActive(true);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[FixPlayerName] OnPersonaStateChange:{ex}");
            }
        }
    }
}
