using UnityEngine;
using System.Collections;

public class DamageFlash : MonoBehaviour
{
    [Header("Configurações")]
    public Renderer targetRenderer;     // O renderer do corpo  
    public Color flashColor = Color.red;
    public float flashDuration = 0.15f;

    private Material material;
    private Color originalColor;
    private bool isFlashing = false;

    void Start()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        material = targetRenderer.material;
        originalColor = material.color;
    }

    public void Flash()
    {
        if (!isFlashing)
            StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        isFlashing = true;

        // troca para vermelho
        material.color = flashColor;
        yield return new WaitForSeconds(flashDuration);

        // volta ao normal
        material.color = originalColor;
        isFlashing = false;
    }
}
