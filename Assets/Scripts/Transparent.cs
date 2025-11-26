using UnityEngine;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class ShadowOnlySkinned : MonoBehaviour
{
    void Start()
    {
        SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
        smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        smr.receiveShadows = false; // если не нужно, чтобы объект принимал тени
        smr.enabled = true; // сам рендерер оставляем включённым для теней
    }
}
