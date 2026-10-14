using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance => instance;

    public GameObject PlayerObject { get; private set; }
    public Player Player { get; private set; }
    public bool IsGameOver { get; private set; }

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

    public void OnPlayerDeath()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        if (PlayerObject != null)
        {
            var ctrl = PlayerObject.GetComponent<PlayerController>();
            if (ctrl != null) ctrl.enabled = false;
        }

        Debug.Log("Game Over");
    }
}