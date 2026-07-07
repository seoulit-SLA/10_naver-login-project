using UnityEngine;

public class AppMain : MonoBehaviour
{
    public static AppMain Instance { get; private set; }

    [SerializeField] private UISystemMessage uiSystemMessagePrefab;

    private UISystemMessage _systemMessageInstance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        var existing = FindFirstObjectByType<AppMain>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        var bootstrapObject = new GameObject("AppMain");
        bootstrapObject.AddComponent<AppMain>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSystemMessage();
    }

    public void ShowSystemMessage(string message)
    {
        var popup = EnsureSystemMessage();
        if (popup == null)
        {
            Debug.LogWarning($"UISystemMessage를 찾을 수 없어 메시지를 표시하지 못했습니다: {message}");
            return;
        }

        popup.ShowMessage(message);
    }

    public void HideSystemMessage()
    {
        var popup = EnsureSystemMessage();
        popup?.Hide();
    }

    private UISystemMessage EnsureSystemMessage()
    {
        if (_systemMessageInstance != null)
        {
            return _systemMessageInstance;
        }

        if (uiSystemMessagePrefab == null)
        {
            uiSystemMessagePrefab = Resources.Load<UISystemMessage>("UISystemMessage");
        }

        if (uiSystemMessagePrefab != null)
        {
            _systemMessageInstance = Instantiate(uiSystemMessagePrefab);
            return _systemMessageInstance;
        }

        _systemMessageInstance = UISystemMessage.EnsureExists();
        return _systemMessageInstance;
    }
}
