using UnityEngine;

public class CharacterSwitcher : MonoBehaviour
{
    private int currentIndex = 0;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchTo(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchTo(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchTo(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchTo(3);
    }

    public void SwitchTo(int targetIndex)
    {
        var team = GameManager.Instance.teamMembers;
        if (targetIndex < 0 || targetIndex >= team.Count) return;
        if (targetIndex == currentIndex) return;

        var oldPlayer = team[currentIndex].GetComponent<Player>();
        if (oldPlayer != null) oldPlayer.CancelAttack();

        team[targetIndex].transform.position = team[currentIndex].transform.position;
        team[targetIndex].transform.rotation = team[currentIndex].transform.rotation;

        SetMemberActive(team[currentIndex], false);
        SetMemberActive(team[targetIndex], true);

        currentIndex = targetIndex;
    }

    private void SetMemberActive(GameObject member, bool active)
    {
        foreach (var sr in member.GetComponentsInChildren<SpriteRenderer>(true))
            sr.enabled = active;

        foreach (var col in member.GetComponentsInChildren<Collider2D>(true))
            col.enabled = active;

        var ctrl = member.GetComponent<PlayerController>();
        if (ctrl != null) ctrl.enabled = active;

        var rb = member.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (!active)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
            }
        }
    }
}