using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text finalScoreText;
    public Button homeButton; // [추가] 홈 버튼 컴포넌트

    void Start()
    {
        // [수정] GameSession에 남아있는 이번 판 점수를 가져와서 출력합니다.
        if (finalScoreText != null)
        {
            finalScoreText.text = GameSession.Score.ToString("N0");
        }

        // [추가] 인스펙터 실수 방지를 위한 클릭 리스너 자동 연결 (Unity 6 표준 권장)
        if (homeButton != null)
        {
            homeButton.onClick.AddListener(OnHomeButtonClick);
        }
    }

    void OnDestroy()
    {
        if (homeButton != null)
        {
            homeButton.onClick.RemoveListener(OnHomeButtonClick);
        }
    }

    public void OnHomeButtonClick()
    {
        SceneManager.LoadScene("HomeScene");
    }
}