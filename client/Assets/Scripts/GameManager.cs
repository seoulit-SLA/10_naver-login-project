using System.Collections;
using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public Slider timerSlider;    
    public TMP_Text scoreText;    
    public Button clickButton;     // [추가] 연타용 버튼 레퍼런스

    private float gameTime = GameSession.GameDurationSeconds; // [수정] 정적 세션의 시간 값 활용
    private float currentTime;
    private bool isGameOver = false;

    void Start()
    {
        currentTime = gameTime;
        isGameOver = false;

        if (timerSlider != null)
        {
            timerSlider.maxValue = gameTime;
            timerSlider.value = gameTime;
        }

        if (clickButton != null)
        {
            clickButton.onClick.AddListener(OnClickButton);
            clickButton.interactable = true;
        }

        UpdateScoreUI();
    }

    void OnDestroy()
    {
        if (clickButton != null)
        {
            clickButton.onClick.RemoveListener(OnClickButton);
        }
    }

    void Update()
    {
        if (isGameOver) return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            if (timerSlider != null)
            {
                timerSlider.value = currentTime;
            }

            if (currentTime <= 0)
            {
                currentTime = 0;
                EndGame();
            }
        }
    }

    public void OnClickButton()
    {
        if (currentTime > 0 && !isGameOver)
        {
            // [수정] GameSession 정적 클래스에 누적
            GameSession.AddScore(GameSession.PointsPerClick);
            UpdateScoreUI();
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = GameSession.Score.ToString("N0");
        }
    }

    void EndGame()
    {
        isGameOver = true;
        
        if (clickButton != null)
        {
            clickButton.interactable = false; // 더이상 클릭 불가 조치
        }

        // [수정] 점수 제출 및 씬 전환 코루틴 실행
        StartCoroutine(SubmitScoreAndTransitionRoutine());
    }

    private IEnumerator SubmitScoreAndTransitionRoutine()
    {
        GlobalLoadingCanvas.Instance?.Show(); // 로딩 연출 시작

        string token = PlayerPrefs.GetString("sessionToken", string.Empty);
        int myCurrentScore = GameSession.Score;
        int currentBestScoreOnServer = 0;

        // 1. 서버에서 기존 최고 기록 조회
        if (!string.IsNullOrEmpty(token))
        {
            yield return NetworkManager.Instance.GetMyBestScore(
                token,
                response =>
                {
                    currentBestScoreOnServer = response.bestScore;
                },
                error =>
                {
                    currentBestScoreOnServer = 0; // 에러 혹은 첫 기록일 경우 0점 처리
                }
            );

            // 2. 신기록인 경우에만 서버에 제출 요청
            if (myCurrentScore > currentBestScoreOnServer)
            {
                Debug.Log($"[GAME] 신기록 달성 ({myCurrentScore} > {currentBestScoreOnServer}). 서버에 제출합니다.");
                yield return NetworkManager.Instance.SubmitBestScore(
                    token,
                    myCurrentScore,
                    response =>
                    {
                        Debug.Log($"[GAME] 서버 제출 완료 및 신기록 등록 성공! 최신 최고 기록: {response.bestScore}");
                    },
                    error =>
                    {
                        Debug.LogError($"[GAME] 서버 점수 제출 중 오류 발생: {error}");
                    }
                );
            }
            else
            {
                Debug.Log($"[GAME] 최고 점수 미달성 ({myCurrentScore} <= {currentBestScoreOnServer}). 제출을 스킵합니다.");
            }
        }

        GlobalLoadingCanvas.Instance?.Hide(); // 로딩 연출 해제
        
        // 3. 결과 화면(GameOver)으로 전환
        SceneManager.LoadScene("GameOverScene");
    }
}