using UnityEngine;

/// <summary>光束结束或场景卸载时释放本次发射创建的材质。</summary>
public sealed class BrimstoneBeamVisual : MonoBehaviour
{
    private Material ownedMaterial;

    public void Initialize(Material material) => ownedMaterial = material;

    private void OnDestroy()
    {
        if (ownedMaterial != null) Destroy(ownedMaterial);
    }
}
