using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Button;
using Object = UnityEngine.Object;
using TheOtherRoles.Patches;
using UnityEngine.SceneManagement;
using TheOtherRoles.Utilities;
using AmongUs.Data;
using Assets.InnerNet;
using System.Linq;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using TMPro;
using System.Text;
using TheOtherRoles.MetaContext;

namespace TheOtherRoles.Modules {
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public class MainMenuPatch {
        private static bool horseButtonState = TORMapOptions.enableHorseMode;

        private static void Prefix(MainMenuManager __instance) {
            // Force Reload of SoundEffectHolder
            SoundEffectsManager.Load();
            CheckAndUnpatch();
        }

        private static void CheckAndUnpatch()
        {
            // 获取所有已加载的插件
            var loadedPlugins = IL2CPPChainloader.Instance.Plugins.Values;
            // 检查是否有目标插件
            var targetPlugin = loadedPlugins.FirstOrDefault(plugin => plugin.Metadata.Name == "MalumMenu");

            if (targetPlugin != null)
            {
                TheOtherRolesPlugin.Logger.LogMessage("Hacker Plugin Found.\n GMNK Does Not Allow You Using Such Plugins.\nWill Unpatch Soon.");
                Harmony.UnpatchAll();//当进入MainMenu时检测加载如果有MM 就自动关闭
            }
        }

        public static void addSceneChangeCallbacks() {
            SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)((scene, _) => {
                if (!scene.name.Equals("MatchMaking", StringComparison.Ordinal)) return;
                TORMapOptions.gameMode = CustomGamemodes.Classic;
                // Add buttons For Guesser Mode, Hide N Seek in this scene.
                // find "HostLocalGameButton"
                var template = GameObject.FindObjectOfType<HostLocalGameButton>();
                var gameButton = template.transform.FindChild("CreateGameButton");
                var gameButtonPassiveButton = gameButton.GetComponentInChildren<PassiveButton>();

                var guesserButton = GameObject.Instantiate<Transform>(gameButton, gameButton.parent);
                guesserButton.transform.localPosition += new Vector3(0f, -0.5f);
                var guesserButtonText = guesserButton.GetComponentInChildren<TMPro.TextMeshPro>();
                var guesserButtonPassiveButton = guesserButton.GetComponentInChildren<PassiveButton>();
                
                guesserButtonPassiveButton.OnClick = new Button.ButtonClickedEvent();
                guesserButtonPassiveButton.OnClick.AddListener((System.Action)(() => {
                    TORMapOptions.gameMode = CustomGamemodes.Guesser;
                    template.OnClick();
                }));

                var HideNSeekButton = GameObject.Instantiate<Transform>(gameButton, gameButton.parent);
                HideNSeekButton.transform.localPosition += new Vector3(1.7f, -0.5f);
                var HideNSeekButtonText = HideNSeekButton.GetComponentInChildren<TMPro.TextMeshPro>();
                var HideNSeekButtonPassiveButton = HideNSeekButton.GetComponentInChildren<PassiveButton>();
                
                HideNSeekButtonPassiveButton.OnClick = new Button.ButtonClickedEvent();
                HideNSeekButtonPassiveButton.OnClick.AddListener((System.Action)(() => {
                    TORMapOptions.gameMode = CustomGamemodes.HideNSeek;
                    template.OnClick();
                }));

                var FreePlayButton = GameObject.Instantiate<Transform>(gameButton, gameButton.parent);
                FreePlayButton.transform.localPosition += new Vector3(3.4f, -0.5f);
                var FreePlaytButtonText = FreePlayButton.GetComponentInChildren<TMPro.TextMeshPro>();
                var FreePlayButtonPassiveButton = FreePlayButton.GetComponentInChildren<PassiveButton>();

                FreePlayButtonPassiveButton.OnClick = new Button.ButtonClickedEvent();
                FreePlayButtonPassiveButton.OnClick.AddListener((System.Action)(() => {
                    TORMapOptions.gameMode = CustomGamemodes.FreePlay;
                    template.OnClick();
                }));

                template.StartCoroutine(Effects.Lerp(0.1f, new System.Action<float>((p) => {
                    guesserButtonText.SetText(ModTranslation.getString("torGuesser"));
                    HideNSeekButtonText.SetText(ModTranslation.getString("torHideNSeek"));
                    FreePlaytButtonText.SetText(ModTranslation.getString("torFreePlay"));
                 })));
            }));
        }
    }

    [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
    public static class VersionShower_Start
    {
        public static void Postfix(VersionShower __instance)
        {
            __instance.text.text = $"Among Us v{DestroyableSingleton<ReferenceDataManager>.Instance.Refdata.userFacingVersion} - " +
                $"{Helpers.GradientColorText("5A5AAD", "5A5AAD", $"The Other Roles GM NK")} <color=#FCCE03FF>v{TheOtherRolesPlugin.VersionString + (TheOtherRolesPlugin.betaDays > 0 ? "-BETA" : "")}</color>";
        }
    }

    // Original credits to Nebula On the Ship
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Awake))]
    public static class MainMenuSetUpPatch
    {
        public static Sprite sprite;
        public static Sprite discordSprite;
        public static GameObject modScreen = null;
        public static GameObject aboutScreen = null;
        public static Dictionary<string, string> contributors = new()
        { { "Imp11", "mainMenuDeveloper" }, { "Amongus", "mainMenuDeveloper" }, 
            { "Fangkuai", "mainMenuArtist" }, { "Yuunozikkyou" , "mainMenuTranslator"}, { "TAIK", "mainMenuTranslator"} };

        public static void Postfix(MainMenuManager __instance)
        {
            VanillaAsset.LoadAssetAtInitialize();
            var leftPanel = __instance.mainMenuUI.transform.FindChild("AspectScaler").FindChild("LeftPanel");
            leftPanel.GetComponent<SpriteRenderer>().size += new Vector2(0f, 0.5f);
            var auLogo = leftPanel.FindChild("Sizer").GetComponent<AspectSize>();
            auLogo.PercentWidth = 0.14f;
            auLogo.DoSetUp();
            auLogo.transform.localPosition += new Vector3(-0.8f, 0.25f, 0f);

            float height = __instance.newsButton.transform.localPosition.y - __instance.myAccountButton.transform.localPosition.y;

            // disable old panels and create a new button
            var reworkedPanel = Helpers.CreateObject<SpriteRenderer>("ReworkedLeftPanel", leftPanel, new Vector3(0f, height * 0.5f, 0f));
            var oldPanel = leftPanel.GetComponent<SpriteRenderer>();
            reworkedPanel.sprite = oldPanel.sprite;
            reworkedPanel.tileMode = oldPanel.tileMode;
            reworkedPanel.drawMode = oldPanel.drawMode;
            reworkedPanel.size = oldPanel.size;
            oldPanel.enabled = false;

            loadSprite();

            // Move the buttons upwards
            foreach (var button in __instance.mainButtons.GetFastEnumerator())
                if (Math.Abs(button.transform.localPosition.x) < 0.1f) button.transform.localPosition += new Vector3(0f, height, 0f);
            leftPanel.FindChild("Main Buttons").FindChild("Divider").transform.localPosition += new Vector3(0f, height, 0f);

            var modButton = Object.Instantiate(__instance.settingsButton, __instance.settingsButton.transform.parent);
            modButton.transform.localPosition += new Vector3(0f, -height, 0f);
            modButton.gameObject.name = "TORButton";
            modButton.gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)((obj) => {
                var icon = obj.transform.FindChild("Icon");
                if (icon != null)
                {
                    icon.localScale = new Vector3(1f, 1f, 1f);
                    icon.GetComponent<SpriteRenderer>().sprite = sprite;
                }
            }));

            var modPassiveButton = modButton.GetComponent<PassiveButton>();
            modPassiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            modPassiveButton.OnClick.AddListener((System.Action)(() =>
            {
                __instance.ResetScreen();
                modScreen.SetActive(true);
                __instance.screenTint.enabled = true;
            }));
            modButton.transform.FindChild("FontPlacer").GetChild(0).GetComponent<TextTranslatorTMP>().SetModText("TheOtherRoles-GM-NK");

            modScreen = Object.Instantiate(__instance.accountButtons, __instance.accountButtons.transform.parent);
            modScreen.name = "modScreen";
            modScreen.transform.GetChild(0).GetChild(0).GetComponent<TextTranslatorTMP>().SetModText("The Other Roles-GM-NK");
            __instance.mainButtons.Add(modButton);

            Object.Destroy(modScreen.transform.GetChild(4).gameObject);

            var temp = modScreen.transform.GetChild(3);
            int index = 0;

            void SetUpButton(string button, Action clickAction)
            {
                GameObject obj = temp.gameObject;
                if (index > 0) obj = Object.Instantiate(obj, obj.transform.parent);

                obj.transform.GetChild(0).GetChild(0).GetComponent<TextTranslatorTMP>().SetModText(button);
                var passiveButton = obj.GetComponent<PassiveButton>();
                passiveButton.OnClick = new ButtonClickedEvent();
                passiveButton.OnClick.AddListener((System.Action)(() => {
                    clickAction.Invoke();
                }));
                obj.transform.localPosition = new Vector3(0f, 0.98f - index * 0.68f, 0f);

                index++;
            }
            SetUpButton(ModTranslation.getString("modAbout"), () => {
                __instance.ResetScreen();
                if (!aboutScreen) createAboutScreen();
                aboutScreen?.SetActive(true);
                __instance.screenTint.enabled = true;
            });
            SetUpButton(ModTranslation.getString("modHowToPlay"), () =>
            {
                PlayManager.Open(__instance);
            });
            SetUpButton(ModTranslation.getString("modAchievements"), () => {
                AchievementViewer.Open(__instance);
            });

            var discordRenderer = Helpers.CreateObject<SpriteRenderer>("DiscordButton", modScreen.transform, new Vector3(2.8f, -1.4f));
            discordRenderer.sprite = discordSprite;
            var discordButton = discordRenderer.gameObject.SetUpButton(true, discordRenderer);
            discordButton.OnMouseOver.AddListener((Action)(() => TORGUIManager.Instance.SetHelpContext(discordButton, ModTranslation.getString("mainMenuDiscordText"))));
            discordButton.OnMouseOut.AddListener((Action)(() => TORGUIManager.Instance.HideHelpContextIf(discordButton)));
            discordButton.OnClick.AddListener((Action)(() => Application.OpenURL("https://discord.gg/vXFXcNcKSM")));
            discordButton.gameObject.AddComponent<CircleCollider2D>().radius = 0.25f;

            void createAboutScreen()
            {
                aboutScreen = Helpers.CreateObject("About", __instance.accountButtons.transform.parent, new Vector3(0, 0, -1f));
                aboutScreen.transform.localScale = modScreen!.transform.localScale;
                var screen = MetaScreen.GenerateScreen(new Vector2(6.2f, 4.1f), aboutScreen.transform, new Vector3(-0.1f, 0, 0f), false, false, false);
                TextAttribute detailAttribute = new(TextAttribute.BoldAttr)
                {
                    FontMaterial = VanillaAsset.StandardMaskedFontMaterial,
                    Size = new Vector2(5.8f, 0.4f),
                    Alignment = TMPro.TextAlignmentOptions.TopLeft,
                    FontSize = 2f,
                    FontMaxSize = 2f,
                    FontMinSize = 1.7f
                };
                TextAttribute descriptionAttribute = new(TextAttribute.ContentAttr)
                {
                    FontMaterial = VanillaAsset.StandardMaskedFontMaterial,
                    Size = new Vector2(5.8f, 0.4f),
                    Alignment = TMPro.TextAlignmentOptions.TopLeft,
                    FontSize = 1.7f,
                    FontMaxSize = 1.7f,
                    FontMinSize = 1.3f
                };
                TextAttribute titleAttribute = new(TextAttribute.NormalAttr)
                {
                    FontMaterial = VanillaAsset.StandardMaskedFontMaterial,
                    Size = new Vector2(3.4f, 0.3f),
                    Alignment = TextAlignmentOptions.Center,
                    FontSize = 4f,
                    FontMaxSize = 4f,
                    FontMinSize = 3.5f
                };

                var inner = new MetaContextOld();
                inner.Append(new MetaContextOld.Text(titleAttribute) { RawText = ModTranslation.getString("modAbout").ToUpper(), Alignment = IMetaContextOld.AlignmentOption.Center });
                inner.Append(new MetaContextOld.VerticalMargin(0.5f));
                foreach (var contributor in contributors)
                {
                    inner.Append(new CombinedContextOld(0.5f,
                    new MetaContextOld.Image(Helpers.loadSpriteFromResources($"TheOtherRoles.Resources.Contributors.{contributor.Key}.png", 50f)) { Width = 0.7f },
                    new MetaContextOld.Text(detailAttribute) { RawText = $"{ModTranslation.getString(contributor.Value)} - {contributor.Key}" })
                    { Alignment = IMetaContextOld.AlignmentOption.Left });
                    inner.Append(new MetaContextOld.VerticalMargin(0.3f));
                    var lines = Helpers.removeLineFeed(ModTranslation.getString($"mainMenu{contributor.Key}AboutSection"));
                    foreach (var l in lines)
                    {
                        inner.Append(new MetaContextOld.Text(descriptionAttribute) { RawText = l });
                    }
                    inner.Append(new CombinedContextOld(0.5f,
                        new MetaContextOld.VerticalMargin(0.3f)
                        ));
                }
                screen.SetContext(new MetaContextOld.ScrollView(new Vector2(6.2f, 4.1f), inner, true) { Alignment = IMetaContextOld.AlignmentOption.Center });
            }
        }

        public static void loadSprite()
        {
            if (sprite == null) sprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.LogoButton.png", 100f);
            if (discordSprite == null) discordSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.DiscordIcon.png", 100f);
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.ResetScreen))]
    public static class MainMenuClearScreenPatch
    {
        public static void Postfix(MainMenuManager __instance)
        {
            if (MainMenuSetUpPatch.modScreen) MainMenuSetUpPatch.modScreen?.SetActive(false);
            if (MainMenuSetUpPatch.aboutScreen) MainMenuSetUpPatch.aboutScreen?.SetActive(false);
        }
    }

    [HarmonyPatch]
    public class MainMenuButtonHoverAnimation
    {

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Start_Postfix(MainMenuManager __instance)
        {
            var mainButtons = GameObject.Find("Main Buttons");
            mainButtons.ForEachChild((Il2CppSystem.Action<GameObject>)Init);
            static void Init(GameObject obj)
            {
                if (obj.name is "BottomButtonBounds" or "Divider") return;
                if (AllButtons.ContainsKey(obj)) return;
                SetButtonStatus(obj, false);
                var pb = obj.GetComponent<PassiveButton>();
                pb.OnMouseOver.AddListener((Action)(() => SetButtonStatus(obj, true)));
                pb.OnMouseOut.AddListener((Action)(() => SetButtonStatus(obj, false)));
            }
        }

        private static Dictionary<GameObject, (Vector3, bool)> AllButtons = new();
        private static void SetButtonStatus(GameObject obj, bool active)
        {
            AllButtons.TryAdd(obj, (obj.transform.position, active));
            AllButtons[obj] = (AllButtons[obj].Item1, active);
        }

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate)), HarmonyPostfix]
        private static void Update_Postfix(MainMenuManager __instance)
        {
            if (GameObject.Find("MainUI") == null) return;

            foreach (var kvp in AllButtons.Where(x => x.Key != null && x.Key.active))
            {
                var button = kvp.Key;
                var pos = button.transform.position;
                var targetPos = kvp.Value.Item1 + new Vector3(kvp.Value.Item2 ? 0.35f : 0f, 0f, 0f);
                if (kvp.Value.Item2 && pos.x > (kvp.Value.Item1.x + 0.2f)) continue;
                button.transform.position = kvp.Value.Item2
                    ? Vector3.Lerp(pos, targetPos, Time.deltaTime * 2f)
                    : Vector3.MoveTowards(pos, targetPos, Time.deltaTime * 2f);
            }
        }
    }
}
