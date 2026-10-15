using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TalentPanel : BasePanel
{
    public Text[] texts;
    public Image[] images;
    public Button[] buttons;
    public Text[] intrTexts;

    private List<TalentBase> currentChoices;
    private Action<TalentBase> onChosen;

    public override void Init()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => OnClickChoice(index));
        }
    }

    public void Show(List<TalentBase> choices, Action<TalentBase> onChosen)
    {
        currentChoices = choices;
        this.onChosen = onChosen;

        int count = Mathf.Min(choices.Count, texts.Length);

        for (int i = 0; i < texts.Length; i++)
        {
            bool active = i < count;
            texts[i].transform.parent.gameObject.SetActive(active);

            if (!active) continue;

            TalentBase talent = choices[i];
            texts[i].text = talent.displayName;
            intrTexts[i].text = talent.description;

            if (images[i] != null)
                images[i].color = GetColor(talent.color);
        }
    }

    private void OnEnable()
    {
        TalentPanelPause.Acquire(this);
    }

    private void OnDisable()
    {
        TalentPanelPause.Release(this);
    }

    private Color GetColor(TalentColor color)
    {
        switch (color)
        {
            case TalentColor.Red:   return new Color(1f, 0.4f, 0.4f);
            case TalentColor.Blue:  return new Color(0.4f, 0.6f, 1f);
            case TalentColor.Green: return new Color(0.4f, 1f, 0.5f);
            default:                return Color.white;
        }
    }

    private void OnClickChoice(int index)
    {
        if (currentChoices == null || index >= currentChoices.Count) return;

        TalentBase selected = currentChoices[index];
        onChosen?.Invoke(selected);
    }
}
