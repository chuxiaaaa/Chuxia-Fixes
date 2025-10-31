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
        private static readonly WaitForSeconds _workInterval = new WaitForSeconds(Plugin.FixPlayerName_WorkInterval.Value); // 可配置的间隔

        [HarmonyPatch]
        [HarmonyWrapSafe]
        public static class UpdateMapTargetPostfixPatch
        {
            static MethodBase TargetMethod()
            {
                // 获取状态机类型 ManualCameraRenderer+<updateMapTarget>d__71
                var stateMachineType = AccessTools.Inner(typeof(ManualCameraRenderer), "<updateMapTarget>d__71");
                return AccessTools.Method(stateMachineType, "MoveNext");
            }

            [HarmonyPostfix]
            public static void Postfix(object __instance, ref bool __result)
            {
                // __result 为 true 表示协程还没结束（有 yield return），false 表示刚刚执行完最后一步
                if (!__result)
                {
                    // 协程刚结束，此处执行你想要的操作
                    var thisField = AccessTools.Field(__instance.GetType(), "<>4__this");
                    var instance = thisField.GetValue(__instance) as ManualCameraRenderer;
                    if (instance != null && instance.targetedPlayer != null)
                    {
                        if (StartOfRound.Instance.mapScreenPlayerName.text != instance.targetedPlayer.playerUsername)
                        {
                            Plugin.Log.LogInfo($"[FixPlayerName] ManualCameraRenderer.updateMapTarget -> {instance.targetedPlayer.playerUsername}|{instance.targetedPlayer.playerSteamId}");
                            StartOfRound.Instance.mapScreenPlayerName.text = instance.targetedPlayer.playerUsername;
                        }
                    }
                }
            }
        }

      
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), "SteamMatchmaking_OnLobbyMemberJoined")]
        public static void SteamMatchmaking_OnLobbyMemberJoinedPostfix()
        {
            if (GameNetworkManager.Instance == null || StartOfRound.Instance == null)
            {
                return;
            }
            if (GameNetworkManager.Instance.disableSteam || !StartOfRound.Instance.shipHasLanded)
            {
                return;
            }
            if (!Plugin.FixPlayerName_Enable.Value)
            {
                return;
            }
            GameNetworkManager.Instance.StartCoroutine(RefreshPlayerNames());
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "SendNewPlayerValuesClientRpc")]

        public static void SendNewPlayerValuesClientRpc()
        {
            if (GameNetworkManager.Instance == null || StartOfRound.Instance == null)
            {
                return;
            }
            if (GameNetworkManager.Instance.disableSteam)
            {
                return;
            }
            if (!Plugin.FixPlayerName_Enable.Value)
            {
                return;
            }
            GameNetworkManager.Instance.StartCoroutine(RefreshPlayerNames());
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
            // 确保只有一个协程在运行
            StopFixCoroutine(__instance);
            _fixCoroutine = __instance.StartCoroutine(FixPlayerNamesRoutine());
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        public static void OnDisconnect()
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
            // 延迟注册事件处理器，避免重复注册
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

            // 清理
            StopFixCoroutine();
        }

        private static IEnumerator RefreshPlayerNames()
        {
            var instance = StartOfRound.Instance;
            if (instance?.allPlayerScripts == null || instance.connectedPlayersAmount == 0)
                yield break;

            var quickMenu = UnityEngine.Object.FindObjectOfType<QuickMenuManager>();
            if (quickMenu == null) yield break;

            var players = instance.allPlayerScripts;

            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                var slot = quickMenu.playerListSlots[i];

                if (player.playerSteamId == 0)
                {
                    // 隐藏空槽位
                    if (slot.slotContainer.activeSelf)
                        slot.slotContainer.SetActive(false);
                    continue;
                }

                // 确保槽位可见
                if (!slot.slotContainer.activeSelf)
                    slot.slotContainer.SetActive(true);

                // 更新玩家名称
                yield return UpdatePlayerName(player, slot);
            }
        }

        private static IEnumerator UpdatePlayerName(PlayerControllerB player, PlayerListSlot slot)
        {
            try
            {
                var friend = new Friend(player.playerSteamId);
                string steamName = friend.Name;

                // 处理未知名称
                if (steamName == "[unknown]")
                {
                    bool requested = SteamFriends.RequestUserInformation((SteamId)player.playerSteamId, true);
                    Plugin.Log.LogInfo($"Requested user info for {player.playerSteamId}: {requested}");
                    yield break; // 等待回调更新
                }

                // 只在名称实际变化时更新
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
                Plugin.Log.LogError($"Error updating player name: {ex}");
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
                    if (player.playerSteamId == friend.Id.Value)
                    {
                        Plugin.Log.LogInfo($"Persona state changed: {friend.Name}");

                        // 立即更新所有相关UI
                        player.playerUsername = friend.Name;
                        player.usernameBillboardText.text = friend.Name;

                        var slot = quickMenu.playerListSlots[i];
                        slot.usernameHeader.text = friend.Name;
                        slot.playerSteamId = friend.Id.Value;
                        if (!slot.slotContainer.activeSelf)
                            slot.slotContainer.SetActive(true);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error in persona state change handler: {ex}");
            }
        }
    }
}
