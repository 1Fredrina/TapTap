using UnityEngine;

public enum StackUpMode
{
    ExtraShot,
    ExtraPierce
}

[System.Serializable]
public abstract class WeaponBase
{
    public string id;
    public string displayName;

    [Min(0.05f)] public float cooldown = 1f;
    [Min(0)] public int damage = 1;
    [Min(0f)] public float range = 12f;
    [Min(0.01f)] public float burstInterval = 0.1f;

    public StackUpMode stackUpMode = StackUpMode.ExtraShot;

    [System.NonSerialized] public float cooldownTimer;
    [System.NonSerialized] public int stack = 1;
    [System.NonSerialized] public int extraShots = 0;
    [System.NonSerialized] public int extraPierce = 0;

    [System.NonSerialized] protected int burstRemaining;
    [System.NonSerialized] protected float burstTimer;
    [System.NonSerialized] protected Player burstOwner;
    [System.NonSerialized] protected Enemy burstTarget;

    public virtual bool RequiresTarget => true;
    public virtual bool RequiresEnemyInRange => false;

    public bool TickCooldown(float deltaTime)
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= deltaTime;
            return false;
        }
        return true;
    }

    public virtual void Tick(float deltaTime)
    {
        if (burstRemaining <= 0) return;

        burstTimer -= deltaTime;
        if (burstTimer > 0f) return;

        burstTimer = burstInterval;
        burstRemaining--;

        FireSingle(burstOwner, burstTarget);
    }

    public virtual void Fire(Player player, Enemy target)
    {
        burstOwner = player;
        burstTarget = target;
        burstRemaining = GetShotCount();
        burstTimer = 0f;
    }

    protected abstract void FireSingle(Player player, Enemy target);

    public virtual void ResetCooldown(float cooldownMultiplier = 1f)
    {
        cooldownTimer = Mathf.Max(0.05f, cooldown) * Mathf.Max(0.05f, cooldownMultiplier);
    }

    public virtual void OnStackUp(int newStack)
    {
        stack = newStack;

        if (stackUpMode == StackUpMode.ExtraShot)
            extraShots++;
        else
            extraPierce++;
    }

    public virtual int GetShotCount()
    {
        return 1 + extraShots;
    }
}
