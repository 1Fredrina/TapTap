using UnityEngine;

[System.Serializable]
public class GameData
{
    [Min(1)] public int MaxLives = 5;
    public int Lives;

    public bool IsGameOver => Lives <= 0;

    public void ResetLives()
    {
        Lives = MaxLives;
    }
}