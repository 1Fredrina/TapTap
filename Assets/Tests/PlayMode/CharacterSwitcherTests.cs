using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class CharacterSwitcherTests
{
    private Scene scene;
    private GameObject manager;
    private GameObject outgoing;
    private GameObject incoming;
    private Component switcher;
    private Component player;
    private Animator animator;
    private FieldInfo instanceField;
    private object previousInstance;

    [SetUp]
    public void SetUp()
    {
        scene = SceneManager.CreateScene("Character Switch Test " + Guid.NewGuid(),
            new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        manager = new GameObject("Test Manager");
        SceneManager.MoveGameObjectToScene(manager, scene);
        // 不运行 GameManager 的队伍生成逻辑，使用实际预制体组成测试队伍。
        manager.SetActive(false);
        Type managerType = Type.GetType("GameManager, Assembly-CSharp", true);
        instanceField = managerType.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        previousInstance = instanceField.GetValue(null);
        instanceField.SetValue(null, manager.AddComponent(managerType));
        switcher = manager.AddComponent(Type.GetType("CharacterSwitcher, Assembly-CSharp", true));
    }

    [TearDown]
    public void TearDown()
    {
        if (outgoing != null) Object.DestroyImmediate(outgoing);
        if (incoming != null) Object.DestroyImmediate(incoming);
        if (manager != null) Object.DestroyImmediate(manager);
        instanceField.SetValue(null, previousInstance);
    }

    [UnityTearDown]
    public IEnumerator CloseScene()
    {
        if (scene.IsValid())
            yield return SceneManager.UnloadSceneAsync(scene);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void SwitchingDuringAttackClearsAttackLock(int character)
    {
        CreateTeam(character);
        Attack();
        animator.Update(0.15f);
        Assert.That(IsAttacking(), Is.True);

        SwitchTo(1);

        Assert.That(IsAttacking(), Is.False, "切走时不能等待已中断动画的结束事件。");
        Assert.That(outgoing.activeSelf, Is.False);
        Assert.That(incoming.activeSelf, Is.True);
    }

    [TestCase(1, 0.05f)]
    [TestCase(1, 0.15f)]
    [TestCase(1, 0.3f)]
    [TestCase(2, 0.15f)]
    [TestCase(3, 0.15f)]
    [TestCase(4, 0.15f)]
    public void SwitchingBackRestoresIdlePoseAndAnimation(int character, float attackTime)
    {
        CreateTeam(character);
        Transform[] parts = outgoing.GetComponentsInChildren<Transform>(true);
        Vector3[] positions = Array.ConvertAll(parts, part => part.localPosition);
        Quaternion[] rotations = Array.ConvertAll(parts, part => part.localRotation);
        bool[] activeStates = Array.ConvertAll(parts, part => part.gameObject.activeSelf);
        Attack();
        animator.Update(attackTime);

        SwitchTo(1);
        incoming.transform.position = new Vector3(7f, 3f, 0f);
        SwitchTo(0);
        animator.Update(0f);

        Assert.That(outgoing.transform.position, Is.EqualTo(incoming.transform.position));
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), Is.True);
        for (int i = 1; i < parts.Length; i++)
        {
            Assert.That(parts[i].gameObject.activeSelf, Is.EqualTo(activeStates[i]), parts[i].name + " 显隐");
            Assert.That(Vector3.Distance(parts[i].localPosition, positions[i]), Is.LessThan(0.001f), parts[i].name + " 位置");
            Assert.That(Quaternion.Angle(parts[i].localRotation, rotations[i]), Is.LessThan(0.01f), parts[i].name + " 旋转");
        }

        float time = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        animator.Update(0.1f);
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.GreaterThan(time));
        player.GetType().GetField("attackCooldownTimer", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(player, 0f);
        Attack();
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Atk"), Is.True);
        animator.Update(0.5f);
        Assert.That(IsAttacking(), Is.False, "再次攻击应该正常结束。");
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void SwitchingBeforeAttackIsEvaluatedDoesNotReplayAttack(int character)
    {
        CreateTeam(character);
        player.GetType().GetMethod("OnAttackInput").Invoke(player, null);
        SwitchTo(1);
        SwitchTo(0);
        animator.Update(0f);
        animator.Update(0.1f);

        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), Is.True);
        Assert.That(IsAttacking(), Is.False);
    }

    private void CreateTeam(int character)
    {
        outgoing = CreateCharacter(character);
        incoming = CreateCharacter(character % 4 + 1);
        incoming.SetActive(false);
        object gameManager = instanceField.GetValue(null);
        var team = (List<GameObject>)gameManager.GetType().GetField("teamMembers").GetValue(gameManager);
        team.Add(outgoing);
        team.Add(incoming);
        player = outgoing.GetComponent(Type.GetType("Player, Assembly-CSharp", true));
        animator = outgoing.GetComponent<Animator>();
        animator.Update(0f);
    }

    private GameObject CreateCharacter(int index)
    {
        GameObject character = Object.Instantiate(Resources.Load<GameObject>("Prefabs/Characters/" + index));
        SceneManager.MoveGameObjectToScene(character, scene);
        Component component = character.GetComponent(Type.GetType("Player, Assembly-CSharp", true));
        component.GetType().GetField("attackEffectPrefab", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(component, null);
        return character;
    }

    private void Attack()
    {
        player.GetType().GetMethod("OnAttackInput").Invoke(player, null);
        animator.Update(0f);
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Atk"), Is.True);
    }

    private bool IsAttacking() => (bool)player.GetType().GetProperty("IsAttacking").GetValue(player);

    private void SwitchTo(int index) => switcher.GetType().GetMethod("SwitchTo").Invoke(switcher, new object[] { index });
}
