using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.TORMapOptions;
using TheOtherRoles.Objects;
using System;
using TheOtherRoles.Utilities;
using UnityEngine;
using Reactor.Utilities;
using AmongUs.QuickChat;
using TheOtherRoles.Modules;
using TheOtherRoles.MetaContext;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using TMPro;

namespace TheOtherRoles.Patches
{
    [HarmonyPatch]
    class MeetingHudPatch {
        static bool[] selections;
        static SpriteRenderer[] renderers;
        private static NetworkedPlayerInfo target = null;
        private const float scale = 0.65f;
        private static TMPro.TextMeshPro meetingExtraButtonText;
        private static PassiveButton[] swapperButtonList;
        private static TMPro.TextMeshPro meetingExtraButtonLabel;
        private static PlayerVoteArea swapped1 = null;
        private static PlayerVoteArea swapped2 = null;
        static TMPro.TextMeshPro[] meetingInfoText = new TMPro.TextMeshPro[4];
        static int meetingTextIndex = 0;

        static private float[] VotingAreaScale = { 1f, 0.95f, 0.76f };
        static private (int x, int y)[] VotingAreaSize = { (3, 5), (3, 6), (4, 6) };
        static private Vector3[] VotingAreaOffset = { Vector3.zero, new(0.1f, 0.145f, 0f), new(-0.355f, 0f, 0f) };
        static private (float x, float y)[] VotingAreaMultiplier = { (1f, 1f), (1f, 0.89f), (0.974f, 1f) };
        static private int GetVotingAreaType(int players) => players <= 15 ? 0 : players <= 18 ? 1 : 2;
        private static Vector3 ToVoteAreaPos(int index, int arrangeType)
        {
            int x = index % VotingAreaSize[arrangeType].x;
            int y = index / VotingAreaSize[arrangeType].x;
            return
                MeetingHud.Instance.VoteOrigin + VotingAreaOffset[arrangeType] +
                new Vector3(
                    MeetingHud.Instance.VoteButtonOffsets.x * VotingAreaScale[arrangeType] * VotingAreaMultiplier[arrangeType].x * (float)x,
                    MeetingHud.Instance.VoteButtonOffsets.y * VotingAreaScale[arrangeType] * VotingAreaMultiplier[arrangeType].y * (float)y,
                    -0.9f - (float)y * 0.01f);
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
        class MeetingCalculateVotesPatch {
            private static Dictionary<byte, int> CalculateVotes(MeetingHud __instance) 
            {
                var dictionary = new Dictionary<byte, int>();
                for (int i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    if (playerVoteArea.VotedFor is not 252 and not 255 and not 254) {
                        PlayerControl player = Helpers.playerById((byte)playerVoteArea.TargetPlayerId);
                        if (player == null || player.Data == null || player.Data.IsDead || player.Data.Disconnected) continue;
                        if (player == MimicA.mimicA && MimicK.mimicK != null && MimicK.hasOneVote && !MimicK.mimicK.Data.IsDead) continue;
                        if (player == BomberB.bomberB && BomberA.bomberA != null && BomberA.hasOneVote && !BomberA.bomberA.Data.IsDead) continue;

                        var amMayorEnabled = Mayor.mayor != null && Mayor.mayor.PlayerId == playerVoteArea.TargetPlayerId;
                        if (amMayorEnabled)
                            Mayor.unlockAch(playerVoteArea.VotedFor);
                        if (Detective.detective != null && Detective.detective.PlayerId == playerVoteArea.TargetPlayerId)
                            Detective.unlockAch(playerVoteArea.VotedFor);
                        if (Jester.jester != null && Jester.jester.PlayerId != playerVoteArea.TargetPlayerId && playerVoteArea.VotedFor == Jester.jester.PlayerId)
                            Jester.unlockAch();

                        int additionalVotes = (Mayor.mayor != null && Mayor.mayor.PlayerId == playerVoteArea.TargetPlayerId) ? Mayor.numVotes : 1; // Mayor vote
                        if (dictionary.TryGetValue(playerVoteArea.VotedFor, out int currentVotes))
                            dictionary[playerVoteArea.VotedFor] = currentVotes + additionalVotes;
                        else
                            dictionary[playerVoteArea.VotedFor] = additionalVotes;
                    }
                }

                for (int i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    if (playerVoteArea.VotedFor is not 252 and not 255 and not 254)
                    {
                        PlayerControl player = Helpers.playerById((byte)playerVoteArea.TargetPlayerId);
                        if (player == null || player.Data == null || player.Data.IsDead || player.Data.Disconnected) continue;
                        if (player == MimicA.mimicA && MimicK.mimicK != null && MimicK.hasOneVote && !MimicK.mimicK.Data.IsDead) continue;
                        if (player == BomberB.bomberB && BomberA.bomberA != null && BomberA.hasOneVote && !BomberA.bomberA.Data.IsDead) continue;

                        var amMayorEnabled = EvilMayor.evilmayor != null && EvilMayor.evilmayor.PlayerId == playerVoteArea.TargetPlayerId;
                        if (amMayorEnabled)
                            EvilMayor.unlockAch(playerVoteArea.VotedFor);
                        if (Detective.detective != null && Detective.detective.PlayerId == playerVoteArea.TargetPlayerId)
                            Detective.unlockAch(playerVoteArea.VotedFor);
                        if (Jester.jester != null && Jester.jester.PlayerId != playerVoteArea.TargetPlayerId && playerVoteArea.VotedFor == Jester.jester.PlayerId)
                            Jester.unlockAch();

                        int additionalVotes = (EvilMayor.evilmayor != null && EvilMayor.evilmayor.PlayerId == playerVoteArea.TargetPlayerId) ? EvilMayor.numVotes : 1; // Mayor vote
                        if (dictionary.TryGetValue(playerVoteArea.VotedFor, out int currentVotes))
                            dictionary[playerVoteArea.VotedFor] = currentVotes + additionalVotes;
                        else
                            dictionary[playerVoteArea.VotedFor] = additionalVotes;
                    }
                }

                if (Swapper.swapper != null && !Swapper.swapper.Data.IsDead)
                {
                    swapped1 = null;
                    swapped2 = null;
                    foreach (PlayerVoteArea playerVoteArea in __instance.playerStates) {
                        if (playerVoteArea.TargetPlayerId == Swapper.playerId1) swapped1 = playerVoteArea;
                        if (playerVoteArea.TargetPlayerId == Swapper.playerId2) swapped2 = playerVoteArea;
                    }

                    if (swapped1 != null && swapped2 != null) {
                        TheOtherRolesPlugin.Logger.LogMessage("Swapping votes");
                        if (!dictionary.ContainsKey(swapped1.TargetPlayerId)) dictionary[swapped1.TargetPlayerId] = 0;
                        if (!dictionary.ContainsKey(swapped2.TargetPlayerId)) dictionary[swapped2.TargetPlayerId] = 0;
                        (dictionary[swapped2.TargetPlayerId], dictionary[swapped1.TargetPlayerId]) = (dictionary[swapped1.TargetPlayerId], dictionary[swapped2.TargetPlayerId]);
                    }
                }

                return dictionary;
            }


            static bool Prefix(MeetingHud __instance) {
                if (__instance.playerStates.All((PlayerVoteArea ps) => ps.AmDead || ps.DidVote)) {
                    // If skipping is disabled, replace skipps/no-votes with self vote
                    if (target == null && blockSkippingInEmergencyMeetings && noVoteIsSelfVote) {
                        foreach (PlayerVoteArea playerVoteArea in __instance.playerStates) {
                            if (playerVoteArea.VotedFor == byte.MaxValue - 1) playerVoteArea.VotedFor = playerVoteArea.TargetPlayerId; // TargetPlayerId
                        }
                    }

			        Dictionary<byte, int> self = CalculateVotes(__instance);
                    bool tie;
			        KeyValuePair<byte, int> max = self.MaxPair(out tie);
                    NetworkedPlayerInfo exiled = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(v => !tie && v.PlayerId == max.Key && !v.IsDead);

                    // TieBreaker 
                    List<NetworkedPlayerInfo> potentialExiled = new();
                    bool skipIsTie = false;
                    if (self.Count > 0) {
                        Tiebreaker.isTiebreak = false;
                        int maxVoteValue = self.Values.Max();
                        PlayerVoteArea tb = null;
                        if (Tiebreaker.tiebreaker != null)
                            tb = __instance.playerStates.ToArray().FirstOrDefault(x => x.TargetPlayerId == Tiebreaker.tiebreaker.PlayerId);
                        bool isTiebreakerSkip = tb == null || tb.VotedFor == 253;
                        if (tb != null && tb.AmDead) isTiebreakerSkip = true;

                        foreach (KeyValuePair<byte, int> pair in self) {
                            if (pair.Value != maxVoteValue || isTiebreakerSkip) continue;
                            if (pair.Key != 253)
                                potentialExiled.Add(GameData.Instance.AllPlayers.ToArray().FirstOrDefault(x => x.PlayerId == pair.Key));
                            else 
                                skipIsTie = true;
                        }
                    }

                    byte forceTargetPlayerId = Yasuna.yasuna != null && !Yasuna.yasuna.Data.IsDead && Yasuna.specialVoteTargetPlayerId != byte.MaxValue ? Yasuna.specialVoteTargetPlayerId : byte.MaxValue;
                    if (forceTargetPlayerId != byte.MaxValue)
                        tie = false;

                    MeetingHud.VoterState[] array = new MeetingHud.VoterState[__instance.playerStates.Length];
                    for (int i = 0; i < __instance.playerStates.Length; i++)
                    {
                        PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                        if (forceTargetPlayerId != byte.MaxValue)
                            playerVoteArea.VotedFor = forceTargetPlayerId;

                        array[i] = new MeetingHud.VoterState {
                            VoterId = playerVoteArea.TargetPlayerId,
                            VotedForId = playerVoteArea.VotedFor
                        };

                        if (Tiebreaker.tiebreaker == null || playerVoteArea.TargetPlayerId != Tiebreaker.tiebreaker.PlayerId) continue;

                        byte tiebreakerVote = playerVoteArea.VotedFor;
                        if (swapped1 != null && swapped2 != null) {
                            if (tiebreakerVote == swapped1.TargetPlayerId) tiebreakerVote = swapped2.TargetPlayerId;
                            else if (tiebreakerVote == swapped2.TargetPlayerId) tiebreakerVote = swapped1.TargetPlayerId;
                        }

                        if (potentialExiled.FindAll(x => x != null && x.PlayerId == tiebreakerVote).Count > 0 && (potentialExiled.Count > 1 || skipIsTie)) {
                            exiled = potentialExiled.ToArray().FirstOrDefault(v => v.PlayerId == tiebreakerVote);
                            tie = false;

                            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetTiebreak, Hazel.SendOption.Reliable, -1);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                            RPCProcedure.setTiebreak();
                        }
                    }

                    if (forceTargetPlayerId != byte.MaxValue)
                        exiled = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(v => v.PlayerId == forceTargetPlayerId && !v.IsDead);

                    // RPCVotingComplete
                    __instance.RpcVotingComplete(array, exiled, tie);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.BloopAVoteIcon))]
        class MeetingHudBloopAVoteIconPatch {
            public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)]NetworkedPlayerInfo voterPlayer, [HarmonyArgument(1)]int index, [HarmonyArgument(2)]Transform parent) {
                var spriteRenderer = UnityEngine.Object.Instantiate<SpriteRenderer>(__instance.PlayerVotePrefab);
                bool localIsWatcher = PlayerControl.LocalPlayer == Watcher.nicewatcher || PlayerControl.LocalPlayer == Watcher.evilwatcher;
                var showVoteColors = !GameManager.Instance.LogicOptions.GetAnonymousVotes() ||
                                     (PlayerControl.LocalPlayer.Data.IsDead && TORMapOptions.ghostsSeeVotes) || localIsWatcher;
                if (showVoteColors)
                {
                    PlayerMaterial.SetColors(localIsWatcher && Watcher.canSeeYasunaVotes && Yasuna.yasuna != null && Yasuna.specialVoteTargetPlayerId != byte.MaxValue ? 
                        Yasuna.yasuna.Data.DefaultOutfit.ColorId : voterPlayer.DefaultOutfit.ColorId, spriteRenderer);
                }
                else
                {
                    PlayerMaterial.SetColors(Palette.DisabledGrey, spriteRenderer);
                }

                var transform = spriteRenderer.transform;
                transform.SetParent(parent);
                transform.localScale = Vector3.zero;
                var component = parent.GetComponent<PlayerVoteArea>();
                if (component != null)
                {
                    spriteRenderer.material.SetInt(PlayerMaterial.MaskLayer, component.MaskLayer);
                }

                __instance.StartCoroutine(Effects.Bloop(index * 0.3f, transform));
                parent.GetComponent<VoteSpreader>().AddVote(spriteRenderer);
                return false;
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
        class OnDestroyPatch
        {
            public static void Postfix()
            {
                Modules.AntiCheat.MeetingTimes = 0;
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.PopulateResults))]
        class MeetingHudPopulateVotesPatch {
            
            static bool Prefix(MeetingHud __instance, Il2CppStructArray<MeetingHud.VoterState> states) {
                // Swapper swap

                PlayerVoteArea swapped1 = null;
                PlayerVoteArea swapped2 = null;
                foreach (PlayerVoteArea playerVoteArea in __instance.playerStates) {
                    if (playerVoteArea.TargetPlayerId == Swapper.playerId1) swapped1 = playerVoteArea;
                    if (playerVoteArea.TargetPlayerId == Swapper.playerId2) swapped2 = playerVoteArea;
                }
                bool doSwap = swapped1 != null && swapped2 != null && Swapper.swapper != null && !Swapper.swapper.Data.IsDead && Yasuna.specialVoteTargetPlayerId != swapped1.TargetPlayerId && Yasuna.specialVoteTargetPlayerId != swapped2.TargetPlayerId;

                if (doSwap) {
                    __instance.StartCoroutine(Effects.Slide3D(swapped1.transform, swapped1.transform.localPosition, swapped2.transform.localPosition, 1.5f));
                    __instance.StartCoroutine(Effects.Slide3D(swapped2.transform, swapped2.transform.localPosition, swapped1.transform.localPosition, 1.5f));
                }

                if (Yasuna.yasuna != null && Yasuna.specialVoteTargetPlayerId != byte.MaxValue && (Yasuna.specialVoteTargetPlayerId == Swapper.playerId1 ||
                    Yasuna.specialVoteTargetPlayerId == Swapper.playerId2) && PlayerControl.LocalPlayer == Swapper.swapper) Swapper.charges++;

                __instance.TitleText.text = FastDestroyableSingleton<TranslationController>.Instance.GetString(StringNames.MeetingVotingResults, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
                int num = 0;
                for (int i = 0; i < __instance.playerStates.Length; i++) {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    byte targetPlayerId = playerVoteArea.TargetPlayerId;
                    // Swapper change playerVoteArea that gets the votes
                    if (doSwap && playerVoteArea.TargetPlayerId == swapped1.TargetPlayerId) playerVoteArea = swapped2;
                    else if (doSwap && playerVoteArea.TargetPlayerId == swapped2.TargetPlayerId) playerVoteArea = swapped1;

                    playerVoteArea.ClearForResults();
                    int num2 = 0;
                    Dictionary<int, int> votesApplied = new();
                    for (int j = 0; j < states.Length; j++) {
                        MeetingHud.VoterState voterState = states[j];
                        NetworkedPlayerInfo playerById = GameData.Instance.GetPlayerById(voterState.VoterId);
                        if (playerById == null) 
                        {
                            Debug.LogError(string.Format("Couldn't find player info for voter: {0}", voterState.VoterId));
                        }
                        else if (i == 0 && voterState.SkippedVote && !playerById.IsDead) {
                            __instance.BloopAVoteIcon(playerById, num, __instance.SkippedVoting.transform);
                            num++;
                        }
                        else if (voterState.VotedForId == targetPlayerId && !playerById.IsDead) {
                            __instance.BloopAVoteIcon(playerById, num2, playerVoteArea.transform);
                            num2++;
                        }

                        if (!votesApplied.ContainsKey(voterState.VoterId))
                            votesApplied[voterState.VoterId] = 0;

                        votesApplied[voterState.VoterId]++;

                        // Major vote, redo this iteration to place a second vote
                        if (Mayor.mayor != null && voterState.VoterId == (sbyte)Mayor.mayor.PlayerId && votesApplied[voterState.VoterId] < Mayor.numVotes) {
                            j--;    
                        }
                        if (EvilMayor.evilmayor != null && voterState.VoterId == (sbyte)EvilMayor.evilmayor.PlayerId && votesApplied[voterState.VoterId] < EvilMayor.numVotes)
                        {
                            j--;
                        }
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
        class MeetingHudVotingCompletedPatch {
            static void Postfix(MeetingHud __instance, [HarmonyArgument(0)]byte[] states, [HarmonyArgument(1)]NetworkedPlayerInfo exiled, [HarmonyArgument(2)]bool tie)
            {
                // Reset swapper values
                Swapper.playerId1 = Byte.MaxValue;
                Swapper.playerId2 = Byte.MaxValue;

                // Disable meeting info text
                if (meetingInfoText != null)
                    foreach (var text in meetingInfoText)
                        text.gameObject.SetActive(false);

                // Lovers, Lawyer & Pursuer save next to be exiled, because RPC of ending game comes before RPC of exiled
                Lovers.notAckedExiledIsLover = false;
                Pursuer.notAckedExiled = false;
                if (exiled != null) {
                    Lovers.notAckedExiledIsLover = (Lovers.lover1 != null && Lovers.lover1.PlayerId == exiled.PlayerId) || (Lovers.lover2 != null && Lovers.lover2.PlayerId == exiled.PlayerId);
                    
                    // Changed this: The Lawyer doesn't die if the target was ejected
                    Pursuer.notAckedExiled = Pursuer.pursuer != null && Pursuer.pursuer.PlayerId == exiled.PlayerId;  //|| (Lawyer.lawyer != null && Lawyer.target != null && Lawyer.target.PlayerId == exiled.PlayerId && Lawyer.target != Jester.jester); // && !Lawyer.isProsecutor
                }

                // Yasuna
                if (Yasuna.isYasuna(PlayerControl.LocalPlayer.PlayerId) && Yasuna.specialVoteTargetPlayerId == byte.MaxValue)
                {
                    for (int i = 0; i < __instance.playerStates.Length; i++)
                    {
                        PlayerVoteArea voteArea = __instance.playerStates[i];
                        Transform t = voteArea.transform.FindChild("SpecialVoteButton");
                        if (t != null)
                            t.gameObject.SetActive(false);
                    }
                }

                // Mini
                if (!Mini.isGrowingUpInMeeting) Mini.timeOfGrowthStart = Mini.timeOfGrowthStart.Add(DateTime.UtcNow.Subtract(Mini.timeOfMeetingStart)).AddSeconds(10);
            }
        }

        public static void SortVotingArea(MeetingHud __instance, Func<NetworkedPlayerInfo, int> rank, float speed = 1f)
        {
            int length = __instance.playerStates.Length;
            int type = GetVotingAreaType(length);
            __instance.playerStates.Do(p => p.transform.localScale = new(VotingAreaScale[type], VotingAreaScale[type], 1f));

            var ordered = __instance.playerStates.OrderBy(p => p.TargetPlayerId + 32 * rank.Invoke(GameData.Instance.GetPlayerById(p.TargetPlayerId))).ToArray();

            for (int i = 0; i < ordered.Length; i++)
                __instance.StartCoroutine(ordered[i].transform.Smooth(ToVoteAreaPos(i, type), 1.6f / speed).WrapToIl2Cpp());
        }


        static void swapperOnClick(int i, MeetingHud __instance) {
            if (__instance.state == MeetingHud.VoteStates.Results || Swapper.charges <= 0) return;
            if (__instance.playerStates[i].AmDead) return;

            int selectedCount = selections.Where(b => b).Count();
            SpriteRenderer renderer = renderers[i];

            if (selectedCount == 0) {
                renderer.color = Color.yellow;
                selections[i] = true;
            } else if (selectedCount == 1) {
                if (selections[i]) {
                    renderer.color = Color.red;
                    selections[i] = false;
                } else {
                    selections[i] = true;
                    renderer.color = Color.yellow;
                    meetingExtraButtonLabel.text = Helpers.cs(Color.yellow, ModTranslation.getString("swapperConfirmSwap"));
                }
            } else if (selectedCount == 2) {
                if (selections[i]) {
                    renderer.color = Color.red;
                    selections[i] = false;
                    meetingExtraButtonLabel.text = Helpers.cs(Color.red, ModTranslation.getString("swapperConfirmSwap"));
                }
            }
        }

        static void swapperConfirm(MeetingHud __instance) {
            __instance.playerStates[0].Cancel();  // This will stop the underlying buttons of the template from showing up
            if (__instance.state == MeetingHud.VoteStates.Results) return;
            if (selections.Where(b => b).Count() != 2) return;
            if (Swapper.charges <= 0 || Swapper.playerId1 != Byte.MaxValue) return;

            PlayerVoteArea firstPlayer = null;
            PlayerVoteArea secondPlayer = null;
            for (int A = 0; A < selections.Length; A++) {
                if (selections[A]) {
                    if (firstPlayer == null) {
                        firstPlayer = __instance.playerStates[A];
                    } else {
                        secondPlayer = __instance.playerStates[A];
                    }
                    renderers[A].color = Color.green;
                } else if (renderers[A] != null) {
                    renderers[A].color = Color.gray;
                    }
                if (swapperButtonList[A] != null) swapperButtonList[A].OnClick.RemoveAllListeners();  // Swap buttons can't be clicked / changed anymore
            }
            if (firstPlayer != null && secondPlayer != null) {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SwapperSwap, Hazel.SendOption.Reliable, -1);
                writer.Write((byte)firstPlayer.TargetPlayerId);
                writer.Write((byte)secondPlayer.TargetPlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);

                RPCProcedure.swapperSwap((byte)firstPlayer.TargetPlayerId, (byte)secondPlayer.TargetPlayerId);
                if (!PlayerControl.LocalPlayer.Data.Role.IsImpostor)
                {
                    Swapper.acTokenChallenge.Value.swapped1 = firstPlayer.TargetPlayerId;
                    Swapper.acTokenChallenge.Value.swapped2 = secondPlayer.TargetPlayerId;
                }
                else
                {
                    Swapper.evilSwapperAcTokenChallenge.Value.swapped1 = firstPlayer.TargetPlayerId;
                    Swapper.evilSwapperAcTokenChallenge.Value.swapped2 = secondPlayer.TargetPlayerId;
                }
                meetingExtraButtonLabel.text = Helpers.cs(Color.green, ModTranslation.getString("swapperSwapping"));
                Swapper.charges--;
                meetingExtraButtonText.text = string.Format(ModTranslation.getString("swapperRemainingSwaps"), Swapper.charges);
            }
        }

        public static void swapperCheckAndReturnSwap(MeetingHud __instance, byte dyingPlayerId) {
            // someone was guessed or dced in the meeting, check if this affects the swapper.
            if (Swapper.swapper == null || __instance.state == MeetingHud.VoteStates.Results) return;

            // reset swap.
            bool reset = false;
            if (dyingPlayerId == Swapper.playerId1 || dyingPlayerId == Swapper.playerId2) {
                reset = true;
                Swapper.playerId1 = Swapper.playerId2 = byte.MaxValue;
            }
            

            // Only for the swapper: Reset all the buttons and charges value to their original state.
            if (PlayerControl.LocalPlayer != Swapper.swapper) return;


            // check if dying player was a selected player (but not confirmed yet)
            for (int i = 0; i < __instance.playerStates.Count; i++) {
                reset = reset || selections[i] && __instance.playerStates[i].TargetPlayerId == dyingPlayerId;
                if (reset) break;
            }

            if (!reset) return;


            for (int i = 0; i < selections.Length; i++) {
                selections[i] = false;
                PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                if (playerVoteArea.AmDead || (playerVoteArea.TargetPlayerId == Swapper.swapper.PlayerId && Swapper.canOnlySwapOthers)) continue;
                renderers[i].color = Color.red;
                int copyI = i;
                swapperButtonList[i].OnClick.RemoveAllListeners();
                swapperButtonList[i].OnClick.AddListener((System.Action)(() => swapperOnClick(copyI, __instance)));
            }
            Swapper.charges++;
            meetingExtraButtonText.text = string.Format(ModTranslation.getString("swapperRemainingSwaps"), Swapper.charges);
            meetingExtraButtonLabel.text = Helpers.cs(Color.red, ModTranslation.getString("swapperConfirmSwap"));

        }

        public static void yasunaCheckAndReturnSpecialVote(MeetingHud __instance, byte dyingPlayerId)
        {
            if (Yasuna.yasuna == null || __instance.state == MeetingHud.VoteStates.Results) return;
            bool reset = false;
            if (dyingPlayerId == Yasuna.specialVoteTargetPlayerId) {
                reset = true;
                Yasuna.specialVoteTargetPlayerId = byte.MaxValue;
            }
            if (PlayerControl.LocalPlayer != Yasuna.yasuna || !reset) return;

            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea voteArea = __instance.playerStates[i];
                Transform t = voteArea.transform.FindChild("SpecialVoteButton");
                if (t != null) t.gameObject.SetActive(!voteArea.AmDead);
            }
            Yasuna._remainingSpecialVotes++;
        }

        public static GameObject guesserUI;
        public static PassiveButton guesserUIExitButton;
        public static byte guesserCurrentTarget;
        public const int MaxOneScreenRole = 40;
        private static List<Transform> RoleButtons;
        private static List<SpriteRenderer> PageButtons;
        public static int Page;

        static void guesserSelectRole(bool SetPage = true)
        {
            if (SetPage) Page = 1;
            foreach (var RoleButton in RoleButtons)
            {
                int index = 0;
                foreach (var RoleBtn in RoleButtons)
                {
                    if (RoleBtn == null) continue;
                    index++;
                    if (index <= (Page - 1) * MaxOneScreenRole) { RoleBtn.gameObject.SetActive(false); continue; }
                    if ((Page * MaxOneScreenRole) < index) { RoleBtn.gameObject.SetActive(false); continue; }
                    RoleBtn.gameObject.SetActive(true);
                }
            }
        }

        static void guesserOnClick(int buttonTarget, MeetingHud __instance) {
            if (guesserUI != null || !(__instance.state == MeetingHud.VoteStates.Voted || __instance.state == MeetingHud.VoteStates.NotVoted)) return;
            Page = 1;
            RoleButtons = new();
            PageButtons = new();
            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(false));

            Transform PhoneUI = UnityEngine.Object.FindObjectsOfType<Transform>().FirstOrDefault(x => x.name == "PhoneUI");
            Transform container = UnityEngine.Object.Instantiate(PhoneUI, __instance.transform);
            container.transform.localPosition = new Vector3(0, 0, -5f);
            guesserUI = container.gameObject;

            int i = 0;
            var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
            var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
            var smallButtonTemplate = __instance.playerStates[0].Buttons.transform.Find("CancelButton");
            var textTemplate = __instance.playerStates[0].NameText;

            guesserCurrentTarget = __instance.playerStates[buttonTarget].TargetPlayerId;

            Transform exitButtonParent = new GameObject().transform;
            exitButtonParent.SetParent(container);
            Transform exitButton = UnityEngine.Object.Instantiate(buttonTemplate.transform, exitButtonParent);
            Transform exitButtonMask = UnityEngine.Object.Instantiate(maskTemplate, exitButtonParent);
            exitButton.gameObject.GetComponent<SpriteRenderer>().sprite = smallButtonTemplate.GetComponent<SpriteRenderer>().sprite;
            exitButtonParent.transform.localPosition = new Vector3(2.725f, 2.1f, -5);
            exitButtonParent.transform.localScale = new Vector3(0.217f, 0.9f, 1);
            guesserUIExitButton = exitButton.GetComponent<PassiveButton>();
            guesserUIExitButton.OnClick.RemoveAllListeners();
            guesserUIExitButton.OnClick.AddListener((System.Action)(() => {
                __instance.playerStates.ToList().ForEach(x => {
                    x.gameObject.SetActive(true);
                    if (PlayerControl.LocalPlayer.Data.IsDead && x.transform.FindChild("ShootButton") != null) UnityEngine.Object.Destroy(x.transform.FindChild("ShootButton").gameObject);
                });
                UnityEngine.Object.Destroy(container.gameObject);
            }));

            static void ReloadPage()
            {
                PageButtons[0].gameObject.SetActive(true);
                PageButtons[1].gameObject.SetActive(true);
                if (((RoleButtons.Count / MaxOneScreenRole) +
                    (RoleButtons.Count % MaxOneScreenRole != 0 ? 1 : 0)) < Page)
                {
                    Page -= 1;
                    PageButtons[1].gameObject.SetActive(false);
                }
                else if (((RoleButtons.Count / MaxOneScreenRole) +
                    (RoleButtons.Count % MaxOneScreenRole != 0 ? 1 : 0)) < Page + 1)
                {
                    PageButtons[1].gameObject.SetActive(false);
                }
                if (Page <= 1)
                {
                    Page = 1;
                    PageButtons[0].gameObject.SetActive(false);
                }
                guesserSelectRole(false);
            }

            void CreatePage(bool IsNext, MeetingHud __instance, Transform container)
            {
                var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
                var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
                var smallButtonTemplate = __instance.playerStates[0].Buttons.transform.Find("CancelButton");
                var textTemplate = __instance.playerStates[0].NameText;
                Transform PagebuttonParent = new GameObject().transform;
                PagebuttonParent.SetParent(container);
                Transform Pagebutton = UnityEngine.Object.Instantiate(buttonTemplate, PagebuttonParent);
                Pagebutton.FindChild("ControllerHighlight").gameObject.SetActive(false);
                Transform PagebuttonMask = UnityEngine.Object.Instantiate(maskTemplate, PagebuttonParent);
                TextMeshPro Pagelabel = UnityEngine.Object.Instantiate(textTemplate, Pagebutton);
                Pagebutton.GetComponent<SpriteRenderer>().sprite = ShipStatus.Instance.CosmeticsCache.GetNameplate("nameplate_NoPlate").Image;
                PagebuttonParent.localPosition = IsNext ? new(3.535f, -2.2f, -200) : new(-3.475f, -2.2f, -200);
                PagebuttonParent.localScale = new(0.55f, 0.55f, 1f);
                Pagelabel.color = Color.white;
                Pagelabel.text = ModTranslation.getString(IsNext ? "guesserNextPage" : "guesserPrevPage");
                Pagelabel.alignment = TextAlignmentOptions.Center;
                Pagelabel.transform.localPosition = new Vector3(0, 0, Pagelabel.transform.localPosition.z);
                Pagelabel.transform.localScale *= 1.6f;
                Pagelabel.autoSizeTextContainer = true;
                Pagebutton.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    if (IsNext) Page += 1;
                    else Page -= 1;
                    ReloadPage();
                }));
                PageButtons.Add(Pagebutton.GetComponent<SpriteRenderer>());
            }

            CreatePage(false, __instance, container);
            CreatePage(true, __instance, container);

            Transform selectedButton = null;

            RoleManagerSelectRolesPatch.RoleAssignmentData roleData = RoleManagerSelectRolesPatch.getRoleAssignmentData();
            foreach (RoleInfo roleInfo in RoleInfo.allRoleInfos) {
                RoleId guesserRole = (Guesser.niceGuesser != null && PlayerControl.LocalPlayer.PlayerId == Guesser.niceGuesser.PlayerId) ? RoleId.NiceGuesser :  RoleId.EvilGuesser;
                if (roleInfo.isModifier || roleInfo.roleId == guesserRole || (!HandleGuesser.evilGuesserCanGuessSpy && guesserRole == RoleId.EvilGuesser && roleInfo.roleId == RoleId.Spy && !HandleGuesser.isGuesserGm)) continue; // Not guessable roles & modifier
                if (HandleGuesser.isGuesserGm && (roleInfo.roleId == RoleId.NiceGuesser || roleInfo.roleId == RoleId.EvilGuesser)) continue; // remove Guesser for guesser game mode
                if (HandleGuesser.isGuesserGm && PlayerControl.LocalPlayer.Data.Role.IsImpostor && !HandleGuesser.evilGuesserCanGuessSpy && roleInfo.roleId == RoleId.Spy) continue;
                // remove all roles that cannot spawn due to the settings from the ui.
                if (roleData.neutralSettings.ContainsKey((byte)roleInfo.roleId) && roleData.neutralSettings[(byte)roleInfo.roleId] == 0) continue;
                else if (roleData.impSettings.ContainsKey((byte)roleInfo.roleId) && roleData.impSettings[(byte)roleInfo.roleId] == 0) continue;
                else if (roleData.crewSettings.ContainsKey((byte)roleInfo.roleId) && roleData.crewSettings[(byte)roleInfo.roleId] == 0) continue;
                else if (new List<RoleId>() { RoleId.Janitor, RoleId.Godfather, RoleId.Mafioso }.Contains(roleInfo.roleId) && (CustomOptionHolder.mafiaSpawnRate.getSelection() == 0 || GameOptionsManager.Instance.currentGameOptions.NumImpostors < 3)) continue;
                else if (roleInfo.roleId == RoleId.Sidekick && (!CustomOptionHolder.jackalCanCreateSidekick.getBool() || CustomOptionHolder.jackalSpawnRate.getSelection() == 0)) continue;
                else if (new List<RoleId>() { RoleId.MimicA, RoleId.MimicK }.Contains(roleInfo.roleId) && (CustomOptionHolder.mimicSpawnRate.getSelection() == 0 || GameOptionsManager.Instance.currentGameOptions.NumImpostors < 2)) continue;
                else if (roleInfo.roleId == RoleId.BomberA && (CustomOptionHolder.bomberSpawnRate.getSelection() == 0 || GameOptionsManager.Instance.currentGameOptions.NumImpostors < 2)) continue;
                if (roleInfo.roleId == RoleId.Deputy && (CustomOptionHolder.deputySpawnRate.getSelection() == 0 || CustomOptionHolder.sheriffSpawnRate.getSelection() == 0)) continue;
                if (roleInfo.roleId == RoleId.Pursuer && CustomOptionHolder.lawyerSpawnRate.getSelection() == 0) continue;
                if (roleInfo.roleId == RoleId.Immoralist && (!CustomOptionHolder.foxCanCreateImmoralist.getBool() || CustomOptionHolder.foxSpawnRate.getSelection() == 0)) continue;
                if (roleInfo.roleId == RoleId.Spy && roleData.impostors.Count <= 1) continue;
                if (roleInfo.roleId == RoleId.BomberB) continue;
                if (roleInfo.roleId == RoleId.Bait && !Bait.canBeGuessed) continue;
                //if (roleInfo.roleId == RoleId.Prosecutor && (CustomOptionHolder.lawyerIsProsecutorChance.getSelection() == 0 || CustomOptionHolder.lawyerSpawnRate.getSelection() == 0)) continue;
                //if (roleInfo.roleId == RoleId.Lawyer && CustomOptionHolder.lawyerSpawnRate.getSelection() == 0) continue;
                if (Snitch.snitch != null && HandleGuesser.guesserCantGuessSnitch) {
                    var (playerCompleted, playerTotal) = TasksHandler.taskInfo(Snitch.snitch.Data);
                    int numberOfLeftTasks = playerTotal - playerCompleted;
                    if (numberOfLeftTasks <= 0 && roleInfo.roleId == RoleId.Snitch) continue;
                }
                CreateRole(roleInfo);
            }

            void CreateRole(RoleInfo roleInfo)
            {
                if (roleInfo == null) TheOtherRolesPlugin.Logger.LogMessage("RoleInfo is null while initializing!");
                if (i >= MaxOneScreenRole) i = 0;
                Transform buttonParent = new GameObject().transform;
                buttonParent.SetParent(container);
                Transform button = UnityEngine.Object.Instantiate(buttonTemplate, buttonParent);
                Transform buttonMask = UnityEngine.Object.Instantiate(maskTemplate, buttonParent);
                TMPro.TextMeshPro label = UnityEngine.Object.Instantiate(textTemplate, button);
                button.GetComponent<SpriteRenderer>().sprite = ShipStatus.Instance.CosmeticsCache.GetNameplate("nameplate_NoPlate").Image;
                RoleButtons.Add(button);
                int row = i/5, col = i%5;
                buttonParent.localPosition = new Vector3(-3.47f + 1.75f * col, 1.5f - 0.45f * row, -5);
                buttonParent.localScale = new Vector3(0.55f, 0.55f, 1f);
                label.text = Helpers.cs(roleInfo.color, roleInfo.name);
                label.alignment = TMPro.TextAlignmentOptions.Center;
                label.transform.localPosition = new Vector3(0, 0, label.transform.localPosition.z);
                label.transform.localScale *= 1.7f;
                int copiedIndex = i;

                button.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
                if (!PlayerControl.LocalPlayer.Data.IsDead && Helpers.playerById(__instance.playerStates[buttonTarget].TargetPlayerId) != null
                    && !Helpers.playerById(__instance.playerStates[buttonTarget].TargetPlayerId).Data.IsDead) button.GetComponent<PassiveButton>().OnClick.AddListener((System.Action)(() => {
                    if (selectedButton != button) {
                        selectedButton = button;
                        RoleButtons.ForEach(x => x.GetComponent<SpriteRenderer>().color = x == selectedButton ? Color.red : Color.white);
                    } else {
                        PlayerControl focusedTarget = Helpers.playerById((byte)__instance.playerStates[buttonTarget].TargetPlayerId);
                        if (!(__instance.state == MeetingHud.VoteStates.Voted || __instance.state == MeetingHud.VoteStates.NotVoted) || focusedTarget == null || (HandleGuesser.remainingShots(PlayerControl.LocalPlayer.PlayerId) <= 0 && PlayerControl.LocalPlayer != Doomsayer.doomsayer)) return;

                        if (!HandleGuesser.killsThroughShield && focusedTarget == Medic.shielded) { // Depending on the options, shooting the shielded player will not allow the guess, notifiy everyone about the kill attempt and close the window
                            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true)); 
                            UnityEngine.Object.Destroy(container.gameObject);

                            MessageWriter murderAttemptWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ShieldedMurderAttempt, Hazel.SendOption.Reliable, -1);
                            AmongUsClient.Instance.FinishRpcImmediately(murderAttemptWriter);
                            RPCProcedure.shieldedMurderAttempt();
                            SoundEffectsManager.play("fail");
                            return;
                        }

                        var mainRoleInfo = RoleInfo.getRoleInfoForPlayer(focusedTarget, false, includeHidden: true).FirstOrDefault();
                        if (mainRoleInfo == null) return;

                        PlayerControl dyingTarget = ((mainRoleInfo == roleInfo) || (roleInfo == RoleInfo.bomberA && mainRoleInfo == RoleInfo.bomberB)) ? focusedTarget : PlayerControl.LocalPlayer;

                        // Reset the GUI
                        __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                        UnityEngine.Object.Destroy(container.gameObject);
                        if ((HandleGuesser.hasMultipleShotsPerMeeting || (PlayerControl.LocalPlayer == LastImpostor.lastImpostor && LastImpostor.hasMultipleShots) || (PlayerControl.LocalPlayer == Doomsayer.doomsayer && Doomsayer.hasMultipleGuesses)) &&
                        (HandleGuesser.remainingShots(PlayerControl.LocalPlayer.PlayerId) > 1 || PlayerControl.LocalPlayer == Doomsayer.doomsayer) && dyingTarget != PlayerControl.LocalPlayer)
                            __instance.playerStates.ToList().ForEach(x => { if (x.TargetPlayerId == dyingTarget.PlayerId && x.transform.FindChild("ShootButton") != null) UnityEngine.Object.Destroy(x.transform.FindChild("ShootButton").gameObject); });
                        else
                            __instance.playerStates.ToList().ForEach(x => { if (x.transform.FindChild("ShootButton") != null) UnityEngine.Object.Destroy(x.transform.FindChild("ShootButton").gameObject); });

                        // Handle Doomsayer wrong guess
                        if (PlayerControl.LocalPlayer == Doomsayer.doomsayer && dyingTarget == PlayerControl.LocalPlayer && Doomsayer.failedGuesses < Doomsayer.maxMisses) {
                            Helpers.showFlash(Palette.ImpostorRed, duration: 0.5f, ModTranslation.getString("doomsayerWrongGuess"));
                            SoundEffectsManager.play("fail");
                            Doomsayer.failedGuesses++;
                            return;
                        }

                        bool isSpecialRole = roleInfo == RoleInfo.niceshifter || roleInfo == RoleInfo.niceSwapper;
                        // Shoot player and send chat info if activated
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GuesserShoot, Hazel.SendOption.Reliable, -1);
                        writer.Write(PlayerControl.LocalPlayer.PlayerId);
                        writer.Write(dyingTarget.PlayerId);
                        writer.Write(focusedTarget.PlayerId);
                        writer.Write((byte)roleInfo.roleId);
                        writer.Write(isSpecialRole);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        RPCProcedure.guesserShoot(PlayerControl.LocalPlayer.PlayerId, dyingTarget.PlayerId, focusedTarget.PlayerId, (byte)roleInfo.roleId, isSpecialRole);
                    }
                }));

                i++;
            }
            container.transform.localScale *= 0.75f;
            guesserSelectRole();
            ReloadPage();
        }

        static void yasunaOnClick(int buttonTarget, MeetingHud __instance)
        {
            if (Yasuna.yasuna != null && (Yasuna.yasuna.Data.IsDead || Yasuna.specialVoteTargetPlayerId != byte.MaxValue)) return;
            if (__instance.state is not (MeetingHud.VoteStates.Voted or MeetingHud.VoteStates.NotVoted or MeetingHud.VoteStates.Results)) return;
            if (__instance.playerStates[buttonTarget].AmDead) return;

            var yasunaPVA = __instance.playerStates.FirstOrDefault(t => t.TargetPlayerId == Yasuna.yasuna.PlayerId);
            if (yasunaPVA != null && yasunaPVA.DidVote)
            {
                SoundEffectsManager.play("fail");
                return;
            }

            byte targetId = __instance.playerStates[buttonTarget].TargetPlayerId;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.YasunaSpecialVote, Hazel.SendOption.Reliable, -1);
            writer.Write(PlayerControl.LocalPlayer.PlayerId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.yasunaSpecialVote(PlayerControl.LocalPlayer.PlayerId, targetId);
            if (!PlayerControl.LocalPlayer.Data.Role.IsImpostor) Yasuna.yasunaAcTokenChallenge.Value.targetId = targetId;
            else Yasuna.evilYasunaAcTokenChallenge.Value.targetId = targetId;

            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea voteArea = __instance.playerStates[i];
                Transform t = voteArea.transform.FindChild("SpecialVoteButton");
                if (t != null && voteArea.TargetPlayerId != targetId)
                    t.gameObject.SetActive(false);
            }

            __instance.playerStates[buttonTarget].VoteForMe();
        }

        [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.Select))]
        class PlayerVoteAreaSelectPatch {
            static bool Prefix(MeetingHud __instance) {
                //return !(PlayerControl.LocalPlayer != null && ((HandleGuesser.isGuesser(PlayerControl.LocalPlayer.PlayerId) && guesserUI != null) || (Yasuna.isYasuna(PlayerControl.LocalPlayer.PlayerId) && Yasuna.specialVoteTargetPlayerId != byte.MaxValue)));
                return !(PlayerControl.LocalPlayer != null && (HandleGuesser.isGuesser(PlayerControl.LocalPlayer.PlayerId) || PlayerControl.LocalPlayer == Doomsayer.doomsayer) && guesserUI != null);
            }
        }

        static void populateButtonsPostfix(MeetingHud __instance) {
            // Add Swapper Buttons
            if (Swapper.swapper != null && PlayerControl.LocalPlayer == Swapper.swapper && !Swapper.swapper.Data.IsDead) {
                selections = new bool[__instance.playerStates.Length];
                renderers = new SpriteRenderer[__instance.playerStates.Length];
                swapperButtonList = new PassiveButton[__instance.playerStates.Length];

                for (int i = 0; i < __instance.playerStates.Length; i++) {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    if (playerVoteArea.AmDead || (playerVoteArea.TargetPlayerId == Swapper.swapper.PlayerId && Swapper.canOnlySwapOthers)) continue;

                    GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
                    GameObject checkbox = UnityEngine.Object.Instantiate(template);
                    checkbox.transform.SetParent(playerVoteArea.transform);
                    checkbox.transform.position = template.transform.position;
                    checkbox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.3f);
                    if (HandleGuesser.isGuesserGm && HandleGuesser.isGuesser(PlayerControl.LocalPlayer.PlayerId)) checkbox.transform.localPosition = new Vector3(-0.5f, 0.03f, -1.3f);
                    SpriteRenderer renderer = checkbox.GetComponent<SpriteRenderer>();
                    renderer.sprite = Swapper.getCheckSprite();
                    renderer.color = Color.red;

                    if (Swapper.charges <= 0) renderer.color = Color.gray;

                    PassiveButton button = checkbox.GetComponent<PassiveButton>();
                    swapperButtonList[i] = button;
                    button.OnClick.RemoveAllListeners();
                    int copiedIndex = i;
                    button.OnClick.AddListener((System.Action)(() => swapperOnClick(copiedIndex, __instance)));
                    button.OnMouseOver.AddListener((Action)(() => TORGUIManager.Instance.SetHelpContext(button, new TORGUIText(GUIAlignment.Left, TORGUIContextEngine.Instance.GetAttribute(AttributeAsset.OverlayContent),
                        new RawTextComponent(string.Format(ModTranslation.getString("buttonLeftClick"), ModTranslation.getString("buttonSwap")))))));
                    button.OnMouseOut.AddListener((Action)(() => TORGUIManager.Instance.HideHelpContextIf(button)));

                    selections[i] = false;
                    renderers[i] = renderer;
                }

                Transform meetingUI = UnityEngine.Object.FindObjectsOfType<Transform>().FirstOrDefault(x => x.name == "PhoneUI");

                var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
                var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
                var textTemplate = __instance.playerStates[0].NameText;
                Transform meetingExtraButtonParent = new GameObject().transform;
                meetingExtraButtonParent.SetParent(meetingUI);
                Transform meetingExtraButton = UnityEngine.Object.Instantiate(buttonTemplate, meetingExtraButtonParent);

                Transform infoTransform = __instance.playerStates[0].NameText.transform.parent.FindChild("Info");
                TMPro.TextMeshPro meetingInfo = infoTransform != null ? infoTransform.GetComponent<TMPro.TextMeshPro>() : null;
                meetingExtraButtonText = UnityEngine.Object.Instantiate(__instance.playerStates[0].NameText, meetingExtraButtonParent);
                meetingExtraButtonText.text = string.Format(ModTranslation.getString("swapperRemainingSwaps"), Swapper.charges);
                meetingExtraButtonText.enableWordWrapping = false;
                meetingExtraButtonText.transform.localScale = Vector3.one * 1.7f;
                meetingExtraButtonText.transform.localPosition = new Vector3(-2.5f, 0f, 0f);

                Transform meetingExtraButtonMask = UnityEngine.Object.Instantiate(maskTemplate, meetingExtraButtonParent);
                meetingExtraButtonLabel = UnityEngine.Object.Instantiate(textTemplate, meetingExtraButton);
                meetingExtraButton.GetComponent<SpriteRenderer>().sprite = ShipStatus.Instance.CosmeticsCache.GetNameplate("nameplate_NoPlate").Image;

                meetingExtraButtonParent.localPosition = new Vector3(0, -2.225f, -5);
                meetingExtraButtonParent.localScale = new Vector3(0.55f, 0.55f, 1f);
                meetingExtraButtonLabel.alignment = TMPro.TextAlignmentOptions.Center;
                meetingExtraButtonLabel.transform.localPosition = new Vector3(0, 0, meetingExtraButtonLabel.transform.localPosition.z);
                meetingExtraButtonLabel.transform.localScale *= 1.7f;
                meetingExtraButtonLabel.text = Helpers.cs(Color.red, ModTranslation.getString("swapperConfirmSwap"));
                PassiveButton passiveButton = meetingExtraButton.GetComponent<PassiveButton>();
                passiveButton.OnClick.RemoveAllListeners();
                if (!PlayerControl.LocalPlayer.Data.IsDead) {
                    passiveButton.OnClick.AddListener((Action)(() => swapperConfirm(__instance)));
                }
                meetingExtraButton.parent.gameObject.SetActive(false);
                __instance.StartCoroutine(Effects.Lerp(7.27f, new Action<float>((p) => { // Button appears delayed, so that its visible in the voting screen only!
                    if (p == 1f) {
                        meetingExtraButton.parent.gameObject.SetActive(true);
                    }
                })));
            }

            //Fix visor in Meetings 
            /**
            foreach (PlayerVoteArea pva in __instance.playerStates) {
                if(pva.PlayerIcon != null && pva.PlayerIcon.VisorSlot != null){
                    pva.PlayerIcon.VisorSlot.transform.position += new Vector3(0, 0, -1f);
                }
            } */

            bool isGuesser = HandleGuesser.isGuesser(PlayerControl.LocalPlayer.PlayerId) || PlayerControl.LocalPlayer == Doomsayer.doomsayer;
            bool isTrackerButton = EvilTracker.canSetTargetOnMeeting && (EvilTracker.target == null || EvilTracker.resetTargetAfterMeeting) && PlayerControl.LocalPlayer == EvilTracker.evilTracker && !PlayerControl.LocalPlayer.Data.IsDead;
            // Add overlay for spelled players
            if (Witch.witch != null && Witch.futureSpelled != null) {
                foreach (PlayerVoteArea pva in __instance.playerStates) {
                    if (Witch.futureSpelled.Any(x => x.PlayerId == pva.TargetPlayerId)) {
                        SpriteRenderer rend = new GameObject().AddComponent<SpriteRenderer>();
                        rend.transform.SetParent(pva.transform);
                        rend.gameObject.layer = pva.Megaphone.gameObject.layer;
                        rend.transform.localPosition = new Vector3(-0.5f, -0.03f, -1f);
                        if (((PlayerControl.LocalPlayer == Swapper.swapper && !PlayerControl.LocalPlayer.Data.IsDead) || isTrackerButton || (PlayerControl.LocalPlayer == Yasuna.yasuna && !PlayerControl.LocalPlayer.Data.IsDead)) && isGuesser)
                            rend.transform.localPosition = new Vector3(-0.725f, -0.15f, -1f);
                        rend.sprite = Witch.getSpelledOverlaySprite();
                        addButtonGuide(rend.gameObject.SetUpButton(), ModTranslation.getString("witchMeetingOverlay"));
                        var collider = rend.gameObject.AddComponent<CircleCollider2D>();
                        collider.isTrigger = true;
                        collider.radius = 0.14f;
                    }
                }
            }

            // Add Track Button for Evil Tracker
            if (isTrackerButton)
            {
                for (int i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    if (playerVoteArea.AmDead || playerVoteArea.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                    GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
                    GameObject targetBox = UnityEngine.Object.Instantiate(template, playerVoteArea.transform);
                    targetBox.name = "EvilTrackerButton";
                    targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.3f);
                    if (HandleGuesser.isGuesserGm && HandleGuesser.isGuesser(PlayerControl.LocalPlayer.PlayerId)) targetBox.transform.localPosition = new Vector3(-0.5f, 0.03f, -1.3f);
                    SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
                    renderer.sprite = EvilTracker.getArrowSprite();
                    renderer.color = Palette.CrewmateBlue;
                    PassiveButton button = targetBox.GetComponent<PassiveButton>();
                    button.OnClick.RemoveAllListeners();
                    int copiedIndex = i;
                    button.OnClick.AddListener((System.Action)(() =>
                    {
                        PlayerControl focusedTarget = Helpers.playerById((byte)__instance.playerStates[copiedIndex].TargetPlayerId);
                        EvilTracker.futureTarget = EvilTracker.target = focusedTarget;
                        _ = new StaticAchievementToken("evilTracker.common1");
                        // Reset the GUI
                        __instance.playerStates.ToList().ForEach(x => { if (x.transform.FindChild("EvilTrackerButton") != null) UnityEngine.Object.Destroy(x.transform.FindChild("EvilTrackerButton").gameObject); });
                        GameObject targetMark = UnityEngine.Object.Instantiate(template, playerVoteArea.transform);
                        targetMark.name = "EvilTrackerMark";
                        PassiveButton button = targetMark.GetComponent<PassiveButton>();
                        targetMark.transform.localPosition = new Vector3(1.1f, 0.03f, -20f);                        
                        GameObject.Destroy(button);
                        SpriteRenderer renderer = targetMark.GetComponent<SpriteRenderer>();
                        renderer.sprite = EvilTracker.getArrowSprite();
                        renderer.color = Palette.CrewmateBlue;
                    }));
                    addButtonGuide(button, string.Format(ModTranslation.getString("buttonLeftClick"), ModTranslation.getString("buttonTrack")));
                }
            }

            // Add Guesser Buttons
            int remainingShots = HandleGuesser.remainingShots(PlayerControl.LocalPlayer.PlayerId);
            var (playerCompleted, playerTotal) = TasksHandler.taskInfo(PlayerControl.LocalPlayer.Data);

            if (isGuesser && !PlayerControl.LocalPlayer.Data.IsDead && (remainingShots > 0 || PlayerControl.LocalPlayer == Doomsayer.doomsayer)) {
                for (int i = 0; i < __instance.playerStates.Length; i++) {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    if (playerVoteArea.AmDead || playerVoteArea.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                    if (PlayerControl.LocalPlayer != null && !Helpers.isEvil(PlayerControl.LocalPlayer) && playerCompleted < HandleGuesser.tasksToUnlock) continue;
                    if (PlayerControl.LocalPlayer != null && LastImpostor.lastImpostor == PlayerControl.LocalPlayer && !LastImpostor.isOriginalGuesser && !LastImpostor.isCounterMax()) continue;
                    GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
                    GameObject targetBox = UnityEngine.Object.Instantiate(template, playerVoteArea.transform);
                    targetBox.name = "ShootButton";
                    targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.3f);
                    SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
                    renderer.sprite = HandleGuesser.getTargetSprite();
                    PassiveButton button = targetBox.GetComponent<PassiveButton>();
                    button.OnClick.RemoveAllListeners();
                    int copiedIndex = i;
                    button.OnClick.AddListener((System.Action)(() => guesserOnClick(copiedIndex, __instance)));
                    addButtonGuide(button, string.Format(ModTranslation.getString("buttonLeftClick"), ModTranslation.getString("buttonGuess")));
                }
            }

            // Add Yasuna Special Buttons
            if (Yasuna.isYasuna(PlayerControl.LocalPlayer.PlayerId) && !Yasuna.yasuna.Data.IsDead && Yasuna.remainingSpecialVotes() > 0)
            {
                for (int i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    if (playerVoteArea.AmDead || playerVoteArea.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId) continue;

                    GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
                    GameObject targetBox = UnityEngine.Object.Instantiate(template, playerVoteArea.transform);
                    targetBox.name = "SpecialVoteButton";
                    targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -2.5f);
                    if (HandleGuesser.isGuesserGm && HandleGuesser.isGuesser(PlayerControl.LocalPlayer.PlayerId)) targetBox.transform.localPosition = new Vector3(-0.5f, 0.03f, -1.3f);
                    SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
                    renderer.sprite = Yasuna.getTargetSprite(PlayerControl.LocalPlayer.Data.Role.IsImpostor);
                    PassiveButton button = targetBox.GetComponent<PassiveButton>();
                    button.OnClick.RemoveAllListeners();
                    int copiedIndex = i;
                    button.OnClick.AddListener((Action)(() => yasunaOnClick(copiedIndex, __instance)));
                    addButtonGuide(button, string.Format(ModTranslation.getString("buttonLeftClick"), ModTranslation.getString("buttonForceExile")));
                }
            }
        }

        public static void addButtonGuide(PassiveButton button, string guide)
        {
            if (!showExtraInfo) return;
            button.OnMouseOver.AddListener((Action)(() => TORGUIManager.Instance.SetHelpContext(button, new TORGUIText(GUIAlignment.Left, TORGUIContextEngine.Instance.GetAttribute(AttributeAsset.OverlayContent),
                        new RawTextComponent(guide)))));
            button.OnMouseOut.AddListener((Action)(() => TORGUIManager.Instance.HideHelpContextIf(button)));
        }

        public static void updateMeetingText(MeetingHud __instance)
        {
            // Uses remaining text for guesser/yasuna etc.
            if (meetingInfoText[0] == null)
            {
                for (int i = 0; i < meetingInfoText.Length; i++)
                {
                    meetingInfoText[i] = UnityEngine.Object.Instantiate(FastDestroyableSingleton<HudManager>.Instance.TaskPanel.taskText, __instance.transform);
                    meetingInfoText[i].alignment = TMPro.TextAlignmentOptions.BottomLeft;
                    meetingInfoText[i].transform.position = Vector3.zero;
                    meetingInfoText[i].transform.localPosition = new Vector3(-3.07f, 3.33f, -20f);
                    meetingInfoText[i].transform.localScale *= 1.1f;
                    meetingInfoText[i].color = Palette.White;
                    meetingInfoText[i].gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < meetingInfoText.Length; i++)
            {
                meetingInfoText[i].text = "";
                meetingInfoText[i].gameObject.SetActive(false);
            }

            if (MeetingHud.Instance.state is not MeetingHud.VoteStates.Voted and
                not MeetingHud.VoteStates.NotVoted and
                not MeetingHud.VoteStates.Discussion)
                return;

            int numGuesses = HandleGuesser.isGuesser(PlayerControl.LocalPlayer.PlayerId) ? HandleGuesser.remainingShots(PlayerControl.LocalPlayer.PlayerId) : 0;
            if (numGuesses > 0 && !PlayerControl.LocalPlayer.Data.IsDead) {
                meetingInfoText.getFirst().text = string.Format(ModTranslation.getString("guesserGuessesLeft"), numGuesses);
            }

            int numSpecialVotes = Yasuna.isYasuna(PlayerControl.LocalPlayer.PlayerId) ? Yasuna.remainingSpecialVotes() : 0;
            if (numSpecialVotes > 0 && !PlayerControl.LocalPlayer.Data.IsDead)
            {
                meetingInfoText.getFirst().text = string.Format(ModTranslation.getString("yasunaSpecialVotes"), numSpecialVotes);
            } if (!PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer == Akujo.akujo && Akujo.timeLeft > 0 && Akujo.honmei == null) {
                meetingInfoText.getFirst().text = string.Format(ModTranslation.getString("akujoTimeRemaining"), $"{TimeSpan.FromSeconds(Akujo.timeLeft):mm\\:ss}");
            } if (!PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer == Cupid.cupid && Cupid.timeLeft > 0 && Cupid.lovers1 == null && Cupid.lovers2 == null) {
                meetingInfoText.getFirst().text = string.Format(ModTranslation.getString("akujoTimeRemaining"), $"{TimeSpan.FromSeconds(Cupid.timeLeft):mm\\:ss}");
            } if (PlayerControl.LocalPlayer == EvilTracker.evilTracker && EvilTracker.target != null) {
                meetingInfoText.getFirst().text = string.Format(ModTranslation.getString("evilTrackerCurrentTarget"), EvilTracker.target?.Data?.PlayerName ?? "");
            } if (!PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer == Lawyer.lawyer && Lawyer.winsAfterMeetings) {
                meetingInfoText.getFirst().text = Lawyer.neededMeetings - Lawyer.meetings > 1 ? string.Format(ModTranslation.getString("lawyerMeetingInfo"), Lawyer.neededMeetings - Lawyer.meetings - 1) : ModTranslation.getString("lawyerAboutToWin");
            } if (!PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer == Doomsayer.doomsayer) {
                meetingInfoText.getFirst().text = string.Format(ModTranslation.getString("doomsayerGuessesLeft"), Math.Max(0, Doomsayer.guessesToWin - Doomsayer.counter));
                meetingInfoText.getFirst().text = string.Format(ModTranslation.getString("doomsayerMissesLeft"), Mathf.Max(0, Doomsayer.maxMisses - Doomsayer.failedGuesses));
            }

            meetingInfoText[meetingTextIndex].gameObject.SetActive(true);
            if (meetingInfoText.totalCounts() == 0) return;
            if (meetingTextIndex + 1 > meetingInfoText.totalCounts())
                meetingTextIndex = meetingInfoText.totalCounts() - 1;
        }

        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
        public static class MeetingTextUpdatePatch
        {
            public static void Postfix(KeyboardJoystick __instance)
            {
                if (meetingInfoText[0] == null || meetingInfoText.totalCounts() == 0) return;
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    meetingTextIndex = (meetingTextIndex + 1) % meetingInfoText.totalCounts();
                }
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                {
                    meetingTextIndex = 0;
                }
                if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                {
                    meetingTextIndex = 1 % meetingInfoText.totalCounts();
                }
                if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                {
                    meetingTextIndex = meetingInfoText.totalCounts() <= 3 ? meetingInfoText.totalCounts() - 1 : 2;
                }
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.ServerStart))]
        class MeetingServerStartPatch {
            static void Postfix(MeetingHud __instance)
            {
                populateButtonsPostfix(__instance);
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Deserialize))]
        class MeetingDeserializePatch {
            static void Postfix(MeetingHud __instance, [HarmonyArgument(0)]MessageReader reader, [HarmonyArgument(1)]bool initialState)
            {
                // Add swapper buttons
                if (initialState) {
                    populateButtonsPostfix(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.PopulateButtons))]
        class MeetingArrangeButtonStartPatch
        {
            public static void Postfix(MeetingHud __instance)
            {
                SortVotingArea(__instance, p => p.IsDead || p.Disconnected ? 2 : 1, 10f);
            }
        }

        [HarmonyPatch(typeof(MeetingIntroAnimation), nameof(MeetingIntroAnimation.CoRun))]
        class MeetingIntroStartPatch
        {
            public static void Postfix(MeetingIntroAnimation __instance, ref Il2CppSystem.Collections.IEnumerator __result)
            {
                __result = Effects.Sequence(
                    Effects.Action((Il2CppSystem.Action)(() =>
                    {
                        MeetingOverlayHolder.OnMeetingStart();
                    })),
                    __result
                    );
            }
        }

        [HarmonyPatch(typeof(MeetingIntroAnimation), nameof(MeetingIntroAnimation.Init))]
        public static class MeetingIntroAnimationInitPatch
        {
            private static Sprite MoriartyIndicator => Helpers.loadSpriteFromResources("TheOtherRoles.Resources.MoriartyIndicator.png", 75f);
            public static void Postfix(MeetingIntroAnimation __instance)
            {
                __instance.ProtectedRecently.SetActive(false);
                SoundManager.Instance.StopSound(__instance.ProtectedRecentlySound);
                if (Moriarty.hasKilled && Moriarty.indicateKills) {
                    __instance.ProtectedRecently.GetComponentInChildren<TMPro.TextMeshPro>().text = ModTranslation.getString("moriartyIndicator");
                    __instance.ProtectedRecently.GetComponentInChildren<SpriteRenderer>().sprite = MoriartyIndicator;
                    SoundManager.Instance.PlaySound(__instance.ProtectedRecentlySound, false, 1f);
                    __instance.ProtectedRecently.SetActive(true);
                    Moriarty.hasKilled = false;
                }
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
        class MeetingHudStartPatch
        {
            static private Image LightColorSprite = SpriteLoader.FromResource("TheOtherRoles.Resources.ColorLight.png", 100f);
            static private Image DarkColorSprite = SpriteLoader.FromResource("TheOtherRoles.Resources.ColorDark.png", 100f);

            static void Postfix(MeetingHud __instance)
            {
                foreach (var player in __instance.playerStates)
                {
                    if (showLighterDarker)
                    {
                        bool isLighter = Helpers.isLighterColor(GameData.Instance.GetPlayerById(player.TargetPlayerId).DefaultOutfit.ColorId);
                        SpriteRenderer renderer = Helpers.CreateObject<SpriteRenderer>("Color", player.transform, new Vector3(1.2f, -0.18f, -1f));
                        renderer.sprite = isLighter ? LightColorSprite.GetSprite() : DarkColorSprite.GetSprite();
                        addButtonGuide(renderer.gameObject.SetUpButton(), isLighter ? ModTranslation.getString("detectiveLightLabel") : ModTranslation.getString("detectiveDarkLabel"));
                        var collider = renderer.gameObject.AddComponent<CircleCollider2D>();
                        collider.isTrigger = true;
                        collider.radius = 0.1f;
                    }
                }
                __instance.StartCoroutine(Effects.Sequence(Effects.Wait(2f), Helpers.Action(() => SortVotingArea(__instance, p => p.IsDead || p.Disconnected ? 2 : 1)).WrapToIl2Cpp()));
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
        class StartMeetingPatch {
            public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)]NetworkedPlayerInfo meetingTarget) {
                // Resett Bait list
                //Bait.active = new Dictionary<DeadPlayer, float>();
                // Save AntiTeleport position, if the player is able to move (i.e. not on a ladder or a gap thingy)
                Bait.reported = true;

                if (PlayerControl.LocalPlayer.MyPhysics.enabled && (PlayerControl.LocalPlayer.moveable || PlayerControl.LocalPlayer.inVent
                    || HudManagerStartPatch.hackerVitalsButton.isEffectActive || HudManagerStartPatch.hackerAdminTableButton.isEffectActive || HudManagerStartPatch.securityGuardCamButton.isEffectActive
                    || Portal.isTeleporting && Portal.teleportedPlayers.Last().playerId == PlayerControl.LocalPlayer.PlayerId))
                {
                    if (!PlayerControl.LocalPlayer.inMovingPlat)
                        AntiTeleport.position = PlayerControl.LocalPlayer.transform.position;
                }

                // Save real tasks
                MapBehaviourPatch.shareRealTasks();

                // Medium meeting start time
                Medium.meetingStartTime = DateTime.UtcNow;
                // Mini
                Mini.timeOfMeetingStart = DateTime.UtcNow;
                Mini.ageOnMeetingStart = Mathf.FloorToInt(Mini.growingProgress() * 18);
                // Reset vampire bitten
                Vampire.bitten = null;
                // Count meetings
                if (meetingTarget == null) meetingsCount++;
                // Save the meeting target
                target = meetingTarget;
                meetingTextIndex = 0;

                BomberA.bombTarget = null;
                BomberB.bombTarget = null;

                Sprinter.sprinting = false;
                Ninja.stealthed = false;
                Fox.stealthed = false;

                // Mimic(Assistant) and Mimic(Killer) reset outfit
                if (MimicA.mimicA != null) MimicA.mimicA.setDefaultLook();
                if (MimicK.mimicK != null) MimicK.mimicK.setDefaultLook();

                TranslatableTag tag = meetingTarget == null ? EventDetail.EmergencyButton : EventDetail.Report;
                if (meetingTarget != null)
                {
                    var player = Helpers.playerById(meetingTarget.PlayerId);
                    if (Bait.bait != null && player == Bait.bait && Bait.reportDelay <= 0f)
                        tag = EventDetail.BaitReport;
                }
                GameStatistics.Event.GameStatistics.RecordEvent(new GameStatistics.Event(
            meetingTarget == null ? GameStatistics.EventVariation.EmergencyButton : GameStatistics.EventVariation.Report, __instance.PlayerId,
                meetingTarget == null ? 0 : (1 << meetingTarget.PlayerId)) { RelatedTag = tag });

                if (PlayerControl.LocalPlayer == Swapper.swapper)
                {
                    if (!PlayerControl.LocalPlayer.Data.Role.IsImpostor)
                    {
                        Swapper.acTokenChallenge.Value.swapped1 = byte.MaxValue;
                        Swapper.acTokenChallenge.Value.swapped2 = byte.MaxValue;
                    }
                    else
                    {
                        Swapper.evilSwapperAcTokenChallenge.Value.swapped1 = byte.MaxValue;
                        Swapper.evilSwapperAcTokenChallenge.Value.swapped2 = byte.MaxValue;
                    }
                }

                if (PlayerControl.LocalPlayer == Seer.seer)
                {
                    Seer.acTokenChallenge.Value.cleared |= Seer.acTokenChallenge.Value.flash >= 5;
                    Seer.acTokenChallenge.Value.flash = 0;
                }

                if (Snitch.snitch != null && PlayerControl.LocalPlayer == Snitch.snitch)
                {
                    var (taskComplete, taskTotal) = TasksHandler.taskInfo(Snitch.snitch.Data);
                    if (!PlayerControl.LocalPlayer.Data.IsDead && taskTotal > 0 && taskComplete == taskTotal) _ = new StaticAchievementToken("snitch.challenge");
                }

                if (PlayerControl.LocalPlayer == Vampire.vampire)
                    Vampire.acTokenChallenge.Value.cleared |= DateTime.UtcNow.Subtract(Vampire.acTokenChallenge.Value.deathTime).TotalSeconds <= 3;

                // Fortune Teller set MeetingFlag
                FortuneTeller.meetingFlag = true;
                PlagueDoctor.meetingFlag = true;

                // Reset the victim for Mimic(Killer)
                MimicK.victim = null;
                MimicA.isMorph = false;

                if (Busker.busker != null && PlayerControl.LocalPlayer == Busker.busker)
                {
                    if (Busker.pseudocideFlag)
                        Busker.dieBusker();
                    else {
                        if (Busker.acTokenChallenge != null) {
                            Busker.acTokenChallenge.Value.cleared |= !Busker.busker.Data.IsDead && DateTime.UtcNow.Subtract(Busker.acTokenChallenge.Value.pseudocide).TotalSeconds <= 3f && __instance != Busker.busker;
                        }
                    }
                }

                // Blackmail target
                if (Blackmailer.blackmailed != null && Blackmailer.blackmailed == PlayerControl.LocalPlayer)
                {
                    Coroutines.Start(Helpers.BlackmailShhh());
                }

                // Add Portal info into Portalmaker Chat:
                if (Portalmaker.portalmaker != null && (PlayerControl.LocalPlayer == Portalmaker.portalmaker || Helpers.shouldShowGhostInfo()) && !Portalmaker.portalmaker.Data.IsDead) {
                    if (Portal.teleportedPlayers.Count > 0) {
                        string msg = ModTranslation.getString("portalmakerLog");
                        foreach (var entry in Portal.teleportedPlayers) {
                            float timeBeforeMeeting = ((float)(DateTime.UtcNow - entry.time).TotalMilliseconds) / 1000;
                            msg += Portalmaker.logShowsTime ? string.Format(ModTranslation.getString("portalmakerLogTime"), (int)timeBeforeMeeting) : "";
                            msg = msg + string.Format(ModTranslation.getString("portalmakerLogName"), entry.name, entry.startingRoom, entry.endingRoom);
                        }
                        FastDestroyableSingleton<HudManager>.Instance.Chat.AddChat(Portalmaker.portalmaker, $"{msg}", false);
                    }
                }

                if (Seer.seer != null && (Seer.seer == PlayerControl.LocalPlayer || Helpers.shouldShowGhostInfo()) && !Seer.seer.Data.IsDead && Seer.canSeeKillTeams) {
                    var killList = new List<string>();
                    var teamInfos = new (Func<Seer.KillInfo, int> selector, Color color, string name)[]
                    {
                        (kt => kt.impostor, Palette.ImpostorRed, "impostor"),
                        (kt => kt.crewmate, Color.white, "crewmate"),
                        (kt => kt.jackal, Jackal.color, "jackal"),
                        (kt => kt.jekyllAndHyde, JekyllAndHyde.color, "jekyllAndHyde"),
                        (kt => kt.moriarty, Moriarty.color, "moriarty")
                    };
                    foreach (var (selector, color, name) in teamInfos) {
                        if (selector(Seer.killTeams) > 0) {
                            killList.Add(Helpers.cs(color, ModTranslation.getString(name)) + ": " + selector(Seer.killTeams).ToString());
                        }
                    }
                    if (killList.Count > 0)
                    {
                        MeetingOverlayHolder.RegisterOverlay(TORGUIContextEngine.API.VerticalHolder(GUIAlignment.Left,
                        new TORGUIText(GUIAlignment.Left, TORGUIContextEngine.API.GetAttribute(AttributeAsset.OverlayTitle), new TranslateTextComponent("seerMeetingInfo")),
                        new TORGUIText(GUIAlignment.Left, TORGUIContextEngine.API.GetAttribute(AttributeAsset.OverlayContent), new RawTextComponent(string.Join("\n", killList))))
                        , MeetingOverlayHolder.IconsSprite[3], Seer.color);
                    }
                    Seer.killTeams = new();
                }

                if (Doomsayer.doomsayer != null && Doomsayer.observed != null && (PlayerControl.LocalPlayer == Doomsayer.doomsayer || Helpers.shouldShowGhostInfo()) && !Doomsayer.doomsayer.Data.IsDead)
                {
                    string msg = "";
                    var list = new List<RoleInfo>();
                    var info = RoleInfo.getRoleInfoForPlayer(Doomsayer.observed, false, true).FirstOrDefault();
                    if (Doomsayer.Killing.Contains(info)) {
                        msg = "doomsayerKillingInfo";
                        list = Doomsayer.Killing;
                    } else if (Doomsayer.Trick.Contains(info)) {
                        msg = "doomsayerTrickInfo";
                        list = Doomsayer.Trick;
                    } else if (Doomsayer.Detect.Contains(info)) {
                        msg = "doomsayerDetectInfo";
                        list = Doomsayer.Detect;
                    } else if (Doomsayer.Panic.Contains(info)) {
                        msg = "doomsayerPanicInfo";
                        list = Doomsayer.Panic;
                    } else if (Doomsayer.Body.Contains(info)) {
                        msg = "doomsayerBodyInfo";
                        list = Doomsayer.Body;
                    } else if (Doomsayer.Team.Contains(info)) {
                        msg = "doomsayerTeamInfo";
                        list = new(Doomsayer.Team);
                        list.RemoveAll(x => x.roleId == RoleId.BomberB);
                    } else if (Doomsayer.Protection.Contains(info)) {
                        msg = "doomsayerProtectionInfo";
                        list = Doomsayer.Protection;
                    } else if (Doomsayer.Outlook.Contains(info)) {
                        msg = "doomsayerOutlookInfo";
                        list = Doomsayer.Outlook;
                    } else if (Doomsayer.Hunting.Contains(info)) {
                        msg = "doomsayerHuntingInfo";
                        list = Doomsayer.Hunting;
                    } else if (info.roleId is RoleId.Crewmate or RoleId.Impostor) {
                        msg = "doomsayerRolelessInfo";
                        list = new() { RoleInfo.crewmate, RoleInfo.impostor };
                    }
                    if (!string.IsNullOrEmpty(msg)) {
                        msg = string.Format(ModTranslation.getString(msg), Doomsayer.observed.Data.PlayerName) + "\n(" + string.Join(", ", list.Select(x => x.name)) +")";
                    }
                    else {
                        msg = string.Format(ModTranslation.getString("doomsayerNoneInfo"), Doomsayer.observed.Data.PlayerName);
                    }
                    
                    if (PlayerControl.LocalPlayer == Doomsayer.doomsayer)
                    {
                        MeetingOverlayHolder.RegisterOverlay(TORGUIContextEngine.API.VerticalHolder(GUIAlignment.Left,
                        new TORGUIText(GUIAlignment.Left, TORGUIContextEngine.API.GetAttribute(AttributeAsset.OverlayTitle), new TranslateTextComponent("doomsayerInfo")),
                        new TORGUIText(GUIAlignment.Left, TORGUIContextEngine.API.GetAttribute(AttributeAsset.OverlayContent), new RawTextComponent(msg)))
                        , MeetingOverlayHolder.IconsSprite[2], Doomsayer.color);
                    }
                    FastDestroyableSingleton<HudManager>.Instance.Chat.AddChat(Doomsayer.doomsayer, msg, false);
                }

                NekoKabocha.meetingKiller = null;

                // Clear props here else something will get wrong
                if (CustomOptionHolder.activateProps.getBool())
                {
                    Props.clearProps();
                }

                BombEffect.clearBombEffects();

                // Reset zoomed out ghosts
                Helpers.toggleZoom(reset: true);

                // Stop all playing sounds
                SoundEffectsManager.stopAll();

                // Close In-Game Settings Display if open
                HudManagerUpdate.CloseSettings();
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
        public class MeetingHudUpdatePatch {
            public static Sprite Overlay => Blackmailer.getBlackmailOverlaySprite();
            static void Postfix(MeetingHud __instance) {
                // Deactivate skip Button if skipping on emergency meetings is disabled
                if (target == null && blockSkippingInEmergencyMeetings)
                    __instance.SkipVoteButton.gameObject.SetActive(false);

                if (__instance.state >= MeetingHud.VoteStates.Discussion)
                {
                    // Remove first kill shield
                    TORMapOptions.firstKillPlayer = null;
                }

                updateMeetingText(__instance);

                if (Blackmailer.blackmailer != null && Blackmailer.blackmailed != null)
                {
                    // Blackmailer show overlay
                    var playerState = __instance.playerStates.FirstOrDefault(x => x.TargetPlayerId == Blackmailer.blackmailed.PlayerId);
                    playerState.Overlay.gameObject.SetActive(true);
                    playerState.Overlay.sprite = Overlay;
                    if (__instance.state != MeetingHud.VoteStates.Animating && !Blackmailer.alreadyShook)
                    {
                        Blackmailer.alreadyShook = true;
                        __instance.StartCoroutine(Effects.SwayX(playerState.transform));
                    }
                }
            }
        }

        [HarmonyPatch(typeof(QuickChatMenu), nameof(QuickChatMenu.Open))]
        public class BlockQuickChatAbility
        {
            public static bool Prefix(QuickChatMenu __instance)
            {
                if (Blackmailer.blackmailer != null && Blackmailer.blackmailed != null && Blackmailer.blackmailed == PlayerControl.LocalPlayer)
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.SetText))]
        public class BlockChatBlackmailed
        {
            public static bool Prefix(TextBoxTMP __instance)
            {
                if (Blackmailer.blackmailer != null && Blackmailer.blackmailed != null && Blackmailer.blackmailed == PlayerControl.LocalPlayer)
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
        public static void MeetingHudIntroPrefix() {
            EventUtility.meetingStartsUpdate();
        }

    }
}
