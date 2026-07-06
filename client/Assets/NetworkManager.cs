using System;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    private const string BaseUrl = "http://127.0.0.1:3000";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator GetMe(string sessionToken, Action<NaverUser> onSuccess, Action<string> onError)
    {
        yield return SendAuthRequest("/auth/me", sessionToken, onSuccess, onError);
    }

    public IEnumerator RefreshSession(string sessionToken, Action<string> onSuccess, Action<string> onError)
    {
        using var request = CreateAuthRequest($"{BaseUrl}/auth/refresh", "POST", sessionToken);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<RefreshResponse>(request.downloadHandler.text);
                if (response != null && !string.IsNullOrEmpty(response.sessionToken))
                {
                    onSuccess?.Invoke(response.sessionToken);
                    yield break;
                }
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Refresh response parse failed: {ex.Message}");
            }

            onError?.Invoke("REAUTH_REQUIRED");
            yield break;
        }

        onError?.Invoke(ParseErrorCode(request.downloadHandler.text, "REAUTH_REQUIRED"));
    }

    public IEnumerator Logout(string sessionToken, Action onComplete)
    {
        using var request = CreateAuthRequest($"{BaseUrl}/auth/logout", "POST", sessionToken);
        yield return request.SendWebRequest();
        onComplete?.Invoke();
    }

    private IEnumerator SendAuthRequest(string path, string sessionToken, Action<NaverUser> onSuccess, Action<string> onError)
    {
        using var request = CreateAuthRequest($"{BaseUrl}{path}", "POST", sessionToken);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var user = JsonConvert.DeserializeObject<NaverUser>(request.downloadHandler.text);
                if (user != null && !string.IsNullOrEmpty(user.uid))
                {
                    onSuccess?.Invoke(user);
                    yield break;
                }
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Auth response parse failed: {ex.Message}");
            }

            onError?.Invoke("SESSION_INVALID");
            yield break;
        }

        onError?.Invoke(ParseErrorCode(request.downloadHandler.text, "SESSION_INVALID"));
    }

    private static UnityWebRequest CreateAuthRequest(string url, string method, string sessionToken)
    {
        var request = new UnityWebRequest(url, method);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", $"Bearer {sessionToken}");
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }

    private static string ParseErrorCode(string responseText, string fallback)
    {
        try
        {
            var error = JsonConvert.DeserializeObject<AuthErrorResponse>(responseText);
            if (error != null && !string.IsNullOrEmpty(error.code))
            {
                return error.code;
            }
        }
        catch (JsonException)
        {
        }

        return fallback;
    }

    [Serializable]
    private class RefreshResponse
    {
        public string sessionToken;
        public string uid;
        public string email;
        public string name;
    }

    [Serializable]
    private class AuthErrorResponse
    {
        public string message;
        public string code;
    }
}
