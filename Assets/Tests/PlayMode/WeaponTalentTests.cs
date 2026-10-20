using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class WeaponTalentTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private Scene scene, previousScene;
    private Component player;
    private object data;
    private FieldInfo activePlayer;
    private object previousPlayer;
    private FieldInfo managerInstance;
    private object previousManager;
    private Dictionary<string, object> talents;
    private readonly List<GameObject> objects = new List<GameObject>();

    private static Type GameType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Hidden | BindingFlags.Public).SetValue(target, value);
    private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target, args);
    private static object Value(object target, string property) => target.GetType().GetProperty(property).GetValue(target);

    [SetUp]
    public void SetUp()
    {
        previousScene = SceneManager.GetActiveScene();
        scene = SceneManager.CreateScene("Weapon Talents " + Guid.NewGuid(), new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        SceneManager.SetActiveScene(scene);
        activePlayer = GameType("Player").GetField("activePlayer", BindingFlags.Static | BindingFlags.NonPublic);
        previousPlayer = activePlayer.GetValue(null);
        activePlayer.SetValue(null, null);
        managerInstance = GameType("GameManager").GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        previousManager = managerInstance.GetValue(null);
        managerInstance.SetValue(null, null);

        GameObject playerObject = new GameObject("Weapon Test Player");
        objects.Add(playerObject);
        SceneManager.MoveGameObjectToScene(playerObject, scene);
        player = playerObject.AddComponent(GameType("Player"));
        data = Activator.CreateInstance(GameType("PlayerData"));
        Set(data, "MaxHealth", 100f);
        Set(data, "AttackDamage", 10);
        Set(data, "BaseExperienceToNextLevel", 100000);
        Call(player, "Initialize", data);

        Type entriesType = typeof(List<>).MakeGenericType(GameType("TalentJsonEntry"));
        MethodInfo read = GameType("LitJson.JsonMapper").GetMethods().Single(method =>
            method.Name == "ToObject" && method.IsGenericMethodDefinition && method.GetParameters().Length == 1 &&
            method.GetParameters()[0].ParameterType == typeof(string));
        object entries = read.MakeGenericMethod(entriesType).Invoke(null,
            new object[] { File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "TalentData.json")) });
        var converted = (IEnumerable)GameType("TalentConverter").GetMethod("Convert").Invoke(null, new[] { entries });
        talents = converted.Cast<object>().ToDictionary(t => (string)t.GetType().GetField("id").GetValue(t));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject obj in scene.GetRootGameObjects()) Object.DestroyImmediate(obj);
        objects.Clear();
        activePlayer.SetValue(null, previousPlayer);
        managerInstance.SetValue(null, previousManager);
        SceneManager.SetActiveScene(previousScene);
    }

    [UnityTearDown]
    public IEnumerator UnloadScene()
    {
        if (scene.IsValid()) yield return SceneManager.UnloadSceneAsync(scene);
    }

    [Test]
    public void ShotgunTalentCreatesSixSpreadBullets()
    {
        Assert.That(Call(player, "TryAddTalent", talents["weapon_shotgun"]), Is.True);
        object weapon = ((IEnumerable)Value(player, "Weapons")).Cast<object>().Single(w => w.GetType().Name == "ShotgunWeapon");
        Component target = NewEnemy(new Vector2(0f, 5f));
        Call(weapon, "Fire", player, target);
        Call(weapon, "Tick", 0.1f);

        Component[] spawned = Object.FindObjectsByType(GameType("Bullet"), FindObjectsSortMode.None)
            .Cast<Component>()
            .Where(b => b.gameObject.scene == scene).ToArray();
        Assert.That(spawned.Length, Is.EqualTo(6));
        float[] angles = spawned.Select(b => Mathf.DeltaAngle(90f, b.transform.eulerAngles.z)).OrderBy(a => a).ToArray();
        Assert.That(angles[0], Is.EqualTo(-21f).Within(0.01f));
        Assert.That(angles[5], Is.EqualTo(21f).Within(0.01f));
        Assert.That(spawned.All(b => (float)b.GetType().GetField("lifeTimer", Hidden).GetValue(b) > 0f), Is.True);
        Assert.That(Call(player, "TryAddTalent", talents["weapon_shotgun"]), Is.True);
        Assert.That(Call(player, "TryAddTalent", talents["weapon_shotgun"]), Is.True);
        Assert.That(Call(player, "TryAddTalent", talents["weapon_shotgun"]), Is.False);
        Assert.That(Call(weapon, "GetShotCount"), Is.EqualTo(3));
    }

    [Test]
    public void BrimstoneIsSpecialAndOnlyAcquiredOnce()
    {
        object talent = talents["special_brimstone"];

        Assert.That(Call(player, "TryAddTalent", talent), Is.True);
        Assert.That(Call(player, "TryAddTalent", talent), Is.False);
        var weapons = ((IEnumerable)Value(player, "Weapons")).Cast<object>().ToList();
        Assert.That(weapons.Count(w => w.GetType().Name == "BrimstoneWeapon"), Is.EqualTo(1));

        Component enemy = NewEnemy(new Vector2(4f, 0f));
        Component behindFirst = NewEnemy(new Vector2(10f, 0f));
        Component offAxis = NewEnemy(new Vector2(4f, 2f));
        Component behindPlayer = NewEnemy(new Vector2(-5f, 0f));
        Component tooFar = NewEnemy(new Vector2(19f, 0f));
        object brimstone = weapons.Single(w => w.GetType().Name == "BrimstoneWeapon");
        Call(brimstone, "Fire", player, enemy);
        Call(brimstone, "Tick", 0.1f);
        Assert.That((float)Value(enemy, "CurrentHealth"), Is.EqualTo(70f));
        Assert.That((float)Value(behindFirst, "CurrentHealth"), Is.EqualTo(70f));
        Assert.That((float)Value(offAxis, "CurrentHealth"), Is.EqualTo(100f));
        Assert.That((float)Value(behindPlayer, "CurrentHealth"), Is.EqualTo(100f));
        Assert.That((float)Value(tooFar, "CurrentHealth"), Is.EqualTo(100f));
    }

    [Test]
    public void BrimstoneFiresWithoutInputAndRespectsCooldownAndPause()
    {
        Call(player, "TryAddTalent", talents["special_brimstone"]);
        object weapon = ((IEnumerable)Value(player, "Weapons")).Cast<object>().Single(w => w.GetType().Name == "BrimstoneWeapon");
        Assert.That(Value(weapon, "RequiresTarget"), Is.False);
        Call(weapon, "Fire", player, null);
        Call(weapon, "Tick", 0f);
        Assert.That(scene.GetRootGameObjects().Count(o => o.name == "Brimstone Beam"), Is.Zero);
        Call(weapon, "Tick", 0.1f);
        Assert.That(scene.GetRootGameObjects().Count(o => o.name == "Brimstone Beam"), Is.EqualTo(1));
        Call(weapon, "ResetCooldown", 1f);
        Assert.That(Call(weapon, "TickCooldown", 0f), Is.False);
        Assert.That(Call(weapon, "TickCooldown", 3f), Is.False);
        Assert.That(Call(weapon, "TickCooldown", 1f), Is.False);
        Assert.That(Call(weapon, "TickCooldown", 0.02f), Is.True);
    }

    [Test]
    public void MeleeRangeVisualMatchesDamageRadiusAndArc()
    {
        player.transform.localScale = Vector3.one * 0.5f;
        Call(player, "TryAddTalent", talents["weapon_melee"]);
        object weapon = ((IEnumerable)Value(player, "Weapons")).Cast<object>().Single(w => w.GetType().Name == "MeleeWeapon");
        Component target = NewEnemy(new Vector2(0f, 2f));
        Component outsideArc = NewEnemy(new Vector2(2f, 0f));
        Component outsideRadius = NewEnemy(new Vector2(0f, 2.6f));
        Call(weapon, "Fire", player, target);
        Call(weapon, "Tick", 0.1f);

        GameObject effect = scene.GetRootGameObjects().Single(o => o.name == "Complement Impact Range");
        LineRenderer line = effect.GetComponent<LineRenderer>();
        Assert.That(line.sharedMaterial.shader.isSupported, Is.True);
        Assert.That(line.loop, Is.True);
        Assert.That(Vector3.Distance(line.GetPosition(0), Vector3.zero), Is.LessThan(0.001f));
        for (int i = 1; i < line.positionCount; i++)
            Assert.That(line.transform.TransformPoint(line.GetPosition(i)).magnitude, Is.EqualTo(2.5f).Within(0.001f));
        Vector3 firstEdge = line.transform.TransformDirection(line.GetPosition(1));
        Vector3 lastEdge = line.transform.TransformDirection(line.GetPosition(line.positionCount - 1));
        Assert.That(Vector3.Angle(firstEdge, Vector3.up), Is.EqualTo(60f).Within(0.01f));
        Assert.That(Vector3.Angle(lastEdge, Vector3.up), Is.EqualTo(60f).Within(0.01f));
        Assert.That((float)Value(target, "CurrentHealth"), Is.EqualTo(80f));
        Assert.That((float)Value(outsideArc, "CurrentHealth"), Is.EqualTo(100f));
        Assert.That((float)Value(outsideRadius, "CurrentHealth"), Is.EqualTo(100f));
    }

    [Test]
    public void AuraRangeFollowsOwnerAndHidesWhileCharacterIsInactive()
    {
        player.transform.localScale = Vector3.one * 0.5f;
        Call(player, "TryAddTalent", talents["weapon_aura"]);
        object weapon = ((IEnumerable)Value(player, "Weapons")).Cast<object>().Single(w => w.GetType().Name == "AuraWeapon");
        GameObject effect = scene.GetRootGameObjects().Single(o => o.name == "Inflammation Aura Range");
        Component visual = effect.GetComponent(GameType("WeaponRangeVisual"));
        LineRenderer line = effect.GetComponent<LineRenderer>();
        MeshRenderer fill = effect.GetComponentInChildren<MeshRenderer>();
        Assert.That(line.sharedMaterial.shader.isSupported, Is.True);
        Assert.That(line.loop, Is.True);
        for (int i = 0; i < line.positionCount; i++)
            Assert.That(line.GetPosition(i).magnitude, Is.EqualTo(3f).Within(0.001f));

        player.transform.position = new Vector3(4f, 5f, 0f);
        player.transform.localScale = new Vector3(2f, 0.75f, 1f);
        MethodInfo update = visual.GetType().GetMethod("LateUpdate", Hidden);
        update.Invoke(visual, null);
        Assert.That(effect.transform.position, Is.EqualTo(player.transform.position));
        Assert.That(Vector3.Distance(line.transform.TransformPoint(line.GetPosition(0)), player.transform.position),
            Is.EqualTo(3f).Within(0.001f));

        player.gameObject.SetActive(false);
        update.Invoke(visual, null);
        Assert.That(line.enabled, Is.False);
        Assert.That(fill.enabled, Is.False);
        player.gameObject.SetActive(true);
        update.Invoke(visual, null);
        Assert.That(line.enabled, Is.True);
        Assert.That(fill.enabled, Is.True);

        Call(player, "TryAddTalent", talents["weapon_aura"]);
        Assert.That(effect.activeSelf, Is.False);
        Assert.That(scene.GetRootGameObjects().Count(o => o.name == "Inflammation Aura Range" && o.activeSelf), Is.EqualTo(1));
        Call(weapon, "Clear");
        Assert.That(scene.GetRootGameObjects().Count(o => o.name == "Inflammation Aura Range" && o.activeSelf), Is.Zero);
    }

    private Component NewEnemy(Vector2 position)
    {
        GameObject enemyObject = new GameObject("Weapon Test Enemy");
        objects.Add(enemyObject);
        enemyObject.layer = 7;
        enemyObject.transform.position = position;
        Component enemy = enemyObject.AddComponent(GameType("Enemy"));
        object enemyData = Activator.CreateInstance(GameType("EnemyData"));
        Set(enemyData, "MaxHealth", 100f);
        Set(enemyData, "MoveSpeed", 0f);
        enemy.GetType().GetMethod("Initialize", new[] { enemyData.GetType() }).Invoke(enemy, new[] { enemyData });
        return enemy;
    }
}
