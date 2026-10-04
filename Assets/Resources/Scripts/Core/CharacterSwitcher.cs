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

        team[targetIndex].transform.position = team[currentIndex].transform.position;
        team[targetIndex].transform.rotation = team[currentIndex].transform.rotation;

        // 关旧开新
        team[currentIndex].SetActive(false);
        team[targetIndex].SetActive(true);

        currentIndex = targetIndex;
    }
}