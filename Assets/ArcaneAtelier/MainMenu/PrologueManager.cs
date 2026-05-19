using ArcaneAtelier.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PrologueManager : MonoBehaviour
{
    private const string WorkshopSceneName = "WorkshopScene";
    private const string MainMenuSceneName = "MainMenuScene";
    private const float AccentLineHeight = 3f;

    private readonly string[] storyPages =
    {
        "The atelier is awake, but the outer wards have failed. Spirits are spilling through the breach faster than the old seals can hold.",
        "You are the last active spellsmith in the tower. Rebuild the workshop lines, fuse higher-order elements, and forge a battle deck strong enough to survive the siege.",
        "Every victory opens deeper tooling. Three breaches stand before the core. The fourth fight is the final boss."
    };

    private int currentPage;
    private float pageStartTime;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle hintStyle;
    private GUIStyle primaryButtonStyle;
    private GUIStyle secondaryButtonStyle;
    [SerializeField] private Texture2D playerCharacterTexture;
    [SerializeField] private Texture2D battleBackdropTexture;

    private Texture2D backgroundTexture;
    private Texture2D panelTexture;
    private Texture2D accentTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;
    private Texture2D backdropTintTexture;
    private Texture2D topTintTexture;
    private Texture2D bottomTintTexture;
    private Texture2D shadowTexture;

    private void Awake()
    {
        pageStartTime = Time.unscaledTime;
        DisableLegacyCanvases();
        AudioManager.PlayMusic(MusicTrack.Prologue);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            AdvanceOrEnterWorkshop();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMenu();
        }
    }

    public void EnterWorkshop()
    {
        SceneManager.LoadScene(WorkshopSceneName);
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private void AdvanceOrEnterWorkshop()
    {
        if (currentPage >= storyPages.Length - 1)
        {
            EnterWorkshop();
            return;
        }

        currentPage += 1;
        pageStartTime = Time.unscaledTime;
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
        {
            return;
        }

        backgroundTexture = CreateSolidTexture(new Color32(7, 10, 18, 255));
        panelTexture = CreateSolidTexture(new Color32(14, 18, 28, 240));
        accentTexture = CreateSolidTexture(new Color32(198, 157, 71, 255));
        buttonTexture = CreateSolidTexture(new Color32(105, 77, 38, 255));
        buttonHoverTexture = CreateSolidTexture(new Color32(146, 107, 46, 255));
        backdropTintTexture = CreateSolidTexture(new Color32(2, 5, 10, 126));
        topTintTexture = CreateSolidTexture(new Color32(8, 13, 22, 178));
        bottomTintTexture = CreateSolidTexture(new Color32(4, 7, 12, 190));
        shadowTexture = CreateSolidTexture(new Color32(0, 0, 0, 96));

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTexture;
        panelStyle.border = new RectOffset(2, 2, 2, 2);
        panelStyle.padding = new RectOffset(26, 26, 22, 22);

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 34;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = new Color32(238, 228, 203, 255);
        titleStyle.alignment = TextAnchor.MiddleLeft;

        bodyStyle = new GUIStyle(GUI.skin.label);
        bodyStyle.fontSize = 22;
        bodyStyle.wordWrap = true;
        bodyStyle.richText = true;
        bodyStyle.normal.textColor = new Color32(220, 220, 220, 255);

        hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = 16;
        hintStyle.wordWrap = true;
        hintStyle.normal.textColor = new Color32(170, 176, 192, 255);

        primaryButtonStyle = new GUIStyle(GUI.skin.button);
        primaryButtonStyle.fontSize = 20;
        primaryButtonStyle.fontStyle = FontStyle.Bold;
        primaryButtonStyle.normal.background = buttonTexture;
        primaryButtonStyle.hover.background = buttonHoverTexture;
        primaryButtonStyle.active.background = buttonHoverTexture;
        primaryButtonStyle.normal.textColor = new Color32(250, 244, 230, 255);
        primaryButtonStyle.hover.textColor = new Color32(255, 251, 239, 255);
        primaryButtonStyle.border = new RectOffset(8, 8, 8, 8);

        secondaryButtonStyle = new GUIStyle(primaryButtonStyle);
        secondaryButtonStyle.normal.background = panelTexture;
        secondaryButtonStyle.hover.background = CreateSolidTexture(new Color32(36, 43, 57, 255));
        secondaryButtonStyle.active.background = secondaryButtonStyle.hover.background;
    }

    private void OnGUI()
    {
        EnsureStyles();

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        DrawSceneBackdrop(screenWidth, screenHeight);

        float leftMargin = Mathf.Clamp(screenWidth * 0.06f, 36f, 86f);
        float panelWidth = Mathf.Min(760f, screenWidth - leftMargin * 2f);
        if (screenWidth >= 1120f)
        {
            panelWidth = Mathf.Min(740f, screenWidth * 0.56f);
        }

        float panelHeight = Mathf.Min(520f, screenHeight - 112f);
        Rect panelRect = new Rect(leftMargin, (screenHeight - panelHeight) * 0.5f, panelWidth, panelHeight);

        GUI.Box(panelRect, GUIContent.none, panelStyle);
        GUI.DrawTexture(new Rect(panelRect.x, panelRect.y, panelRect.width, AccentLineHeight), accentTexture, ScaleMode.StretchToFill);

        Rect titleRect = new Rect(panelRect.x + 30f, panelRect.y + 26f, panelRect.width - 60f, 44f);
        GUI.Label(titleRect, "The First Breach", titleStyle);

        Rect chapterRect = new Rect(panelRect.x + 30f, panelRect.y + 76f, panelRect.width - 60f, 24f);
        GUI.Label(chapterRect, $"Prologue {currentPage + 1}/{storyPages.Length}  /  Spellsmith briefing", hintStyle);

        Rect bodyRect = new Rect(panelRect.x + 30f, panelRect.y + 118f, panelRect.width - 60f, panelRect.height - 230f);
        GUI.Label(bodyRect, GetVisibleStoryText(), bodyStyle);

        DrawMovingPlayer(screenWidth, screenHeight, panelRect);

        Rect hintRect = new Rect(panelRect.x + 30f, panelRect.yMax - 98f, panelRect.width - 60f, 26f);
        string hintText = currentPage >= storyPages.Length - 1
            ? "Press Space or Enter to enter the workshop. Press Esc to return to menu."
            : "Press Space or Enter to continue. Press Esc to return to menu.";
        GUI.Label(hintRect, hintText, hintStyle);

        float buttonY = panelRect.yMax - 58f;
        if (GUI.Button(new Rect(panelRect.x + 30f, buttonY, 220f, 38f), currentPage >= storyPages.Length - 1 ? "Enter Workshop" : "Continue", primaryButtonStyle))
        {
            AdvanceOrEnterWorkshop();
        }

        if (GUI.Button(new Rect(panelRect.x + 266f, buttonY, 170f, 38f), "Back To Menu", secondaryButtonStyle))
        {
            ReturnToMenu();
        }

        //Rect legendRect = new Rect(panelRect.xMax - 260f, buttonY + 4f, 230f, 24f);
        //GUI.Label(legendRect, "", hintStyle);
    }

    private void DrawSceneBackdrop(float screenWidth, float screenHeight)
    {
        if (battleBackdropTexture != null)
        {
            GUI.DrawTexture(new Rect(0f, 0f, screenWidth, screenHeight), battleBackdropTexture, ScaleMode.ScaleAndCrop);
        }
        else
        {
            GUI.DrawTexture(new Rect(0f, 0f, screenWidth, screenHeight), backgroundTexture, ScaleMode.StretchToFill);
        }

        GUI.DrawTexture(new Rect(0f, 0f, screenWidth, screenHeight), backdropTintTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(0f, 0f, screenWidth, 110f), topTintTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(0f, screenHeight - 150f, screenWidth, 150f), bottomTintTexture, ScaleMode.StretchToFill);
    }

    private void DrawMovingPlayer(float screenWidth, float screenHeight, Rect storyPanelRect)
    {
        if (playerCharacterTexture == null)
        {
            return;
        }

        float figureWidth = Mathf.Clamp(screenWidth * 0.16f, 110f, 210f);
        float figureHeight = figureWidth * 1.28f;
        float walk = Mathf.Sin(Time.unscaledTime * 0.95f) * 24f;
        float bob = Mathf.Sin(Time.unscaledTime * 3.4f) * 7f;
        float rightMargin = Mathf.Clamp(screenWidth * 0.09f, 44f, 128f);
        float groundY = screenHeight - Mathf.Clamp(screenHeight * 0.12f, 54f, 96f);
        float x = screenWidth - figureWidth - rightMargin + walk;

        if (x < storyPanelRect.xMax + 18f)
        {
            figureWidth = Mathf.Clamp(screenWidth * 0.12f, 82f, 130f);
            figureHeight = figureWidth * 1.28f;
            x = screenWidth - figureWidth - 24f + walk * 0.35f;
            groundY = screenHeight - 44f;
        }

        Rect shadowRect = new Rect(x + figureWidth * 0.18f, groundY - 10f, figureWidth * 0.64f, 14f);
        GUI.DrawTexture(shadowRect, shadowTexture, ScaleMode.StretchToFill);
        Rect figureRect = new Rect(x, groundY - figureHeight + bob, figureWidth, figureHeight);
        DrawTextureContain(figureRect, playerCharacterTexture);
    }

    private string GetVisibleStoryText()
    {
        string fullText = storyPages[Mathf.Clamp(currentPage, 0, storyPages.Length - 1)];
        int visibleLength = Mathf.Clamp(Mathf.FloorToInt((Time.unscaledTime - pageStartTime) * 38f), 0, fullText.Length);
        return fullText.Substring(0, visibleLength);
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

    private static void DrawTextureContain(Rect rect, Texture2D texture)
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

        GUI.DrawTexture(drawRect, texture, ScaleMode.StretchToFill, true);
    }
}
