using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    public Button loginButton;

    private const string LoginUrl = "http://127.0.0.1:3000/auth/naver";

    void Start()
    {
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        }
        else
        {
            Debug.LogWarning("loginButton is not assigned.");
        }
    }

    void OnDestroy()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(OnLoginButtonClicked);
        }
    }

    private void OnLoginButtonClicked()
    {
        Application.OpenURL(LoginUrl);
    }
}
