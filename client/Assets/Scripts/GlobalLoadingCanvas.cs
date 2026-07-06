// client/Assets/GlobalLoadingCanvas.cs
using UnityEngine;

public class GlobalLoadingCanvas : MonoBehaviour
{
    public static GlobalLoadingCanvas Instance { get; private set; }

    [SerializeField] private GameObject loadingPanel; // 회전 아이콘과 어두운 배경이 들어간 패널

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Hide(); // 시작할 때는 숨김 처리
    }

    public void Show()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
    }

    public void Hide()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }
}