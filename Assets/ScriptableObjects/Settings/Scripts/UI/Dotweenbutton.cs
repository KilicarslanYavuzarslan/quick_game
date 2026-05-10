using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening; // DOTween kütüphanesini dahil ediyoruz

[RequireComponent(typeof(CanvasGroup))] // Bu script eklendiğinde CanvasGroup'u otomatik ekler
public class DOTweenButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Animasyon Ayarları")]
    [Tooltip("Tıklandığında hedef boyut (0.9 içeri göçme hissi verir, 1.1 dışarı büyüme)")]
    public float scaleMultiplier = 0.9f; 
    [Tooltip("Tıklandığında hedef saydamlık")]
    public float fadeAlpha = 0.6f;
    [Tooltip("Animasyon hızı")]
    public float duration = 0.15f;

    private Vector3 originalScale;
    private CanvasGroup canvasGroup;

    void Start()
    {
        originalScale = transform.localScale;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Çakışmaları önlemek için obje üzerindeki aktif tween'leri öldür
        transform.DOKill();
        canvasGroup.DOKill();

        transform.DOScale(originalScale * scaleMultiplier, duration).SetEase(Ease.OutQuad);
        canvasGroup.DOFade(fadeAlpha, duration).SetEase(Ease.OutQuad);
    }

    // Bırakıldığı an
    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOKill();
        canvasGroup.DOKill();

        transform.DOScale(originalScale, duration).SetEase(Ease.OutBack);
        canvasGroup.DOFade(1f, duration).SetEase(Ease.OutQuad);
    }
    private void OnDestroy()
    {
        transform.DOKill();
        if (canvasGroup != null) canvasGroup.DOKill();
    }
}