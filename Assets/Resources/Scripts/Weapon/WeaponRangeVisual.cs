using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>用世界单位绘制实际伤害范围，不受角色缩放影响。</summary>
public sealed class WeaponRangeVisual : MonoBehaviour
{
    private Player owner;
    private LineRenderer outline;
    private MeshRenderer fill;
    private Material material;
    private Mesh mesh;
    private Color[] fillColors;
    private Color tint;
    private float lifetime;
    private float age;
    private bool followOwner;

    public static GameObject Create(Player owner, float radius, float arcAngle, Vector2 direction,
        Color color, float lifetime = 0f)
    {
        GameObject effect = new GameObject(lifetime > 0f ? "Complement Impact Range" : "Inflammation Aura Range");
        SceneManager.MoveGameObjectToScene(effect, owner.gameObject.scene);
        effect.layer = owner.gameObject.layer;
        effect.transform.position = owner.transform.position;
        effect.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        effect.AddComponent<WeaponRangeVisual>().Initialize(owner, radius, arcAngle, color, lifetime);
        return effect;
    }

    private void Initialize(Player player, float radius, float arcAngle, Color color, float duration)
    {
        owner = player;
        tint = color;
        lifetime = duration;
        followOwner = duration <= 0f;
        radius = Mathf.Max(0f, radius);
        arcAngle = Mathf.Clamp(arcAngle, 1f, 360f);
        bool circle = arcAngle >= 360f;
        int segments = Mathf.Max(12, Mathf.CeilToInt(64f * arcAngle / 360f));

        // 与项目现有光束特效使用相同的透明 Sprite 材质。
        material = new Material(Shader.Find("Sprites/Default"));
        outline = gameObject.AddComponent<LineRenderer>();
        outline.sharedMaterial = material;
        outline.useWorldSpace = false;
        outline.loop = true;
        outline.widthMultiplier = 0.055f;
        outline.numCornerVertices = 3;
        outline.sortingOrder = 10;
        outline.positionCount = circle ? segments : segments + 2;

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];
        fillColors = new Color[vertices.Length];
        for (int i = 0; i <= segments; i++)
        {
            float angle = (-arcAngle * 0.5f + arcAngle * i / segments) * Mathf.Deg2Rad;
            Vector3 point = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            vertices[i + 1] = point;
            if (!circle || i < segments) outline.SetPosition(circle ? i : i + 1, point);
            if (i == segments) continue;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        if (!circle) outline.SetPosition(0, Vector3.zero);

        mesh = new Mesh { name = "Weapon Range Fill" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        GameObject surface = new GameObject("Range Fill");
        surface.layer = gameObject.layer;
        surface.transform.SetParent(transform, false);
        surface.AddComponent<MeshFilter>().sharedMesh = mesh;
        fill = surface.AddComponent<MeshRenderer>();
        fill.sharedMaterial = material;
        fill.sortingOrder = 9;
        UpdateAppearance();
    }

    private void LateUpdate()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        age += Time.deltaTime;
        if (lifetime > 0f && age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
        if (followOwner) transform.position = owner.transform.position;
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        bool visible = owner != null && owner.isActiveAndEnabled && !owner.IsDead;
        outline.enabled = visible;
        fill.enabled = visible;
        // 只改变透明度，不改变半径，保持边界与伤害判定一致。
        float opacity = followOwner ? 0.7f + 0.15f * Mathf.Sin(age * 3f) : 1f - age / lifetime;
        Color color = tint;
        color.a *= opacity;
        outline.startColor = color;
        outline.endColor = color;
        color.a *= 0.12f;
        for (int i = 0; i < fillColors.Length; i++) fillColors[i] = color;
        mesh.colors = fillColors;
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }
}
