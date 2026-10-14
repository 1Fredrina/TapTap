using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("敌人")]
    [SerializeField] private string enemyPrefabPath = "Prefabs/Enemy/1";

    [Header("刷怪节奏")]
    [SerializeField, Min(0.1f)] private float spawnInterval = 2f;
    [SerializeField, Min(1)] private int spawnPerWave = 1;

    [Header("刷怪范围")]
    [SerializeField, Min(0f)] private float spawnRadius = 0.5f;

    [Header("上限")]
    [SerializeField, Min(1)] private int maxAlive = 50;

    private float timer;
    private GameObject enemyPrefab;
    private int aliveCount;

    private void Awake()
    {
        enemyPrefab = Resources.Load<GameObject>(enemyPrefabPath);
        if (enemyPrefab == null)
            Debug.LogError($"[EnemySpawner] 找不到敌人预设体: {enemyPrefabPath}");
    }

    private void Update()
    {
        if (enemyPrefab == null) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = spawnInterval;

        for (int i = 0; i < spawnPerWave; i++)
        {
            if (aliveCount >= maxAlive) break;
            SpawnOne();
        }
    }

    private void SpawnOne()
    {
        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = transform.position + (Vector3)offset;

        GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
        aliveCount++;

        // 监听敌人死亡，减计数
        Enemy e = enemy.GetComponent<Enemy>();
        if (e != null)
        {
            EnemyDeathNotifier notifier = enemy.AddComponent<EnemyDeathNotifier>();
            notifier.Init(this);
        }
    }

    public void OnEnemyDied()
    {
        aliveCount = Mathf.Max(0, aliveCount - 1);
    }
}