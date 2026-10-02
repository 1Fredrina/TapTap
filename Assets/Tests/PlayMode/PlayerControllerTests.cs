using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class PlayerControllerTests
{
    private Scene scene;
    private PhysicsScene2D physics;
    private Component controller;
    private Rigidbody2D body;
    private GameObject floor;

    [SetUp]
    public void SetUp()
    {
        scene = SceneManager.CreateScene("Movement Test " + Guid.NewGuid(),
            new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        physics = scene.GetPhysicsScene2D();
        floor = Box("Floor", new Vector2(0f, -0.5f), new Vector2(20f, 1f));
        GameObject player = new GameObject("Player");
        SceneManager.MoveGameObjectToScene(player, scene);
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 3f;
        player.AddComponent<CapsuleCollider2D>().size = new Vector2(1f, 2f);
        controller = player.AddComponent(Type.GetType("PlayerController, Assembly-CSharp", true));
        Step(60);
    }

    [TearDown]
    public void TearDown()
    {
        if (controller != null)
            Object.DestroyImmediate(controller.gameObject);
    }

    [UnityTearDown]
    public IEnumerator CloseScene()
    {
        if (scene.IsValid())
            yield return SceneManager.UnloadSceneAsync(scene);
    }

    [Test]
    public void LegacyInputCanBeRead()
    {
        Assert.DoesNotThrow(() => controller.GetType()
            .GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, null));
    }

    [Test]
    public void MovesInBothDirectionsAndStops()
    {
        Set("moveInput", 1f);
        Step(10);
        Assert.That(body.position.x, Is.GreaterThan(0.5f));
        float rightPosition = body.position.x;
        Set("moveInput", -1f);
        Step(10);
        Assert.That(body.position.x, Is.LessThan(rightPosition - 0.5f));
        Set("moveInput", 0f);
        Step();
        Assert.That(body.linearVelocity.x, Is.Zero.Within(0.01f));
    }

    [Test]
    public void JumpsToConfiguredHeightWithoutAirJump()
    {
        float start = body.position.y;
        Set("jumpRequested", true);
        Step();
        float initialVelocity = body.linearVelocity.y;
        Assert.That(initialVelocity, Is.GreaterThan(0f));
        Set("jumpRequested", true);
        Step();
        Assert.That(body.linearVelocity.y, Is.LessThan(initialVelocity));

        float apex = body.position.y;
        for (int i = 0; i < 100; i++)
        {
            Step();
            apex = Mathf.Max(apex, body.position.y);
        }
        Assert.That(apex - start, Is.EqualTo(2.5f).Within(0.2f));
        Set("jumpRequested", true);
        Step();
        Assert.That(body.linearVelocity.y, Is.GreaterThan(0f));
    }

    [Test]
    public void DashUsesLastDirectionExpiresAndPreservesGravity()
    {
        Set("moveInput", -1f);
        Step();
        Set("moveInput", 0f);
        Set("dashRequested", true);
        Step();
        Assert.That(body.linearVelocity.x, Is.LessThan(-12f));
        Assert.That(body.gravityScale, Is.EqualTo(3f));
        Step(10);
        Assert.That(body.linearVelocity.x, Is.Zero.Within(0.01f));
    }

    [Test]
    public void DashDoesNotPassThroughThinWall()
    {
        Box("Wall", new Vector2(2.5f, 2f), new Vector2(0.1f, 4f));
        Physics2D.SyncTransforms();
        Set("dashRequested", true);
        Step(12);
        Assert.That(body.position.x, Is.GreaterThan(0.5f));
        Assert.That(body.position.x, Is.LessThan(2.02f));
    }

    [Test]
    public void TouchingWallDoesNotEnableJump()
    {
        floor.SetActive(false);
        body.position = new Vector2(0f, 4f);
        Box("Wall", new Vector2(1f, 4f), new Vector2(1f, 20f));
        Physics2D.SyncTransforms();
        Set("moveInput", 1f);
        Step(8);
        Set("jumpRequested", true);
        Step();
        Assert.That(body.linearVelocity.y, Is.LessThanOrEqualTo(0f));
    }

    [Test]
    public void DisableClearsDashAndPendingJump()
    {
        Set("dashRequested", true);
        Step();
        ((Behaviour)controller).enabled = false;
        Assert.That(body.linearVelocity.x, Is.Zero);
        ((Behaviour)controller).enabled = true;
        Step();
        Assert.That(body.linearVelocity.x, Is.Zero.Within(0.01f));
        Assert.That(body.linearVelocity.y, Is.LessThanOrEqualTo(0.05f));
    }

    private GameObject Box(string name, Vector2 position, Vector2 size)
    {
        GameObject box = new GameObject(name);
        SceneManager.MoveGameObjectToScene(box, scene);
        box.transform.position = position;
        box.AddComponent<BoxCollider2D>().size = size;
        return box;
    }

    // 只注入按键产生的请求，物理计算使用实际的 Rigidbody2D。
    private void Set(string field, object value) =>
        controller.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, value);

    private void Step(int frames = 1)
    {
        for (int i = 0; i < frames; i++)
        {
            controller.GetType().GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
            physics.Simulate(Time.fixedDeltaTime);
        }
    }
}
