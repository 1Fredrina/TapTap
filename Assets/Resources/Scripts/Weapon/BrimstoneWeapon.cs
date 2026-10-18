using UnityEngine;

/// <summary>定时释放的长直硫磺火光束。它属于特殊天赋，因此不会通过武器堆叠升级。</summary>
[System.Serializable]
public sealed class BrimstoneWeapon : WeaponBase
{
    [Min(0.05f)] public float beamWidth = 0.75f;
    [Min(0.01f)] public float beamDuration = 0.16f;

    // 即使当前没有敌人也按固定间隔释放，方向使用最近目标或角色朝向。
    public override bool RequiresTarget => false;

    public override void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        base.Tick(deltaTime);
    }

    protected override void FireSingle(Player player, Enemy target)
    {
        if (player == null || !player.isActiveAndEnabled || player.IsDead ||
            (GameManager.Instance != null && GameManager.Instance.IsGameOver)) return;

        float beamLength = Mathf.Max(0.1f, range * (1f + player.GetStatBonus(StatType.AttackRange)));

        target = target != null && !target.IsDead
            ? target
            : player.FindClosestEnemyInRange(beamLength);

        Vector2 direction = target != null && !target.IsDead
            ? ((Vector2)target.transform.position - (Vector2)player.transform.position).normalized
            : player.Controller != null
                ? new Vector2(player.Controller.FacingDirection, 0f)
                : Vector2.right;
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.right;

        Vector2 origin = player.transform.position;
        float halfWidth = Mathf.Max(0.05f, beamWidth) * 0.5f;
        int finalDamage = Mathf.Max(0, Mathf.RoundToInt(damage * player.DamageMultiplier));

        foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (!player.CanTargetEnemy(enemy)) continue;

            Vector2 offset = (Vector2)enemy.transform.position - origin;
            float along = Vector2.Dot(offset, direction);
            if (along < 0f || along > beamLength) continue;

            float perpendicular = Mathf.Abs(offset.x * direction.y - offset.y * direction.x);
            if (perpendicular <= halfWidth)
                enemy.TakeDamageFrom(finalDamage, origin);
        }

        CreateBeamVisual(player, origin, direction, beamLength);
    }

    private void CreateBeamVisual(Player player, Vector2 origin, Vector2 direction, float length)
    {
        GameObject beam = new GameObject("Brimstone Beam");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(beam, player.gameObject.scene);
        LineRenderer line = beam.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, origin);
        line.SetPosition(1, origin + direction * length);
        line.startWidth = Mathf.Max(0.05f, beamWidth);
        line.endWidth = line.startWidth;
        line.startColor = new Color(1f, 0.15f, 0.05f, 1f);
        line.endColor = new Color(1f, 0.85f, 0.15f, 0.8f);
        line.sortingOrder = 100;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            Material material = new Material(shader);
            line.sharedMaterial = material;
            beam.AddComponent<BrimstoneBeamVisual>().Initialize(material);
        }
        Object.Destroy(beam, Mathf.Max(0.01f, beamDuration));
    }
}
