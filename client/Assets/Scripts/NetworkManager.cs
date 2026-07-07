using System;
using System.Collections;
using System.Collections.Generic;
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

    // [추가] 최고 점수 응답 데이터 모델
    [Serializable]
    public class ScoreMeResponse
    {
        public string uid;
        public int bestScore;
        public string updatedAt;
    }

    [Serializable]
    public class ScoreSubmitResponse
    {
        public string uid;
        public int submittedScore;
        public int bestScore;
        public int previousBestScore;
        public bool isNewRecord;
        public string updatedAt;
    }

    [Serializable]
    public class RankingsResponse
    {
        public List<RankEntry> rankings;
    }

    public IEnumerator GetRankings(int limit, Action<List<RankEntry>> onSuccess, Action<string> onError)
    {
        string url = $"{BaseUrl}/scores/rankings?limit={limit}";
        using var request = UnityWebRequest.Get(url);
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<RankingsResponse>(request.downloadHandler.text);
                if (response?.rankings != null)
                {
                    onSuccess?.Invoke(response.rankings);
                    yield break;
                }
            }
            catch (JsonException ex)
            {
                Debug.LogError($"GetRankings parse failed: {ex.Message}");
            }

            onError?.Invoke("PARSE_ERROR");
            yield break;
        }

        onError?.Invoke(ParseErrorCode(request.downloadHandler.text, "REQUEST_FAILED"));
    }

    // [추가] 내 최고 점수 조회 API (POST /scores/me)
    public IEnumerator GetMyBestScore(string sessionToken, Action<ScoreMeResponse> onSuccess, Action<string> onError)
    {
        using var request = CreateAuthRequest($"{BaseUrl}/scores/me", "POST", sessionToken);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<ScoreMeResponse>(request.downloadHandler.text);
                if (response != null)
                {
                    onSuccess?.Invoke(response);
                    yield break;
                }
            }
            catch (JsonException ex)
            {
                Debug.LogError($"GetBestScore parse failed: {ex.Message}");
            }
            onError?.Invoke("PARSE_ERROR");
        }
        else
        {
            onError?.Invoke(ParseErrorCode(request.downloadHandler.text, "REQUEST_FAILED"));
        }
    }

    // [추가] 최고 점수 제출 API (POST /scores/submit)
    public IEnumerator SubmitBestScore(string sessionToken, int score, Action<ScoreSubmitResponse> onSuccess, Action<string> onError)
    {
        string jsonBody = $"{{\"score\": {score}}}";
        using var request = new UnityWebRequest($"{BaseUrl}/scores/submit", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        
        request.SetRequestHeader("Authorization", $"Bearer {sessionToken}");
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<ScoreSubmitResponse>(request.downloadHandler.text);
                if (response != null)
                {
                    onSuccess?.Invoke(response);
                    yield break;
                }
            }
            catch (JsonException ex)
            {
                Debug.LogError($"SubmitBestScore parse failed: {ex.Message}");
            }
            onError?.Invoke("PARSE_ERROR");
        }
        else
        {
            onError?.Invoke(ParseErrorCode(request.downloadHandler.text, "REQUEST_FAILED"));
        }
    }

    // --- 기존의 회원 인증 API ---
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