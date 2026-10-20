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

    [SerializeField, Min(1)] private int baseSpawnPerWave = 1;
    [SerializeField, Min(1)] private int maxSpawnPerWave = 4;
    [Tooltip("刷怪密度达到上限时的对局进度；0.75 对应 12 分钟对局的第 9 分钟。")]
    [SerializeField, Range(0.01f, 1f)] private float spawnRampEndProgress = 0.75f;

    [Header("刷怪范围")]
    [SerializeField, Min(0f)] private float spawnRadius = 0.5f;

    [Header("上限")]
    [SerializeField, Min(1)] private int maxAlive = 60;

    [Header("血量成长（按对局进度的 S 曲线）")]
    [SerializeField, Min(1f)] private float maxHealthMultiplier = 6f;
    [SerializeField, Min(1f)] private float growthCurveSteepness = 6f;
    [SerializeField, Range(0f, 1f)] private float growthCurveMidPoint = 0.5f;

    private float timer;
    private GameObject[] enemyPrefabs;
    private int aliveCount;
    private float elapsedTime;

    // 全场刷怪点共用有效游玩时间，暂停与天赋选择期间不会推进成长。
    private float RunProgress => Mathf.Clamp01(
        (GameManager.Instance != null ? GameManager.Instance.EnemyTalents.ElapsedTime : elapsedTime) /
        GameManager.VictoryTime);
    private float SpawnProgress => Mathf.Clamp01(RunProgress / Mathf.Max(0.01f, spawnRampEndProgress));

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
        if (Time.timeScale <= 0f || Time.deltaTime <= 0f) return;
        if (GameManager.Instance != null &&
            (GameManager.Instance.IsGameOver || GameManager.Instance.Player == null || GameManager.Instance.Player.IsDead)) return;
        if (GameManager.Instance == null) elapsedTime += Time.deltaTime;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = GetSpawnInterval();

        int count = GetSpawnPerWave();
        for (int i = 0; i < count; i++)
        {
            if (aliveCount >= maxAlive) break;
            SpawnOne();
        }
    }

    private float GetSpawnInterval()
    {
        return Mathf.Lerp(baseSpawnInterval, minSpawnInterval, SpawnProgress);
    }

    private int GetSpawnPerWave()
    {
        return Mathf.Clamp(Mathf.FloorToInt(Mathf.Lerp(baseSpawnPerWave, maxSpawnPerWave, SpawnProgress)),
            baseSpawnPerWave, maxSpawnPerWave);
    }

    private float GetHealthMultiplier()
    {
        float curve = EvaluateGrowthCurve(RunProgress);
        float start = EvaluateGrowthCurve(0f);
        float end = EvaluateGrowthCurve(1f);
        // 归一化端点：开局基础血量，通关时达到配置上限，保留中段加速的 S 曲线。
        float growth = Mathf.InverseLerp(start, end, curve);
        return Mathf.Lerp(1f, maxHealthMultiplier, growth);
    }

    private float EvaluateGrowthCurve(float progress) =>
        1f / (1f + Mathf.Exp(-growthCurveSteepness * (progress - growthCurveMidPoint)));

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
