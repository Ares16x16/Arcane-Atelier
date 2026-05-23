using ArcaneAtelier.Audio;
using ArcaneAtelier.Battle;
using ArcaneAtelier.Workshop;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MainMenuManager : MonoBehaviour
{
    private const string PrologueSceneName = "PrologueScene";
    private const string WorkshopSceneName = "WorkshopScene";
    private const string MusicVolumeKey = "arcane.settings.music";
    private const string SfxVolumeKey = "arcane.settings.sfx";
    private const string FullscreenKey = "arcane.settings.fullscreen";
    private const string VsyncKey = "arcane.settings.vsync";
    private const string TargetFpsKey = "arcane.settings.target_fps";
    private const float HeaderAccentHeight = 3f;
    private const float SpriteBobFrequency = 1.35f;

    [SerializeField] private Texture2D battleBackdropTexture;
    [SerializeField] private Texture2D playerCharacterTexture;
    [SerializeField] private Texture2D bossCharacterTexture;
    [SerializeField] private Texture2D logoImageTexture;

    private MenuPage currentPage = MenuPage.Landing;
    private string archiveMessage = string.Empty;
    private float settingsMusicVolume = 0.6f;
    private float settingsSfxVolume = 0.8f;
    private bool settingsFullscreen;
    private bool settingsVsync;
    private int settingsTargetFps = 60;

    private GUIStyle titleStyle;
    private GUIStyle pageTitleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle sectionStyle;
    private GUIStyle bodyStyle;
    private GUIStyle statStyle;
    private GUIStyle buttonStyle;
    private GUIStyle archivePrimaryButtonStyle;
    private GUIStyle secondaryButtonStyle;
    private GUIStyle smallButtonStyle;
    private GUIStyle footerStyle;

    private Texture2D fallbackBackgroundTexture;
    private Texture2D screenTintTexture;
    private Texture2D topGlowTexture;
    private Texture2D bottomFadeTexture;
    private Texture2D panelTexture;
    private Texture2D panelSoftTexture;
    private Texture2D panelShadowTexture;
    private Texture2D accentTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;
    private Texture2D secondaryButtonTexture;
    private Texture2D secondaryButtonHoverTexture;

    private enum MenuPage
    {
        Landing,
        LegacyArchive,
        Settings
    }

    private void Awake()
    {
        MetaProgressionStore.Load();
        LoadSettings();
        ApplySettings();
        DisableLegacyCanvases();
    }

    private void OnEnable()
    {
        MetaProgressionStore.EnsureLoaded();
        AudioManager.PlayMusic(MusicTrack.MainMenu);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            if (currentPage == MenuPage.LegacyArchive)
            {
                StartLoadedRun();
            }
            else if (currentPage == MenuPage.Settings)
            {
                currentPage = MenuPage.Landing;
            }
            else if (MetaProgressionStore.HasSaveFile)
            {
                LoadGame();
            }
            else
            {
                StartNewGame();
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentPage == MenuPage.LegacyArchive || currentPage == MenuPage.Settings)
            {
                currentPage = MenuPage.Landing;
                return;
            }

            QuitGame();
        }
    }

    public void StartGame()
    {
        StartNewGame();
    }

    public void StartNewGame()
    {
        AudioManager.PlaySFX(SFXType.ButtonClick);
        MetaProgressionStore.StartNewSave();
        BeginRun(true);
    }

    public void LoadGame()
    {
        AudioManager.PlaySFX(SFXType.ButtonClick);
        MetaProgressionStore.Load();
        if (MetaProgressionStore.SealedCycles > 0)
        {
            archiveMessage = string.IsNullOrWhiteSpace(MetaProgressionStore.LastOutcome)
                ? "The archive is ready."
                : MetaProgressionStore.LastOutcome;
            currentPage = MenuPage.LegacyArchive;
            return;
        }

        BeginRun(false);
    }

    public void QuitGame()
    {
        AudioManager.PlaySFX(SFXType.ButtonClick);
        Debug.Log("Exiting Game...");
        Application.Quit();
    }

    private void StartLoadedRun()
    {
        AudioManager.PlaySFX(SFXType.ButtonClick);
        BeginRun(false);
    }

    private static void BeginRun(bool playPrologue)
    {
        WorkshopBattlePayloadBridge.Clear();
        BattleResultBridge.Clear();
        WorkshopRunStateBridge.Clear();
        RunProgressBridge.Reset();
        SceneManager.LoadScene(playPrologue ? PrologueSceneName : WorkshopSceneName);
    }

    private void OnGUI()
    {
        EnsureStyles();

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        DrawBackdrop(screenWidth, screenHeight);
        DrawSaveBadge(screenWidth);

        if (currentPage == MenuPage.LegacyArchive)
        {
            DrawLegacyArchive(screenWidth, screenHeight);
        }
        else if (currentPage == MenuPage.Settings)
        {
            DrawSettingsPage(screenWidth, screenHeight);
        }
        else
        {
            DrawLanding(screenWidth, screenHeight);
        }

        Rect footerRect = new Rect(24f, screenHeight - 42f, screenWidth - 48f, 18f);
        string footer = currentPage == MenuPage.LegacyArchive
            ? "Enter / Space to start next run  /  Esc to return"
            : currentPage == MenuPage.Settings
                ? "Enter / Space to return  /  Esc to return"
            : "Enter / Space to continue  /  Esc to quit";
        GUI.Label(footerRect, footer, footerStyle);
    }

    private void DrawLanding(float screenWidth, float screenHeight)
    {
        float titleWidth = Mathf.Min(840f, screenWidth - 64f);

        if (logoImageTexture == null){
            Rect titleRect = new Rect((screenWidth - titleWidth) * 0.5f, Mathf.Clamp(screenHeight * 0.14f, 44f, 124f), titleWidth, 72f);
            Rect subtitleRect = new Rect(titleRect.x, titleRect.yMax + 10f, titleRect.width, 28f);

            GUI.Label(titleRect, "Arcane Atelier", titleStyle);
            GUI.Label(subtitleRect, "Forge spells. Hold the breach.", subtitleStyle);

        }
        else
        {
            float logoAspect = logoImageTexture.width / (float)logoImageTexture.height;
            float calculatedHeight = titleWidth * logoAspect;

            Rect logoRect = new Rect((screenWidth - titleWidth) * 0.5f, (screenHeight * 0.25f) - (calculatedHeight * 0.5f), titleWidth, calculatedHeight);
            GUI.DrawTexture(logoRect, logoImageTexture, ScaleMode.ScaleToFit);
        }

        DrawCharacterFrame(screenWidth, screenHeight);

        float panelWidth = Mathf.Clamp(screenWidth * 0.28f, 340f, 460f);
        float panelHeight = 292f;
        Rect panelRect = new Rect((screenWidth - panelWidth) * 0.5f, Mathf.Clamp(screenHeight * 0.55f, 286f, screenHeight - panelHeight - 64f), panelWidth, panelHeight);
        DrawMenuPanel(panelRect);
    }

    private void DrawLegacyArchive(float screenWidth, float screenHeight)
    {
        float panelWidth = Mathf.Min(screenWidth - 64f, 920f);
        float panelHeight = Mathf.Min(screenHeight - 96f, 676f);
        Rect panelRect = new Rect((screenWidth - panelWidth) * 0.5f, (screenHeight - panelHeight) * 0.5f, panelWidth, panelHeight);

        GUI.DrawTexture(new Rect(panelRect.x + 8f, panelRect.y + 10f, panelRect.width, panelRect.height), panelShadowTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(panelRect, panelTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(panelRect.x, panelRect.y, panelRect.width, HeaderAccentHeight), accentTexture, ScaleMode.StretchToFill);

        GUI.BeginGroup(panelRect);
        GUI.Label(new Rect(28f, 22f, panelRect.width - 56f, 32f), "Legacy Archive", pageTitleStyle);
        GUI.Label(new Rect(32f, 62f, panelRect.width - 64f, 22f), "Carve permanent sigils, read the cycle omen, then enter the next breach.", subtitleStyle);

        float statY = 106f;
        DrawArchiveStat(new Rect(28f, statY, 190f, 74f), MetaProgressionStore.SealedCycles.ToString(), "Sealed Cycles");
        DrawArchiveStat(new Rect(234f, statY, 190f, 74f), MetaProgressionStore.LegacySigils.ToString(), "Legacy Sigils");
        DrawArchiveStat(new Rect(440f, statY, 190f, 74f), MetaProgressionStore.BestTokensEarnedInRun.ToString(), "Best Run Tokens");

        DrawArchiveOmen(new Rect(28f, 200f, panelRect.width - 56f, 72f));

        GUI.Label(new Rect(28f, 288f, panelRect.width - 56f, 22f), "Archive Offerings", sectionStyle);
        float itemY = 320f;
        LegacyArchiveUpgrade[] upgrades = MetaProgressionStore.AvailableUpgrades.ToArray();
        if (upgrades.Length == 0)
        {
            GUI.Label(new Rect(28f, itemY + 8f, panelRect.width - 56f, 40f), "Every sigil path has been fully carved. The archive holds steady until a new system is added.", bodyStyle);
        }
        else
        {
            for (int i = 0; i < upgrades.Length; i++)
            {
                DrawArchiveUpgrade(new Rect(28f, itemY, panelRect.width - 56f, 78f), upgrades[i]);
                itemY += 88f;
            }
        }

        float archiveActionWidth = 172f;
        float archiveActionX = panelRect.width - archiveActionWidth - 42f;
        GUI.Label(new Rect(28f, panelRect.height - 100f, panelRect.width - 280f, 38f), archiveMessage, bodyStyle);
        if (GUI.Button(new Rect(archiveActionX, panelRect.height - 98f, archiveActionWidth, 34f), "Enter Next Breach", archivePrimaryButtonStyle))
        {
            StartLoadedRun();
        }

        if (GUI.Button(new Rect(archiveActionX, panelRect.height - 56f, archiveActionWidth, 30f), "Back", secondaryButtonStyle))
        {
            AudioManager.PlaySFX(SFXType.ButtonClick);
            currentPage = MenuPage.Landing;
        }

        GUI.EndGroup();
    }

    private void DrawSettingsPage(float screenWidth, float screenHeight)
    {
        float panelWidth = Mathf.Min(screenWidth - 64f, 760f);
        float panelHeight = Mathf.Min(screenHeight - 96f, 520f);
        Rect panelRect = new Rect((screenWidth - panelWidth) * 0.5f, (screenHeight - panelHeight) * 0.5f, panelWidth, panelHeight);

        GUI.DrawTexture(new Rect(panelRect.x + 8f, panelRect.y + 10f, panelRect.width, panelRect.height), panelShadowTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(panelRect, panelTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(panelRect.x, panelRect.y, panelRect.width, HeaderAccentHeight), accentTexture, ScaleMode.StretchToFill);

        GUI.BeginGroup(panelRect);
        GUI.Label(new Rect(28f, 22f, panelRect.width - 56f, 32f), "Settings", pageTitleStyle);
        GUI.Label(new Rect(32f, 62f, panelRect.width - 64f, 22f), "Audio and display controls for the atelier.", subtitleStyle);

        Rect audioRect = new Rect(32f, 112f, panelRect.width - 64f, 132f);
        DrawSettingsPanel(audioRect, "Audio");
        float musicValue = DrawVolumeRow(new Rect(audioRect.x + 20f, audioRect.y + 42f, audioRect.width - 40f, 28f), "Music", settingsMusicVolume);
        float sfxValue = DrawVolumeRow(new Rect(audioRect.x + 20f, audioRect.y + 82f, audioRect.width - 40f, 28f), "SFX", settingsSfxVolume);
        if (!Mathf.Approximately(musicValue, settingsMusicVolume) || !Mathf.Approximately(sfxValue, settingsSfxVolume))
        {
            settingsMusicVolume = musicValue;
            settingsSfxVolume = sfxValue;
            SaveSettings();
            ApplySettings();
        }

        Rect videoRect = new Rect(32f, 262f, panelRect.width - 64f, 142f);
        DrawSettingsPanel(videoRect, "Video");
        bool oldFullscreen = settingsFullscreen;
        bool oldVsync = settingsVsync;
        settingsFullscreen = DrawSettingToggle(new Rect(videoRect.x + 20f, videoRect.y + 42f, 180f, 28f), "Fullscreen", settingsFullscreen, "settings_fullscreen");
        settingsVsync = DrawSettingToggle(new Rect(videoRect.x + 220f, videoRect.y + 42f, 160f, 28f), "VSync", settingsVsync, "settings_vsync");
        if (oldFullscreen != settingsFullscreen || oldVsync != settingsVsync)
        {
            SaveSettings();
            ApplySettings();
        }

        DrawFpsSelector(new Rect(videoRect.x + 20f, videoRect.y + 86f, videoRect.width - 40f, 32f));

        if (GUI.Button(new Rect(panelRect.width - 220f, panelRect.height - 58f, 178f, 34f), "Back", secondaryButtonStyle))
        {
            AudioManager.PlaySFX(SFXType.ButtonClick);
            currentPage = MenuPage.Landing;
        }

        GUI.EndGroup();
    }

    private void DrawSettingsPanel(Rect rect, string title)
    {
        GUI.DrawTexture(rect, panelSoftTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, HeaderAccentHeight), accentTexture, ScaleMode.StretchToFill);
        GUI.Label(new Rect(rect.x + 18f, rect.y + 12f, rect.width - 36f, 20f), title, sectionStyle);
    }

    private float DrawVolumeRow(Rect rect, string label, float value)
    {
        GUI.Label(new Rect(rect.x, rect.y + 4f, 86f, 18f), label, bodyStyle);
        float newValue = GUI.HorizontalSlider(new Rect(rect.x + 96f, rect.y + 7f, rect.width - 166f, 18f), value, 0f, 1f);
        GUI.Label(new Rect(rect.x + rect.width - 58f, rect.y + 4f, 56f, 18f), $"{Mathf.RoundToInt(newValue * 100f)}%", footerStyle);
        return newValue;
    }

    private bool DrawSettingToggle(Rect rect, string label, bool value, string interactionId)
    {
        GUI.DrawTexture(rect, value ? buttonTexture : secondaryButtonTexture, ScaleMode.StretchToFill);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 5f, rect.width - 24f, 18f), value ? $"{label}: On" : $"{label}: Off", footerStyle);
        if (DrawRawButton(rect, interactionId))
        {
            value = !value;
        }

        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, HeaderAccentHeight), value ? accentTexture : secondaryButtonHoverTexture, ScaleMode.StretchToFill);
        return value;
    }

    private void DrawFpsSelector(Rect rect)
    {
        GUI.Label(new Rect(rect.x, rect.y + 6f, 96f, 18f), "Frame Cap", bodyStyle);
        int[] fpsValues = { 30, 60, 120 };
        float buttonWidth = 74f;
        for (int i = 0; i < fpsValues.Length; i++)
        {
            int fps = fpsValues[i];
            Rect buttonRect = new Rect(rect.x + 108f + i * (buttonWidth + 10f), rect.y, buttonWidth, 28f);
            GUIStyle style = settingsTargetFps == fps ? buttonStyle : secondaryButtonStyle;
            if (GUI.Button(buttonRect, fps.ToString(), style))
            {
                AudioManager.PlaySFX(SFXType.ButtonClick);
                settingsTargetFps = fps;
                SaveSettings();
                ApplySettings();
            }
        }
    }

    private void DrawBackdrop(float screenWidth, float screenHeight)
    {
        if (battleBackdropTexture != null)
        {
            GUI.DrawTexture(new Rect(0f, 0f, screenWidth, screenHeight), battleBackdropTexture, ScaleMode.ScaleAndCrop);
        }
        else
        {
            GUI.DrawTexture(new Rect(0f, 0f, screenWidth, screenHeight), fallbackBackgroundTexture, ScaleMode.StretchToFill);
        }

        GUI.DrawTexture(new Rect(0f, 0f, screenWidth, screenHeight), screenTintTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(0f, 0f, screenWidth, screenHeight * 0.32f), topGlowTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(0f, screenHeight * 0.8f, screenWidth, screenHeight * 0.2f), bottomFadeTexture, ScaleMode.StretchToFill);
    }

    private void DrawSaveBadge(float screenWidth)
    {
        Rect badgeRect = new Rect(screenWidth - 250f, 22f, 226f, 64f);
        GUI.DrawTexture(badgeRect, panelSoftTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(badgeRect.x, badgeRect.y, badgeRect.width, HeaderAccentHeight), accentTexture, ScaleMode.StretchToFill);
        GUI.Label(new Rect(badgeRect.x + 12f, badgeRect.y + 10f, badgeRect.width - 24f, 18f), $"Cycle {MetaProgressionStore.SealedCycles}", sectionStyle);
        GUI.Label(new Rect(badgeRect.x + 12f, badgeRect.y + 34f, badgeRect.width - 24f, 18f), $"Legacy Sigils {MetaProgressionStore.LegacySigils}", footerStyle);
    }

    private void DrawCharacterFrame(float screenWidth, float screenHeight)
    {
        if (playerCharacterTexture == null || bossCharacterTexture == null)
        {
            return;
        }

        float centerX = screenWidth * 0.5f;
        float baseY = screenHeight * 0.79f;
        float leftWidth = Mathf.Clamp(screenWidth * 0.12f, 116f, 172f);
        float rightWidth = Mathf.Clamp(screenWidth * 0.14f, 124f, 196f);
        float leftHeight = leftWidth * 1.28f;
        float rightHeight = rightWidth * 1.28f;

        if (screenWidth < 920f)
        {
            leftWidth *= 0.82f;
            rightWidth *= 0.82f;
            leftHeight = leftWidth * 1.28f;
            rightHeight = rightWidth * 1.28f;
        }

        float playerBob = Mathf.Sin(Time.unscaledTime * SpriteBobFrequency) * 7f;
        float bossBob = Mathf.Sin(Time.unscaledTime * (SpriteBobFrequency * 0.82f) + 1f) * 5f;

        Rect playerRect = new Rect(centerX - 280f - leftWidth, baseY - leftHeight + playerBob, leftWidth, leftHeight);
        Rect bossRect = new Rect(centerX + 280f, baseY - rightHeight + bossBob, rightWidth, rightHeight);

        if (screenWidth < 920f)
        {
            playerRect.x = 26f;
            bossRect.x = screenWidth - rightWidth - 26f;
        }

        DrawShadow(new Rect(playerRect.x + leftWidth * 0.16f, baseY - 10f, leftWidth * 0.68f, 18f), 0.28f);
        DrawShadow(new Rect(bossRect.x + rightWidth * 0.1f, baseY - 10f, rightWidth * 0.76f, 18f), 0.34f);
        DrawTextureContain(playerRect, playerCharacterTexture, 0.94f);
        DrawTextureContain(bossRect, bossCharacterTexture, 0.9f);
    }

    private void DrawMenuPanel(Rect panelRect)
    {
        GUI.DrawTexture(new Rect(panelRect.x + 6f, panelRect.y + 8f, panelRect.width, panelRect.height), panelShadowTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(panelRect, panelTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(panelRect.x, panelRect.y, panelRect.width, HeaderAccentHeight), accentTexture, ScaleMode.StretchToFill);

        GUI.Label(new Rect(panelRect.x, panelRect.y + 18f, panelRect.width, 22f), "MAIN MENU", sectionStyle);

        float buttonWidth = panelRect.width - 52f;
        float buttonX = panelRect.x + 26f;
        float firstButtonY = panelRect.y + 54f;
        bool hasSave = MetaProgressionStore.HasSaveFile;
        Rect continueButtonRect = new Rect(buttonX, firstButtonY, buttonWidth, 44f);
        Rect newButtonRect = new Rect(buttonX, firstButtonY + 56f, buttonWidth, 44f);
        Rect settingsButtonRect = new Rect(buttonX, firstButtonY + 116f, buttonWidth, 38f);
        Rect quitButtonRect = new Rect(buttonX, firstButtonY + 164f, buttonWidth, 34f);

        bool previousEnabled = GUI.enabled;
        GUI.enabled = hasSave;
        ReportHover(continueButtonRect, "continue");
        if (GUI.Button(continueButtonRect, hasSave ? "Continue" : "No Save", buttonStyle))
        {
            LoadGame();
        }
        GUI.enabled = previousEnabled;

        ReportHover(newButtonRect, "new_game");
        if (GUI.Button(newButtonRect, "New Game", buttonStyle))
        {
            StartNewGame();
        }

        ReportHover(settingsButtonRect, "settings");
        if (GUI.Button(settingsButtonRect, "Settings", secondaryButtonStyle))
        {
            AudioManager.PlaySFX(SFXType.ButtonClick);
            currentPage = MenuPage.Settings;
        }

        ReportHover(quitButtonRect, "quit");
        if (GUI.Button(quitButtonRect, "Quit", secondaryButtonStyle))
        {
            QuitGame();
        }

        string saveText = hasSave
            ? $"Last save loaded / Cycle {MetaProgressionStore.SealedCycles} / Sigils {MetaProgressionStore.LegacySigils}"
            : "No save yet. New Game creates one.";
        GUI.Label(new Rect(panelRect.x + 26f, panelRect.y + 258f, buttonWidth, 18f), saveText, footerStyle);
    }

    private void DrawArchiveStat(Rect rect, string value, string label)
    {
        GUI.DrawTexture(rect, panelSoftTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, HeaderAccentHeight), accentTexture, ScaleMode.StretchToFill);
        GUI.Label(new Rect(rect.x, rect.y + 14f, rect.width, 28f), value, statStyle);
        GUI.Label(new Rect(rect.x, rect.y + 46f, rect.width, 18f), label, footerStyle);
    }

    private void DrawArchiveUpgrade(Rect rect, LegacyArchiveUpgrade upgrade)
    {
        bool maxed = upgrade.IsMaxed;
        bool affordable = MetaProgressionStore.LegacySigils >= upgrade.SigilCost;
        GUI.DrawTexture(rect, panelSoftTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, HeaderAccentHeight), maxed ? secondaryButtonHoverTexture : accentTexture, ScaleMode.StretchToFill);
        GUI.Label(new Rect(rect.x + 16f, rect.y + 10f, rect.width - 220f, 20f), upgrade.DisplayName, sectionStyle);
        GUI.Label(new Rect(rect.x + 16f, rect.y + 28f, 120f, 16f), upgrade.CategoryLabel, footerStyle);
        GUI.Label(new Rect(rect.x + 120f, rect.y + 28f, 90f, 16f), $"{upgrade.CurrentRank}/{upgrade.MaxRank} carved", footerStyle);
        GUI.Label(new Rect(rect.x + 16f, rect.y + 46f, rect.width - 190f, 24f), upgrade.Description, bodyStyle);
        GUI.Label(new Rect(rect.x + rect.width - 154f, rect.y + 14f, 130f, 18f), $"{upgrade.SigilCost} Sigils", footerStyle);

        bool previousEnabled = GUI.enabled;
        GUI.enabled = !maxed && affordable;
        string buttonLabel = maxed ? "Maxed" : affordable ? "Carve" : "Locked";
        if (GUI.Button(new Rect(rect.x + rect.width - 154f, rect.y + 40f, 130f, 26f), buttonLabel, smallButtonStyle))
        {
            if (MetaProgressionStore.TryPurchaseUpgrade(upgrade.Id, out string message))
            {
                AudioManager.PlaySFX(SFXType.ButtonClick);
                archiveMessage = message;
            }
            else
            {
                archiveMessage = message;
            }
        }
        GUI.enabled = previousEnabled;
    }

    private void DrawArchiveOmen(Rect rect)
    {
        LegacyOmenView omen = MetaProgressionStore.ActiveOmen;
        GUI.DrawTexture(rect, panelSoftTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, HeaderAccentHeight), accentTexture, ScaleMode.StretchToFill);
        GUI.Label(new Rect(rect.x + 16f, rect.y + 10f, rect.width - 32f, 18f), "Cycle Omen", sectionStyle);
        GUI.Label(new Rect(rect.x + 16f, rect.y + 30f, rect.width - 32f, 18f), omen != null ? omen.DisplayName : "None", bodyStyle);
        GUI.Label(new Rect(rect.x + 16f, rect.y + 48f, rect.width - 32f, 18f), MetaProgressionStore.BuildRunModifierSummary(), footerStyle);
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        fallbackBackgroundTexture = CreateSolidTexture(new Color32(7, 10, 18, 255));
        screenTintTexture = CreateSolidTexture(new Color32(3, 6, 12, 164));
        topGlowTexture = CreateSolidTexture(new Color32(22, 16, 7, 86));
        bottomFadeTexture = CreateSolidTexture(new Color32(3, 5, 10, 112));
        panelTexture = CreateSolidTexture(new Color32(10, 14, 22, 236));
        panelSoftTexture = CreateSolidTexture(new Color32(18, 25, 38, 232));
        panelShadowTexture = CreateSolidTexture(new Color32(0, 0, 0, 96));
        accentTexture = CreateSolidTexture(new Color32(198, 157, 71, 255));
        buttonTexture = CreateSolidTexture(new Color32(111, 80, 37, 255));
        buttonHoverTexture = CreateSolidTexture(new Color32(149, 107, 45, 255));
        secondaryButtonTexture = CreateSolidTexture(new Color32(29, 37, 52, 255));
        secondaryButtonHoverTexture = CreateSolidTexture(new Color32(42, 53, 74, 255));

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 52;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color32(244, 232, 205, 255);

        pageTitleStyle = new GUIStyle(titleStyle);
        pageTitleStyle.fontSize = 30;

        subtitleStyle = new GUIStyle(GUI.skin.label);
        subtitleStyle.fontSize = 18;
        subtitleStyle.fontStyle = FontStyle.Bold;
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        subtitleStyle.normal.textColor = new Color32(214, 205, 184, 255);

        sectionStyle = new GUIStyle(GUI.skin.label);
        sectionStyle.fontSize = 14;
        sectionStyle.fontStyle = FontStyle.Bold;
        sectionStyle.alignment = TextAnchor.MiddleCenter;
        sectionStyle.normal.textColor = new Color32(174, 182, 198, 255);

        bodyStyle = new GUIStyle(GUI.skin.label);
        bodyStyle.fontSize = 13;
        bodyStyle.wordWrap = true;
        bodyStyle.normal.textColor = new Color32(214, 205, 184, 255);

        statStyle = new GUIStyle(GUI.skin.label);
        statStyle.fontSize = 28;
        statStyle.fontStyle = FontStyle.Bold;
        statStyle.alignment = TextAnchor.MiddleCenter;
        statStyle.normal.textColor = new Color32(251, 245, 233, 255);

        footerStyle = new GUIStyle(GUI.skin.label);
        footerStyle.fontSize = 12;
        footerStyle.alignment = TextAnchor.MiddleCenter;
        footerStyle.normal.textColor = new Color32(149, 159, 178, 255);

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 21;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.normal.background = buttonTexture;
        buttonStyle.hover.background = buttonHoverTexture;
        buttonStyle.active.background = buttonHoverTexture;
        buttonStyle.normal.textColor = new Color32(251, 245, 233, 255);
        buttonStyle.hover.textColor = new Color32(255, 250, 240, 255);
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.border = new RectOffset(10, 10, 10, 10);

        archivePrimaryButtonStyle = new GUIStyle(buttonStyle);
        archivePrimaryButtonStyle.fontSize = 16;

        secondaryButtonStyle = new GUIStyle(buttonStyle);
        secondaryButtonStyle.fontSize = 17;
        secondaryButtonStyle.normal.background = secondaryButtonTexture;
        secondaryButtonStyle.hover.background = secondaryButtonHoverTexture;
        secondaryButtonStyle.active.background = secondaryButtonHoverTexture;

        smallButtonStyle = new GUIStyle(secondaryButtonStyle);
        smallButtonStyle.fontSize = 13;
    }

    private void LoadSettings()
    {
        settingsMusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, AudioManager.GetMusicVolume());
        settingsSfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, AudioManager.GetSFXVolume());
        settingsFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0;
        settingsVsync = PlayerPrefs.GetInt(VsyncKey, QualitySettings.vSyncCount > 0 ? 1 : 0) != 0;
        settingsTargetFps = PlayerPrefs.GetInt(TargetFpsKey, 60);
        if (settingsTargetFps != 30 && settingsTargetFps != 60 && settingsTargetFps != 120)
        {
            settingsTargetFps = 60;
        }
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, settingsMusicVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, settingsSfxVolume);
        PlayerPrefs.SetInt(FullscreenKey, settingsFullscreen ? 1 : 0);
        PlayerPrefs.SetInt(VsyncKey, settingsVsync ? 1 : 0);
        PlayerPrefs.SetInt(TargetFpsKey, settingsTargetFps);
        PlayerPrefs.Save();
    }

    private void ApplySettings()
    {
        AudioManager.SetMusicVolume(settingsMusicVolume);
        AudioManager.SetSFXVolume(settingsSfxVolume);
        Screen.fullScreen = settingsFullscreen;
        QualitySettings.vSyncCount = settingsVsync ? 1 : 0;
        Application.targetFrameRate = settingsVsync ? -1 : settingsTargetFps;
    }

    private bool DrawRawButton(Rect rect, string interactionId)
    {
        ReportHover(rect, interactionId);
        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        {
            AudioManager.PlaySFX(SFXType.ButtonClick);
            return true;
        }

        return false;
    }

    private static void DrawShadow(Rect rect, float alpha)
    {
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
        GUI.color = previous;
    }

    private static Texture2D CreateSolidTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private static void DisableLegacyCanvases()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].enabled = false;
        }
    }

    private static void DrawTextureContain(Rect rect, Texture2D texture, float alpha)
    {
        if (texture == null)
        {
            return;
        }

        float textureAspect = texture.width / (float)texture.height;
        float rectAspect = rect.width / rect.height;
        Rect drawRect = rect;

        if (textureAspect > rectAspect)
        {
            float height = rect.width / textureAspect;
            drawRect.y += (rect.height - height) * 0.5f;
            drawRect.height = height;
        }
        else
        {
            float width = rect.height * textureAspect;
            drawRect.x += (rect.width - width) * 0.5f;
            drawRect.width = width;
        }

        Color previous = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(drawRect, texture, ScaleMode.StretchToFill, true);
        GUI.color = previous;
    }

    private static void ReportHover(Rect rect, string controlId)
    {
        Event current = Event.current;
        if (current == null || !rect.Contains(current.mousePosition))
        {
            return;
        }

        AudioManager.ReportUIHover($"main_menu:{controlId}");
    }
}
