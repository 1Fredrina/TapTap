using UnityEngine;

public class TrapSpawnerEnemy : Enemy
{
    [Header("检测")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private LayerMask playerLayer;

    [Header("生成设置")]
    [SerializeField] private GameObject trapPrefab;
    [SerializeField] private float spawnInterval = 2f;

    private float spawnTimer;
    private Transform playerTarget;

    protected override void Update()
    {
        base.Update();
        if (IsDead || IsStunned) return;

        DetectPlayer();

        if (playerTarget != null)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnTrap();
                spawnTimer = spawnInterval;
            }
        }
        else
        {
            spawnTimer = spawnInterval;
        }
    }

    private void DetectPlayer()
    {
        playerTarget = null;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, playerLayer);

        foreach (var hit in hits)
        {
            playerTarget = hit.transform;
            break;
        }
    }

    private void SpawnTrap()
    {
        if (trapPrefab == null) return;
        Instantiate(trapPrefab, transform.position, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}