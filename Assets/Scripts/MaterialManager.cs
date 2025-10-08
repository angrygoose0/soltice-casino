using UnityEngine;

public class MaterialManager : MonoBehaviour
{
    public void SetGlowMaterial(GameObject objectWithMaterial, float? scale = null, Color? baseColor = null, Color? glowColor = null, float? glowIntensity = null)
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        Renderer targetRenderer = objectWithMaterial.GetComponent<Renderer>();

        targetRenderer.GetPropertyBlock(mpb, 0); // 1 is glow material index.

        // Only set properties that are not null
        if (glowIntensity.HasValue)
        {
            mpb.SetFloat("_GlowIntensity", glowIntensity.Value);
        }
        
        if (baseColor.HasValue)
        {
            mpb.SetColor("_BaseColor", baseColor.Value);
        }
        
        if (glowColor.HasValue)
        {
            mpb.SetColor("_GlowColor", glowColor.Value);
        }
        
        if (scale.HasValue)
        {
            mpb.SetFloat("_Scale", scale.Value);
        }
        
        targetRenderer.SetPropertyBlock(mpb);
    }

}