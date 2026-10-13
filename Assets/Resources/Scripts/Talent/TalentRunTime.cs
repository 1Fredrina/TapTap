[System.Serializable]
public class TalentRuntime
{
    public TalentBase data;
    public int stack;

    public bool CanStack => stack < data.maxStack;
}