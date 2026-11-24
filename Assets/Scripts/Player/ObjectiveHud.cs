using UnityEngine;

/// <summary>
/// Заглушка задания: по Tab открывается панель с текущим заданием.
/// Вешается на игрока автоматически (см. PlayerDeathBootstrap), сцену менять не нужно.
/// </summary>
[DisallowMultipleComponent]
public class ObjectiveHud : MonoBehaviour
{
    [SerializeField] private string objectiveIndex = "4";
    [SerializeField] private string objectiveTitle = "Связь вась";
    [SerializeField] private string objectiveNote = "Найди способ связаться с вась.";
    [SerializeField] private string closeHint = "Tab — закрыть";

    private bool isOpen;
    private PlayerDeath playerDeath;

    private Texture2D pixel;
    private GUIStyle indexStyle;
    private GUIStyle titleStyle;
    private GUIStyle noteStyle;
    private GUIStyle hintStyle;

    private void Awake()
    {
        playerDeath = GetComponent<PlayerDeath>();
    }

    private void Update()
    {
        // На экране смерти панель не нужна.
        if (playerDeath != null && playerDeath.IsDead)
        {
            isOpen = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
            isOpen = !isOpen;
        else if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            isOpen = false;
    }

    private void OnGUI()
    {
        if (!isOpen)
            return;

        EnsureResources();

        float width = Mathf.Min(Screen.width * 0.52f, 760f);
        float height = Mathf.Min(width * 0.4f, Screen.height * 0.5f);
        Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        float pad = width * 0.055f;

        Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.6f));
        Fill(panel, new Color(0.06f, 0.06f, 0.07f, 0.95f));
        Fill(new Rect(panel.x, panel.y, Mathf.Max(4f, width * 0.008f), panel.height),
            new Color(0.85f, 0.16f, 0.12f, 0.95f));
        Outline(panel, new Color(1f, 1f, 1f, 0.15f));

        float textWidth = width - pad * 2f;
        float y = panel.y + pad;

        GUI.Label(new Rect(panel.x + pad, y, textWidth, indexStyle.fontSize * 1.5f),
            "ЗАДАНИЕ " + objectiveIndex, indexStyle);
        y += indexStyle.fontSize * 1.7f;

        GUI.Label(new Rect(panel.x + pad, y, textWidth, titleStyle.fontSize * 1.6f),
            objectiveTitle, titleStyle);
        y += titleStyle.fontSize * 1.9f;

        if (!string.IsNullOrEmpty(objectiveNote))
        {
            GUI.Label(new Rect(panel.x + pad, y, textWidth, noteStyle.fontSize * 2.6f),
                objectiveNote, noteStyle);
        }

        GUI.Label(new Rect(panel.x + pad, panel.yMax - pad - hintStyle.fontSize * 1.6f,
                textWidth, hintStyle.fontSize * 1.6f), closeHint, hintStyle);
    }

    private void Fill(Rect rect, Color color)
    {
        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, pixel);
        GUI.color = oldColor;
    }

    private void Outline(Rect rect, Color color)
    {
        float thickness = Mathf.Max(1f, Screen.height * 0.002f);
        Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
        Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
        Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private void EnsureResources()
    {
        if (pixel == null)
        {
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "ObjectiveHudPixel" };
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply(false, false);
        }

        if (indexStyle != null)
            return;

        indexStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = Mathf.Max(12, Mathf.RoundToInt(Screen.height * 0.024f)),
            normal = { textColor = new Color(0.85f, 0.3f, 0.26f) }
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = Mathf.Max(20, Mathf.RoundToInt(Screen.height * 0.05f)),
            normal = { textColor = new Color(0.95f, 0.94f, 0.9f) }
        };

        noteStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(13, Mathf.RoundToInt(Screen.height * 0.025f)),
            wordWrap = true,
            normal = { textColor = new Color(0.75f, 0.75f, 0.78f) }
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(12, Mathf.RoundToInt(Screen.height * 0.022f)),
            normal = { textColor = new Color(0.6f, 0.6f, 0.63f) }
        };
    }

    private void OnDestroy()
    {
        if (pixel != null)
            Destroy(pixel);
    }
}
