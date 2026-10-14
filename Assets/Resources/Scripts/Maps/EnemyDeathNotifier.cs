using UnityEngine;

public class EnemyDeathNotifier : MonoBehaviour
{
    private EnemySpawner spawner;

    public void Init(EnemySpawner spawner)
    {
        this.spawner = spawner;
    }

    private void OnDestroy()
    {
        spawner?.OnEnemyDied();
    }
}