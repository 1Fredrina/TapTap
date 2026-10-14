using UnityEngine;

public enum TalentColor
{
    Red,
    Blue,
    Green 
}
[System.Serializable]
public abstract class TalentBase
{
    public string id;
    public string displayName;
    [TextArea] public string description;
    public TalentColor color;
    [Min(1)] public int maxStack = 1;
    [Min(1)] public int weight = 10;

    [Header("前置条件")]
    public string[] prerequisiteIds;

    public const int MaxPerColor = 7;

    public abstract void OnAcquire(Player player, int stack);
}