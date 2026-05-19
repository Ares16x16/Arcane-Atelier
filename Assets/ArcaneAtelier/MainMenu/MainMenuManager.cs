using ArcaneAtelier.Audio;
using ArcaneAtelier.Battle;
using ArcaneAtelier.Workshop;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MainMenuManager : MonoBehaviour
{
    private const string PrologueSceneName = "PrologueScene";
    private const float HeaderAccentHeight = 3f;
    private const float SpriteBobFrequency = 1.35f;

    [SerializeField] private Texture2D battleBackdropTexture;
    [SerializeField] private Texture2D playerCharacterTexture;
    [SerializeField] private Texture2D bossCharacterTexture;

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle sectionStyle;
    private GUIStyle buttonStyle;
    private GUIStyle secondaryButtonStyle;
    private GUIStyle footerStyle;

    private Texture2D fallbackBackgroundTexture;
    private Texture2D screenTintTexture;
    private Texture2D topGlowTexture;
    private Texture2D bottomFadeTexture;
    private Texture2D panelTexture;
    private Texture2D panelShadowTexture;
    private Texture2D accentTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;
    private Texture2D secondaryButtonTexture;
    private Texture2D secondaryButtonHoverTexture;

    private void Awake()
    {
        DisableLegacyCanvases();
    }

    private void OnEnable()
    {
        AudioManager.PlayMusic(MusicTrack.MainMenu);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            StartGame();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }
    }

    public void StartGame()
    {
        AudioManager.PlaySFX(SFXType.ButtonClick);
        WorkshopBattlePayloadBridge.Clear();
        BattleResultBridge.Clear();
        WorkshopRunStateBridge.Clear();
        RunProgressBridge.Reset();
        SceneManager.LoadScene(PrologueSceneName);
    }

    public void QuitGame()
    {
        AudioManager.PlaySFX(SFXType.ButtonClick);
        Debug.Log("Exiting Game...");
        Application.Quit();
    }

    private void OnGUI()
    {
        EnsureStyles();

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        DrawBackdrop(screenWidth, screenHeight);

        float titleWidth = Mathf.Min(840f, screenWidth - 64f);
        Rect titleRect = new Rect((screenWidth - titleWidth) * 0.5f, Mathf.Clamp(screenHeight * 0.14f, 44f, 124f), titleWidth, 72f);
        Rect subtitleRect = new Rect(titleRect.x, titleRect.yMax + 10f, titleRect.width, 28f);

        GUI.Label(titleRect, "Arcane Atelier", titleStyle);
        GUI.Label(subtitleRect, "Forge spells. Hold the breach.", subtitleStyle);

        DrawCharacterFrame(screenWidth, screenHeight);

        float panelWidth = Mathf.Clamp(screenWidth * 0.26f, 320f, 430f);
        float panelHeight = 186f;
        Rect panelRect = new Rect((screenWidth - panelWidth) * 0.5f, Mathf.Clamp(screenHeight * 0.57f, 292f, screenHeight - panelHeight - 64f), panelWidth, panelHeight);
        DrawMenuPanel(panelRect);

        Rect footerRect = new Rect(24f, screenHeight - 42f, screenWidth - 48f, 18f);
        GUI.Label(footerRect, "Enter / Space to start  •  Esc to quit", footerStyle);
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

        GUI.Label(new Rect(panelRect.x, panelRect.y + 20f, panelRect.width, 22f), "MAIN MENU", sectionStyle);

        float buttonWidth = panelRect.width - 52f;
        float buttonX = panelRect.x + 26f;
        float firstButtonY = panelRect.y + 56f;
        Rect startButtonRect = new Rect(buttonX, firstButtonY, buttonWidth, 48f);
        Rect quitButtonRect = new Rect(buttonX, firstButtonY + 62f, buttonWidth, 42f);

        ReportHover(startButtonRect, "start_run");
        if (GUI.Button(startButtonRect, "Start Run", buttonStyle))
        {
            StartGame();
        }

        ReportHover(quitButtonRect, "quit");
        if (GUI.Button(quitButtonRect, "Quit", secondaryButtonStyle))
        {
            QuitGame();
        }
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

        footerStyle = new GUIStyle(GUI.skin.label);
        footerStyle.fontSize = 12;
        footerStyle.alignment = TextAnchor.MiddleCenter;
        footerStyle.normal.textColor = new Color32(149, 159, 178, 255);

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 22;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.normal.background = buttonTexture;
        buttonStyle.hover.background = buttonHoverTexture;
        buttonStyle.active.background = buttonHoverTexture;
        buttonStyle.normal.textColor = new Color32(251, 245, 233, 255);
        buttonStyle.hover.textColor = new Color32(255, 250, 240, 255);
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.border = new RectOffset(10, 10, 10, 10);

        secondaryButtonStyle = new GUIStyle(buttonStyle);
        secondaryButtonStyle.fontSize = 18;
        secondaryButtonStyle.normal.background = secondaryButtonTexture;
        secondaryButtonStyle.hover.background = secondaryButtonHoverTexture;
        secondaryButtonStyle.active.background = secondaryButtonHoverTexture;
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
