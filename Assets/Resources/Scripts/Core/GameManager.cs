using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance => instance;

    public GameObject PlayerObject { get; private set; }
    public Player Player { get; private set; }

    [SerializeField, Min(1)] private int maxLives = 5;
    public int Lives { get; private set; }
    public bool IsGameOver { get; private set; }

    [Header("临时生命")]
    [SerializeField, Min(0)] private int maxTempLives = 5;
    [SerializeField, Min(0f)] private float tempLifeDecayInterval = 5f;
    public int TempLives { get; private set; }
    private float tempLifeDecayTimer;

    private float skillCooldownTimer;
    private float teamDoubleActionTimer;
    private float teamMoveSpeedBonus;
    private float teamWeaponCooldownMultiplier = 1f;

    public bool TeamDoubleActionActive => teamDoubleActionTimer > 0f;
    public float TeamMoveSpeedBonus => TeamDoubleActionActive ? teamMoveSpeedBonus : 0f;
    public float TeamWeaponCooldownMultiplier => TeamDoubleActionActive ? teamWeaponCooldownMultiplier : 1f;

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
        if (GameDataMgr.Instance == null || GameDataMgr.Instance.playerDatas.Count == 0)
        {
            return;
        }

        PlayerData playerData = GameDataMgr.Instance.playerDatas[0];

        PlayerObject = Instantiate(
            Resources.Load<GameObject>("Prefabs/Characters/" + (playerData.id > 0 ? playerData.id : 1)),
            Vector3.zero,
            Quaternion.identity);

        PlayerController controller = PlayerObject.GetComponent<PlayerController>();
        if (controller != null)
            controller.Initialize(playerData);

        Player = PlayerObject.GetComponent<Player>();
        if (Player != null)
            Player.Initialize(playerData);

        PlayerObject.name = playerData.name;
    }

    private void Update()
    {
        UpdateSkillCooldown();

        if (teamDoubleActionTimer > 0f)
        {
            teamDoubleActionTimer = Mathf.Max(0f, teamDoubleActionTimer - Time.deltaTime);

            if (teamDoubleActionTimer <= 0f)
            {
                teamMoveSpeedBonus = 0f;
                teamWeaponCooldownMultiplier = 1f;
            }
        }

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

    public void ActivateTeamDoubleAction(float duration, float moveSpeedBonus, float weaponCooldownMultiplier)
    {
        teamDoubleActionTimer = Mathf.Max(0f, duration);
        teamMoveSpeedBonus = moveSpeedBonus;
        teamWeaponCooldownMultiplier = Mathf.Max(0.1f, weaponCooldownMultiplier);
    }

    private void UpdateSkillCooldown()
    {
        if (skillCooldownTimer > 0f)
            skillCooldownTimer = Mathf.Max(0f, skillCooldownTimer - Time.deltaTime);
    }

    public bool TryStartSkillCooldown(Player player, float cooldown)
    {
        if (player == null) return false;
        if (skillCooldownTimer > 0f) return false;

        skillCooldownTimer = Mathf.Max(0f, cooldown);
        return true;
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

        if (PlayerObject != null)
        {
            var ctrl = PlayerObject.GetComponent<PlayerController>();
            if (ctrl != null) ctrl.enabled = false;
        }

        // UIManager.Instance.ShowPanel<GameOverPanel>();
        Debug.Log("Game Over");
    }
}