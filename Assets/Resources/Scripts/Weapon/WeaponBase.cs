using UnityEngine;

[System.Serializable]
public abstract class WeaponBase
{
    public string id;
    public string displayName;

    [Min(0.05f)] public float cooldown = 1f;
    [Min(0)] public int damage = 1;
    [Min(0f)] public float range = 12f;

    [System.NonSerialized] public float cooldownTimer;
    [System.NonSerialized] public int stack = 1;

    public bool TickCooldown(float deltaTime)
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= deltaTime;
            return false;
        }
        return true;
    }

    public abstract void Fire(Player player, Enemy target);

    public virtual void ResetCooldown(float cooldownMultiplier = 1f)
    {
        float baseCd = Mathf.Max(0.05f, cooldown - (stack - 1) * 0.05f);
        cooldownTimer = baseCd * Mathf.Max(0.05f, cooldownMultiplier);
    }

    public virtual void OnStackUp(int newStack)
    {
        stack = newStack;
    }
}