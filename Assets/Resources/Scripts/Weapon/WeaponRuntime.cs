[System.Serializable]
public class WeaponRuntime
{
    public WeaponBase data;
    public int stack = 1;
    public float cooldownTimer;

    public bool Ready => cooldownTimer <= 0f;
}
