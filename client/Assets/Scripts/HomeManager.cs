using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class HomeManager : MonoBehaviour
{
    [Header("UI References")]
    public Button gameStartButton; 
    public Button logoutButton;    
    public TMP_Text scoreText;     // [추가] 최고 점수를 표시할 텍스트

    private const string SessionTokenKey = "sessionToken";

    void Start()
    {
        if (gameStartButton != null)
        {
            gameStartButton.onClick.AddListener(OnGameStartClick);
        }

        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnNaverLogoutClick);
        }

        // 로그인 상태 검증 및 최고 점수 로드
        string token = PlayerPrefs.GetString(SessionTokenKey, string.Empty);

        if (!string.IsNullOrEmpty(token))
        {
            if (gameStartButton != null)
            {
                gameStartButton.interactable = true;
            }
            // [추가] 로그인 성공 상태이므로 서버에서 최고 점수 로드
            StartCoroutine(LoadBestScoreRoutine(token));
        }
        else
        {
            if (gameStartButton != null)
            {
                gameStartButton.interactable = false;
            }
            Debug.LogWarning("로그인 세션 토큰이 없어 로그인 씬으로 강제 이동합니다.");
            SceneManager.LoadScene("LoginScene");
        }
    }

    void OnDestroy()
    {
        if (gameStartButton != null)
        {
            gameStartButton.onClick.RemoveListener(OnGameStartClick);
        }

        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveListener(OnNaverLogoutClick);
        }
    }

    // [추가] 서버로부터 내 최고 기록을 받아오는 코루틴
    private IEnumerator LoadBestScoreRoutine(string token)
    {
        GlobalLoadingCanvas.Instance?.Show(); // 로딩 UI 켜기
        
        yield return NetworkManager.Instance.GetMyBestScore(
            token,
            response =>
            {
                if (scoreText != null)
                {
                    scoreText.text = response.bestScore.ToString("N0");
                }
                Debug.Log($"[HOME] 최고 점수 로드 성공: {response.bestScore}");
            },
            error =>
            {
                if (scoreText != null)
                {
                    scoreText.text = "0";
                }
                Debug.LogWarning($"[HOME] 최고 점수 로드 실패(기록 없음 등): {error}");
            }
        );

        GlobalLoadingCanvas.Instance?.Hide(); // 로딩 UI 끄기
    }

    public void OnGameStartClick()
    {
        // [수정] 게임 시작 시 세션 점수 리셋
        GameSession.ResetScore();
        SceneManager.LoadScene("GameScene");
    }

    public void OnNaverLogoutClick()
    {
        Debug.Log("네이버 로그아웃 시도");
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

        Debug.Log("네이버 로그아웃 완료! 로그인 씬으로 이동합니다.");
        SceneManager.LoadScene("LoginScene");
    }
}