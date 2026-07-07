using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class RankingManager : MonoBehaviour
{
    [Header("Lobby UI")]
    public Button gameStartButton;
    public Button rankingButton;
    public Button logoutButton;
    public TMP_Text scoreText;

    [Header("Ranking UI")]
    public GameObject rankingCanvas;
    public RectTransform contentRoot;
    public Button closeRankingButton;
    public CellView cellViewPrefab;

    private const string SessionTokenKey = "sessionToken";
    private readonly List<CellView> _spawnedCells = new List<CellView>();

    void Awake()
    {
        ResolveReferences();
        HideRankingPanel();
    }

    void Start()
    {
        if (gameStartButton != null)
        {
            gameStartButton.onClick.AddListener(OnGameStartClick);
        }

        if (rankingButton != null)
        {
            rankingButton.onClick.AddListener(OnRankingButtonClick);
        }

        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnNaverLogoutClick);
        }

        if (closeRankingButton != null)
        {
            closeRankingButton.onClick.AddListener(HideRankingPanel);
        }

        string token = PlayerPrefs.GetString(SessionTokenKey, string.Empty);
        if (!string.IsNullOrEmpty(token))
        {
            if (gameStartButton != null)
            {
                gameStartButton.interactable = true;
            }

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

        if (rankingButton != null)
        {
            rankingButton.onClick.RemoveListener(OnRankingButtonClick);
        }

        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveListener(OnNaverLogoutClick);
        }

        if (closeRankingButton != null)
        {
            closeRankingButton.onClick.RemoveListener(HideRankingPanel);
        }
    }

    private void ResolveReferences()
    {
        if (rankingCanvas == null)
        {
            rankingCanvas = GameObject.Find("Ranking Canvas");
        }

        if (contentRoot == null)
        {
            var content = GameObject.Find("Content");
            if (content != null)
            {
                contentRoot = content.GetComponent<RectTransform>();
            }
        }

        if (closeRankingButton == null)
        {
            var dim = GameObject.Find("dim");
            if (dim != null)
            {
                closeRankingButton = dim.GetComponent<Button>();
            }
        }

        if (rankingButton == null)
        {
            var ranking = GameObject.Find("rankingButton");
            if (ranking != null)
            {
                rankingButton = ranking.GetComponent<Button>();
            }
        }

        if (cellViewPrefab == null)
        {
            cellViewPrefab = Resources.Load<CellView>("CellView");
        }
    }

    private IEnumerator LoadBestScoreRoutine(string token)
    {
        GlobalLoadingCanvas.Instance?.Show();

        yield return NetworkManager.Instance.GetMyBestScore(
            token,
            response =>
            {
                if (scoreText != null)
                {
                    scoreText.text = response.bestScore.ToString("N0");
                }

                Debug.Log($"[RANKING] 최고 점수 로드 성공: {response.bestScore}");
            },
            error =>
            {
                if (scoreText != null)
                {
                    scoreText.text = "0";
                }

                Debug.LogWarning($"[RANKING] 최고 점수 로드 실패: {error}");
            }
        );

        GlobalLoadingCanvas.Instance?.Hide();
    }

    public void OnGameStartClick()
    {
        GameSession.ResetScore();
        SceneManager.LoadScene("GameScene");
    }

    public void OnRankingButtonClick()
    {
        StartCoroutine(LoadRankingsRoutine());
    }

    private IEnumerator LoadRankingsRoutine()
    {
        GlobalLoadingCanvas.Instance?.Show();

        yield return NetworkManager.Instance.GetRankings(
            100,
            rankings =>
            {
                RenderRankings(rankings);
                ShowRankingPanel();
                Debug.Log($"[RANKING] 랭킹 {rankings.Count}건 표시");
            },
            error =>
            {
                Debug.LogError($"[RANKING] 랭킹 로드 실패: {error}");
            }
        );

        GlobalLoadingCanvas.Instance?.Hide();
    }

    private void RenderRankings(IReadOnlyList<RankEntry> rankings)
    {
        ClearRankingCells();

        if (cellViewPrefab == null || contentRoot == null)
        {
            Debug.LogError("[RANKING] CellView prefab 또는 Content가 없습니다.");
            return;
        }

        foreach (var entry in rankings)
        {
            var cell = Instantiate(cellViewPrefab, contentRoot);
            cell.Bind(entry);
            _spawnedCells.Add(cell);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
    }

    private void ClearRankingCells()
    {
        foreach (var cell in _spawnedCells)
        {
            if (cell != null)
            {
                Destroy(cell.gameObject);
            }
        }

        _spawnedCells.Clear();
    }

    private void ShowRankingPanel()
    {
        if (rankingCanvas == null)
        {
            return;
        }

        rankingCanvas.SetActive(true);
        var rectTransform = rankingCanvas.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
        }
    }

    public void HideRankingPanel()
    {
        if (rankingCanvas != null)
        {
            rankingCanvas.SetActive(false);
        }
    }

    public void OnNaverLogoutClick()
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
        SceneManager.LoadScene("LoginScene");
    }
}
