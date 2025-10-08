using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MaterialManager : MonoBehaviour
{
    private readonly Dictionary<Image, Material> _imageMaterialInstances = new Dictionary<Image, Material>();

    public void SetGlowMaterial(GameObject objectWithMaterial, float? scale = null, Color? baseColor = null, Color? glowColor = null, float? glowIntensity = null)
    {
        if (objectWithMaterial == null) return;

        // Try 3D Renderer first
        Renderer targetRenderer = objectWithMaterial.GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(mpb, 0);

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
            return;
        }

        // Then try UI Image (expects a material with the same property names)
        Image image = objectWithMaterial.GetComponent<Image>();
        if (image != null)
        {
            // Ensure this Image has its own material instance to avoid editing shared assets
            if (!_imageMaterialInstances.TryGetValue(image, out Material imgMat) || imgMat == null || imgMat != image.material)
            {
                Material sourceMat = image.material != null ? image.material : null;
                if (sourceMat == null) return;
                imgMat = new Material(sourceMat);
                image.material = imgMat;
                _imageMaterialInstances[image] = imgMat;
            }

            if (glowIntensity.HasValue)
            {
                imgMat.SetFloat("_GlowIntensity", glowIntensity.Value);
            }
            if (baseColor.HasValue)
            {
                imgMat.SetColor("_BaseColor", baseColor.Value);
            }
            if (glowColor.HasValue)
            {
                imgMat.SetColor("_GlowColor", glowColor.Value);
            }
            if (scale.HasValue)
            {
                imgMat.SetFloat("_Scale", scale.Value);
            }

            return;
        }
    }

}