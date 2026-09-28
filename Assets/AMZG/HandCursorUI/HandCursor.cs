using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HandCursor : MonoBehaviour
{
    [Header("Hand Settings")]
    public RectTransform handIcon;
    public float scaleNormal = 1f;
    public float scalePressed = 0.85f;
    public float tweenTime = 0.1f;

    private Canvas canvas;
    private Tween scaleTween;

    void Start()
    {
        if (handIcon == null) handIcon = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        handIcon.localScale = Vector3.one * scaleNormal;

        // ?n con tr? chu?t m?c ??nh n?u mu?n
        Cursor.visible = false;
    }

    void Update()
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.worldCamera,
            out pos
        );
        handIcon.localPosition = pos;

        if (Input.GetMouseButtonDown(0))
        {
            scaleTween?.Kill();
            scaleTween = handIcon.DOScale(Vector3.one * scalePressed, tweenTime)
                .SetEase(Ease.OutQuad);
        }

        if (Input.GetMouseButtonUp(0))
        {
            scaleTween?.Kill();
            scaleTween = handIcon.DOScale(Vector3.one * scaleNormal, tweenTime)
                .SetEase(Ease.OutBack);
        }
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
        scaleTween = null;
    }
}
