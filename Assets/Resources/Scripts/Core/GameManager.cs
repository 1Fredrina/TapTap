using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance => instance;

    public GameObject PlayerObject { get; private set; }
    public Player Player { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsVictory { get; private set; }
    private const float VictoryTime = 20f * 60f;

    [Header("敌人异变")]
    [SerializeField, Min(1f)] private float enemyTalentInterval = 300f;
    public EnemyTalentRuntime EnemyTalents { get; private set; } = new EnemyTalentRuntime();

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
        EnemyTalents = new EnemyTalentRuntime(enemyTalentInterval);
    }

    void Start()
{
    if (GameDataMgr.Instance == null || GameDataMgr.Instance.playerDatas.Count == 0)
    {
        Debug.LogError("没有可加载的角色数据");
        return;
    }

    int selectedId = GameDataMgr.Instance.selectedCharacterId;

    PlayerData playerData = null;
    foreach (var d in GameDataMgr.Instance.playerDatas)
    {
        if (d.id == selectedId)
        {
            playerData = d;
            break;
        }
    }

    if (playerData == null)
        playerData = GameDataMgr.Instance.playerDatas[0];

    PlayerObject = Instantiate(
        Resources.Load<GameObject>("Prefabs/Characters/" + playerData.id),
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
        UpdateEnemyTalents(Time.deltaTime);
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
    }

    public void ActivateTeamDoubleAction(float duration, float moveSpeedBonus, float weaponCooldownMultiplier)
    {
        teamDoubleActionTimer = Mathf.Max(0f, duration);
        teamMoveSpeedBonus = moveSpeedBonus;
        teamWeaponCooldownMultiplier = Mathf.Max(0.1f, weaponCooldownMultiplier);
    }

    private void UpdateEnemyTalents(float deltaTime)
    {
        if (IsGameOver || Player == null || Player.IsDead || Time.timeScale <= 0f) return;
        EnemyTalents.Tick(deltaTime);
        // 胜利优先于 20 分钟的敌人异变选择，共用有效游玩时间（不计暂停）。
        if (EnemyTalents.ElapsedTime >= VictoryTime)
        {
            OnVictory();
            return;
        }
        if (!EnemyTalents.IsChoiceDue || EnemyTalents.IsChoosing) return;

        // 优先等玩家升级选择结束，避免两个面板争抢点击。
        if (UIManager.Instance.GetPanel<TalentPanel>() != null ||
            UIManager.Instance.GetPanel<EnemyTalentPanel>() != null) return;
        var choices = EnemyTalents.RollChoices(GameDataMgr.Instance.enemyTalentDatas);
        if (choices.Count == 0) return;

        EnemyTalentPanel panel = UIManager.Instance.ShowPanel<EnemyTalentPanel>();
        panel.Show(choices, selected =>
        {
            if (IsGameOver || !EnemyTalents.Choose(selected)) return;
            foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (enemy.gameObject.scene == gameObject.scene)
                    enemy.RefreshTalentHealth();
            UIManager.Instance.HidePanel<EnemyTalentPanel>(false);
        });
    }

    private void CloseEnemyTalentPanel()
    {
        if (!EnemyTalents.IsChoosing) return;
        EnemyTalents.CancelChoice();
        UIManager.Instance.HidePanel<EnemyTalentPanel>(false);
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        CloseEnemyTalentPanel();
        UIManager.Instance.HidePanel<OverPanel>(false);
        TalentPanelPause.Release(this);
        instance = null;
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

    private void OnVictory()
    {
        if (IsGameOver) return;
        IsVictory = true;
        IsGameOver = true;
        TalentPanelPause.Acquire(this);
        CloseEnemyTalentPanel();
        if (Player != null && Player.Controller != null)
            Player.Controller.enabled = false;

        Debug.Log("Victory: survived 20 minutes.");
        ShowOverPanel(true);
    }

    public void OnPlayerDeath()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        TalentPanelPause.Acquire(this);
        CloseEnemyTalentPanel();

        if (PlayerObject != null)
        {
            var ctrl = PlayerObject.GetComponent<PlayerController>();
            if (ctrl != null) ctrl.enabled = false;
        }

        Debug.Log("Game Over");
        ShowOverPanel(false);
    }

    private void ShowOverPanel(bool victory)
    {
        OverPanel panel = UIManager.Instance.ShowPanel<OverPanel>();
        panel.ShowResult(victory);
    }
}
