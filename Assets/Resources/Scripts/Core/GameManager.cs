using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance => instance;

    public List<GameObject> teamMembers = new List<GameObject>();
    [SerializeField] private int teamSize = 4;

    [Header("共享生命")]
    [SerializeField, Min(1)] private int maxLives = 5;
    public int Lives { get; private set; }
    public bool IsGameOver { get; private set; }

    [Header("临时生命")]
    [SerializeField, Min(0)] private int maxTempLives = 5;
    [SerializeField, Min(0f)] private float tempLifeDecayInterval = 5f;   // 每 5 秒
    public int TempLives { get; private set; }
    private float tempLifeDecayTimer;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Lives = maxLives;
        TempLives = 0;
        tempLifeDecayTimer = tempLifeDecayInterval;
    }

    void Start()
    {      
        for (int i = 0; i < GameDataMgr.Instance.playerDatas.Count; i++)
        {
            PlayerData playerData = GameDataMgr.Instance.playerDatas[i];
            GameObject PlayerObj = Instantiate(Resources.Load<GameObject>("Prefabs/Characters/"+ (i+1)), Vector3.zero, Quaternion.identity);

            PlayerController controller = PlayerObj.GetComponent<PlayerController>();
            controller.Initialize(playerData);

            Player player = PlayerObj.GetComponent<Player>();
            player.Initialize(playerData);

            PlayerObj.name = playerData.name;
            teamMembers.Add(PlayerObj);
            PlayerObj.SetActive(i == 0);
        }
    }
    private void Update()
    {
        if (TempLives > 0)
        {
            tempLifeDecayTimer -= Time.deltaTime;
            if (tempLifeDecayTimer <= 0f)
            {
                tempLifeDecayTimer = tempLifeDecayInterval;
                TempLives = Mathf.Max(0, TempLives - 1);
            }
        }
        else
        {
            tempLifeDecayTimer = tempLifeDecayInterval;
        }
    }

    public void LoseLife(int amount = 1)
    {
        if (IsGameOver) return;

        int remaining = amount;
        if (TempLives > 0)
        {
            int absorbed = Mathf.Min(TempLives, remaining);
            TempLives -= absorbed;
            remaining -= absorbed;
        }

        if (remaining > 0)
        {
            Lives = Mathf.Max(0, Lives - remaining);

            if (Lives <= 0)
                OnGameOver();
        }
    }
    public void AddTempLives(int amount)
    {
        if (IsGameOver) return;
        TempLives = Mathf.Min(maxTempLives, TempLives + amount);
        tempLifeDecayTimer = tempLifeDecayInterval;
        Debug.Log($"[TempLife] Added {amount}, now {TempLives}");
    }

    private void OnGameOver()
    {
        IsGameOver = true;

        foreach (var member in teamMembers)
        {
            if (member == null) continue;
            var ctrl = member.GetComponent<PlayerController>();
            if (ctrl != null) ctrl.enabled = false;
        }
        // UIManager.Instance.ShowPanel<GameOverPanel>();
        Debug.Log("Game Over");
    }
}
