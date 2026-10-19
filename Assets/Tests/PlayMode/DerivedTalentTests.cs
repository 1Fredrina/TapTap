using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ReactiveTalentTests
{
    private object Derived => Value(player, "DerivedEffects");
    private void TickDerived(float seconds) => Call(Derived, "Tick", seconds);
    private object ProjectileEffects(Component bullet) => bullet.GetType().GetField("talentEffects", Private).GetValue(bullet);

    [TestCase("regeneration_pulse", "cell_repair", "healing_affinity")]
    [TestCase("dodge_insight", "membrane_dodge", "antibody_critical")]
    [TestCase("dash_repair", "dash_extension", "cell_repair")]
    [TestCase("critical_leech", "antibody_critical", "healing_affinity")]
    [TestCase("ricochet_hunt", "antibody_ricochet", "antibody_homing")]
    [TestCase("mature_explosion", "antibody_persistence", "antibody_size")]
    public void DerivedConfigRequiresBothFoundationsAndCanOnlyBeAcquiredOnce(string id, string first, string second)
    {
        Assert.That(talents[id].GetType().Name, Is.EqualTo("DerivedTalent"));
        Assert.That(Call(player, "HasPrerequisites", talents[id]), Is.False);
        Acquire(first);
        Assert.That(Call(player, "HasPrerequisites", talents[id]), Is.False);
        Acquire(second);
        Assert.That(Call(player, "HasPrerequisites", talents[id]), Is.True);
        Acquire(id);
        Assert.That(Call(player, "TryAddTalent", talents[id]), Is.False);
        object previous = Derived;
        Call(player, "Initialize", data);
        Assert.That(Derived, Is.Not.SameAs(previous));
        Assert.That(Call(player, "HasPrerequisites", talents[id]), Is.False);
    }

    [Test]
    public void HealingPulseCountsActualHealingAndStoresOnlyOnePendingPulse()
    {
        Acquire("regeneration_pulse");
        Component enemy = EnemyAt(Vector2.right);
        Call(player, "Heal", 100f); // 满血不充能。
        Assert.That(Value(enemy, "CurrentHealth"), Is.EqualTo(100f));
        Call(player, "TakeDamage", 90);
        Call(player, "Heal", 19f);
        Assert.That(Value(enemy, "CurrentHealth"), Is.EqualTo(100f));
        Call(player, "Heal", 1f);
        Assert.That(Value(enemy, "CurrentHealth"), Is.EqualTo(70f));
        Call(player, "Heal", 100f); // 冷却期间的大额治疗最多储存一轮。
        TickDerived(0f);
        Assert.That(Value(enemy, "CurrentHealth"), Is.EqualTo(70f));
        TickDerived(1f);
        Assert.That(Value(enemy, "CurrentHealth"), Is.EqualTo(40f));
        TickDerived(1f);
        Assert.That(Value(enemy, "CurrentHealth"), Is.EqualTo(40f));
    }

    [Test]
    public void HealingPulseKillHealingCannotChargeAnotherPulse()
    {
        Acquire("regeneration_pulse");
        Call(player, "ApplyStatTalent", System.Enum.Parse(GameType("StatType"), "HealOnKill"), 20f, 1);
        Component fragile = EnemyAt(Vector2.right);
        Call(fragile, "TakeDamage", 80);
        Component survivor = EnemyAt(Vector2.up);
        Call(player, "TakeDamage", 90);
        Call(player, "Heal", 20f);
        Assert.That(Value(fragile, "IsDead"), Is.True);
        Assert.That(Number("CurrentHealth"), Is.EqualTo(50f));
        TickDerived(1f);
        Assert.That(Value(survivor, "CurrentHealth"), Is.EqualTo(70f));
        Call(player, "Heal", 20f);
        Assert.That(Value(survivor, "CurrentHealth"), Is.EqualTo(40f));
    }

    [TestCase("Bullet")]
    [TestCase("PiercingBullet")]
    public void DodgeInsightGuaranteesOnlyThreeUniqueHitsAndExpires(string type)
    {
        Acquire("dodge_insight");
        Call(player, "ApplyStatTalent", System.Enum.Parse(GameType("StatType"), "DodgeChance"), 1f, 1);
        Call(player, "TakeDamage", 10); // 验证真实闪避入口。
        Component bullet = FoundationBullet(type, 10);
        Component first = EnemyAt(Vector2.right);
        Hit(bullet, first);
        Hit(bullet, first); // 重复碰撞不消耗次数。
        Assert.That(Value(first, "CurrentHealth"), Is.EqualTo(80f));
        for (int i = 0; i < 3; i++)
        {
            Component target = EnemyAt(Vector2.up * (i + 1));
            Hit(bullet, target);
            Assert.That(Value(target, "CurrentHealth"), Is.EqualTo(i < 2 ? 80f : 90f));
        }
        EndInvincibility();
        Call(player, "TakeDamage", 10);
        TickDerived(4f);
        Component expired = EnemyAt(Vector2.left);
        Hit(bullet, expired);
        Assert.That(Value(expired, "CurrentHealth"), Is.EqualTo(90f));
    }

    [Test]
    public void DashRepairScalesMissingHealthAndHealingWithIndependentCooldown()
    {
        Acquire("dash_repair");
        Acquire("healing_affinity");
        Call(player, "TakeDamage", 50);
        Call(player, "OnDashPerformed");
        Assert.That(Number("CurrentHealth"), Is.EqualTo(56f).Within(0.001f));
        Call(player, "OnDashPerformed");
        TickDerived(0f);
        Call(player, "OnDashPerformed");
        Assert.That(Number("CurrentHealth"), Is.EqualTo(56f).Within(0.001f));
        TickDerived(4f);
        Call(player, "OnDashPerformed");
        Assert.That(Number("CurrentHealth"), Is.EqualTo(61.28f).Within(0.001f));
    }

    [TestCase("Bullet")]
    [TestCase("PiercingBullet")]
    public void CriticalLeechSharesCooldownAcrossProjectilesAndBenefitsFromHealing(string type)
    {
        Acquire("critical_leech");
        Acquire("healing_affinity");
        Call(player, "ApplyStatTalent", System.Enum.Parse(GameType("StatType"), "ProjectileCriticalChance"), 1f, 1);
        Call(player, "TakeDamage", 50);
        Hit(FoundationBullet(type), EnemyAt(Vector2.right));
        Assert.That(Number("CurrentHealth"), Is.EqualTo(52.4f).Within(0.001f));
        Hit(FoundationBullet(type), EnemyAt(Vector2.up));
        Assert.That(Number("CurrentHealth"), Is.EqualTo(52.4f).Within(0.001f));
        TickDerived(0.5f);
        Hit(FoundationBullet(type), EnemyAt(Vector2.left));
        Assert.That(Number("CurrentHealth"), Is.EqualTo(54.8f).Within(0.001f));
    }

    [TestCase("Bullet")]
    [TestCase("PiercingBullet")]
    public void RicochetHuntAddsDamageOnlyAfterSuccessfulBounce(string type)
    {
        Acquire("ricochet_hunt");
        Acquire("antibody_ricochet", 2);
        Component bullet = FoundationBullet(type, 1);
        Component first = EnemyAt(Vector2.right);
        Component second = EnemyAt(Vector2.up);
        Component third = EnemyAt(Vector2.left);
        Component fourth = EnemyAt(Vector2.down);
        Hit(bullet, first);
        Hit(bullet, second);
        Hit(bullet, third);
        Hit(bullet, fourth);
        Assert.That(Value(first, "CurrentHealth"), Is.EqualTo(90f));
        Assert.That(Value(second, "CurrentHealth"), Is.EqualTo(90f));
        Assert.That(Value(third, "CurrentHealth"), Is.EqualTo(86f));
        Assert.That(Value(fourth, "CurrentHealth"), Is.EqualTo(83f));
        Assert.That((bool)bullet.GetType().GetField("spent", Private).GetValue(bullet), Is.True);
    }

    [TestCase("Bullet")]
    [TestCase("PiercingBullet")]
    public void MatureExplosionWaitsForAgeAndExplodesOnceExcludingDirectVictim(string type)
    {
        Acquire("mature_explosion");
        Component bullet = FoundationBullet(type, 10);
        object effects = ProjectileEffects(bullet);
        Component first = EnemyAt(Vector2.right);
        Component second = EnemyAt(Vector2.up);
        Component bystander = EnemyAt(Vector2.zero);
        Component far = EnemyAt(new Vector2(10f, 0f));
        Call(effects, "Steer", Vector2.zero, Vector2.right, 0f);
        Hit(bullet, first);
        Assert.That(Value(bystander, "CurrentHealth"), Is.EqualTo(100f));
        Call(effects, "Steer", Vector2.zero, Vector2.right, 1f); // 无追踪也必须增长弹龄。
        Hit(bullet, second);
        Assert.That(Value(second, "CurrentHealth"), Is.EqualTo(90f));
        Assert.That(Value(bystander, "CurrentHealth"), Is.EqualTo(95f));
        Assert.That(Value(first, "CurrentHealth"), Is.EqualTo(85f));
        Assert.That(Value(far, "CurrentHealth"), Is.EqualTo(100f));
        Hit(bullet, bystander);
        Assert.That(Value(first, "CurrentHealth"), Is.EqualTo(85f));
    }

    [Test]
    public void MatureExplosionSnapshotsAcquisitionAndCannotSurviveOwnerReinitialization()
    {
        Component old = FoundationBullet("Bullet", 10);
        Acquire("mature_explosion");
        Component fresh = FoundationBullet("Bullet", 10);
        Component target = EnemyAt(Vector2.right);
        Component bystander = EnemyAt(Vector2.up);
        Call(ProjectileEffects(old), "Steer", Vector2.zero, Vector2.right, 2f);
        Hit(old, target);
        Assert.That(Value(bystander, "CurrentHealth"), Is.EqualTo(100f));
        Call(player, "Initialize", data);
        Call(ProjectileEffects(fresh), "Steer", Vector2.zero, Vector2.right, 2f);
        Hit(fresh, target);
        Assert.That(Value(bystander, "CurrentHealth"), Is.EqualTo(100f));
    }

    [Test]
    public void DerivedPulsesRespectLayersScenesDisabledOwnerAndDeath()
    {
        Acquire("regeneration_pulse");
        player.GetType().GetField("enemyLayers", Private).SetValue(player, (LayerMask)(1 << 7));
        Component normal = EnemyAt(Vector2.right);
        Component excluded = EnemyAt(Vector2.right, 0);
        Component elsewhere = EnemyAt(Vector2.right);
        otherScene = SceneManager.CreateScene("Derived Other");
        SceneManager.MoveGameObjectToScene(elsewhere.gameObject, otherScene);
        // 预先存满一次治疗脉冲，再验证禁用、死亡和敌人筛选。
        Call(player, "TakeDamage", 40);
        Call(Derived, "OnHealed", 20f);
        EndInvincibility();
        foreach (Component enemy in new[] { normal, excluded, elsewhere })
            enemy.GetType().GetField("currentHealth", Private).SetValue(enemy, 100f);
        Call(Derived, "OnHealed", 20f);
        player.gameObject.SetActive(false);
        TickDerived(6f);
        Assert.That(Value(normal, "CurrentHealth"), Is.EqualTo(100f));
        player.gameObject.SetActive(true);
        TickDerived(6f);
        Assert.That(Value(normal, "CurrentHealth"), Is.EqualTo(70f));
        Assert.That(Value(excluded, "CurrentHealth"), Is.EqualTo(100f));
        Assert.That(Value(elsewhere, "CurrentHealth"), Is.EqualTo(100f));
        TickDerived(6f);
        Call(player, "TakeDamage", 1000);
        Call(Derived, "OnHealed", 20f);
        TickDerived(6f);
        Assert.That(Value(normal, "CurrentHealth"), Is.EqualTo(70f));
    }
}
