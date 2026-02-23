using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Zenject;

// Bloğun tüm görsel işlemlerinden sorumlu MonoBehaviour; oyun mantığını bilmez
public class BlockVisuals : MonoBehaviour
{
    private BlockColor blockColor;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Transform horizontalArrow, verticalArrow;
    [SerializeField] private TextMeshPro overlayCountText;
    [SerializeField] private BlockFacade block;
    [Inject] private GameConfig gameConfig;

    public void Initialize(BlockData data, List<Vector2Int> shapeCells, BlockFacade block)
    {
        this.block = block;
        block.OnExitStarted += DisableVisuals;

        blockColor = data.Color;
        SetBlockSize(shapeCells);
        SetColor(data.Color);
        SetAxisArrows(data.MovementAxis);
    }
    private void SetBlockSize(List<Vector2Int> cells)
    {
        if (meshRenderer == null) { Debug.LogError("[BlockVisuals] MeshRenderer atanmadı!"); return; }
        const float offset = 0.05f;
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        foreach (Vector2Int cell in cells)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
            if (cell.y < minY) minY = cell.y;
            if (cell.y > maxY) maxY = cell.y;
        }
        float width = maxX - minX + 1;
        float depth = maxY - minY + 1;
        meshRenderer.material.SetVector("_BlockSize", new Vector3(width + offset, 1f + offset, depth + offset));

        string[] props = { "_Top", "_Bottom", "_Left", "_Right" };
        foreach (string prop in props)
            meshRenderer.material.SetFloat(prop, 1f);
    }

    // Çıkış animasyonu: ilgili yön kenarını materialda 1'den 0'a düşürür
    public void PlayExitAnimation(ExitSide exitSide, float blockTravelDuration)
    {
        if (meshRenderer == null) { Debug.LogError("[BlockVisuals] MeshRenderer atanmadı!"); return; }
        string prop = exitSide switch
        {
            ExitSide.Top => "_Top",
            ExitSide.Bottom => "_Bottom",
            ExitSide.Left => "_Left",
            ExitSide.Right => "_Right",
            _ => null
        };
        if (prop != null)
            meshRenderer.material.DOFloat(0f, prop, blockTravelDuration).SetEase(Ease.Linear);
    }

    public void SetColor(BlockColor color)
    {
        if (meshRenderer == null) { Debug.LogError("[BlockVisuals] MeshRenderer atanmadı!"); return; }
        meshRenderer.material.SetColor("_BaseColor", gameConfig.BlockColors[(int)color]);
    }

    // Üst katman doku ve sayaç metnini etkinleştirir (Ice gibi davranışlar kullanır)
    public void ApplyOverlay(Texture2D tex, int count)
    {
        if (meshRenderer == null) return;
        meshRenderer.material.SetTexture("_BaseMap", tex);
        meshRenderer.material.SetColor("_BaseColor", Color.white);
        if (overlayCountText != null) { overlayCountText.gameObject.SetActive(true); overlayCountText.text = count.ToString(); }
    }

    // Kalan sayacı günceller
    public void UpdateOverlayCount(int count)
    {
        if (overlayCountText != null) overlayCountText.text = count.ToString();
    }
    public void RemoveOverlay()
    {
        if (meshRenderer == null) return;
        meshRenderer.material.SetTexture("_BaseMap", null);
        if (overlayCountText != null) overlayCountText.gameObject.SetActive(false);
        SetColor(blockColor);
    }

    // SingleAxis bloklarda hareket eksenini gösterir
    public void SetAxisArrows(MovementAxis axis)
    {
        if (horizontalArrow == null || verticalArrow == null) { Debug.LogError("[BlockVisuals] Arrow referansları atanmadı!"); return; }
        horizontalArrow.gameObject.SetActive(axis == MovementAxis.Horizontal);
        verticalArrow.gameObject.SetActive(axis == MovementAxis.Vertical);
    }

    public void DisableVisuals()
    {
        if (horizontalArrow != null) horizontalArrow.gameObject.SetActive(false);
        if (verticalArrow != null) verticalArrow.gameObject.SetActive(false);
        if (overlayCountText != null) overlayCountText.gameObject.SetActive(false);
        if (block != null) block.OnExitStarted -= DisableVisuals;
    }
}
