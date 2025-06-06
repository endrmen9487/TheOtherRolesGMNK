using TheOtherRoles.CustomGameModes;
using UnityEngine;

namespace TheOtherRoles
{

    public class SpecimenVital
    {
        public static Vector3 pos = new(35.39f, -22.10f, 1.0f);
        public static bool flag = false;
        public static void clearAndReload()
        {
            flag = false;
        }

        public static void moveVital()
        {
            if (flag || HideNSeek.isHideNSeekGM || GameOptionsManager.Instance.currentGameOptions.GameMode == AmongUs.GameOptions.GameModes.HideNSeek) return;
            if (GameOptionsManager.Instance.currentNormalGameOptions.MapId == 2 && CustomOptionHolder.specimenVital.getBool())
            {
                var panel = GameObject.Find("panel_vitals");
                if (panel != null)
                {
                    var transform = panel.GetComponent<Transform>();
                    transform.SetPositionAndRotation(pos, transform.rotation);
                    flag = true;
                }
            }
        }
    }
}
