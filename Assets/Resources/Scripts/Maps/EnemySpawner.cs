using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("敌人预设体路径")]
    [SerializeField] private string[] enemyPrefabPaths = new string[]
    {
        "Prefabs/Enemy/1",
        "Prefabs/Enemy/2",
        "Prefabs/Enemy/3"
    };

    [Header("刷怪节奏")]
    [SerializeField, Min(0.1f)] private float baseSpawnInterval = 5f;
    [SerializeField, Min(0.1f)] private float minSpawnInterval = 1f;
    [SerializeField, Min(0f)] private float intervalDecreasePerWave = 0.08f;

    [SerializeField, Min(1)] private int baseSpawnPerWave = 1;
    [SerializeField, Min(1)] private int maxSpawnPerWave = 4;
    [SerializeField, Min(0f)] private float spawnCountIncreasePerWave = 0.06f;

    [Header("刷怪范围")]
    [SerializeField, Min(0f)] private float spawnRadius = 0.5f;

    [Header("上限")]
    [SerializeField, Min(1)] private int maxAlive = 60;

    [Header("血量成长（S 曲线）")]
    [SerializeField, Min(1f)] private float maxHealthMultiplier = 6f;
    [SerializeField, Min(1f)] private float growthCurveSteepness = 6f;
    [SerializeField, Range(0f, 1f)] private float growthCurveMidPoint = 0.5f;
    [SerializeField, Min(1f)] private float growthCurveScale = 30f;

    private float timer;
    private GameObject[] enemyPrefabs;
    private int aliveCount;
    private int waveCount;

    private void Awake()
    {
        enemyPrefabs = new GameObject[enemyPrefabPaths.Length];
        for (int i = 0; i < enemyPrefabPaths.Length; i++)
        {
            enemyPrefabs[i] = Resources.Load<GameObject>(enemyPrefabPaths[i]);
        }
    }

    private void Update()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = GetSpawnInterval();
        waveCount++;

        int count = GetSpawnPerWave();
        for (int i = 0; i < count; i++)
        {
            if (aliveCount >= maxAlive) break;
            SpawnOne();
        }
    }

    private float GetSpawnInterval()
    {
        return Mathf.Max(minSpawnInterval, baseSpawnInterval - waveCount * intervalDecreasePerWave);
    }

    private int GetSpawnPerWave()
    {
        int extra = Mathf.FloorToInt(waveCount * spawnCountIncreasePerWave);
        return Mathf.Clamp(baseSpawnPerWave + extra, baseSpawnPerWave, maxSpawnPerWave);
    }

    private float GetHealthMultiplier()
    {
        float t = waveCount / growthCurveScale;
        float curve = 1f / (1f + Mathf.Exp(-growthCurveSteepness * (t - growthCurveMidPoint)));
        return 1f + curve * (maxHealthMultiplier - 1f);
    }

    private void SpawnOne()
    {
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        if (prefab == null) return;

        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = transform.position + (Vector3)offset;

        GameObject enemyObj = Instantiate(prefab, pos, Quaternion.identity);

        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            float hpMult = GetHealthMultiplier();
            enemy.Initialize(enemy.Data, hpMult);

            EnemyDeathNotifier notifier = enemyObj.AddComponent<EnemyDeathNotifier>();
            notifier.Init(this);
        }

        aliveCount++;
    }

    public void OnEnemyDied()
    {
        aliveCount = Mathf.Max(0, aliveCount - 1);
    }
}