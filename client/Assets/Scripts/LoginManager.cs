using System;
using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // [Unity 6 호환] 씬 전환을 위해 네임스페이스를 추가합니다.

// 클래스 이름을 LoginManager로 변경했습니다. 파일 이름도 반드시 LoginManager.cs여야 합니다.
public class LoginManager : MonoBehaviour
{
    public Button loginButton;
    
    [Tooltip("로그인 씬에서는 이 버튼을 할당하지 않고 비워두셔도 됩니다.")]
    public Button logoutButton;

    private const string SessionTokenKey = "sessionToken";
    private const string LoginUrl = "http://127.0.0.1:3000/auth/naver";
    private const string CallbackPrefix = "http://127.0.0.1:7777/naver-login/";

    private HttpListener _listener;
    private string _pendingToken;
    private string _pendingError;
    private bool _isLoggedIn;

    void Start()
    {
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(StartOAuthLogin);
            loginButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("loginButton is not assigned.");
        }

        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(Logout);
            logoutButton.gameObject.SetActive(false);
        }

        StartCoroutine(TryAutoLogin());
    }

    void OnDestroy()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(StartOAuthLogin);
        }

        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveListener(Logout);
        }

        StopListener();
    }

    private IEnumerator TryAutoLogin()
    {
        string token = PlayerPrefs.GetString(SessionTokenKey, string.Empty);
        if (string.IsNullOrEmpty(token))
        {
            ShowLoginButton();
            yield break;
        }

        yield return NetworkManager.Instance.GetMe(
            token,
            user => EnterMainScreen(user),
            errorCode => HandleAuthFailure(token, errorCode)
        );
    }

    private void HandleAuthFailure(string token, string errorCode)
    {
        if (errorCode == "SESSION_EXPIRED")
        {
            StartCoroutine(TryRefresh(token));
            return;
        }

        ClearSession();
        ShowLoginButton();
    }

    private IEnumerator TryRefresh(string token)
    {
        yield return NetworkManager.Instance.RefreshSession(
            token,
            newToken =>
            {
                PlayerPrefs.SetString(SessionTokenKey, newToken);
                PlayerPrefs.Save();
                StartCoroutine(TryAutoLogin());
            },
            _ =>
            {
                ClearSession();
                ShowLoginButton();
            }
        );
    }

    private void StartOAuthLogin()
    {
        if (loginButton != null)
        {
            loginButton.interactable = false;
        }

        StartCoroutine(BeginOAuthLoginRoutine());
    }

    private IEnumerator BeginOAuthLoginRoutine()
    {
        yield return NetworkManager.Instance.CheckServerAvailability(
            () =>
            {
                StartListener();
                Application.OpenURL(LoginUrl);
            },
            _ =>
            {
                ShowLoginButton();
            }
        );
    }

    private void StartListener()
    {
        StopListener();

        _listener = new HttpListener();
        _listener.Prefixes.Add(CallbackPrefix);
        _listener.Start();
        _listener.BeginGetContext(OnCallbackReceived, null);
        Debug.Log($"OAuth callback listener started: {CallbackPrefix}");
    }

    private void OnCallbackReceived(IAsyncResult result)
    {
        if (_listener == null || !_listener.IsListening)
        {
            return;
        }

        HttpListenerContext context = _listener.EndGetContext(result);
        string query = context.Request.Url.Query;

        _pendingToken = GetQueryValue(query, "token");
        _pendingError = GetQueryValue(query, "error");

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes("Login complete. You can close this window.");
        context.Response.ContentLength64 = buffer.Length;
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.OutputStream.Close();

        if (_listener.IsListening)
        {
            _listener.BeginGetContext(OnCallbackReceived, null);
        }
    }

    void Update()
    {
        if (!string.IsNullOrEmpty(_pendingToken))
        {
            string token = _pendingToken;
            _pendingToken = null;

            PlayerPrefs.SetString(SessionTokenKey, token);
            PlayerPrefs.Save();
            StopListener();
            StartCoroutine(LoadUserAfterOAuth(token));
        }
        else if (!string.IsNullOrEmpty(_pendingError))
        {
            string error = _pendingError;
            _pendingError = null;

            Debug.LogError($"Login failed: {error}");
            UISystemMessage.EnsureExists()?.ShowMessage(GetCallbackErrorMessage(error));
            StopListener();
            ShowLoginButton();
        }
    }

    private IEnumerator LoadUserAfterOAuth(string token)
    {
        yield return NetworkManager.Instance.GetMe(
            token,
            user => EnterMainScreen(user),
            _ =>
            {
                ClearSession();
                ShowLoginButton();
            }
        );
    }

    private void EnterMainScreen(NaverUser user)
    {
        _isLoggedIn = true;

        if (loginButton != null)
        {
            loginButton.gameObject.SetActive(false);
        }

        if (logoutButton != null)
        {
            logoutButton.gameObject.SetActive(true);
        }

        Debug.Log($"<color=green>Login success</color>");
        Debug.Log($"uid : {user.uid}");
        Debug.Log($"email : {user.email}");
        Debug.Log($"name : {user.name}");

        // ==========================================
        // [수정] 로그인 성공 시 메인 로비(HomeScene)로 화면을 전환합니다.
        // 자동 로그인 처리가 완료되었을 때도 이 함수를 거쳐 자동으로 이동하게 됩니다.
        // ==========================================
        SceneManager.LoadScene("RankingScene");
    }

    private void ShowLoginButton()
    {
        _isLoggedIn = false;

        if (loginButton != null)
        {
            loginButton.gameObject.SetActive(true);
            loginButton.interactable = true;
        }

        if (logoutButton != null)
        {
            logoutButton.gameObject.SetActive(false);
        }
    }

    public void Logout()
    {
        StartCoroutine(LogoutRoutine());
    }

    private IEnumerator LogoutRoutine()
    {
        string token = PlayerPrefs.GetString(SessionTokenKey, string.Empty);
        if (!string.IsNullOrEmpty(token))
        {
            yield return NetworkManager.Instance.Logout(token, null);
        }

        PlayerPrefs.DeleteKey(SessionTokenKey);
        PlayerPrefs.Save();
        ShowLoginButton();
        Debug.Log("Logged out.");
    }

    private void ClearSession()
    {
        string token = PlayerPrefs.GetString(SessionTokenKey, string.Empty);
        if (!string.IsNullOrEmpty(token))
        {
            StartCoroutine(NetworkManager.Instance.Logout(token, null));
        }

        PlayerPrefs.DeleteKey(SessionTokenKey);
        PlayerPrefs.Save();
    }

    private void StopListener()
    {
        if (_listener == null)
        {
            return;
        }

        if (_listener.IsListening)
        {
            _listener.Stop();
        }

        _listener.Close();
        _listener = null;
    }

    private static string GetQueryValue(string query, string key)
    {
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        if (query.StartsWith("?"))
        {
            query = query.Substring(1);
        }

        string[] pairs = query.Split('&');
        foreach (string pair in pairs)
        {
            string[] values = pair.Split('=');
            if (values.Length == 2 && values[0] == key)
            {
                return System.Uri.UnescapeDataString(values[1]);
            }
        }

        return null;
    }

    private static string GetCallbackErrorMessage(string error)
    {
        return error switch
        {
            "auth_failed" => "[AUTH_FAILED] 네이버 로그인에 실패했습니다. 다시 시도해 주세요.",
            "db_failed" => "[DB_ERROR] 로그인 정보 저장 중 오류가 발생했습니다.",
            _ => $"[LOGIN_ERROR] 로그인 처리 중 오류가 발생했습니다. ({error})",
        };
    }
}