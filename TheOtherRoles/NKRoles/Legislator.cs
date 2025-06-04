using UnityEngine;
using System.Linq;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using TheOtherRoles.Objects;
using TheOtherRoles.Utilities;
using TheOtherRoles.CustomGameModes;
using static TheOtherRoles.TheOtherRoles;
using AmongUs.Data;
using Hazel;
using TheOtherRoles.Patches;
using Reactor.Utilities.Extensions;
using TheOtherRoles.Modules;
using AmongUs.GameOptions;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using Reactor.Utilities;
using TheOtherRoles.MetaContext;

namespace TheOtherRoles
{
    public static class Legislator
    {
        public static PlayerControl legislator = new();
        public static Color Color = new Color32(77, 187, 193, byte.MaxValue);
        private static Sprite buttonSprite;
        public static AchievementToken<(byte votedFor, bool cleared)> acTokenChallenge = null;
        public static Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.JealousButton.png", 115f);
            return buttonSprite;
        }

        public static void onAchievementActivate()
        {
            if (legislator == null || PlayerControl.LocalPlayer != legislator) return;
            acTokenChallenge ??= new("mayor.challenge", (byte.MaxValue, false), (val, _) => val.cleared);
        }

        public static void clearAndReload()
        {
            legislator = new PlayerControl();
        }
    }
}
