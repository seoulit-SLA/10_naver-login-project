using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    public Button loginButton;

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

        StartCoroutine(TryAutoLogin());
    }

    void OnDestroy()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(StartOAuthLogin);
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
            loginButton.gameObject.SetActive(false);
        }

        StartListener();
        Application.OpenURL(LoginUrl);
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

        Debug.Log($"<color=green>Login success</color>");
        Debug.Log($"uid : {user.uid}");
        Debug.Log($"email : {user.email}");
        Debug.Log($"name : {user.name}");
    }

    private void ShowLoginButton()
    {
        _isLoggedIn = false;

        if (loginButton != null)
        {
            loginButton.gameObject.SetActive(true);
        }
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
}
