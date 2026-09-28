using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace VTLTools.UIAnimation
{
    public class MenuItemMove : MenuItem
    {
        [Button, BoxGroup("Tween setting")] public Vector3 showPos;
        [Button, BoxGroup("Tween setting")] public Vector3 hidePos;
        [Button, BoxGroup("Tween setting")] public Ease easeShow = Ease.Linear;
        [Button, BoxGroup("Tween setting")] public Ease easeHide = Ease.Linear;

        private RectTransform ThisRectTransform
        {
            get
            {
                return GetComponent<RectTransform>();
            }
        }

        public override void StartShow()
        {
            if (this.gameObject.activeSelf)
                StartCoroutine(IEStartShow());
        }

        public override IEnumerator IEStartShow()
        {
            ThisMenuItemState = MenuItemState.Showing;

            ThisRectTransform.anchoredPosition = hidePos;
            yield return new WaitForSeconds(DelayShow);
            ThisRectTransform.DOAnchorPos(showPos, TimeShow).SetEase(easeShow).OnComplete(() =>
            {
                ThisMenuItemState = MenuItemState.Showed;
            });

        }

        public override void StartHide()
        {
            if (this.gameObject.activeSelf)
                StartCoroutine(IEStartHide());
        }

        public override IEnumerator IEStartHide()
        {
            ThisMenuItemState = MenuItemState.Hiding;

            yield return new WaitForSeconds(DelayHide);
            ThisRectTransform.DOAnchorPos(hidePos, TimeHide).SetEase(easeHide).OnComplete(() =>
            {
                ThisMenuItemState = MenuItemState.Hidden;
            });
        }

        public override void PreviewHide()
        {
            ThisRectTransform.anchoredPosition = hidePos;
            ThisMenuItemState = MenuItemState.Hidden;
        }

        public override void PreviewShow()
        {
            ThisRectTransform.anchoredPosition = showPos;
            ThisMenuItemState = MenuItemState.Showed;
        }

        public override void SetThisAsShow()
        {
            showPos = ThisRectTransform.anchoredPosition;
        }

        public override void SetThisAsHide()
        {
            hidePos = ThisRectTransform.anchoredPosition;
        }
    }
}