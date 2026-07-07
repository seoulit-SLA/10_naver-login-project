using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class CellView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI datetimeText;

    public void Bind(RankEntry entry)
    {
        if (rankText != null)
        {
            rankText.text = entry.rank.ToString();
        }

        if (nameText != null)
        {
            nameText.text = entry.name ?? string.Empty;
        }

        if (scoreText != null)
        {
            scoreText.text = entry.score.ToString();
        }

        if (datetimeText != null)
        {
            datetimeText.text = FormatTime(entry.time);
        }
    }

    private static string FormatTime(string isoTime)
    {
        if (string.IsNullOrEmpty(isoTime))
        {
            return string.Empty;
        }

        if (DateTime.TryParse(isoTime, null, DateTimeStyles.RoundtripKind, out var dateTime))
        {
            return dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }

        return isoTime;
    }
}
