// client/Assets/GameSession.cs
public static class GameSession
{
    public const float GameDurationSeconds = 10f;
    public const int PointsPerClick = 10;

    public static int Score { get; private set; }

    public static void ResetScore() => Score = 0;
    public static void AddScore(int points) => Score += points;
}