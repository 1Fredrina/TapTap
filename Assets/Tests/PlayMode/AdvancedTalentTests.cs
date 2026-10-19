using NUnit.Framework;
using System.Linq;
using UnityEngine;

public partial class ReactiveTalentTests
{
    private object Advanced => Value(player, "AdvancedEffects");
    private void TickAdvanced(float seconds) => Call(Advanced, "Tick", seconds);
    private void AdvancedKill(string id, Vector2 at)
    {
        Component source = EnemyAt(at);
        Call(Advanced, "OnEnemyDefeated", source);
    }

    [Test]
    public void NineMechanicAndFiveTerminalTalentsLoadAndUnlockFromTheirConfiguredPrerequisites()
    {
        string[] foundationIds =
        {
            "damage_echo", "dash_bloom", "kill_bank", "stillness_charge", "low_health_burst",
            "shield_thorns", "phase_shift", "execution_mark", "ammo_cycle"
        };
        string[] terminalIds =
        {
            "apex_protocol", "bio_singularity", "immortal_cycle", "arsenal_overdrive", "perfect_hunt"
        };

        foreach (string id in foundationIds.Concat(terminalIds))
        {
            Assert.That(talents[id].GetType().Name, Is.EqualTo("AdvancedTalent"), id);
            var maxStack = (int)talents[id].GetType().GetField("maxStack").GetValue(talents[id]);
            Assert.That(maxStack, Is.EqualTo(terminalIds.Contains(id) ? 1 : 3), id);
            if (terminalIds.Contains(id))
            {
                Assert.That(Call(player, "HasPrerequisites", talents[id]), Is.False, id);
                foreach (string prerequisite in (string[])talents[id].GetType().GetField("prerequisiteIds").GetValue(talents[id]))
                    Acquire(prerequisite);
                Assert.That(Call(player, "HasPrerequisites", talents[id]), Is.True, id);
            }
            else Assert.That(Call(player, "HasPrerequisites", talents[id]), Is.True, id);
        }
    }

    [Test]
    public void DamageEchoAndStillnessChargeAreConsumedBySingleProjectiles()
    {
        Acquire("damage_echo");
        Acquire("stillness_charge");
        TickAdvanced(2f);
        Call(player, "TakeDamage", 10);

        Component first = EnemyAt(Vector2.right);
        Hit(FoundationBullet("Bullet"), first);
        Assert.That(Value(first, "CurrentHealth"), Is.EqualTo(70f)); // 静止 1.5 倍后，再受击回响翻倍。

        Component second = EnemyAt(Vector2.up);
        Hit(FoundationBullet("Bullet"), second);
        Assert.That(Value(second, "CurrentHealth"), Is.EqualTo(90f));
    }

    [Test]
    public void DashBloomAndAmmoCycleCreateNonRecursiveBlastOnFollowingHit()
    {
        Acquire("dash_bloom");
        Call(player, "OnDashPerformed");
        Component direct = EnemyAt(Vector2.right);
        Component bystander = EnemyAt(new Vector2(1f, 1f));
        Hit(FoundationBullet("Bullet"), direct);
        Assert.That(Value(direct, "CurrentHealth"), Is.EqualTo(90f));
        Assert.That(Value(bystander, "CurrentHealth"), Is.EqualTo(94f));
        Assert.That(Value(bystander, "IsStunned"), Is.True);

        Call(player, "Initialize", data);
        Acquire("ammo_cycle");
        for (int i = 0; i < 6; i++)
            Hit(FoundationBullet("Bullet"), EnemyAt(new Vector2(i * 10f, 0f)));

        Component nextHit = EnemyAt(new Vector2(100f, 0f));
        Component blastTarget = EnemyAt(new Vector2(101f, 1f));
        Hit(FoundationBullet("Bullet"), nextHit);
        Assert.That(Value(nextHit, "CurrentHealth"), Is.EqualTo(90f));
        Assert.That(Value(blastTarget, "CurrentHealth"), Is.EqualTo(95f));
    }

    [Test]
    public void KillBankUsesWindowAndRankChangesItsThresholdAndReward()
    {
        Acquire("kill_bank", 2);
        for (int i = 0; i < 3; i++) AdvancedKill("kill_bank", Vector2.right * i);
        Assert.That(Number("CurrentShield"), Is.Zero);
        AdvancedKill("kill_bank", Vector2.right * 4);
        Assert.That(Number("CurrentShield"), Is.EqualTo(20f));

        Call(player, "Initialize", data);
        Acquire("kill_bank");
        for (int i = 0; i < 4; i++) AdvancedKill("kill_bank", Vector2.right * i);
        TickAdvanced(8f);
        AdvancedKill("kill_bank", Vector2.right * 5);
        Assert.That(Number("CurrentShield"), Is.Zero);
    }

