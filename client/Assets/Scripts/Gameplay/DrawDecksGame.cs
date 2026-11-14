using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DrawDecks.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DrawDecks.Client.Gameplay
{
    /// <summary>
    /// Runtime-built first playable client. It deliberately uses the shared domain
    /// package so local play exercises the same rules engine as the server.
    /// </summary>
    public sealed class DrawDecksGame : MonoBehaviour
    {
        private const int MaxSeats = 4;
        private const float DefaultTurnSeconds = 90f;
        private static bool installed;

        private Canvas canvas;
        private Font uiFont;
        private AudioSource audioSource;
        private AudioClip clickClip;
        private AudioClip playClip;
        private Transform screenRoot;
        private Transform pageBody;

        private CardCatalog catalog;
        private MatchState match;
        private string selectedCardId;
        private string selectedTargetId;
        private Suit selectedSuit = Suit.Hearts;
        private string notice = "Select a card, draw, or end your turn.";
        private string lastTurnPlayerId;
        private string handoffAcknowledgedId;
        private bool cpuRoutineRunning;
        private int matchGeneration;
        private bool hasDrawnThisTurn;
        private bool paused;
        private bool resultsRecorded;
        private float turnTimeSeconds = DefaultTurnSeconds;
        private float turnTimeRemaining = DefaultTurnSeconds;
        private Text timerText;

        private int playerCount = 2;
        private int rulesetIndex;
        private int gameModeIndex;
        private int deckIndex;
        private int timerIndex;
        private string setupMode = "Local Multiplayer";
        private readonly string[] defaultNames = { "Player 1", "Player 2", "Player 3", "Player 4" };
        private readonly string[] slotNames = { "Player 1", "Player 2", "Player 3", "Player 4" };
        private readonly SeatKind[] slotKinds = { SeatKind.Human, SeatKind.Cpu, SeatKind.Cpu, SeatKind.Cpu };
        private readonly CpuDifficulty[] slotDifficulties = { CpuDifficulty.Normal, CpuDifficulty.Normal, CpuDifficulty.Normal, CpuDifficulty.Normal };
        private readonly InputField[] nameInputs = new InputField[MaxSeats];
        private readonly Text[] kindLabels = new Text[MaxSeats];
        private readonly Text[] difficultyLabels = new Text[MaxSeats];
        private readonly Transform[] setupSeatRows = new Transform[MaxSeats];
        private readonly Dictionary<string, CpuDifficulty> cpuDifficulties = new Dictionary<string, CpuDifficulty>(StringComparer.Ordinal);
        private readonly CpuDifficulty[] lastCpuDifficulties = { CpuDifficulty.Normal, CpuDifficulty.Normal, CpuDifficulty.Normal, CpuDifficulty.Normal };
        private int cpuDelayIndex = 1;
        private int cpuBudgetIndex = 1;
        private Text playerCountLabel;
        private Text rulesetLabel;
        private Text gameModeLabel;
        private Text deckLabel;
        private Text timerChoiceLabel;
        private Text cpuDelayLabel;
        private Text cpuBudgetLabel;
        private string activeDeckProfileId;
        private string activeRulesProfileId;
        private readonly Dictionary<string, int> deckEditorCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        private InputField deckProfileNameInput;
        private InputField rulesProfileNameInput;
        private InputField targetScoreInput;
        private InputField startingHandInput;
        private InputField maximumHandInput;
        private InputField maximumPlayersInput;
        private InputField startingHealthInput;
        private InputField damagePerHitInput;
        private InputField maximumWavesInput;
        private InputField survivalRewardInput;
        private Text rulesModeLabel;
        private string rulesEditorMode = "CREATE";

        private SeatDefinition[] lastSeats;
        private Ruleset lastRules;
        private string[] lastDeckIds;

        private int wins;
        private int losses;
        private int matchesPlayed;
        private int survivalBestScore;

        private static readonly Color Background = new Color(0.035f, 0.055f, 0.085f, 1f);
        private static readonly Color Panel = new Color(0.075f, 0.105f, 0.15f, 1f);
        private static readonly Color PanelRaised = new Color(0.11f, 0.15f, 0.205f, 1f);
        private static readonly Color Accent = new Color(0.24f, 0.72f, 0.66f, 1f);
        private static readonly Color AccentDim = new Color(0.13f, 0.39f, 0.38f, 1f);
        private static readonly Color Gold = new Color(0.93f, 0.72f, 0.34f, 1f);
        private static readonly Color TextPrimary = new Color(0.94f, 0.96f, 0.98f, 1f);
        private static readonly Color TextMuted = new Color(0.65f, 0.71f, 0.78f, 1f);
        private static readonly Color Danger = new Color(0.73f, 0.25f, 0.28f, 1f);
        private static readonly Color Legal = new Color(0.18f, 0.47f, 0.35f, 1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (installed || FindObjectOfType<DrawDecksGame>() != null) return;
            installed = true;
            var host = new GameObject("Draw Decks - Game Runtime");
            DontDestroyOnLoad(host);
            host.AddComponent<DrawDecksGame>();
        }

        private void Awake()
        {
            Application.runInBackground = true;
            catalog = CardCatalog.CreateDefault();
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            clickClip = CreateTone("ui-click", 620f, 0.055f);
            playClip = CreateTone("card-play", 390f, 0.11f);
            EnsureEventSystem();
            BuildCanvas();
        }

        private void Start()
        {
            LoadStatistics();
            activeDeckProfileId = PlayerPrefs.GetString("DrawDecks.ActiveDeckProfile", string.Empty);
            activeRulesProfileId = PlayerPrefs.GetString("DrawDecks.ActiveRulesProfile", string.Empty);
            var activeRules = LocalCustomizationStore.LoadRules().FirstOrDefault(item => item.id == activeRulesProfileId);
            if (activeRules != null) gameModeIndex = Mathf.Clamp(activeRules.gameMode, 0, 4);
            timerIndex = PlayerPrefs.GetInt("DrawDecks.DefaultTimer", 1) == 1 ? 0 : 2;
            ShowMainMenu();
        }

        private void OnDestroy()
        {
            installed = false;
        }

        private void Update()
        {
            if (match == null || match.Status != MatchStatus.Active || paused || cpuRoutineRunning) return;
            if (match.CurrentPlayer.Kind != SeatKind.Human || string.IsNullOrEmpty(handoffAcknowledgedId) ||
                handoffAcknowledgedId != match.CurrentPlayer.Id || turnTimeSeconds <= 0f) return;

            turnTimeRemaining -= Time.unscaledDeltaTime;
            if (timerText != null)
                timerText.text = "TURN TIMER  " + Mathf.CeilToInt(Mathf.Max(0f, turnTimeRemaining)) + "s";
            if (turnTimeRemaining <= 0f)
            {
                notice = "Time expired. Turn passed automatically.";
                if (match.PendingAttack != null)
                    ApplyResult(GameEngine.RespondToAttack(match, match.CurrentPlayer.Id, null));
                else
                    ApplyResult(GameEngine.EndTurn(match, match.CurrentPlayer.Id));
                SyncTurnFlow();
            }
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(eventSystem);
        }

        private void BuildCanvas()
        {
            var canvasObject = new GameObject("Draw Decks Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private AudioClip CreateTone(string clipName, float frequency, float duration)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            var samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float envelope = 1f - (float)i / sampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * 0.12f;
            }
            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void PlayUiSound(bool card = false)
        {
            if (PlayerPrefs.GetInt("DrawDecks.Sound", 1) == 0 || audioSource == null) return;
            audioSource.PlayOneShot(card ? playClip : clickClip);
        }

        private void BeginPage(string title, string subtitle)
        {
            if (screenRoot != null) Destroy(screenRoot.gameObject);
            var rootObject = new GameObject("Screen - " + title, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            rootObject.transform.SetParent(canvas.transform, false);
            screenRoot = rootObject.transform;
            Stretch(rootObject.GetComponent<RectTransform>());
            rootObject.GetComponent<Image>().color = Background;

            CreateText(screenRoot, title.ToUpperInvariant(), 42, Gold, TextAnchor.MiddleCenter, 88f,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(1500f, 88f), FontStyle.Bold);
            CreateText(screenRoot, subtitle, 18, TextMuted, TextAnchor.MiddleCenter, 42f,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(1600f, 42f));

            var bodyObject = new GameObject("Page Content", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            bodyObject.transform.SetParent(screenRoot, false);
            var bodyRect = bodyObject.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.07f, 0.045f);
            bodyRect.anchorMax = new Vector2(0.93f, 0.79f);
            bodyRect.offsetMin = Vector2.zero;
            bodyRect.offsetMax = Vector2.zero;
            bodyObject.GetComponent<Image>().color = Panel;
            var layout = bodyObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 18, 18);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            pageBody = bodyObject.transform;
            StartCoroutine(FadeIn(rootObject.GetComponent<CanvasGroup>()));
        }

        private IEnumerator FadeIn(CanvasGroup group)
        {
            if (group == null) yield break;
            group.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < 0.18f && group != null)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(elapsed / 0.18f);
                yield return null;
            }
            if (group != null) group.alpha = 1f;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private Text CreateText(Transform parent, string value, int size, Color color, TextAnchor alignment,
            float preferredHeight, Vector2? anchorMin = null, Vector2? anchorMax = null,
            Vector2? anchoredPosition = null, Vector2? sizeDelta = null, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text - " + value, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = uiFont;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            if (anchorMin.HasValue)
            {
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = anchorMin.Value;
                rect.anchorMax = anchorMax ?? anchorMin.Value;
                rect.anchoredPosition = anchoredPosition ?? Vector2.zero;
                rect.sizeDelta = sizeDelta ?? new Vector2(400f, preferredHeight);
            }
            else
            {
                var element = go.AddComponent<LayoutElement>();
                element.minHeight = preferredHeight;
                element.preferredHeight = preferredHeight;
                element.flexibleWidth = 1f;
            }
            return text;
        }

        private Button CreateButton(Transform parent, string label, Action action, float height = 58f,
            float preferredWidth = -1f, Color? background = null)
        {
            var go = new GameObject("Button - " + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = background ?? PanelRaised;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = background ?? PanelRaised,
                highlightedColor = new Color(0.18f, 0.27f, 0.34f, 1f),
                pressedColor = AccentDim,
                selectedColor = AccentDim,
                disabledColor = new Color(0.18f, 0.2f, 0.23f, 0.7f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleWidth = preferredWidth < 0f ? 1f : 0f;
            if (preferredWidth > 0f) element.preferredWidth = preferredWidth;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(go.transform, false);
            Stretch(labelObject.GetComponent<RectTransform>());
            var text = labelObject.GetComponent<Text>();
            text.font = uiFont;
            text.text = label;
            text.fontSize = 20;
            text.fontStyle = FontStyle.Bold;
            text.color = TextPrimary;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            button.onClick.AddListener(() =>
            {
                PlayUiSound();
                if (action != null) action();
            });
            return button;
        }

        private InputField CreateInput(Transform parent, string value, float width = 380f)
        {
            var go = new GameObject("Name Input", typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.045f, 0.07f, 0.10f, 1f);
            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = width;
            element.flexibleWidth = 1f;
            element.minHeight = 54f;
            element.preferredHeight = 54f;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 5f);
            textRect.offsetMax = new Vector2(-14f, -5f);
            var text = textGo.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = 19;
            text.color = TextPrimary;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGo.transform.SetParent(go.transform, false);
            var placeholderRect = placeholderGo.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(14f, 5f);
            placeholderRect.offsetMax = new Vector2(-14f, -5f);
            var placeholder = placeholderGo.GetComponent<Text>();
            placeholder.font = uiFont;
            placeholder.fontSize = 18;
            placeholder.fontStyle = FontStyle.Italic;
            placeholder.color = TextMuted;
            placeholder.text = "Player name";

            var input = go.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = value;
            input.characterLimit = 20;
            input.targetGraphic = go.GetComponent<Image>();
            return input;
        }

        private Transform CreateRow(Transform parent, float height = 64f, float spacing = 10f)
        {
            var go = new GameObject("Horizontal Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleWidth = 1f;
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            return go.transform;
        }

        private void ShowMainMenu()
        {
            matchGeneration++;
            cpuRoutineRunning = false;
            match = null;
            paused = false;
            BeginPage("Draw Decks", "A tactical card game. Build your table, choose your strategy, and play.");
            CreateButton(pageBody, "PLAY  ·  Configure a Match", () => ShowMatchSetup(false), 62f, background: AccentDim);
            CreateButton(pageBody, "SOLO PRACTICE  ·  1 Human vs 1 CPU", () => ShowMatchSetup(true), 58f);
            CreateButton(pageBody, "LOCAL MULTIPLAYER  ·  Hot-Seat", () => ShowMatchSetup(false), 58f);
            CreateButton(pageBody, "ONLINE MULTIPLAYER  ·  Coming in a Later Phase", ShowOnlineNotice, 58f);
            var row = CreateRow(pageBody, 58f);
            CreateButton(row, "DECK BUILDER", ShowDeckBuilder, 58f);
            CreateButton(row, "CUSTOM RULES", ShowRulesEditor, 58f);
            CreateButton(row, "COLLECTION", ShowCollection, 58f);
            CreateButton(row, "STATISTICS", ShowStatistics, 58f);
            var row2 = CreateRow(pageBody, 58f);
            CreateButton(row2, "SETTINGS", ShowSettings, 58f);
            CreateButton(row2, "SUPPORT THE PROJECT", ShowSupport, 58f);
            CreateButton(row2, "EXIT", ExitApplication, 58f, background: Danger);
            CreateText(screenRoot, "LOCAL PLAYABLE BUILD  ·  1–4 SEATS  ·  HUMAN + CPU", 14, TextMuted,
                TextAnchor.MiddleCenter, 28f, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(900f, 28f));
        }

        private void ExitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowMatchSetup(bool soloPractice)
        {
            setupMode = soloPractice ? "Solo Practice · Human vs CPU" : "Local Multiplayer · Hot-Seat";
            if (soloPractice)
            {
                playerCount = 2;
                slotKinds[0] = SeatKind.Human;
                slotKinds[1] = SeatKind.Cpu;
                slotNames[0] = "You";
                slotNames[1] = "Practice CPU";
            }
            else
            {
                playerCount = Mathf.Clamp(playerCount, 2, 4);
                slotKinds[0] = SeatKind.Human;
                if ((GameMode)gameModeIndex != GameMode.TeamBattle &&
                    (GameMode)gameModeIndex != GameMode.Survival &&
                    (GameMode)gameModeIndex != GameMode.Elimination)
                {
                    if (slotKinds[1] != SeatKind.Human) slotKinds[1] = SeatKind.Human;
                }
                slotNames[0] = string.IsNullOrWhiteSpace(slotNames[0]) ? defaultNames[0] : slotNames[0];
                slotNames[1] = string.IsNullOrWhiteSpace(slotNames[1]) ? defaultNames[1] : slotNames[1];
            }

            BeginPage("Match Setup", setupMode + "  ·  Configure 2–4 total seats, player names, control types, rules, deck, and timer.");
            var settingsRow = CreateRow(pageBody, 58f);
            playerCountLabel = CreateButton(settingsRow, "Players: " + playerCount, CyclePlayerCount, 58f, background: AccentDim)
                .GetComponentInChildren<Text>();
            rulesetLabel = CreateButton(settingsRow, RulesetDisplay(), CycleRuleset, 58f)
                .GetComponentInChildren<Text>();
            deckLabel = CreateButton(settingsRow, DeckDisplay(), CycleDeck, 58f)
                .GetComponentInChildren<Text>();
            timerChoiceLabel = CreateButton(settingsRow, TimerDisplay(), CycleTimer, 58f)
                .GetComponentInChildren<Text>();
            gameModeLabel = CreateButton(pageBody, GameModeDisplay(), CycleGameMode, 54f, background: AccentDim)
                .GetComponentInChildren<Text>();

            CreateText(pageBody, "PLAYER SEATS  ·  Tap HUMAN / CPU to change each seat", 17, Gold,
                TextAnchor.MiddleLeft, 32f, style: FontStyle.Bold);

            for (int i = 0; i < MaxSeats; i++)
            {
                int seatIndex = i;
                var row = CreateRow(pageBody, 64f);
                setupSeatRows[i] = row;
                row.gameObject.SetActive(i < playerCount);
                CreateText(row, "SEAT " + (i + 1), 16, TextMuted, TextAnchor.MiddleCenter, 54f,
                    sizeDelta: new Vector2(110f, 54f));
                nameInputs[i] = CreateInput(row, slotNames[i], 360f);
                var kindButton = CreateButton(row, KindDisplay(slotKinds[i]), () => CycleSeatKind(seatIndex), 54f, 150f,
                    slotKinds[i] == SeatKind.Human ? AccentDim : PanelRaised);
                kindLabels[i] = kindButton.GetComponentInChildren<Text>();
                var difficultyButton = CreateButton(row, DifficultyDisplay(slotKinds[i], slotDifficulties[i]),
                    () => CycleSeatDifficulty(seatIndex), 54f, 180f, PanelRaised);
                difficultyLabels[i] = difficultyButton.GetComponentInChildren<Text>();
                difficultyButton.interactable = slotKinds[i] == SeatKind.Cpu;
            }

            var aiSettingsRow = CreateRow(pageBody, 56f);
            cpuDelayLabel = CreateButton(aiSettingsRow, CpuDelayDisplay(), CycleCpuDelay, 54f)
                .GetComponentInChildren<Text>();
            cpuBudgetLabel = CreateButton(aiSettingsRow, CpuBudgetDisplay(), CycleCpuBudget, 54f)
                .GetComponentInChildren<Text>();

            CreateText(pageBody, "CPU DIFFICULTY: Easy / Normal / Hard / Expert per seat. Thinking delay and evaluation budget are configurable.", 
                14, TextMuted, TextAnchor.MiddleCenter, 34f);

            CreateText(pageBody, "Rulesets: Classic (100 points), Quick Match (50), Marathon (150). " +
                "Deck presets: Confluence Standard (52 standard + 14 effect definitions) or Classic 52. " +
                "Effect stacking is disabled in this ruleset.", 14, TextMuted, TextAnchor.MiddleCenter, 44f);
            CreateButton(pageBody, "START MATCH", StartMatchFromSetup, 66f, background: AccentDim);
            CreateButton(pageBody, "BACK TO MAIN MENU", ShowMainMenu, 52f);
        }

        private void CyclePlayerCount()
        {
            if ((GameMode)gameModeIndex == GameMode.TeamBattle)
                playerCount = 4;
            else
                playerCount = playerCount >= 4 ? 2 : playerCount + 1;
            if (playerCountLabel != null) playerCountLabel.text = "Players: " + playerCount;
            for (int i = 0; i < setupSeatRows.Length; i++)
                if (setupSeatRows[i] != null) setupSeatRows[i].gameObject.SetActive(i < playerCount);
        }

        private string GameModeDisplay()
        {
            return "MODE: " + ((GameMode)gameModeIndex).ToString().ToUpperInvariant() + "  ·  TAP TO CHANGE";
        }

        private void CycleGameMode()
        {
            gameModeIndex = (gameModeIndex + 1) % 5;
            var mode = (GameMode)gameModeIndex;
            if (mode == GameMode.TeamBattle)
            {
                playerCount = 4;
                slotKinds[0] = SeatKind.Human; slotKinds[1] = SeatKind.Cpu;
                slotKinds[2] = SeatKind.Human; slotKinds[3] = SeatKind.Cpu;
            }
            else if (mode == GameMode.Survival)
            {
                playerCount = 4;
                slotKinds[0] = SeatKind.Human;
                for (int i = 1; i < 4; i++) slotKinds[i] = SeatKind.Cpu;
            }
            else if (mode == GameMode.Elimination)
            {
                playerCount = Mathf.Clamp(playerCount, 2, 4);
                slotKinds[0] = SeatKind.Human;
                for (int i = 1; i < playerCount; i++) slotKinds[i] = SeatKind.Cpu;
            }
            if (gameModeLabel != null) gameModeLabel.text = GameModeDisplay();
            ShowMatchSetup(false);
        }

        private string RulesetDisplay()
        {
            var profile = LocalCustomizationStore.LoadRules().FirstOrDefault(item => item.id == activeRulesProfileId);
            return profile == null
                ? new[] { "Rules: Classic 100", "Rules: Quick 50", "Rules: Marathon 150" }[rulesetIndex % 3]
                : "Rules: " + profile.name;
        }

        private string DeckDisplay()
        {
            var profile = LocalCustomizationStore.LoadDecks().FirstOrDefault(item => item.id == activeDeckProfileId);
            return profile == null ? (deckIndex % 2 == 0 ? "Deck: Confluence 65" : "Deck: Classic 52") : "Deck: " + profile.name;
        }

        private string TimerDisplay()
        {
            return new[] { "Timer: 90 sec", "Timer: 60 sec", "Timer: Unlimited" }[timerIndex % 3];
        }

        private void CycleRuleset()
        {
            rulesetIndex = (rulesetIndex + 1) % 3;
            if (rulesetLabel != null) rulesetLabel.text = RulesetDisplay();
        }

        private void CycleDeck()
        {
            deckIndex = (deckIndex + 1) % 2;
            if (deckLabel != null) deckLabel.text = DeckDisplay();
        }

        private void CycleTimer()
        {
            timerIndex = (timerIndex + 1) % 3;
            if (timerChoiceLabel != null) timerChoiceLabel.text = TimerDisplay();
        }

        private static string KindDisplay(SeatKind kind)
        {
            return kind == SeatKind.Human ? "HUMAN" : "CPU";
        }

        private static string DifficultyDisplay(SeatKind kind, CpuDifficulty difficulty)
        {
            return kind == SeatKind.Cpu ? "AI: " + difficulty.ToString().ToUpperInvariant() : "AI: —";
        }

        private string CpuDelayDisplay()
        {
            return new[] { "CPU DELAY: FAST", "CPU DELAY: STANDARD", "CPU DELAY: SLOW" }[cpuDelayIndex % 3];
        }

        private string CpuBudgetDisplay()
        {
            return new[] { "AI BUDGET: FAST", "AI BUDGET: BALANCED", "AI BUDGET: DEEP" }[cpuBudgetIndex % 3];
        }

        private void CycleCpuDelay()
        {
            cpuDelayIndex = (cpuDelayIndex + 1) % 3;
            if (cpuDelayLabel != null) cpuDelayLabel.text = CpuDelayDisplay();
        }

        private void CycleCpuBudget()
        {
            cpuBudgetIndex = (cpuBudgetIndex + 1) % 3;
            if (cpuBudgetLabel != null) cpuBudgetLabel.text = CpuBudgetDisplay();
        }

        private void CycleSeatDifficulty(int index)
        {
            if (slotKinds[index] != SeatKind.Cpu) return;
            slotDifficulties[index] = (CpuDifficulty)(((int)slotDifficulties[index] + 1) % 4);
            if (difficultyLabels[index] != null)
                difficultyLabels[index].text = DifficultyDisplay(slotKinds[index], slotDifficulties[index]);
        }

        private void CycleSeatKind(int index)
        {
            slotKinds[index] = slotKinds[index] == SeatKind.Human ? SeatKind.Cpu : SeatKind.Human;
            if (kindLabels[index] != null) kindLabels[index].text = KindDisplay(slotKinds[index]);
            if (difficultyLabels[index] != null)
                difficultyLabels[index].text = DifficultyDisplay(slotKinds[index], slotDifficulties[index]);
            var row = difficultyLabels[index] == null ? null : difficultyLabels[index].GetComponentInParent<Button>();
            if (row != null) row.interactable = slotKinds[index] == SeatKind.Cpu;
        }

        private Ruleset BuildSelectedRules()
        {
            int target = rulesetIndex == 1 ? 50 : rulesetIndex == 2 ? 150 : 100;
            GameMode mode = (GameMode)gameModeIndex;
            int startingHand = 5, maximumHand = 7, maximumPlayers = mode == GameMode.TeamBattle ? 4 : playerCount;
            int startingHealth = 20, damage = 5, maximumWaves = 0, survivalReward = 10;
            bool stacking = false;

            var profile = LocalCustomizationStore.LoadRules().FirstOrDefault(item => item.id == activeRulesProfileId);
            if (profile != null)
            {
                target = profile.targetScore;
                startingHand = profile.startingHandSize;
                maximumHand = profile.maximumHandSize;
                maximumPlayers = mode == GameMode.TeamBattle ? 4 : Mathf.Clamp(profile.maximumPlayers, 2, 4);
                startingHealth = profile.startingHealth;
                damage = profile.damagePerHit;
                maximumWaves = profile.survivalMaximumWaves;
                survivalReward = profile.survivalScorePerCpu;
                stacking = profile.allowStacking;
            }
            return new Ruleset(startingHandSize: startingHand, maximumHandSize: maximumHand,
                maximumPlayers: maximumPlayers, allowStacking: stacking, targetScore: target,
                mode: mode, startingHealth: startingHealth, damagePerHit: damage,
                survivalMaximumWaves: maximumWaves, survivalScorePerCpu: survivalReward);
        }

        private string[] BuildDeckIds()
        {
            var saved = LocalCustomizationStore.LoadDecks().FirstOrDefault(item => item.id == activeDeckProfileId);
            if (saved != null && saved.cardIds != null && saved.cardIds.Length >= 10)
                return saved.cardIds.Where(id => catalog.Definitions.Any(definition => definition.Id == id)).ToArray();

            IEnumerable<CardDefinition> definitions = catalog.Definitions;
            if (deckIndex % 2 == 1)
                definitions = definitions.Where(d => d.Kind == CardKind.Standard);
            var ids = definitions.Select(d => d.Id).Where(id => id != "attack-damage").ToList();
            if ((GameMode)gameModeIndex == GameMode.Elimination ||
                (GameMode)gameModeIndex == GameMode.TeamBattle ||
                (GameMode)gameModeIndex == GameMode.Survival)
                for (int copy = 0; copy < 4; copy++) ids.Add("attack-damage");
            return ids.ToArray();
        }

        private void StartMatchFromSetup()
        {
            for (int i = 0; i < playerCount; i++)
            {
                if (nameInputs[i] != null && !string.IsNullOrWhiteSpace(nameInputs[i].text))
                    slotNames[i] = nameInputs[i].text.Trim();
                if (string.IsNullOrWhiteSpace(slotNames[i])) slotNames[i] = defaultNames[i];
            }

            if (!slotKinds.Take(playerCount).Any(k => k == SeatKind.Human))
            {
                ShowNoticePage("At least one human seat is required. Change one seat from CPU to Human.");
                return;
            }
            if (playerCount < 2 || playerCount > MaxSeats)
            {
                ShowNoticePage("A local match requires 2–4 total seats.");
                return;
            }
            if ((GameMode)gameModeIndex == GameMode.TeamBattle && playerCount != 4)
            {
                ShowNoticePage("Team Battle requires exactly four seats. Select four players.");
                return;
            }
            if ((GameMode)gameModeIndex == GameMode.Survival &&
                (slotKinds[0] != SeatKind.Human || slotKinds.Skip(1).Take(playerCount - 1).Any(kind => kind != SeatKind.Cpu)))
            {
                ShowNoticePage("Survival requires one human survivor in seat 1 and CPU opponents in all other seats.");
                return;
            }

            var seats = new List<SeatDefinition>();
            cpuDifficulties.Clear();
            for (int i = 0; i < playerCount; i++)
            {
                string seatId = "seat-" + (i + 1);
                seats.Add(new SeatDefinition(seatId, slotNames[i], slotKinds[i]));
                lastCpuDifficulties[i] = slotDifficulties[i];
                if (slotKinds[i] == SeatKind.Cpu)
                    cpuDifficulties[seatId] = slotDifficulties[i];
            }
            var rules = BuildSelectedRules();
            var deckIds = BuildDeckIds();
            CreateMatch(seats.ToArray(), rules, deckIds);
        }

        private void CreateMatch(SeatDefinition[] seats, Ruleset rules, string[] deckIds)
        {
            matchGeneration++;
            cpuRoutineRunning = false;
            try
            {
                lastSeats = seats.Select(s => new SeatDefinition(s.Id, s.DisplayName, s.Kind)).ToArray();
                lastRules = rules;
                lastDeckIds = deckIds.ToArray();
                ulong seed = unchecked((ulong)DateTime.UtcNow.Ticks);
                match = MatchFactory.Create(Guid.NewGuid().ToString("N"), seed, seats, catalog, rules, deckIds);
                selectedCardId = null;
                selectedTargetId = null;
                selectedSuit = Suit.Hearts;
                lastTurnPlayerId = null;
                handoffAcknowledgedId = null;
                cpuRoutineRunning = false;
                hasDrawnThisTurn = false;
                paused = false;
                resultsRecorded = false;
                turnTimeSeconds = timerIndex == 1 ? 60f : timerIndex == 2 ? 0f : DefaultTurnSeconds;
                turnTimeRemaining = turnTimeSeconds;
                notice = "Match started. Play a card, draw one, or pass.";
                SyncTurnFlow();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShowNoticePage("Unable to start this match: " + exception.Message);
            }
        }

        private void SyncTurnFlow()
        {
            if (match == null) return;
            if (match.Status != MatchStatus.Active)
            {
                ShowResults();
                return;
            }

            var current = match.CurrentPlayer;
            if (lastTurnPlayerId != current.Id)
            {
                handoffAcknowledgedId = null;
                lastTurnPlayerId = current.Id;
                turnTimeRemaining = turnTimeSeconds;
                hasDrawnThisTurn = false;
                selectedCardId = null;
                selectedTargetId = null;
            }

            if (current.Kind == SeatKind.Cpu)
            {
                ShowCpuThinking(current);
                if (!cpuRoutineRunning) StartCoroutine(CpuTurnRoutine());
                return;
            }

            bool hasMultipleHumans = match.Players.Count(p => p.Kind == SeatKind.Human && !p.IsEliminated) > 1;
            if (!hasMultipleHumans) handoffAcknowledgedId = current.Id;
            if (hasMultipleHumans && handoffAcknowledgedId != current.Id)
            {
                ShowHandoff(current);
                return;
            }
            RefreshGameplay();
        }

        private void ShowCpuThinking(PlayerState player)
        {
            CpuDifficulty difficulty = cpuDifficulties.TryGetValue(player.Id, out var configured)
                ? configured
                : CpuDifficulty.Normal;
            BeginPage("CPU Turn", player.DisplayName + " is considering legal actions.");
            CreateText(pageBody, player.DisplayName.ToUpperInvariant(), 34, Gold,
                TextAnchor.MiddleCenter, 76f, style: FontStyle.Bold);
            CreateText(pageBody, "DIFFICULTY  ·  " + difficulty.ToString().ToUpperInvariant(), 20,
                TextPrimary, TextAnchor.MiddleCenter, 48f);
            CreateText(pageBody, "The CPU uses only its own hand and publicly available match information.",
                16, TextMuted, TextAnchor.MiddleCenter, 56f);
            CreateButton(pageBody, "PAUSE MATCH", ShowPauseMenu, 56f);
        }

        private void ShowHandoff(PlayerState player)
        {
            BeginPage("Hot-Seat Handoff", "Private handoff. The previous player's hand is hidden until the next player is ready.");
            CreateText(pageBody, "PASS THE DEVICE TO", 20, TextMuted, TextAnchor.MiddleCenter, 44f, style: FontStyle.Bold);
            CreateText(pageBody, player.DisplayName, 38, Gold, TextAnchor.MiddleCenter, 86f, style: FontStyle.Bold);
            CreateText(pageBody, "Seat " + (match.Players.ToList().FindIndex(p => p.Id == player.Id) + 1) +
                "  ·  Your hand will be shown only after you confirm.", 18, TextPrimary, TextAnchor.MiddleCenter, 56f);
            CreateButton(pageBody, "I'M READY — SHOW MY HAND", () =>
            {
                handoffAcknowledgedId = player.Id;
                notice = player.DisplayName + "'s turn.";
                RefreshGameplay();
            }, 72f, background: AccentDim);
        }

        private void RefreshGameplay()
        {
            if (match == null || match.Status != MatchStatus.Active) return;
            var current = match.CurrentPlayer;
            BeginPage("Draw Decks  ·  Match", current.DisplayName + " is active. Select a card to inspect its legal action.");
            var topRow = CreateRow(pageBody, 52f);
            string modeStatus = match.Rules.Mode == GameMode.Survival
                ? "WAVE " + match.SurvivalWave + "  ·  SURVIVAL " + match.SurvivalScore
                : "TURN " + match.TurnNumber;
            CreateText(topRow, modeStatus, 17, Gold, TextAnchor.MiddleCenter, 48f);
            timerText = CreateText(topRow, turnTimeSeconds <= 0f ? "TURN TIMER  OFF" :
                "TURN TIMER  " + Mathf.CeilToInt(turnTimeRemaining) + "s", 17, TextPrimary, TextAnchor.MiddleCenter, 48f);
            CreateButton(topRow, "PAUSE", ShowPauseMenu, 48f, 150f);

            var scoreboard = new List<string>();
            foreach (var player in match.Players)
            {
                string status = player.IsEliminated ? "ELIMINATED" : player.Kind.ToString().ToUpperInvariant();
                string cpuLevel = player.Kind == SeatKind.Cpu && cpuDifficulties.ContainsKey(player.Id)
                    ? " / " + cpuDifficulties[player.Id].ToString().ToUpperInvariant()
                    : "";
                string health = (match.Rules.Mode == GameMode.Elimination ||
                    match.Rules.Mode == GameMode.TeamBattle || match.Rules.Mode == GameMode.Survival)
                    ? "  ·  " + player.Health + " HP" : "";
                string team = match.Rules.Mode == GameMode.TeamBattle ? "  ·  " + player.TeamId : "";
                scoreboard.Add(player.DisplayName + team + "  ·  " + status + cpuLevel + health + "  ·  " + player.Score +
                    " pts  ·  " + player.Hand.Count + " cards");
            }
            CreateText(pageBody, string.Join("     |     ", scoreboard), 17, TextPrimary,
                TextAnchor.MiddleCenter, 72f);
            if (match.Rules.Mode == GameMode.TeamBattle)
                CreateText(pageBody, "TEAM SCORE  ·  " + string.Join("     |     ",
                    match.TeamScores.Select(pair => pair.Key + "  " + pair.Value + " pts")),
                    17, Gold, TextAnchor.MiddleCenter, 36f);
            if (match.Rules.Mode == GameMode.Survival)
                CreateText(pageBody, "SURVIVAL PROGRESS  ·  " + match.CpuDefeatedThisWave +
                    " CPU OPPONENTS DEFEATED THIS WAVE", 15, Accent, TextAnchor.MiddleCenter, 32f);
            var opponentHands = match.Players.Where(p => p.Id != current.Id && !p.IsEliminated)
                .Select(p => p.DisplayName + "  ·  FACE-DOWN × " + p.Hand.Count);
            CreateText(pageBody, "OPPONENT HANDS  ·  " + string.Join("     |     ", opponentHands), 14,
                TextMuted, TextAnchor.MiddleCenter, 30f);

            var boardRow = CreateRow(pageBody, 88f);
            CreateText(boardRow, "DRAW PILE\n" + match.DrawPileCount + " cards", 18, TextPrimary,
                TextAnchor.MiddleCenter, 78f);
            var topDiscard = match.DiscardPile.Count > 0
                ? match.DiscardPile[match.DiscardPile.Count - 1].Definition.Name
                : "Empty";
            CreateText(boardRow, "DISCARD PILE\n" + topDiscard + " (" + match.DiscardPile.Count + ")", 18, Gold,
                TextAnchor.MiddleCenter, 78f);
            CreateText(boardRow, "ACTIVE SUIT\n" + (match.ActiveSuit == Suit.None ? "Any" : match.ActiveSuit.ToString()), 18,
                Accent, TextAnchor.MiddleCenter, 78f);

            if (match.PendingAttack != null)
            {
                var attacker = match.Players.FirstOrDefault(p => p.Id == match.PendingAttack.AttackerId);
                CreateText(pageBody, "ATTACK RESPONSE  ·  " + (attacker == null ? "Opponent" : attacker.DisplayName) +
                    " used " + match.PendingAttack.Effect + ". Play Shield, Reflect, or Negate — or accept the effect.",
                    18, Gold, TextAnchor.MiddleCenter, 58f);
            }
            else
            {
                CreateText(pageBody, notice, 16, TextMuted, TextAnchor.MiddleCenter, 38f);
            }

            CreateText(pageBody, current.DisplayName.ToUpperInvariant() + "  ·  YOUR HAND", 18, Gold,
                TextAnchor.MiddleLeft, 34f, style: FontStyle.Bold);
            var handRow = CreateRow(pageBody, 112f, 8f);
            int cardIndex = 0;
            foreach (var card in current.Hand)
            {
                bool legal = IsCardLegal(card);
                bool selected = selectedCardId == card.InstanceId;
                string label = card.Definition.Name + "\n" + card.Definition.Kind +
                    (card.Definition.Kind == CardKind.Standard ? " · " + card.Definition.Suit : "") +
                    "\n" + (legal ? "LEGAL" : "NOT AVAILABLE");
                Color cardColor = selected ? AccentDim : legal ? Legal : PanelRaised;
                var cardButton = CreateButton(handRow, label,
                    () =>
                    {
                        selectedCardId = card.InstanceId;
                        PlayUiSound(true);
                        RefreshGameplay();
                    }, 104f, background: cardColor);
                cardButton.GetComponentInChildren<Text>().fontSize = 15;
                StartCoroutine(CardReveal(cardButton.transform, cardIndex++ * 0.025f));
            }

            if (current.Hand.Count == 0)
                CreateText(pageBody, "Your hand is empty. Draw a card to continue.", 16, TextMuted,
                    TextAnchor.MiddleCenter, 40f);

            var actionRow = CreateRow(pageBody, 56f);
            CreateButton(actionRow, "TARGET: " + TargetDisplay(), CycleTarget, 54f, background: PanelRaised);
            CreateButton(actionRow, "WILD SUIT: " + selectedSuit, CycleSuit, 54f, background: PanelRaised);

            var controls = CreateRow(pageBody, 62f);
            if (match.PendingAttack != null)
            {
                CreateButton(controls, "PLAY SELECTED DEFENSE", RespondWithSelectedDefense, 60f, background: AccentDim);
                CreateButton(controls, "ACCEPT ATTACK", AcceptAttack, 60f, background: Danger);
            }
            else
            {
                CreateButton(controls, "PLAY SELECTED CARD", PlaySelectedCard, 60f, background: AccentDim);
                CreateButton(controls, hasDrawnThisTurn ? "DRAWN" : "DRAW ONE", DrawOne, 60f);
                CreateButton(controls, "END TURN", EndTurn, 60f);
            }

            var events = match.Events.Skip(Math.Max(0, match.Events.Count - 5))
                .Select(e => "#" + e.Sequence + "  " + e.Type + "  ·  " + PlayerName(e.PlayerId) +
                    (string.IsNullOrWhiteSpace(e.Data) ? "" : "  ·  " + e.Data));
            CreateText(pageBody, "MATCH HISTORY\n" + string.Join("\n", events), 13, TextMuted,
                TextAnchor.MiddleLeft, 84f);
        }

        private IEnumerator CardReveal(Transform card, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            if (card == null) yield break;
            card.localScale = Vector3.one * 0.92f;
            float elapsed = 0f;
            while (elapsed < 0.13f && card != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / 0.13f);
                card.localScale = Vector3.Lerp(Vector3.one * 0.92f, Vector3.one, progress);
                yield return null;
            }
            if (card != null) card.localScale = Vector3.one;
        }

        private bool IsCardLegal(CardInstance card)
        {
            if (match == null || card == null) return false;

            string playerId = match.CurrentPlayer.Id;
            if (match.PendingAttack != null)
                return GameEngine.ValidateRespondToAttack(match, playerId, card.InstanceId) == ActionError.None;

            string targetId = null;
            if (NeedsTarget(card.Definition.Effect))
                targetId = card.Definition.Effect == CardEffect.SkipTurn
                    ? NextLivingSeatId(playerId)
                    : GetSelectedTargetId();

            Suit chosenSuit = card.Definition.Effect == CardEffect.Wild ? selectedSuit : Suit.None;
            return GameEngine.ValidatePlayCard(match, playerId, card.InstanceId, targetId, chosenSuit) == ActionError.None;
        }

        private static bool NeedsTarget(CardEffect effect)
        {
            return effect == CardEffect.StealCard || effect == CardEffect.DiscardRandomCard ||
                   effect == CardEffect.BlockDraw || effect == CardEffect.Damage || effect == CardEffect.SkipTurn ||
                   effect == CardEffect.SwapHands;
        }

        private string TargetDisplay()
        {
            var target = match == null ? null : match.Players.FirstOrDefault(p => p.Id == GetSelectedTargetId());
            return target == null ? "Choose Opponent" : target.DisplayName;
        }

        private string GetSelectedTargetId()
        {
            if (match == null) return null;
            var available = match.Players.Where(p => p.Id != match.CurrentPlayer.Id && !p.IsEliminated &&
                (match.Rules.Mode != GameMode.TeamBattle || p.TeamId != match.CurrentPlayer.TeamId)).ToList();
            if (available.Count == 0) return null;
            if (!string.IsNullOrEmpty(selectedTargetId) && available.Any(p => p.Id == selectedTargetId))
                return selectedTargetId;
            selectedTargetId = available[0].Id;
            return selectedTargetId;
        }

        private void CycleTarget()
        {
            var available = match.Players.Where(p => p.Id != match.CurrentPlayer.Id && !p.IsEliminated &&
                (match.Rules.Mode != GameMode.TeamBattle || p.TeamId != match.CurrentPlayer.TeamId)).ToList();
            if (available.Count == 0) return;
            int index = available.FindIndex(p => p.Id == selectedTargetId);
            selectedTargetId = available[(index + 1) % available.Count].Id;
            RefreshGameplay();
        }

        private void CycleSuit()
        {
            selectedSuit = selectedSuit == Suit.Spades ? Suit.Clubs : (Suit)((int)selectedSuit + 1);
            RefreshGameplay();
        }

        private void PlaySelectedCard()
        {
            if (string.IsNullOrEmpty(selectedCardId))
            {
                notice = "Select a card from your hand first.";
                RefreshGameplay();
                return;
            }
            var card = match.CurrentPlayer.Hand.FirstOrDefault(c => c.InstanceId == selectedCardId);
            if (card == null)
            {
                notice = "That card is no longer in your hand.";
                RefreshGameplay();
                return;
            }
            if (!IsCardLegal(card))
            {
                notice = "Illegal action. Check the card, active suit, target, or attack-response phase.";
                RefreshGameplay();
                return;
            }

            string targetId = card.Definition.Effect == CardEffect.SkipTurn ? NextLivingSeatId(match.CurrentPlayer.Id) :
                (NeedsTarget(card.Definition.Effect) ? GetSelectedTargetId() : null);
            var result = GameEngine.PlayCard(match, match.CurrentPlayer.Id, selectedCardId, targetId,
                card.Definition.Effect == CardEffect.Wild ? selectedSuit : Suit.None);
            if (result.Success) PlayUiSound(true);
            ApplyResult(result);
            selectedCardId = null;
            SyncTurnFlow();
        }

        private void DrawOne()
        {
            if (hasDrawnThisTurn)
            {
                notice = "You have already drawn this turn.";
                RefreshGameplay();
                return;
            }
            var result = GameEngine.Draw(match, match.CurrentPlayer.Id, 1);
            if (result.Success)
            {
                hasDrawnThisTurn = true;
                notice = match.CurrentPlayer.DisplayName + " drew one card.";
            }
            else
                notice = "Draw unavailable: " + result.Error + ".";
            ApplyResult(result);
            RefreshGameplay();
        }

        private void EndTurn()
        {
            var result = GameEngine.EndTurn(match, match.CurrentPlayer.Id);
            if (result.Success) notice = "Turn passed.";
            else notice = "Unable to end turn: " + result.Error + ".";
            ApplyResult(result);
            SyncTurnFlow();
        }

        private void RespondWithSelectedDefense()
        {
            if (string.IsNullOrEmpty(selectedCardId))
            {
                notice = "Select Shield, Reflect, or Negate from your hand.";
                RefreshGameplay();
                return;
            }
            var result = GameEngine.RespondToAttack(match, match.CurrentPlayer.Id, selectedCardId);
            notice = result.Success ? "Attack response resolved." : "Defense unavailable: " + result.Error + ".";
            ApplyResult(result);
            selectedCardId = null;
            SyncTurnFlow();
        }

        private void AcceptAttack()
        {
            var result = GameEngine.RespondToAttack(match, match.CurrentPlayer.Id, null);
            notice = result.Success ? "Attack accepted and resolved." : "Unable to resolve attack: " + result.Error + ".";
            ApplyResult(result);
            SyncTurnFlow();
        }

        private void ApplyResult(ActionResult result)
        {
            if (result == null) return;
            if (!result.Success)
                Debug.LogWarning("Draw Decks action rejected: " + result.Error);
        }

        private float CpuThinkingDelaySeconds()
        {
            float delay = new[] { 0.15f, 0.65f, 1.15f }[cpuDelayIndex % 3];
            if (match != null && match.Rules.Mode == GameMode.Survival)
                delay = Mathf.Max(0.08f, delay - (match.SurvivalWave - 1) * 0.04f);
            return delay;
        }

        private CpuAiOptions BuildCpuAiOptions(string playerId, bool hasDrawn)
        {
            CpuDifficulty difficulty = cpuDifficulties.TryGetValue(playerId, out var value)
                ? value
                : CpuDifficulty.Normal;
            if (match != null && match.Rules.Mode == GameMode.Survival)
                difficulty = (CpuDifficulty)Mathf.Min((int)CpuDifficulty.Expert,
                    (int)difficulty + Mathf.Max(0, (match.SurvivalWave - 1) / 2));
            int[] budgets = { 48, 96, 192 };
            int[] depths = { 1, 2, 4 };
            return new CpuAiOptions(
                difficulty: difficulty,
                seed: match == null ? 0UL : match.Seed,
                hasDrawnThisTurn: hasDrawn,
                maximumEvaluations: budgets[cpuBudgetIndex % budgets.Length],
                planningDepth: depths[cpuBudgetIndex % depths.Length],
                useDeckCompositionEstimates: difficulty >= CpuDifficulty.Hard,
                useOpponentModeling: difficulty == CpuDifficulty.Expert);
        }

        private ActionResult ExecuteCpuDecision(string playerId, CpuAiDecision decision)
        {
            switch (decision.Action.Kind)
            {
                case CpuActionKind.Draw:
                    return GameEngine.Draw(match, playerId, 1);
                case CpuActionKind.PlayCard:
                    return GameEngine.PlayCard(match, playerId, decision.Action.CardInstanceId,
                        decision.Action.TargetPlayerId, decision.Action.ChosenSuit);
                case CpuActionKind.PlayDefense:
                    return GameEngine.RespondToAttack(match, playerId, decision.Action.CardInstanceId);
                case CpuActionKind.AcceptAttack:
                    return GameEngine.RespondToAttack(match, playerId, null);
                case CpuActionKind.EndTurn:
                    return GameEngine.EndTurn(match, playerId);
                default:
                    throw new InvalidOperationException("Unsupported CPU action: " + decision.Action.Kind);
            }
        }

        private IEnumerator CpuTurnRoutine()
        {
            int generation = matchGeneration;
            cpuRoutineRunning = true;
            yield return new WaitForSecondsRealtime(CpuThinkingDelaySeconds());

            while (paused && generation == matchGeneration)
                yield return null;

            if (generation != matchGeneration)
                yield break;

            if (match == null || match.Status != MatchStatus.Active)
            {
                cpuRoutineRunning = false;
                yield break;
            }

            var cpu = match.CurrentPlayer;
            if (cpu.Kind != SeatKind.Cpu)
            {
                cpuRoutineRunning = false;
                SyncTurnFlow();
                yield break;
            }

            try
            {
                CpuAiDecision decision = CpuAi.Decide(
                    match, cpu.Id, catalog, BuildCpuAiOptions(cpu.Id, false));

                if (decision.Action.Kind == CpuActionKind.Draw)
                {
                    var drawResult = GameEngine.Draw(match, cpu.Id, 1);
                    if (!drawResult.Success)
                        throw new InvalidOperationException("Validated CPU draw was rejected: " + drawResult.Error);

                    notice = cpu.DisplayName + " drew a card.";
                    decision = CpuAi.Decide(
                        match, cpu.Id, catalog, BuildCpuAiOptions(cpu.Id, true));
                }

                if (match != null && match.Status == MatchStatus.Active &&
                    match.CurrentPlayer.Id == cpu.Id)
                {
                    if (decision.Action.Kind == CpuActionKind.Draw)
                        throw new InvalidOperationException("CPU policy selected a second draw in one turn.");

                    var result = ExecuteCpuDecision(cpu.Id, decision);
                    if (!result.Success)
                        throw new InvalidOperationException(
                            "CPU action was rejected: " + result.Error + " / " +
                            decision.Action.Kind + " / " + decision.Reason);

                    notice = cpu.DisplayName + " (" + decision.Difficulty + ") " + decision.Reason;
                    if (decision.Action.Kind == CpuActionKind.PlayCard ||
                        decision.Action.Kind == CpuActionKind.PlayDefense)
                        PlayUiSound(true);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("CPU turn failed for " + cpu.DisplayName +
                    "; attempting a validated recovery action.");
                Debug.LogException(exception);

                if (match != null && match.Status == MatchStatus.Active &&
                    match.CurrentPlayer.Id == cpu.Id)
                {
                    ActionResult fallback = match.PendingAttack != null
                        ? GameEngine.RespondToAttack(match, cpu.Id, null)
                        : GameEngine.EndTurn(match, cpu.Id);

                    if (fallback.Success)
                    {
                        notice = cpu.DisplayName + " recovered using a safe fallback action.";
                    }
                    else
                    {
                        Debug.LogError("CPU recovery action was rejected: " + fallback.Error +
                            ". Returning to the main menu to prevent a stuck match.");
                        match = null;
                        ShowMainMenu();
                    }
                }
            }

            if (generation != matchGeneration)
                yield break;

            cpuRoutineRunning = false;
            if (match != null && match.Status == MatchStatus.Active)
                SyncTurnFlow();
        }

        private string NextLivingSeatId(string playerId)
        {
            int index = match.Players.ToList().FindIndex(p => p.Id == playerId);
            var actor = match.Players[index];
            for (int step = 1; step <= match.Players.Count; step++)
            {
                int candidate = (index + (match.TurnDirection * step) + match.Players.Count * 2) % match.Players.Count;
                var target = match.Players[candidate];
                if (!target.IsEliminated && target.Id != playerId &&
                    (match.Rules.Mode != GameMode.TeamBattle || target.TeamId != actor.TeamId))
                    return target.Id;
            }
            return null;
        }

        private string PlayerName(string playerId)
        {
            var player = match == null ? null : match.Players.FirstOrDefault(p => p.Id == playerId);
            return player == null ? playerId : player.DisplayName;
        }

        private void ShowPauseMenu()
        {
            paused = true;
            BeginPage("Match Paused", "The match state is preserved. Resume to continue the current turn.");
            CreateText(pageBody, "CURRENT PLAYER  ·  " + (match == null ? "—" : match.CurrentPlayer.DisplayName),
                20, Gold, TextAnchor.MiddleCenter, 58f);
            CreateButton(pageBody, "RESUME MATCH", () =>
            {
                paused = false;
                SyncTurnFlow();
            }, 66f, background: AccentDim);
            CreateButton(pageBody, "RETURN TO MAIN MENU", () =>
            {
                paused = false;
                match = null;
                ShowMainMenu();
            }, 60f, background: Danger);
        }

        private void ShowResults()
        {
            if (resultsRecorded) return;
            resultsRecorded = true;
            matchesPlayed++;
            if (match.Rules.Mode == GameMode.Survival)
            {
                var survivor = match.Players.FirstOrDefault(player => player.Kind == SeatKind.Human);
                if (survivor != null)
                {
                    var leaderboardEntry = SurvivalLeaderboardEntry.FromCompletedMatch(match, survivor.Id);
                    survivalBestScore = Mathf.Max(survivalBestScore, leaderboardEntry.Score);
                }
            }
            var winner = match.WinnerPlayerId == null ? null : match.Players.FirstOrDefault(p => p.Id == match.WinnerPlayerId);
            bool humanTeamWon = match.WinnerTeamId != null &&
                match.Players.Any(player => player.Kind == SeatKind.Human && player.TeamId == match.WinnerTeamId);
            if ((winner != null && winner.Kind == SeatKind.Human) || humanTeamWon) wins++;
            else if (winner != null || match.WinnerTeamId != null ||
                (match.Rules.Mode == GameMode.Survival && match.Status == MatchStatus.Completed)) losses++;
            SaveStatistics();

            string winnerText = match.WinnerTeamId != null ? match.WinnerTeamId.ToUpperInvariant() + " WINS" :
                winner == null ? (match.Rules.Mode == GameMode.Survival && match.Status == MatchStatus.Completed ? "SURVIVAL RUN ENDED" : "DRAW") :
                winner.DisplayName + " wins the match.";
            BeginPage("Match Results", winnerText);
            CreateText(pageBody, match.WinnerTeamId != null ? winnerText :
                winner == null ? (match.Rules.Mode == GameMode.Survival ? "SURVIVAL SCORE  ·  " + match.SurvivalScore : "DRAW") :
                winner.DisplayName.ToUpperInvariant() + "  ·  WINNER",
                32, Gold, TextAnchor.MiddleCenter, 78f, style: FontStyle.Bold);
            if (match.Rules.Mode == GameMode.Survival)
                CreateText(pageBody, "WAVES CLEARED  ·  " + match.SurvivalWave +
                    "     ·     SURVIVAL SCORE  ·  " + match.SurvivalScore +
                    "     ·     PERSONAL BEST  ·  " + survivalBestScore,
                    20, Accent, TextAnchor.MiddleCenter, 48f);
            if (match.Rules.Mode == GameMode.TeamBattle)
                CreateText(pageBody, "TEAM SCORES  ·  " + string.Join("     |     ",
                    match.TeamScores.Select(pair => pair.Key + "  " + pair.Value)),
                    18, Gold, TextAnchor.MiddleCenter, 44f);
            CreateText(pageBody, string.Join("\n", match.Players.Select(p =>
                p.DisplayName + "  ·  " + p.Score + " points  ·  " + p.Health + " HP  ·  " + p.Hand.Count + " cards remaining" +
                (p.IsEliminated ? "  ·  ELIMINATED" : ""))), 20, TextPrimary, TextAnchor.MiddleCenter, 150f);
            CreateText(pageBody, "Turns: " + match.TurnNumber + "  ·  Events: " + match.Events.Count +
                "  ·  Seed: " + match.Seed, 15, TextMuted, TextAnchor.MiddleCenter, 38f);
            CreateButton(pageBody, "REMATCH", StartRematch, 66f, background: AccentDim);
            CreateButton(pageBody, "RETURN TO MAIN MENU", ShowMainMenu, 58f);
        }

        private void StartRematch()
        {
            if (lastSeats == null || lastRules == null || lastDeckIds == null)
            {
                ShowMainMenu();
                return;
            }
            CreateMatch(lastSeats, lastRules, lastDeckIds);
        }

        private void ShowDeckBuilder()
        {
            var saved = LocalCustomizationStore.LoadDecks();
            var active = saved.FirstOrDefault(item => item.id == activeDeckProfileId);
            deckEditorCounts.Clear();
            if (active != null)
                foreach (var id in active.cardIds) deckEditorCounts[id] = deckEditorCounts.TryGetValue(id, out int count) ? count + 1 : 1;
            else
                foreach (var id in BuildDefaultDeckForEditor()) deckEditorCounts[id] = deckEditorCounts.TryGetValue(id, out int count) ? count + 1 : 1;

            BeginPage("Deck Builder", "Create a named deck, edit per-card quantities, select a saved deck, and persist it on this device.");
            var top = CreateRow(pageBody, 58f);
            deckProfileNameInput = CreateInput(top, active == null ? "My Custom Deck" : active.name, 420f);
            CreateButton(top, "SAVE DECK", SaveDeckEditor, 56f, background: AccentDim);
            CreateButton(top, "NEW", () => { activeDeckProfileId = null; ShowDeckBuilder(); }, 56f);
            int total = deckEditorCounts.Values.Sum();
            int unique = deckEditorCounts.Count(pair => pair.Value > 0);
            CreateText(pageBody, "DECK SIZE  ·  " + total + " CARDS     ·     UNIQUE DEFINITIONS  ·  " + unique +
                "     ·     TARGET 30–120 CARDS", 16, Gold, TextAnchor.MiddleCenter, 34f);

            var savedRow = CreateRow(pageBody, 54f);
            CreateText(savedRow, "SAVED DECKS", 15, TextMuted, TextAnchor.MiddleCenter, 48f, sizeDelta: new Vector2(140f, 48f));
            foreach (var profile in saved.Take(4))
                CreateButton(savedRow, profile.name, () => LoadDeckProfile(profile.id), 48f, preferredWidth: 190f);
            if (saved.Count > 0)
                CreateButton(savedRow, "DELETE ACTIVE", DeleteActiveDeck, 48f, preferredWidth: 170f, background: Danger);

            var scroll = CreateEditorScroll(pageBody, 430f);
            foreach (var definition in catalog.Definitions.OrderBy(item => item.Kind).ThenBy(item => item.Name))
            {
                var row = CreateRow(scroll, 48f, 8f);
                int quantity = deckEditorCounts.TryGetValue(definition.Id, out int count) ? count : 0;
                CreateText(row, definition.Name + "  ·  " + definition.Kind + "  ·  " + definition.Rarity,
                    14, TextPrimary, TextAnchor.MiddleLeft, 44f);
                CreateButton(row, "−", () => ChangeDeckQuantity(definition.Id, -1), 44f, 52f);
                CreateText(row, quantity.ToString(), 16, Gold, TextAnchor.MiddleCenter, 44f, sizeDelta: new Vector2(44f, 44f));
                CreateButton(row, "+", () => ChangeDeckQuantity(definition.Id, 1), 44f, 52f, background: AccentDim);
            }
            CreateButton(pageBody, "BACK TO MENU", ShowMainMenu, 50f);
        }

        private IEnumerable<string> BuildDefaultDeckForEditor()
        {
            return catalog.Definitions.Select(definition => definition.Id).Where(id => id != "attack-damage");
        }

        private Transform CreateEditorScroll(Transform parent, float height)
        {
            var root = new GameObject("Profile Editor Scroll", typeof(RectTransform), typeof(Image),
                typeof(ScrollRect), typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            root.GetComponent<Image>().color = new Color(0.045f, 0.065f, 0.09f, 1f);
            var element = root.GetComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleWidth = 1f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(root.transform, false);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Image>().enabled = false;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = root.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return content.transform;
        }

        private void ChangeDeckQuantity(string cardId, int delta)
        {
            int current = deckEditorCounts.TryGetValue(cardId, out int count) ? count : 0;
            int next = Mathf.Clamp(current + delta, 0, 4);
            if (next == 0) deckEditorCounts.Remove(cardId);
            else deckEditorCounts[cardId] = next;
            ShowDeckBuilder();
        }

        private void SaveDeckEditor()
        {
            string name = deckProfileNameInput == null ? "My Custom Deck" : deckProfileNameInput.text.Trim();
            string[] cards = deckEditorCounts.SelectMany(pair => Enumerable.Repeat(pair.Key, pair.Value)).ToArray();
            if (string.IsNullOrWhiteSpace(name) || cards.Length < 30 || cards.Length > 120)
            {
                ShowNoticePage("Deck save rejected. Enter a name and configure between 30 and 120 cards. Each catalog definition is limited to four copies.");
                return;
            }
            var profile = new LocalDeckProfile
            {
                id = string.IsNullOrWhiteSpace(activeDeckProfileId) ? Guid.NewGuid().ToString("N") : activeDeckProfileId,
                name = name,
                cardIds = cards
            };
            try
            {
                LocalCustomizationStore.SaveDeck(profile);
                activeDeckProfileId = profile.id;
                PlayerPrefs.SetString("DrawDecks.ActiveDeckProfile", activeDeckProfileId);
                PlayerPrefs.Save();
                deckIndex = 0;
                ShowNoticePage("Deck saved locally: " + profile.name + " (" + cards.Length + " cards). It is now selected for new matches.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShowNoticePage("Deck could not be saved: " + exception.Message);
            }
        }

        private void LoadDeckProfile(string id)
        {
            var profile = LocalCustomizationStore.LoadDecks().FirstOrDefault(item => item.id == id);
            if (profile == null) { ShowNoticePage("That saved deck is unavailable or invalid."); return; }
            activeDeckProfileId = profile.id;
            PlayerPrefs.SetString("DrawDecks.ActiveDeckProfile", activeDeckProfileId);
            PlayerPrefs.Save();
            ShowDeckBuilder();
        }

        private void DeleteActiveDeck()
        {
            if (string.IsNullOrWhiteSpace(activeDeckProfileId))
            {
                ShowNoticePage("Select a saved deck before deleting it.");
                return;
            }
            LocalCustomizationStore.DeleteDeck(activeDeckProfileId);
            activeDeckProfileId = null;
            PlayerPrefs.DeleteKey("DrawDecks.ActiveDeckProfile");
            PlayerPrefs.Save();
            ShowDeckBuilder();
        }

        private void ShowRulesEditor()
        {
            var saved = LocalCustomizationStore.LoadRules();
            var active = saved.FirstOrDefault(item => item.id == activeRulesProfileId);
            if (active == null)
                active = new LocalRulesProfile { id = Guid.NewGuid().ToString("N"), name = "My Custom Rules" };
            rulesEditorMode = ((GameMode)Mathf.Clamp(active.gameMode, 0, 4)).ToString().ToUpperInvariant();
            BeginPage("Custom Rules Editor", "Configure match mode, scoring, health, hand limits, seat count, and Survival progression. Values are validated before saving.");
            rulesProfileNameInput = CreateEditorField(pageBody, "PROFILE NAME", active.name);
            rulesModeLabel = CreateButton(pageBody, "GAME MODE  ·  " + rulesEditorMode + "  ·  TAP TO CYCLE",
                CycleRulesEditorMode, 54f, background: AccentDim).GetComponentInChildren<Text>();
            targetScoreInput = CreateEditorField(pageBody, "TARGET SCORE (10–1000)", active.targetScore.ToString());
            startingHandInput = CreateEditorField(pageBody, "STARTING HAND (1–20)", active.startingHandSize.ToString());
            maximumHandInput = CreateEditorField(pageBody, "MAXIMUM HAND (STARTING HAND–40)", active.maximumHandSize.ToString());
            maximumPlayersInput = CreateEditorField(pageBody, "MAXIMUM PLAYERS (2–4)", active.maximumPlayers.ToString());
            startingHealthInput = CreateEditorField(pageBody, "STARTING HEALTH (1–500)", active.startingHealth.ToString());
            damagePerHitInput = CreateEditorField(pageBody, "DAMAGE PER HIT (1–100)", active.damagePerHit.ToString());
            maximumWavesInput = CreateEditorField(pageBody, "SURVIVAL MAX WAVES (0 = ENDLESS)", active.survivalMaximumWaves.ToString());
            survivalRewardInput = CreateEditorField(pageBody, "SURVIVAL CPU REWARD (0–1000)", active.survivalScorePerCpu.ToString());
            CreateButton(pageBody, "SAVE CUSTOM RULESET", SaveRulesEditor, 62f, background: AccentDim);
            var savedRow = CreateRow(pageBody, 52f);
            foreach (var profile in saved.Take(4))
                CreateButton(savedRow, profile.name, () => LoadRulesProfile(profile.id), 48f, preferredWidth: 200f);
            if (activeRulesProfileId != null)
                CreateButton(savedRow, "DELETE ACTIVE", DeleteActiveRules, 48f, preferredWidth: 160f, background: Danger);
            CreateButton(pageBody, "BACK TO MENU", ShowMainMenu, 50f);
        }

        private InputField CreateEditorField(Transform parent, string label, string value)
        {
            var row = CreateRow(parent, 54f);
            CreateText(row, label, 14, TextMuted, TextAnchor.MiddleLeft, 50f, sizeDelta: new Vector2(370f, 50f));
            return CreateInput(row, value, 260f);
        }

        private void CycleRulesEditorMode()
        {
            int next = ((int)Enum.Parse(typeof(GameMode), rulesEditorMode, true) + 1) % 5;
            rulesEditorMode = ((GameMode)next).ToString().ToUpperInvariant();
            if (rulesModeLabel != null) rulesModeLabel.text = "GAME MODE  ·  " + rulesEditorMode + "  ·  TAP TO CYCLE";
        }

        private bool TryReadRange(InputField input, int minimum, int maximum, out int value)
        {
            value = 0;
            return input != null && int.TryParse(input.text, out value) && value >= minimum && value <= maximum;
        }

        private void SaveRulesEditor()
        {
            int target, startingHand, maximumHand, players, health, damage, waves, reward;
            bool valid = TryReadRange(targetScoreInput, 10, 1000, out target) &&
                TryReadRange(startingHandInput, 1, 20, out startingHand) &&
                TryReadRange(maximumHandInput, startingHand, 40, out maximumHand) &&
                TryReadRange(maximumPlayersInput, 2, 4, out players) &&
                TryReadRange(startingHealthInput, 1, 500, out health) &&
                TryReadRange(damagePerHitInput, 1, 100, out damage) &&
                TryReadRange(maximumWavesInput, 0, 1000, out waves) &&
                TryReadRange(survivalRewardInput, 0, 1000, out reward);
            string name = rulesProfileNameInput == null ? "" : rulesProfileNameInput.text.Trim();
            if (!valid || string.IsNullOrWhiteSpace(name))
            {
                ShowNoticePage("Ruleset save rejected. Check the profile name and every numeric range; maximum hand must be at least starting hand.");
                return;
            }
            var profile = new LocalRulesProfile
            {
                id = string.IsNullOrWhiteSpace(activeRulesProfileId) ? Guid.NewGuid().ToString("N") : activeRulesProfileId,
                name = name,
                gameMode = (int)Enum.Parse(typeof(GameMode), rulesEditorMode, true),
                targetScore = target,
                startingHandSize = startingHand,
                maximumHandSize = maximumHand,
                maximumPlayers = players,
                startingHealth = health,
                damagePerHit = damage,
                survivalMaximumWaves = waves,
                survivalScorePerCpu = reward,
                allowStacking = false
            };
            try
            {
                LocalCustomizationStore.SaveRules(profile);
                activeRulesProfileId = profile.id;
                PlayerPrefs.SetString("DrawDecks.ActiveRulesProfile", activeRulesProfileId);
                PlayerPrefs.Save();
                gameModeIndex = profile.gameMode;
                ShowNoticePage("Custom ruleset saved locally: " + profile.name + ". It is selected for new matches.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShowNoticePage("Ruleset could not be saved: " + exception.Message);
            }
        }

        private void LoadRulesProfile(string id)
        {
            var profile = LocalCustomizationStore.LoadRules().FirstOrDefault(item => item.id == id);
            if (profile == null) { ShowNoticePage("That saved ruleset is unavailable or invalid."); return; }
            activeRulesProfileId = profile.id;
            PlayerPrefs.SetString("DrawDecks.ActiveRulesProfile", activeRulesProfileId);
            PlayerPrefs.Save();
            gameModeIndex = Mathf.Clamp(profile.gameMode, 0, 4);
            ShowRulesEditor();
        }

        private void DeleteActiveRules()
        {
            if (string.IsNullOrWhiteSpace(activeRulesProfileId))
            {
                ShowNoticePage("Select a saved ruleset before deleting it.");
                return;
            }
            LocalCustomizationStore.DeleteRules(activeRulesProfileId);
            activeRulesProfileId = null;
            PlayerPrefs.DeleteKey("DrawDecks.ActiveRulesProfile");
            PlayerPrefs.Save();
            ShowRulesEditor();
        }

        private void ShowCollection()
        {
            BeginPage("Collection", "Card catalog and collection overview.");
            CreateText(pageBody, "OWNED CATALOG  ·  " + catalog.Definitions.Count + " DEFINITIONS",
                21, Gold, TextAnchor.MiddleCenter, 52f, style: FontStyle.Bold);
            var lines = catalog.Definitions.Select(d => d.Name + "  ·  " + d.Kind + "  ·  " + d.Rarity);
            CreateText(pageBody, string.Join("\n", lines), 14, TextPrimary, TextAnchor.MiddleLeft, 470f);
            CreateButton(pageBody, "BACK", ShowMainMenu, 52f);
        }

        private void ShowStatistics()
        {
            LoadStatistics();
            BeginPage("Statistics", "Local profile performance. Results are stored on this device.");
            CreateText(pageBody, "MATCHES PLAYED  ·  " + matchesPlayed, 24, Gold, TextAnchor.MiddleCenter, 58f);
            CreateText(pageBody, "HUMAN WINS  ·  " + wins, 22, TextPrimary, TextAnchor.MiddleCenter, 52f);
            CreateText(pageBody, "HUMAN LOSSES  ·  " + losses, 22, TextPrimary, TextAnchor.MiddleCenter, 52f);
            CreateText(pageBody, "SURVIVAL BEST  ·  " + survivalBestScore, 22, Gold, TextAnchor.MiddleCenter, 52f);
            CreateText(pageBody, "WIN RATE  ·  " + (matchesPlayed == 0 ? "—" :
                (100f * wins / matchesPlayed).ToString("0.0") + "%"), 22, Accent, TextAnchor.MiddleCenter, 52f);
            CreateButton(pageBody, "BACK", ShowMainMenu, 52f);
        }

        private void ShowSettings()
        {
            BeginPage("Settings", "Local client preferences.");
            int sound = PlayerPrefs.GetInt("DrawDecks.Sound", 1);
            int timer = PlayerPrefs.GetInt("DrawDecks.DefaultTimer", 1);
            CreateButton(pageBody, "SOUND EFFECTS  ·  " + (sound == 1 ? "ON" : "OFF"), () =>
            {
                PlayerPrefs.SetInt("DrawDecks.Sound", sound == 1 ? 0 : 1);
                ShowSettings();
            }, 62f);
            CreateButton(pageBody, "DEFAULT TURN TIMER  ·  " + (timer == 1 ? "90 SECONDS" : "UNLIMITED"), () =>
            {
                PlayerPrefs.SetInt("DrawDecks.DefaultTimer", timer == 1 ? 0 : 1);
                ShowSettings();
            }, 62f);
            CreateText(pageBody, "The match setup timer control overrides the default setting for that match.",
                16, TextMuted, TextAnchor.MiddleCenter, 52f);
            CreateButton(pageBody, "BACK", ShowMainMenu, 52f);
        }

        private void ShowSupport()
        {
            BeginPage("Support the Project", "Draw Decks is an independently developed game.");
            CreateText(pageBody, "Thank you for testing the local playable build. No payment processing or external donation endpoint is configured in this milestone. " +
                "Support links will be added only after an official project channel is selected.", 20, TextPrimary,
                TextAnchor.MiddleCenter, 160f);
            CreateButton(pageBody, "BACK", ShowMainMenu, 52f);
        }

        private void ShowOnlineNotice()
        {
            ShowNoticePage("Online multiplayer is not active in this milestone. Local hot-seat and CPU matches are playable. Online lobbies, authentication, chat, and authoritative network hosting are scheduled for a later phase.");
        }

        private void ShowNoticePage(string message)
        {
            BeginPage("Draw Decks", message);
            CreateText(pageBody, message, 20, TextPrimary, TextAnchor.MiddleCenter, 130f);
            CreateButton(pageBody, "MATCH SETUP", () => ShowMatchSetup(false), 58f, background: AccentDim);
            CreateButton(pageBody, "MAIN MENU", ShowMainMenu, 52f);
        }

        private void LoadStatistics()
        {
            matchesPlayed = PlayerPrefs.GetInt("DrawDecks.Matches", 0);
            wins = PlayerPrefs.GetInt("DrawDecks.Wins", 0);
            losses = PlayerPrefs.GetInt("DrawDecks.Losses", 0);
            survivalBestScore = PlayerPrefs.GetInt("DrawDecks.SurvivalBest", 0);
        }

        private void SaveStatistics()
        {
            PlayerPrefs.SetInt("DrawDecks.Matches", matchesPlayed);
            PlayerPrefs.SetInt("DrawDecks.Wins", wins);
            PlayerPrefs.SetInt("DrawDecks.Losses", losses);
            PlayerPrefs.SetInt("DrawDecks.SurvivalBest", survivalBestScore);
            PlayerPrefs.Save();
        }
    }
}