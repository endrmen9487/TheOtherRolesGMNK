using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheOtherRoles.Modules;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TheOtherRoles.MetaContext
{
    public class MouseOverPopup : MonoBehaviour
    {
        private MetaScreen myScreen = null!;
        private SpriteRenderer background = null!;
        private Vector2 screenSize;
        private PassiveUiElement relatedButton;

        public PassiveUiElement RelatedObject => relatedButton;
        static MouseOverPopup()
        {
            ClassInjector.RegisterTypeInIl2Cpp<MouseOverPopup>();
        }

        public void Awake()
        {
            background = Helpers.CreateObject<SpriteRenderer>("Background", transform, Vector3.zero, LayerMask.NameToLayer("UI"));
            background.sprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.StatisticsBackground.png", 100f);
            background.drawMode = SpriteDrawMode.Sliced;
            background.tileMode = SpriteTileMode.Continuous;
            background.color = new Color(0.14f, 0.14f, 0.14f, 1f);

            screenSize = new Vector2(7f, 4f);
            myScreen = MetaScreen.GenerateScreen(screenSize, transform, Vector3.zero, false, false, false);

            gameObject.SetActive(false);
        }

        public void Irrelevantize()
        {
            relatedButton = null;
        }

        public void SetContextOld(PassiveUiElement related, IMetaContextOld context)
        {
            myScreen.SetContext(null);

            if (context == null)
            {
                gameObject.SetActive(false);
                relatedButton = null;
                return;
            }

            gameObject.SetActive(true);

            relatedButton = related;
            transform.SetParent(Helpers.FindCamera(LayerMask.NameToLayer("UI"))!.transform);

            bool isLeft = Input.mousePosition.x < Screen.width / 2f;
            bool isLower = Input.mousePosition.y < Screen.height / 2f;

            float height = myScreen.SetContext(context, out var width);

            if (width.min > width.max)
            {
                gameObject.SetActive(false);
                return;
            }

            float[] xRange = new float[2], yRange = new float[2];
            xRange[0] = -screenSize.x / 2f - 0.15f;
            yRange[1] = screenSize.y / 2f + 0.15f;
            xRange[1] = xRange[0] + (width.max - width.min) + 0.3f;
            yRange[0] = yRange[1] - height - 0.3f;

            Vector2 anchorPoint = new(xRange[isLeft ? 0 : 1], yRange[isLower ? 0 : 1]);

            var pos = Helpers.ScreenToWorldPoint(Input.mousePosition, LayerMask.NameToLayer("UI"));
            pos.z = -800f;
            transform.position = pos - (Vector3)anchorPoint;

            //範囲外にはみ出た表示の是正
            {
                var lower = Helpers.ScreenToWorldPoint(new(10f, 10f), LayerMask.NameToLayer("UI"));
                var upper = Helpers.ScreenToWorldPoint(new(Screen.width - 10f, Screen.height - 10f), LayerMask.NameToLayer("UI"));
                float diff;

                diff = (transform.position.x + xRange[0]) - lower.x;
                if (diff < 0f) transform.position -= new Vector3(diff, 0f);

                diff = (transform.position.y + yRange[0]) - lower.y;
                if (diff < 0f) transform.position -= new Vector3(0f, diff);

                diff = (transform.position.x + xRange[1]) - upper.x;
                if (diff > 0f) transform.position -= new Vector3(diff, 0f);

                diff = (transform.position.y + yRange[1]) - upper.y;
                if (diff > 0f) transform.position -= new Vector3(0f, diff);
            }


            background.transform.localPosition = new Vector3((width.min + width.max) / 2f, screenSize.y / 2f - height / 2f, 1f);
            background.size = new Vector2((width.max - width.min) + 0.22f, height + 0.1f);

            Update();
        }

        public void SetContext(PassiveUiElement related, GUIContext context)
        {
            myScreen.SetContext(null);

            if (context == null)
            {
                gameObject.SetActive(false);
                relatedButton = null;
                return;
            }

            gameObject.SetActive(true);

            relatedButton = related;
            transform.SetParent(Helpers.FindCamera(LayerMask.NameToLayer("UI"))!.transform);

            bool isLeft = Input.mousePosition.x < Screen.width / 2f;
            bool isLower = Input.mousePosition.y < Screen.height / 2f;

            myScreen.SetContext(context, new Vector2(0.5f, 0.5f), out var size);



            float[] xRange = new float[2], yRange = new float[2];
            xRange[0] = -size.Width * 0.5f - 0.15f;
            xRange[1] = size.Width * 0.5f + 0.15f;
            yRange[0] = -size.Height * 0.5f - 0.15f;
            yRange[1] = size.Height * 0.5f + 0.15f;

            Vector2 anchorPoint = new(xRange[isLeft ? 0 : 1], yRange[isLower ? 0 : 1]);

            var pos = Helpers.ScreenToWorldPoint(Input.mousePosition, LayerMask.NameToLayer("UI"));
            pos.z = -800f;
            transform.position = pos - (Vector3)anchorPoint;

            //範囲外にはみ出た表示の是正
            {
                var lower = Helpers.ScreenToWorldPoint(new(10f, 10f), LayerMask.NameToLayer("UI"));
                var upper = Helpers.ScreenToWorldPoint(new(Screen.width - 10f, Screen.height - 10f), LayerMask.NameToLayer("UI"));
                float diff;

                diff = (transform.position.x + xRange[0]) - lower.x;
                if (diff < 0f) transform.position -= new Vector3(diff, 0f);

                diff = (transform.position.y + yRange[0]) - lower.y;
                if (diff < 0f) transform.position -= new Vector3(0f, diff);

                diff = (transform.position.x + xRange[1]) - upper.x;
                if (diff > 0f) transform.position -= new Vector3(diff, 0f);

                diff = (transform.position.y + yRange[1]) - upper.y;
                if (diff > 0f) transform.position -= new Vector3(0f, diff);
            }


            background.transform.localPosition = new Vector3(0f, 0f, 1f);
            background.size = new Vector2(size.Width + 0.22f, size.Height + 0.1f);

            Update();
        }

        public void Update()
        {
            if (relatedButton is not null && !relatedButton)
            {
                SetContext(null, null);
            }

        }
    }

    public abstract class AbstractGUIContext : GUIContext
    {
        internal override GUIAlignment Alignment { get; init; }
        internal override GameObject Instantiate(Anchor anchor, Size size, out Size actualSize)
        {
            var obj = Instantiate(size, out actualSize);

            if (obj != null)
            {
                UnityEngine.Vector3 localPos = anchor.anchoredPosition -
                    new UnityEngine.Vector3(
                        actualSize.Width * (anchor.pivot.x - 0.5f),
                        actualSize.Height * (anchor.pivot.y - 0.5f),
                        0f);

                obj.transform.localPosition = localPos;
            }

            return obj;
        }

        public AbstractGUIContext(GUIAlignment alignment)
        {
            Alignment = alignment;
        }

        protected static float CalcWidth(GUIAlignment alignment, float myWidth, float maxWidth)
        {
            return Calc(alignment, myWidth, maxWidth, GUIAlignment.Left, GUIAlignment.Right);
        }

        protected static float CalcHeight(GUIAlignment alignment, float myHeight, float maxHeight)
        {
            return Calc(alignment, myHeight, maxHeight, GUIAlignment.Bottom, GUIAlignment.Top);
        }

        private static float Calc(GUIAlignment alignment, float myParam, float maxParam, GUIAlignment lower, GUIAlignment higher)
        {
            if ((int)(alignment & lower) != 0)
                return (myParam - maxParam) * 0.5f;
            if ((int)(alignment & higher) != 0)
                return (maxParam - myParam) * 0.5f;
            return 0f;
        }
    }

    public class GUIScrollView : AbstractGUIContext
    {
        //スクローラの位置を記憶する
        private static Dictionary<string, float> distDic = new();
        public static void RemoveDistHistory(string tag) => distDic.Remove(tag);
        public static void UpdateDistHistory(string tag, float y) => distDic[tag] = y;
        public static float TryGetDistHistory(string tag) => distDic.TryGetValue(tag, out var val) ? val : 0f;
        public static Action GetDistHistoryUpdater(Func<float> y, string tag) => () => UpdateDistHistory(tag, y.Invoke());


        public class InnerScreen : GUIScreen
        {
            public bool IsValid => screen;

            private GameObject screen;
            private Size innerSize;
            private float scrollViewSizeY;
            private Scroller scroller;
            private Collider2D scrollerCollider;
            private Anchor myAnchor;

            public InnerScreen(GameObject screen, Size innerSize, Scroller scroller, Collider2D scrollerCollider, float scrollViewSizeY)
            {
                this.screen = screen;
                this.innerSize = innerSize;
                this.scroller = scroller;
                this.scrollerCollider = scrollerCollider;
                this.scrollViewSizeY = scrollViewSizeY;
                this.myAnchor = new(new(0f, 1f), new(-innerSize.Width * 0.5f, innerSize.Height * 0.5f, -0.01f));
            }

            public void SetContext(GUIContext context, out Size actualSize)
            {
                screen.ForEachChild((Il2CppSystem.Action<GameObject>)(obj => GameObject.Destroy(obj)));

                if (context != null)
                {
                    var obj = context.Instantiate(myAnchor, innerSize, out actualSize);
                    if (obj != null)
                    {
                        obj.transform.SetParent(screen.transform, false);

                        scroller.SetBounds(new FloatRange(0, actualSize.Height - scrollViewSizeY), null);
                        scroller.ScrollRelative(UnityEngine.Vector2.zero);

                        foreach (var button in screen.GetComponentsInChildren<PassiveButton>()) button.ClickMask = scrollerCollider;
                    }
                }
                else
                {
                    actualSize = new Size(0f, 0f);
                }
            }
        }

        public string ScrollerTag { get; init; } = null;
        public UnityEngine.Vector2 Size { get; init; }
        public bool WithMask { get; init; } = true;

        internal ListArtifact<InnerScreen> InnerArtifact { get; private init; }
        public Artifact<GUIScreen> Artifact { get; private init; }

        public GUIContext Inner { get; init; } = null;

        public GUIScrollView(GUIAlignment alignment, UnityEngine.Vector2 size, GUIContext inner) : base(alignment)
        {
            this.Size = size;

            this.InnerArtifact = new();
            this.Artifact = new GeneralizedArtifact<GUIScreen, InnerScreen>(InnerArtifact);
            this.Inner = inner;
        }


        internal override GameObject Instantiate(Size size, out Size actualSize)
        {
            var view = Helpers.CreateObject("ScrollView", null, new UnityEngine.Vector3(0f, 0f, 0f), LayerMask.NameToLayer("UI"));
            var inner = Helpers.CreateObject("Inner", view.transform, new UnityEngine.Vector3(-0.2f, 0f, -0.1f));
            var innerSize = Size - new UnityEngine.Vector2(0.4f, 0f);

            if (WithMask)
            {
                view.AddComponent<SortingGroup>();
                var mask = Helpers.CreateObject<SpriteMask>("Mask", view.transform, new UnityEngine.Vector3(-0.2f, 0, 0));
                mask.sprite = VanillaAsset.FullScreenSprite;
                mask.transform.localScale = innerSize;
            }

            var scroller = VanillaAsset.GenerateScroller(Size, view.transform, new UnityEngine.Vector2(Size.x / 2 - 0.15f, 0f), inner.transform, new FloatRange(0, Size.y), Size.y);
            var hitBox = scroller.GetComponent<Collider2D>();
            var innerScreen = new InnerScreen(inner, new(innerSize), scroller, hitBox, Size.y);
            InnerArtifact.Values.Add(innerScreen);

            innerScreen.SetContext(Inner, out var innerActualSize);
            float height = innerActualSize.Height;


            if (ScrollerTag != null && distDic.TryGetValue(ScrollerTag, out var val))
                scroller.Inner.transform.localPosition = scroller.Inner.transform.localPosition +
                    new UnityEngine.Vector3(0f, Mathf.Clamp(val + scroller.ContentYBounds.min, scroller.ContentYBounds.min, scroller.ContentYBounds.max), 0f);

            if (ScrollerTag != null)
                scroller.Inner.gameObject.AddComponent<ScriptBehaviour>().UpdateHandler += () => { distDic[ScrollerTag] = scroller.Inner.transform.localPosition.y - scroller.ContentYBounds.min; };

            actualSize = new(Size.x + 0.15f, Size.y + 0.08f);

            return view;
        }
    }

    public abstract class ContextsHolder : AbstractGUIContext
    {
        protected IEnumerable<GUIContext> contexts;

        public ContextsHolder(GUIAlignment alignment, IEnumerable<GUIContext> contexts) : base(alignment)
        {
            this.contexts = contexts;
        }
    }

    public class VerticalContextsHolder : ContextsHolder
    {

        public VerticalContextsHolder(GUIAlignment alignment, IEnumerable<GUIContext> contexts) : base(alignment, contexts) { }
        public VerticalContextsHolder(GUIAlignment alignment, params GUIContext[] contexts) : base(alignment, contexts) { }
        public float? FixedWidth { get; init; } = null;
        internal override GameObject Instantiate(Size size, out Size actualSize)
        {
            var results = contexts.Select(c => (c.Instantiate(size, out var acSize), acSize, c)).ToArray();
            (float maxWidth, float sumHeight) = results.Aggregate((0f, 0f), (r, current) => (Math.Max(r.Item1, current.acSize.Width), r.Item2 + current.acSize.Height));
            if (FixedWidth != null) maxWidth = FixedWidth.Value;

            GameObject myObj = Helpers.CreateObject("ContextsHolder", null, UnityEngine.Vector3.zero);


            float height = sumHeight * 0.5f;
            foreach (var r in results)
            {
                if (r.Item1 != null)
                {
                    r.Item1.transform.SetParent(myObj.transform);
                    r.Item1.transform.localPosition = new UnityEngine.Vector3(CalcWidth(r.c.Alignment, r.acSize.Width, maxWidth), height - r.acSize.Height * 0.5f, 0f);
                }
                height -= r.acSize.Height;
            }

            actualSize = new(maxWidth, sumHeight);
            return myObj;
        }
    }

    public class HorizontalContextsHolder : ContextsHolder
    {

        public HorizontalContextsHolder(GUIAlignment alignment, IEnumerable<GUIContext> contexts) : base(alignment, contexts) { }
        public HorizontalContextsHolder(GUIAlignment alignment, params GUIContext[] contexts) : base(alignment, contexts) { }
        public float? FixedHeight { get; init; } = null;

        internal override GameObject Instantiate(Size size, out Size actualSize)
        {
            var results = contexts.Select(c => (c.Instantiate(size, out var acSize), acSize, c)).ToArray();
            (float sumWidth, float maxHeight) = results.Aggregate((0f, 0f), (r, current) => (r.Item1 + current.acSize.Width, Math.Max(r.Item2, current.acSize.Height)));
            if (FixedHeight != null) maxHeight = FixedHeight.Value;

            GameObject myObj = Helpers.CreateObject("ContextsHolder", null, UnityEngine.Vector3.zero);


            float width = -sumWidth * 0.5f;
            foreach (var r in results)
            {
                if (r.Item1 != null)
                {
                    r.Item1.transform.SetParent(myObj.transform);
                    r.Item1.transform.localPosition = new UnityEngine.Vector3(width + r.acSize.Width * 0.5f, CalcHeight(r.c.Alignment, r.acSize.Height, maxHeight), 0f);
                }
                width += r.acSize.Width;
            }

            actualSize = new(sumWidth, maxHeight);
            return myObj;
        }
    }

    public class TORGUIManager : MonoBehaviour
    {
        static public TORGUIManager Instance { get; private set; } = null!;

        //テキスト情報表示
        private MouseOverPopup mouseOverPopup = null!;

        static TORGUIManager()
        {
            ClassInjector.RegisterTypeInIl2Cpp<TORGUIManager>();
        }

        public void Awake()
        {
            Instance = this;
            gameObject.layer = LayerMask.NameToLayer("UI");

            mouseOverPopup = Helpers.CreateObject<MouseOverPopup>("MouseOverPopup", transform, Vector3.zero);
        }

        public void HideHelpContext() => mouseOverPopup.SetContext(null, null);
        public void HideHelpContextIf(PassiveUiElement related)
        {
            if (HelpRelatedObject == related) mouseOverPopup.SetContext(null, null);
        }
        public void SetHelpContext(PassiveUiElement related, IMetaContextOld context) => mouseOverPopup.SetContextOld(related, context);
        public void SetHelpContext(PassiveUiElement related, GUIContext context) => mouseOverPopup.SetContext(related, context);
        public void SetHelpContext(PassiveUiElement related, string rawText)
        {
            if (rawText != null)
            {
                SetHelpContext(related, new MetaContextOld.VariableText(TextAttribute.ContentAttr) { Alignment = IMetaContextOld.AlignmentOption.Left, RawText = rawText });
            }
        }
        public PassiveUiElement HelpRelatedObject => mouseOverPopup.RelatedObject;
    }

    public class TORGUIContextEngine : GUI
    {
        public static TORGUIContextEngine Instance { get; private set; } = new();
        public static GUI API => Instance;

        private Dictionary<AttributeParams, TextAttributes> allAttr = new();
        private Dictionary<AttributeAsset, TextAttributes> allAttrAsset = new();

        public GUIContext EmptyContext => GUIEmptyContext.Default;

        public TextAttributes GetAttribute(AttributeParams attribute)
        {
            if (allAttr.TryGetValue(attribute, out var attr))
            {
                return attr;
            }
            else
            {
                var isFlexible = attribute.HasFlag((AttributeParams)AttributeTemplateFlag.IsFlexible);
                var newAttr = GenerateAttribute(attribute, new Color(255, 255, 255), new FontSize(2.2f, 1.2f, 2.5f), new Size(isFlexible ? 10f : 3f, isFlexible ? 10f : 0.5f));
                allAttr[attribute] = newAttr;
                return newAttr;
            }
        }

        public TextAttributes GetAttribute(AttributeAsset attribute)
        {
            if (!allAttrAsset.TryGetValue(attribute, out var attr))
            {
                allAttrAsset[attribute] = attribute switch
                {
                    AttributeAsset.OblongHeader => new TextAttributes(TextAlignment.Left, GetFont(FontAsset.Oblong), FontStyle.Normal, new(5.2f, false), new(0.45f, 3f), new(255, 255, 255), true),
                    AttributeAsset.StandardMediumMasked => new TextAttributes(TextAlignment.Center, GetFont(FontAsset.Gothic), FontStyle.Bold, new(1.6f, 0.8f, 1.6f), new(1.45f, 0.3f), new(255, 255, 255), false),
                    AttributeAsset.StandardLargeWideMasked => new TextAttributes(TextAlignment.Center, GetFont(FontAsset.Gothic), FontStyle.Bold, new(1.7f, 1f, 1.7f), new(2.9f, 0.45f), new(255, 255, 255), false),
                    AttributeAsset.OverlayContent => new TextAttributes(Instance.GetAttribute(AttributeParams.StandardBaredLeft)) { FontSize = new(1.5f, 1.1f, 1.5f), Size = new(5f, 6f) },
                    AttributeAsset.OverlayTitle => new TextAttributes(Instance.GetAttribute(AttributeParams.StandardBaredBoldLeft)) { FontSize = new(1.8f) },
                    AttributeAsset.MetaRoleButton => new TextAttributes(TextAlignment.Center, GetFont(FontAsset.GothicMasked), FontStyle.Bold, new(1.8f, 1f, 2f), new(1.4f, 0.26f), new(255, 255, 255), false),
                    AttributeAsset.DocumentTitle => new TextAttributes(Instance.GetAttribute(AttributeParams.StandardBoldLeft)) { FontSize = new(2.2f, 0.6f, 2.2f), Size = new(5f, 6f) },
                    AttributeAsset.DocumentStandard => new TextAttributes(Instance.GetAttribute(AttributeParams.StandardLeft)) { FontSize = new(1.2f, 0.6f, 1.2f), Size = new(7f, 6f) },
                    _ => null!
                };
            }

            return allAttrAsset[attribute];
        }

        public GUIContext Arrange(GUIAlignment alignment, IEnumerable<GUIContext> inner, int perLine)
        {
            List<GUIContext> widgets = new();
            List<GUIContext> horizontalWidgets = new();
            foreach (var widget in inner)
            {
                if (widget == null) continue;

                horizontalWidgets.Add(widget);
                if (horizontalWidgets.Count == perLine)
                {
                    widgets.Add(HorizontalHolder(alignment, horizontalWidgets.ToArray()));
                    horizontalWidgets.Clear();
                }
            }
            if (horizontalWidgets.Count > 0) widgets.Add(HorizontalHolder(alignment, horizontalWidgets));

            return VerticalHolder(alignment, widgets);
        }

        public TextAttributes GenerateAttribute(AttributeParams attribute, Color color, FontSize fontSize, Size size)
        {
            TextAlignment alignment =
                ((AttributeTemplateFlag)attribute & AttributeTemplateFlag.AlignmentMask) switch
                {
                    AttributeTemplateFlag.AlignmentLeft => TextAlignment.Left,
                    AttributeTemplateFlag.AlignmentRight => TextAlignment.Right,
                    _ => TextAlignment.Center
                };
            Font font = GetFont(
                ((AttributeTemplateFlag)attribute & (AttributeTemplateFlag.FontMask | AttributeTemplateFlag.MaterialMask)) switch
                {
                    AttributeTemplateFlag.FontStandard | AttributeTemplateFlag.MaterialBared => FontAsset.Gothic,
                    AttributeTemplateFlag.FontStandard => FontAsset.GothicMasked,
                    AttributeTemplateFlag.FontOblong | AttributeTemplateFlag.MaterialBared => FontAsset.Oblong,
                    AttributeTemplateFlag.FontOblong => FontAsset.OblongMasked,
                    _ => FontAsset.GothicMasked,
                }
                );

            FontStyle style = 0;
            if (((AttributeTemplateFlag)attribute & AttributeTemplateFlag.StyleBold) != 0) style |= FontStyle.Bold;

            bool isFlexible = ((AttributeTemplateFlag)attribute & AttributeTemplateFlag.IsFlexible) != 0;
            return new TextAttributes(alignment, font, style, fontSize, size, color, isFlexible);
        }

        public Font GetFont(FontAsset font)
        {
            return font switch
            {
                FontAsset.Prespawn => new StaticFont(null, VanillaAsset.PreSpawnFont),
                FontAsset.Barlow => new StaticFont(null, VanillaAsset.VersionFont),
                FontAsset.Gothic => new StaticFont(null, VanillaAsset.StandardTextPrefab.font),
                FontAsset.GothicMasked => new StaticFont(VanillaAsset.StandardMaskedFontMaterial, VanillaAsset.StandardTextPrefab.font),
                FontAsset.Oblong => new StaticFont(null, VanillaAsset.BrookFont),
                FontAsset.OblongMasked => new StaticFont(VanillaAsset.OblongMaskedFontMaterial, VanillaAsset.BrookFont),
                _ => new StaticFont(null, VanillaAsset.StandardTextPrefab.font),
            };
        }

        public GUIContext HorizontalHolder(GUIAlignment alignment, IEnumerable<GUIContext> inner, float? fixedHeight = null) => new HorizontalContextsHolder(alignment, inner) { FixedHeight = fixedHeight };

        public GUIContext VerticalHolder(GUIAlignment alignment, IEnumerable<GUIContext> inner, float? fixedWidth = null) => new VerticalContextsHolder(alignment, inner) { FixedWidth = fixedWidth };

        public GUIContext Image(GUIAlignment alignment, Image image, FuzzySize size) => new TORGUIImage(alignment, image, size);

        public GUIContext ScrollView(GUIAlignment alignment, Size size, string scrollerTag, GUIContext inner, out Artifact<GUIScreen> artifact)
        {
            var result = new GUIScrollView(alignment, size.ToUnityVector(), inner) { ScrollerTag = scrollerTag, WithMask = true };
            artifact = result.Artifact;
            return result;
        }

        public GUIContext LocalizedButton(GUIAlignment alignment, TextAttributes attribute, string translationKey, Action onClick, Action onMouseOver = null, Action onMouseOut = null, Action onRightClick = null, Color? color = null, Color? selectedColor = null)
            => new GUIButton(alignment, attribute, new TranslateTextComponent(translationKey)) { OnClick = onClick, OnMouseOver = onMouseOver, OnMouseOut = onMouseOut, OnRightClick = onRightClick, Color = color, SelectedColor = selectedColor };
        public GUIContext RawButton(GUIAlignment alignment, TextAttributes attribute, string rawText, Action onClick, Action onMouseOver = null, Action onMouseOut = null, Action onRightClick = null, Color? color = null, Color? selectedColor = null)
            => new GUIButton(alignment, attribute, new RawTextComponent(rawText)) { OnClick = onClick, OnMouseOver = onMouseOver, OnMouseOut = onMouseOut, OnRightClick = onRightClick, Color = color, SelectedColor = selectedColor };
        public GUIContext Button(GUIAlignment alignment, TextAttributes attribute, TextComponent text, Action onClick, Action onMouseOver = null, Action onMouseOut = null, Action onRightClick = null, Color? color = null, Color? selectedColor = null)
            => new GUIButton(alignment, attribute, text) { OnClick = onClick, OnMouseOver = onMouseOver, OnMouseOut = onMouseOut, OnRightClick = onRightClick, Color = color, SelectedColor = selectedColor };


        public GUIContext LocalizedText(GUIAlignment alignment, TextAttributes attribute, string translationKey) => new TORGUIText(alignment, attribute, new TranslateTextComponent(translationKey));

        public GUIContext RawText(GUIAlignment alignment, TextAttributes attribute, string rawText) => new TORGUIText(alignment, attribute, new RawTextComponent(rawText));

        public GUIContext Text(GUIAlignment alignment, TextAttributes attribute, TextComponent text) => new TORGUIText(alignment, attribute, text);

        public GUIContext Margin(FuzzySize margin) => new TORGUIMargin(GUIAlignment.Center, new(margin.Width ?? 0f, margin.Height ?? 0f));

        public TextComponent TextComponent(Color color, string transrationKey) => new ColorTextComponent(color, new TranslateTextComponent(transrationKey));
        public TextComponent RawTextComponent(string rawText) => new RawTextComponent(rawText);
        public TextComponent LocalizedTextComponent(string translationKey) => new TranslateTextComponent(translationKey);
        public TextComponent ColorTextComponent(Color color, TextComponent component) => new ColorTextComponent(color, component);

    }

    public class GUIEmptyContext : AbstractGUIContext
    {
        static public GUIEmptyContext Default = new();

        public GUIEmptyContext(GUIAlignment alignment = GUIAlignment.Left) : base(alignment)
        {
        }

        internal override GameObject Instantiate(Size size, out Size actualSize)
        {
            actualSize = new(0f, 0f);
            return null;
        }
    }

    public class TORGUIImage : AbstractGUIContext
    {
        protected Image Image;
        protected FuzzySize Size;
        public Color? Color = null;
        public bool IsMasked { get; init; }

        public TORGUIImage(GUIAlignment alignment, Image image, FuzzySize size, Color? color = null) : base(alignment)
        {
            this.Image = image;
            this.Size = size;
            this.Color = color;
        }

        internal override GameObject Instantiate(Size size, out Size actualSize)
        {
            var renderer = Helpers.CreateObject<SpriteRenderer>("Image", null, UnityEngine.Vector3.zero, LayerMask.NameToLayer("UI"));
            renderer.sprite = Image.GetSprite();
            renderer.sortingOrder = 10;
            if (IsMasked) renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            var spriteSize = renderer.sprite.bounds.size;
            float scale = Math.Min(
                Size.Width.HasValue ? (Size.Width.Value / spriteSize.x) : float.MaxValue,
                Size.Height.HasValue ? (Size.Height.Value / spriteSize.y) : float.MaxValue
                );
            renderer.transform.localScale = UnityEngine.Vector3.one * scale;

            if (Color != null) renderer.color = Color.Value;

            actualSize = new(spriteSize.x * scale, spriteSize.y * scale);

            return renderer.gameObject;
        }
    }

    public class GUIButton : TORGUIText
    {
        public Action OnClick { get; init; }
        public Action OnRightClick { get; init; }
        public Action OnMouseOver { get; init; }
        public Action OnMouseOut { get; init; }
        public Color? Color { get; init; }
        public Color? SelectedColor { get; init; }

        public string RawText { init { Text = new RawTextComponent(value); } }
        public string TranslationKey { init { Text = new TranslateTextComponent(value); } }
        public bool AsMaskedButton { get; init; }
        public float TextMargin { get; init; } = 0.26f;

        public GUIButton(GUIAlignment alignment, TextAttributes attribute, TextComponent text) : base(alignment, attribute, text)
        {
            Attr = attribute;
            AsMaskedButton = attribute.Font.FontMaterial != null;
        }


        internal override GameObject Instantiate(Size size, out Size actualSize)
        {
            var inner = base.Instantiate(size, out actualSize)!;

            var button = Helpers.CreateObject<SpriteRenderer>("Button", null, UnityEngine.Vector3.zero, LayerMask.NameToLayer("UI"));
            button.sprite = VanillaAsset.TextButtonSprite;
            button.drawMode = SpriteDrawMode.Sliced;
            button.tileMode = SpriteTileMode.Continuous;
            button.size = actualSize.ToUnityVector() + new UnityEngine.Vector2(TextMargin * 0.84f, TextMargin * 0.84f);
            if (AsMaskedButton) button.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            inner.transform.SetParent(button.transform);
            inner.transform.localPosition += new UnityEngine.Vector3(0, 0, -0.05f);

            var collider = button.gameObject.AddComponent<BoxCollider2D>();
            collider.size = actualSize.ToUnityVector() + new UnityEngine.Vector2(TextMargin * 0.6f, TextMargin * 0.6f);
            collider.isTrigger = true;

            var passiveButton = button.gameObject.SetUpButton(true, button, Color, SelectedColor);
            if (OnClick != null) passiveButton.OnClick.AddListener(OnClick);
            if (OnMouseOut != null) passiveButton.OnMouseOut.AddListener(OnMouseOut);
            if (OnMouseOver != null) passiveButton.OnMouseOver.AddListener(OnMouseOver);

            if (OnRightClick != null) passiveButton.gameObject.AddComponent<ExtraPassiveBehaviour>().OnRightClicked += OnRightClick;

            actualSize.Width += TextMargin + 0.1f;
            actualSize.Height += TextMargin + 0.1f;

            return button.gameObject;
        }
    }

    public class ExtraPassiveBehaviour : MonoBehaviour
    {
        static ExtraPassiveBehaviour() => ClassInjector.RegisterTypeInIl2Cpp<ExtraPassiveBehaviour>();

        private PassiveUiElement myElement = null!;

        public void Start()
        {
            myElement = gameObject.GetComponent<PassiveUiElement>();
        }

        public void Update()
        {
            if (PassiveButtonManager.Instance.Buttons.Contains(myElement))
            {
                OnPiled?.Invoke();

                if (Input.GetKeyUp(KeyCode.Mouse1)) OnRightClicked?.Invoke();
            }
        }

        public Action OnPiled;
        public Action OnRightClicked;
    }

    public class TORGUIMargin : AbstractGUIContext
    {
        protected UnityEngine.Vector2 margin;

        public TORGUIMargin(GUIAlignment alignment, UnityEngine.Vector2 margin) : base(alignment)
        {
            this.margin = margin;
        }

        internal override GameObject Instantiate(Size size, out Size actualSize)
        {
            actualSize = new(margin);
            return null;
        }
    }

    public class TORGUIText : AbstractGUIContext
    {
        protected TextAttributes Attr;
        protected TextComponent Text;
        public GUIContextSupplier OverlayContext { get; init; } = null;
        public (Action action, bool reopenOverlay)? OnClickText { get; init; } = null;
        virtual protected bool AllowGenerateCollider => true;
        public TORGUIText(GUIAlignment alignment, TextAttributes attribute, TextComponent text) : base(alignment)
        {
            Attr = attribute;
            Text = text;
        }

        private void ReflectMyAttribute(TMPro.TextMeshPro text, float width)
        {
            text.color = Attr.Color;
            text.alignment = (TMPro.TextAlignmentOptions)Attr.Alignment;
            text.fontStyle = (TMPro.FontStyles)Attr.Style;
            text.fontSize = Attr.FontSize.FontSizeDefault;
            text.fontSizeMin = Attr.FontSize.FontSizeMin;
            text.fontSizeMax = Attr.FontSize.FontSizeMax;
            text.enableAutoSizing = Attr.FontSize.AllowAutoSizing;
            text.rectTransform.sizeDelta = new(Math.Min(width, Attr.Size.Width), Attr.Size.Height);
            text.rectTransform.anchorMin = new UnityEngine.Vector2(0.5f, 0.5f);
            text.rectTransform.anchorMax = new UnityEngine.Vector2(0.5f, 0.5f);
            text.rectTransform.pivot = new UnityEngine.Vector2(0.5f, 0.5f);
            if (Attr.Font != null)
            {
                text.font = Attr.Font.Font;
                if (Attr.Font.FontMaterial != null) text.fontMaterial = Attr.Font.FontMaterial;
            }
        }

        internal override GameObject Instantiate(Size size, out Size actualSize)
        {
            if (Text == null)
            {
                actualSize = new(0f, 0f);
                return null;
            }

            var text = UnityEngine.Object.Instantiate(VanillaAsset.StandardTextPrefab, null);
            text.transform.localPosition = new UnityEngine.Vector3(0f, 0f, 0f);

            ReflectMyAttribute(text, size.Width);
            text.text = Text.GetString();
            text.sortingOrder = 10;

            text.ForceMeshUpdate();

            if (Attr.IsFlexible)
            {
                float prefferedWidth = Math.Min(text.rectTransform.sizeDelta.x, text.preferredWidth);
                float prefferedHeight = Math.Min(text.rectTransform.sizeDelta.y, text.preferredHeight);
                text.rectTransform.sizeDelta = new(prefferedWidth, prefferedHeight);

                text.ForceMeshUpdate();
            }

            if (AllowGenerateCollider && (OverlayContext != null || OnClickText != null))
            {
                var button = text.gameObject.SetUpButton(false);
                var collider = text.gameObject.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = text.rectTransform.sizeDelta;

                if (OverlayContext != null)
                {
                    button.OnMouseOver.AddListener((Action)(() => TORGUIManager.Instance.SetHelpContext(button, OverlayContext())));
                    button.OnMouseOut.AddListener((Action)(() => TORGUIManager.Instance.HideHelpContextIf(button)));
                }
                if (OnClickText != null)
                {
                    button.OnClick.AddListener((Action)(() =>
                    {
                        OnClickText.Value.action.Invoke();
                        if (OnClickText.Value.reopenOverlay)
                        {
                            button.OnMouseOut.Invoke();
                            button.OnMouseOver.Invoke();
                        }
                    }));
                }
            }

            actualSize = new Size(text.rectTransform.sizeDelta);
            return text.gameObject;
        }
    }

    public class GUIFixedView : AbstractGUIContext
    {
        public class InnerScreen : GUIScreen
        {
            public bool IsValid => screen;

            private GameObject screen;
            private Size innerSize;
            private Anchor myAnchor;

            public InnerScreen(GameObject screen, Size innerSize)
            {
                this.screen = screen;
                this.innerSize = innerSize;
                this.myAnchor = new(new(0f, 1f), new(-innerSize.Width * 0.5f, innerSize.Height * 0.5f, -0.01f));
            }

            public void SetContext(GUIContext widget, out Size actualSize)
            {
                screen.ForEachChild((Il2CppSystem.Action<GameObject>)(obj => GameObject.Destroy(obj)));

                if (widget != null)
                {
                    var obj = widget.Instantiate(myAnchor, innerSize, out actualSize);
                    if (obj != null) obj.transform.SetParent(screen.transform, false);
                }
                else
                {
                    actualSize = new Size(0f, 0f);
                }
            }
        }

        public UnityEngine.Vector2 Size { get; init; }
        public bool WithMask { get; init; } = true;

        internal ListArtifact<InnerScreen> InnerArtifact { get; private init; }
        public Artifact<GUIScreen> Artifact { get; private init; }

        public GUIContextSupplier Inner { get; init; } = null;

        public GUIFixedView(GUIAlignment alignment, UnityEngine.Vector2 size, GUIContextSupplier inner) : base(alignment)
        {
            this.Size = size;

            this.InnerArtifact = new();
            this.Artifact = new GeneralizedArtifact<GUIScreen, InnerScreen>(InnerArtifact);
            this.Inner = inner;
        }


        internal override GameObject Instantiate(Size size, out Size actualSize)
        {
            var view = Helpers.CreateObject("FixedView", null, new UnityEngine.Vector3(0f, 0f, 0f), LayerMask.NameToLayer("UI"));
            var inner = Helpers.CreateObject("Inner", view.transform, new UnityEngine.Vector3(-0.2f, 0f, -0.1f));
            var innerSize = Size - new UnityEngine.Vector2(0.4f, 0f);

            if (WithMask)
            {
                view.AddComponent<SortingGroup>();
                var mask = Helpers.CreateObject<SpriteMask>("Mask", view.transform, new UnityEngine.Vector3(-0.2f, 0, 0));
                mask.sprite = VanillaAsset.FullScreenSprite;
                mask.transform.localScale = innerSize;
            }

            var innerScreen = new InnerScreen(inner, new(innerSize));
            InnerArtifact.Values.Add(innerScreen);

            innerScreen.SetContext(Inner?.Invoke(), out var innerActualSize);
            float height = innerActualSize.Height;

            actualSize = new(Size.x + 0.15f, Size.y + 0.08f);

            return view;
        }
    }
}
