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
    private const int RequestTimeoutSeconds = 5;

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

    [Serializable]
    public class HealthCheckResponse
    {
        public string status;
        public string timestamp;
    }

    [Serializable]
    private class ApiErrorResponse
    {
        public string code;
        public string message;
    }

    public IEnumerator GetRankings(int limit, Action<List<RankEntry>> onSuccess, Action<string> onError)
    {
        string url = $"{BaseUrl}/scores/rankings?limit={limit}";
        using var request = UnityWebRequest.Get(url);
        ConfigureRequest(request);
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

            HandleClientSideError("PARSE_ERROR", "서버 응답을 해석할 수 없습니다.");
            onError?.Invoke("PARSE_ERROR");
            yield break;
        }

        var error = BuildErrorResponse(request, "REQUEST_FAILED", "요청 처리 중 오류가 발생했습니다.");
        ShowSystemMessage(error);
        onError?.Invoke(error.code);
    }

    public IEnumerator CheckServerAvailability(Action onSuccess, Action<string> onError)
    {
        using var request = UnityWebRequest.Get($"{BaseUrl}/health");
        ConfigureRequest(request);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            onSuccess?.Invoke();
            yield break;
        }

        var error = BuildErrorResponse(request, "REQUEST_FAILED", "서버 상태 확인에 실패했습니다.");
        ShowSystemMessage(error);
        onError?.Invoke(error.code);
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
            HandleClientSideError("PARSE_ERROR", "서버 응답을 해석할 수 없습니다.");
            onError?.Invoke("PARSE_ERROR");
        }
        else
        {
            var error = BuildErrorResponse(request, "REQUEST_FAILED", "최고 점수 조회에 실패했습니다.");
            ShowSystemMessage(error);
            onError?.Invoke(error.code);
        }
    }

    // [추가] 최고 점수 제출 API (POST /scores/submit)
    public IEnumerator SubmitBestScore(string sessionToken, int score, Action<ScoreSubmitResponse> onSuccess, Action<string> onError)
    {
        string jsonBody = $"{{\"score\": {score}}}";
        using var request = new UnityWebRequest($"{BaseUrl}/scores/submit", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        ConfigureRequest(request, sessionToken);

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
            HandleClientSideError("PARSE_ERROR", "서버 응답을 해석할 수 없습니다.");
            onError?.Invoke("PARSE_ERROR");
        }
        else
        {
            var error = BuildErrorResponse(request, "REQUEST_FAILED", "점수 제출에 실패했습니다.");
            ShowSystemMessage(error);
            onError?.Invoke(error.code);
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

            HandleClientSideError("REAUTH_REQUIRED", "세션 갱신 응답을 해석할 수 없습니다.");
            onError?.Invoke("REAUTH_REQUIRED");
            yield break;
        }

        var error = BuildErrorResponse(request, "REAUTH_REQUIRED", "세션 갱신에 실패했습니다.");
        ShowSystemMessage(error);
        onError?.Invoke(error.code);
    }

    public IEnumerator Logout(string sessionToken, Action onComplete)
    {
        using var request = CreateAuthRequest($"{BaseUrl}/auth/logout", "POST", sessionToken);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            var error = BuildErrorResponse(request, "REQUEST_FAILED", "로그아웃 요청에 실패했습니다.");
            ShowSystemMessage(error);
        }

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

            HandleClientSideError("SESSION_INVALID", "서버 응답을 해석할 수 없습니다.");
            onError?.Invoke("SESSION_INVALID");
            yield break;
        }

        var error = BuildErrorResponse(request, "SESSION_INVALID", "인증 요청에 실패했습니다.");
        ShowSystemMessage(error);
        onError?.Invoke(error.code);
    }

    private static UnityWebRequest CreateAuthRequest(string url, string method, string sessionToken)
    {
        var request = new UnityWebRequest(url, method);
        ConfigureRequest(request, sessionToken);
        return request;
    }

    private static void ConfigureRequest(UnityWebRequest request, string sessionToken = null)
    {
        request.timeout = RequestTimeoutSeconds;
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        if (!string.IsNullOrEmpty(sessionToken))
        {
            request.SetRequestHeader("Authorization", $"Bearer {sessionToken}");
        }
    }

    private static ApiErrorResponse BuildErrorResponse(UnityWebRequest request, string fallbackCode, string fallbackMessage)
    {
        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            if (request.error != null && request.error.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new ApiErrorResponse
                {
                    code = "REQUEST_TIMEOUT",
                    message = $"서버 응답이 5초를 초과했습니다. 잠시 후 다시 시도해 주세요.",
                };
            }

            return new ApiErrorResponse
            {
                code = "NETWORK_ERROR",
                message = "서버에 연결할 수 없습니다. 서버 실행 상태를 확인해 주세요.",
            };
        }

        try
        {
            var error = JsonConvert.DeserializeObject<ApiErrorResponse>(request.downloadHandler.text);
            if (error != null && !string.IsNullOrEmpty(error.code))
            {
                if (string.IsNullOrEmpty(error.message))
                {
                    error.message = fallbackMessage;
                }

                return error;
            }
        }
        catch (JsonException)
        {
        }

        return new ApiErrorResponse
        {
            code = fallbackCode,
            message = fallbackMessage,
        };
    }

    private static void HandleClientSideError(string code, string message)
    {
        ShowSystemMessage(new ApiErrorResponse
        {
            code = code,
            message = message,
        });
    }

    private static void ShowSystemMessage(ApiErrorResponse error)
    {
        if (AppMain.Instance != null)
        {
            AppMain.Instance.ShowSystemMessage($"[{error.code}] {error.message}");
            return;
        }

        var popup = UISystemMessage.EnsureExists();
        popup?.ShowMessage($"[{error.code}] {error.message}");
    }

    [Serializable]
    private class RefreshResponse
    {
        public string sessionToken;
        public string uid;
        public string email;
        public string name;
    }

}