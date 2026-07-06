using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

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

    public IEnumerator PostLogin(GoogleUser googleUser)
    {
        if (googleUser == null)
        {
            Debug.LogWarning("GoogleUser is null.");
            yield break;
        }

        string json = JsonConvert.SerializeObject(googleUser);
        Debug.Log($"Sending login request: {json}");

        using var request = new UnityWebRequest("http://localhost:3000/login", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            Debug.Log($"Login response: {responseText}");

            try
            {
                var response = JsonConvert.DeserializeObject<LoginResponse>(responseText);
                if (response != null && !string.IsNullOrEmpty(response.sub))
                {
                    Debug.Log($"<color=yellow>{response.sub}</color>");
                }
                else
                {
                    Debug.LogWarning("Login response does not contain sub.");
                }
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Login response is not valid JSON: {ex.Message}\nRaw response: {responseText}");
            }
        }
        else
        {
            Debug.LogError($"Login failed: {request.error}");
        }
    }

    [System.Serializable]
    private class LoginResponse
    {
        public string sub;
    }
}
