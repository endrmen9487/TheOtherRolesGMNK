using Hazel;
using TheOtherRoles.Modules;
using TheOtherRoles;
using UnityEngine;

public static class EvilMayor
{
    public static PlayerControl evilmayor;
    public static Color color = Palette.ImpostorRoleRed;
    public static Minigame emergency = null;
    public static Sprite emergencySprite = null;
    public static int remoteMeetingsLeft = 1;

    public static bool meetingButton = true;
    public static int numVotes = 2;

    public static AchievementToken<(byte votedFor, bool cleared)> acTokenChallenge = null;

    public static Sprite getMeetingSprite()
    {
        if (emergencySprite) return emergencySprite;
        emergencySprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.EmergencyButton.png", 550f);
        return emergencySprite;
    }

    public static void onAchievementActivate()
    {
        if (evilmayor == null || PlayerControl.LocalPlayer != evilmayor) return;
        acTokenChallenge ??= new("mayor.challenge", (byte.MaxValue, false), (val, _) => val.cleared);
    }

    public static void unlockAch(byte votedFor)
    {
        var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UnlockMayorAcCommon, SendOption.Reliable, -1);
        writer.Write(votedFor);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCProcedure.unlockMayorAcCommon(votedFor);
    }

    public static void clearAndReload()
    {
        evilmayor = null;
        emergency = null;
        emergencySprite = null;
        remoteMeetingsLeft = Mathf.RoundToInt(CustomOptionHolder.mayorMaxRemoteMeetings.getFloat());
        meetingButton = CustomOptionHolder.mayorMeetingButton.getBool();
        numVotes = (int)CustomOptionHolder.mayorNumVotes.getFloat();
        acTokenChallenge = null;
    }
}