    [Test]
    public void ShieldThornsAndPhaseShiftTriggerFromTheirMatchingDamageEvents()
    {
        Acquire("shield_thorns");
        Acquire("phase_shift");
        Component thornTarget = EnemyAt(Vector2.right);
        Call(player, "AddShield", 5f);
        Call(player, "TakeDamage", 10);
        Assert.That(Value(thornTarget, "CurrentHealth"), Is.EqualTo(75f));

        Component projectileTarget = EnemyAt(Vector2.up);
        Component bullet = FoundationBullet("Bullet");
        Assert.That((int)bullet.GetType().GetField("pierceRemaining", Private).GetValue(bullet), Is.EqualTo(1));
        Hit(bullet, projectileTarget);
        Assert.That(Value(projectileTarget, "CurrentHealth"), Is.EqualTo(90f));
    }

    [Test]
    public void LowHealthBurstAndExecutionMarkUseEnemyHealthAndPlayerHealthThresholds()
    {
        Acquire("low_health_burst");
        Call(player, "TakeDamage", 70);
        Component direct = EnemyAt(Vector2.right);
        Component bystander = EnemyAt(new Vector2(1f, 1f));
        Hit(FoundationBullet("Bullet"), direct);
        Assert.That(Value(direct, "CurrentHealth"), Is.EqualTo(90f));
        Assert.That(Value(bystander, "CurrentHealth"), Is.EqualTo(92f));

        Call(player, "Initialize", data);
        Acquire("execution_mark");
        Component lowHealthEnemy = EnemyAt(Vector2.right);
        Call(lowHealthEnemy, "TakeDamage", 80);
        Hit(FoundationBullet("Bullet"), lowHealthEnemy);
        Assert.That(Value(lowHealthEnemy, "CurrentHealth"), Is.Zero);
    }

    [Test]
    public void ApexProtocolWaitsForKillThresholdThenFiresOneCappedPulse()
    {
        Acquire("apex_protocol");
        Component location = EnemyAt(Vector2.right);
        Component target = EnemyAt(Vector2.up);
        for (int i = 0; i < 11; i++) Call(Advanced, "OnEnemyDefeated", location);
        Assert.That(Value(target, "CurrentHealth"), Is.EqualTo(100f));
        Call(Advanced, "OnEnemyDefeated", location);
        Assert.That(Value(target, "CurrentHealth"), Is.EqualTo(30f));
        Assert.That(Value(target, "IsStunned"), Is.True);
        for (int i = 0; i < 12; i++) Call(Advanced, "OnEnemyDefeated", location);
        Assert.That(Value(target, "CurrentHealth"), Is.EqualTo(30f));
    }

    [Test]
    public void BioSingularityRequiresHealingAndKillsWithinOneWindowAndDoesNotRecursivelyCharge()
    {
        Acquire("bio_singularity");
        Component location = EnemyAt(Vector2.right);
        Component survivor = EnemyAt(Vector2.up);
        Call(player, "TakeDamage", 50);
        Call(player, "Heal", 30f);
        for (int i = 0; i < 7; i++) Call(Advanced, "OnEnemyDefeated", location);
        Assert.That(Value(survivor, "CurrentHealth"), Is.EqualTo(100f));
        Call(Advanced, "OnEnemyDefeated", location);
        Assert.That(Value(survivor, "CurrentHealth"), Is.EqualTo(0f));
        Assert.That(Number("CurrentHealth"), Is.EqualTo(95f));
    }

    [Test]
    public void ImmortalCyclePreventsOnlyOneDeathUntilCooldownEnds()
    {
        Acquire("immortal_cycle");
        Call(player, "TakeDamage", 1000);
        Assert.That(Number("CurrentHealth"), Is.EqualTo(1f));
        Assert.That(Number("CurrentShield"), Is.EqualTo(50f));
        Assert.That(Value(player, "IsInvincible"), Is.True);
        EndInvincibility();
        Call(player, "TakeDamage", 1000);
        Assert.That(Value(player, "IsDead"), Is.True);
    }

    [Test]
    public void ArsenalOverdriveAndPerfectHuntBuildTheirTemporaryAndStoredCharges()
    {
        Acquire("arsenal_overdrive");
        for (int i = 0; i < 8; i++) AdvancedKill("arsenal_overdrive", Vector2.right * i);
        Assert.That((float)Value(Advanced, "WeaponAttackSpeedMultiplier"), Is.EqualTo(1.75f));
        object boosted = Call(Advanced, "CreateProjectileState");
        Assert.That((float)boosted.GetType().GetField("damageMultiplier").GetValue(boosted), Is.EqualTo(1.75f));
        TickAdvanced(6f);
        Assert.That((float)Value(Advanced, "WeaponAttackSpeedMultiplier"), Is.EqualTo(1f));

        Call(player, "Initialize", data);
        Acquire("perfect_hunt");
        for (int i = 0; i < 10; i++) AdvancedKill("perfect_hunt", Vector2.right * i);
        for (int i = 0; i < 3; i++)
        {
            object state = Call(Advanced, "CreateProjectileState");
            Assert.That(state.GetType().GetField("guaranteedCritical").GetValue(state), Is.True);
            Assert.That(Call(Advanced, "GetBonusPierce", state), Is.EqualTo(1));
        }
        object spentState = Call(Advanced, "CreateProjectileState");
        Assert.That(spentState.GetType().GetField("guaranteedCritical").GetValue(spentState), Is.False);
    }
}
