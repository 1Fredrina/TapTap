using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PiercingWeaponConfigurationTests
{
    [TestCase(1f, 0f)]
    [TestCase(0f, 1f)]
    [TestCase(-1f, -1f)]
    public void PiercingBulletSpriteFacesTravelDirection(float x, float y)
    {
        var prefab = Resources.Load<GameObject>("Prefabs/Weapons/PiercingBullet");
        Assert.That(prefab, Is.Not.Null);
        var projectile = UnityEngine.Object.Instantiate(prefab);

        try
        {
            var component = projectile.GetComponent(Type.GetType("PiercingBullet, Assembly-CSharp", true));
            Assert.That(component, Is.Not.Null);
            var direction = new Vector2(x, y).normalized;
            component.GetType().GetMethod("Init").Invoke(component, new object[] { direction, 1f, 1, 1f });

            Vector2 spriteForward = projectile.transform.rotation * new Vector2(1f, 1f).normalized;
            Assert.That(Vector2.Angle(spriteForward, direction), Is.LessThan(1f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(projectile);
        }
    }

    [Test]
    public void StartingPiercingWeaponUsesMatchingProjectile()
    {
        Type managerType = Type.GetType("GameDataMgr, Assembly-CSharp", true);
        object manager = managerType.GetProperty("Instance").GetValue(null);
        var talents = (IEnumerable)managerType.GetField("talentDatas").GetValue(manager);

        object talent = null;
        foreach (object candidate in talents)
        {
            if ((string)candidate.GetType().BaseType.GetField("id").GetValue(candidate) == "weapon_piercing")
            {
                talent = candidate;
                break;
            }
        }

        Assert.That(talent, Is.Not.Null);
        object weapon = talent.GetType().GetMethod("CreateWeapon", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(talent, null);
        Assert.That(weapon, Is.Not.Null);
        Assert.That(weapon.GetType().Name, Is.EqualTo("PiercingWeapon"));

        var prefab = (GameObject)weapon.GetType().GetField("bulletPrefab").GetValue(weapon);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent(Type.GetType("PiercingBullet, Assembly-CSharp", true)), Is.Not.Null);
    }
}
