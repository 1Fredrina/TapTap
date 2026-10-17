using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class AchievementTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags StaticHidden = BindingFlags.Static | BindingFlags.NonPublic;
    private string directory, path;
    private object progress, oldProgress, oldManager, oldPlayer, oldUI, ui;
    private Component manager, player;
    private float timeScale;
    private static Type TypeOf(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name).Invoke(target, args);
    private static object Value(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
    private static FieldInfo StaticField(string type, string name) => TypeOf(type).GetField(name, StaticHidden);
    private void Advance(float seconds) => manager.GetType().GetMethod("UpdateEnemyTalents", Hidden).Invoke(manager, new object[] { seconds });
    private object Definition(string id) => ((IEnumerable)TypeOf("AchievementProgress").GetProperty("Definitions").GetValue(null))
        .Cast<object>().Single(d => (string)Value(d, "Id") == id);
    private int Count(string id) => (int)Call(progress, "GetProgress", Definition(id));
    private bool Unlocked(string id) => (bool)Call(progress, "IsUnlocked", Definition(id));

    [SetUp]
    public void SetUp()
    {
        timeScale = Time.timeScale;
        Time.timeScale = 1f;
        directory = Path.Combine(Path.GetTempPath(), "AchievementTests_" + Guid.NewGuid().ToString("N"));
        path = Path.Combine(directory, "achievements.json");
        progress = Activator.CreateInstance(TypeOf("AchievementProgress"), path);
        oldProgress = StaticField("AchievementProgress", "instance").GetValue(null);
        oldManager = StaticField("GameManager", "instance").GetValue(null);
        oldPlayer = StaticField("Player", "activePlayer").GetValue(null);
        oldUI = StaticField("UIManager", "instance").GetValue(null);
        StaticField("AchievementProgress", "instance").SetValue(null, progress);
        StaticField("GameManager", "instance").SetValue(null, null);
        StaticField("Player", "activePlayer").SetValue(null, null);
        ui = Activator.CreateInstance(TypeOf("UIManager"), true);
        StaticField("UIManager", "instance").SetValue(null, ui);
        manager = new GameObject("Achievement Test Manager").AddComponent(TypeOf("GameManager"));
        ((Behaviour)manager).enabled = false;
        player = new GameObject("Achievement Test Player").AddComponent(TypeOf("Player"));
        manager.GetType().GetProperty("Player").SetValue(manager, player);
        manager.GetType().GetField("achievementProgress", Hidden).SetValue(manager, progress);
    }

    [TearDown]
    public void TearDown()
    {
        if (manager != null) Object.DestroyImmediate(manager.gameObject);
        if (player != null) Object.DestroyImmediate(player.gameObject);
        Transform canvas = (Transform)ui.GetType().GetField("canvasTrans", Hidden).GetValue(ui);
        if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
        StaticField("AchievementProgress", "instance").SetValue(null, oldProgress);
        StaticField("GameManager", "instance").SetValue(null, oldManager);
        StaticField("Player", "activePlayer").SetValue(null, oldPlayer);
        StaticField("UIManager", "instance").SetValue(null, oldUI);
        Time.timeScale = timeScale;
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    [Test]
    public void CatalogContainsTenUniqueAchievementsAndFreshSaveIsLocked()
    {
        var definitions = ((IEnumerable)TypeOf("AchievementProgress").GetProperty("Definitions").GetValue(null)).Cast<object>().ToList();
        Assert.That(definitions.Count, Is.EqualTo(10));
        Assert.That(definitions.Select(d => Value(d, "Id")).Distinct().Count(), Is.EqualTo(10));
        Assert.That(Value(progress, "UnlockedCount"), Is.EqualTo(0));
        Assert.That(File.Exists(path), Is.False);
    }

    [Test]
    public void RealBattleStartRecordsFirstRunAndInitialLevel()
    {
        Object.DestroyImmediate(player.gameObject);
        player = null;
        try
        {
            manager.GetType().GetMethod("Start", Hidden).Invoke(manager, null);
            player = (Component)Value(manager, "Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(Unlocked("first_run"), Is.True);
            Assert.That(Count("level_5"), Is.EqualTo(1));
            Assert.That(Unlocked("first_talent"), Is.False);
        }
        finally
        {
            var spawned = Value(manager, "PlayerObject") as GameObject;
            if (spawned != null) Object.DestroyImmediate(spawned);
        }
    }

    [Test]
    public void KillThresholdsUnlockExactlyAtTargetsAndPersistWithoutExplicitSave()
    {
        Call(progress, "RecordKill");
        Assert.That(Unlocked("first_kill"), Is.True);
        for (int i = 1; i < 49; i++) Call(progress, "RecordKill");
        Assert.That(Unlocked("kills_50"), Is.False);
        Call(progress, "RecordKill");
        Assert.That(Unlocked("kills_50"), Is.True);
        for (int i = 50; i < 199; i++) Call(progress, "RecordKill");
        Assert.That(Unlocked("kills_200"), Is.False);
        Call(progress, "RecordKill");
        Assert.That(Unlocked("kills_200"), Is.True);
        progress = Activator.CreateInstance(TypeOf("AchievementProgress"), path);
        Assert.That(Count("kills_200"), Is.EqualTo(200));
        Assert.That(Value(progress, "UnlockedCount"), Is.EqualTo(3));
    }

    [Test]
    public void NewRunsAccumulateKillsButDoNotAddTogetherLevelOrSurvival()
    {
        Call(progress, "BeginRun");
        Call(progress, "RecordLevel", 4);
        Call(progress, "RecordSurvival", 59.9f);
        for (int i = 0; i < 25; i++) Call(progress, "RecordKill");
        Call(progress, "BeginRun");
        Call(progress, "RecordLevel", 3);
        Call(progress, "RecordSurvival", 45f);
        for (int i = 0; i < 25; i++) Call(progress, "RecordKill");
        Assert.That(Unlocked("kills_50"), Is.True);
        Assert.That(Unlocked("level_5"), Is.False);
        Assert.That(Unlocked("survive_60"), Is.False);
        Call(progress, "RecordLevel", 10);
        Call(progress, "RecordSurvival", 300f);
        Call(progress, "Save");
        progress = Activator.CreateInstance(TypeOf("AchievementProgress"), path);
        Assert.That(Unlocked("level_10"), Is.True);
        Assert.That(Unlocked("survive_300"), Is.True);
    }

    [Test]
    public void EnemyDeathCountsOnceIncludingKillsWithoutExperienceAndStopsAfterGameOver()
    {
        Component enemy = new GameObject("Achievement Test Enemy").AddComponent(TypeOf("Enemy"));
        try
        {
            Call(enemy, "EliminateWithoutExperience");
            Call(enemy, "EliminateWithoutExperience");
            Assert.That(Count("kills_50"), Is.EqualTo(1));
            Call(manager, "OnPlayerDeath");
            TypeOf("Player").GetMethod("NotifyEnemyDefeated", new[] { typeof(int) }).Invoke(null, new object[] { 0 });
            Assert.That(Count("kills_50"), Is.EqualTo(1));
            Assert.That(Unlocked("first_win"), Is.False);
        }
        finally { if (enemy != null) Object.DestroyImmediate(enemy.gameObject); }
    }

    [Test]
    public void LevelAndTalentHooksTrackRealPlayerActionsAndRejectInvalidChoice()
    {
        // 禁用抽选池避免测试弹出随机面板；仍执行真实经验与等级逻辑。
        object gameData = TypeOf("GameDataMgr").GetProperty("Instance").GetValue(null);
        FieldInfo talents = gameData.GetType().GetField("talentDatas");
        object previousPool = talents.GetValue(gameData);
        try
        {
            talents.SetValue(gameData, null);
            for (int i = 1; i < 10; i++) Call(player, "AddExperience", (int)Value(player, "ExperienceToNextLevel"));
            Assert.That(Unlocked("level_5"), Is.True);
            Assert.That(Unlocked("level_10"), Is.True);
            object talent = Activator.CreateInstance(TypeOf("StatTalent"));
            talent.GetType().GetField("id").SetValue(talent, "achievement_test");
            Assert.That(Call(player, "ChooseTalent", talent), Is.False);
            Assert.That(Unlocked("first_talent"), Is.False);
            ((IList)player.GetType().GetField("pendingChoices", Hidden).GetValue(player)).Add(talent);
            Assert.That(Call(player, "ChooseTalent", talent), Is.True);
            Assert.That(Call(player, "ChooseTalent", talent), Is.False);
            Assert.That(Unlocked("first_talent"), Is.True);
        }
        finally { talents.SetValue(gameData, previousPool); }
    }

    [Test]
    public void SurvivalIgnoresPauseVictoryCountsOnceAndDefeatCannotWin()
    {
        Advance(59f);
        Time.timeScale = 0f;
        Advance(1000f);
        Assert.That(Count("survive_60"), Is.EqualTo(59));
        Time.timeScale = 1f;
        Advance(1f);
        Assert.That(Unlocked("survive_60"), Is.True);
        Advance(1140f);
        Assert.That(Unlocked("survive_300"), Is.True);
        Assert.That(Unlocked("first_win"), Is.True);
        manager.GetType().GetMethod("OnVictory", Hidden).Invoke(manager, null);
        Call(manager, "OnPlayerDeath");
        var json = File.ReadAllText(path);
        Assert.That(json, Does.Contain("\"victories\": 1"));
        Assert.That(Value(manager, "IsVictory"), Is.True);
    }

    [Test]
    public void PanelPrefabShowsTenBoundRowsAndRefreshesAfterProgressChanges()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/AchievementPanel");
        GameObject go = Object.Instantiate(prefab);
        try
        {
            Component panel = go.GetComponent(TypeOf("AchievementPanel"));
            Call(panel, "ShowMe");
            var rows = (Text[])panel.GetType().GetField("achievementTexts").GetValue(panel);
            var summary = (Text)panel.GetType().GetField("txtSummary").GetValue(panel);
            Assert.That(rows.Length, Is.EqualTo(10));
            Assert.That(rows.All(t => t != null && t.text.Contains("未完成")), Is.True);
            Call(progress, "BeginRun");
            Assert.That(summary.text, Does.Contain("1 / 10"));
            Assert.That(rows[0].text, Does.Contain("已完成"));
            Assert.That(rows[0].text, Does.Contain("1/1"));
            go.SetActive(false);
            Call(progress, "RecordKill");
            go.SetActive(true);
            Assert.That(summary.text, Does.Contain("2 / 10"));
        }
        finally { Object.DestroyImmediate(go); }
    }
}
