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

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Lives = maxLives;
    }

    void Start()
    {      
        for (int i = 0; i < GameDataMgr.Instance.playerDatas.Count; i++)
        {
            PlayerData playerData = GameDataMgr.Instance.playerDatas[i];
            GameObject PlayerObj = Instantiate(Resources.Load<GameObject>("Prefabs/Characters/"+ (i+1)), Vector3.zero, Quaternion.identity);

            PlayerController controller = PlayerObj.GetComponent<PlayerController>();
            controller.Initialize(playerData);

            PlayerObj.name = playerData.name;
            teamMembers.Add(PlayerObj);
            PlayerObj.SetActive(i == 0);
        }
    }

     public void LoseLife(int amount = 1)
    {
        if (IsGameOver) return;

        Lives = Mathf.Max(0, Lives - amount);
        Debug.Log($"Lost {amount} life(s). Remaining lives: {Lives}");

        if (Lives <= 0)
            OnGameOver();
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
