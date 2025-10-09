using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class FishEyeEffect : MonoBehaviour
{
    public Material fishEyeMat;
    [Range(-0.5f, 0.5f)] public float strengthX = 0.1f;
    [Range(-0.5f, 0.5f)] public float strengthY = 0.1f;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (fishEyeMat != null)
        {
            fishEyeMat.SetFloat("_StrengthX", strengthX);
            fishEyeMat.SetFloat("_StrengthY", strengthY);
            Graphics.Blit(src, dest, fishEyeMat);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}
