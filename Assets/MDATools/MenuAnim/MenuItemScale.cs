using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace VTLTools.UIAnimation
{
    public class MenuItemScale : MenuItem
    {
        [Button, BoxGroup("Tween setting")] public Vector3 showScale = Vector3.one;
        [Button, BoxGroup("Tween setting")] public Vector3 hideScale = Vector3.zero;
        [Button, BoxGroup("Tween setting")] public AnimationCurve easeShow = AnimationCurve.Linear(0, 0, 1, 1);
        [Button, BoxGroup("Tween setting")] public AnimationCurve easeHide = AnimationCurve.Linear(0, 0, 1, 1);

        private RectTransform ThisRectTransform
        {
            get
            {
                return GetComponent<RectTransform>();
            }
        }

        public override void StartShow()
        {
            StartCoroutine(IEStartShow());
        }

        public override IEnumerator IEStartShow()
        {
            ThisMenuItemState = MenuItemState.Showing;

            ThisRectTransform.localScale = hideScale;
            yield return new WaitForSeconds(DelayShow);
            ThisRectTransform.DOScale(showScale, TimeShow).SetEase(easeShow).OnComplete(() =>
            {
                ThisMenuItemState = MenuItemState.Showed;
            });
        }

        public override void StartHide()
        {
            StartCoroutine(IEStartHide());
        }

        public override IEnumerator IEStartHide()
        {
            ThisMenuItemState = MenuItemState.Hiding;

            yield return new WaitForSeconds(DelayHide);
            ThisRectTransform.DOScale(hideScale, TimeHide).SetEase(easeHide).OnComplete(() =>
            {
                ThisMenuItemState = MenuItemState.Hidden;
            });
        }

        public override void PreviewHide()
        {
            ThisRectTransform.localScale = hideScale;
            ThisMenuItemState = MenuItemState.Hidden;
        }

        public override void PreviewShow()
        {
            ThisRectTransform.localScale = showScale;
            ThisMenuItemState = MenuItemState.Showed;
        }

        public override void SetThisAsShow()
        {
            showScale = ThisRectTransform.localScale;
        }

        public override void SetThisAsHide()
        {
            hideScale = ThisRectTransform.localScale;
        }
    }
}
