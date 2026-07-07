using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISystemMessage : MonoBehaviour
{
    public static UISystemMessage Instance { get; private set; }

    [SerializeField] private TMP_Text messageText;

    private Button _dimButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        WireDimCloseButton();
        Hide();
    }

    public static UISystemMessage EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        var existing = FindFirstObjectByType<UISystemMessage>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            existing.WireDimCloseButton();
            return existing;
        }

        var prefab = Resources.Load<UISystemMessage>("UISystemMessage");
        if (prefab != null)
        {
            return Instantiate(prefab);
        }

        return CreateFallbackInstance();
    }

    public void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }

        gameObject.SetActive(true);

        if (transform is RectTransform rectTransform)
        {
            rectTransform.localScale = Vector3.one;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void WireDimCloseButton()
    {
        var dimTransform = transform.Find("dim");
        if (dimTransform == null)
        {
            return;
        }

        _dimButton = dimTransform.GetComponent<Button>();
        if (_dimButton == null)
        {
            _dimButton = dimTransform.gameObject.AddComponent<Button>();
            var image = dimTransform.GetComponent<Image>();
            if (image != null)
            {
                _dimButton.targetGraphic = image;
            }
        }

        _dimButton.onClick.RemoveListener(Hide);
        _dimButton.onClick.AddListener(Hide);
    }

    private static UISystemMessage CreateFallbackInstance()
    {
        var root = new GameObject("UISystemMessage");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var dim = new GameObject("dim", typeof(RectTransform), typeof(Image), typeof(Button));
        dim.transform.SetParent(root.transform, false);
        var dimRect = dim.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.47f);

        var bg = new GameObject("bg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(1080f, 153f);
        bg.GetComponent<Image>().color = Color.white;

        var text = new GameObject("MessageText", typeof(RectTransform), typeof(TextMeshProUGUI));
        text.transform.SetParent(root.transform, false);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(334f, 100f);
        textRect.anchoredPosition = new Vector2(5f, 25f);

        var tmp = text.GetComponent<TextMeshProUGUI>();
        tmp.text = "[시스템 메시지]";
        tmp.fontSize = 36;
        tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;

        var systemMessage = root.AddComponent<UISystemMessage>();
        systemMessage.messageText = tmp;
        return systemMessage;
    }
}