using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyTalentPanel : BasePanel
{
    public Text[] texts;
    public Button[] buttons;
    public Text[] intrTexts;

    private List<EnemyTalentData> currentChoices;
    private Action<EnemyTalentData> onChosen;
    private bool hasChosen;

    private void OnEnable()
    {
        TalentPanelPause.Acquire(this);
    }

    private void OnDisable()
    {
        TalentPanelPause.Release(this);
    }

    public override void Init()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => OnClickChoice(index));
        }
    }

    public void Show(List<EnemyTalentData> choices, Action<EnemyTalentData> onChosen)
    {
        Init();
        currentChoices = new List<EnemyTalentData>(choices);
        this.onChosen = onChosen;
        hasChosen = false;
        int count = Mathf.Min(choices.Count, Mathf.Min(texts.Length, Mathf.Min(buttons.Length, intrTexts.Length)));
        for (int i = 0; i < buttons.Length; i++)
        {
            bool active = i < count;
            buttons[i].gameObject.SetActive(active);
            buttons[i].interactable = active;
            if (i < texts.Length) texts[i].gameObject.SetActive(active);
            if (i < intrTexts.Length) intrTexts[i].gameObject.SetActive(active);
            if (!active) continue;
            texts[i].text = choices[i].displayName;
            intrTexts[i].text = choices[i].description;
        }
    }

    private void OnClickChoice(int index)
    {
        if (hasChosen || currentChoices == null || index < 0 || index >= currentChoices.Count) return;
        hasChosen = true;
        foreach (Button button in buttons) button.interactable = false;
        var callback = onChosen;
        onChosen = null;
        callback?.Invoke(currentChoices[index]);
    }
}
