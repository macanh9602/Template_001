using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VTLTools.UIAnimation
{
    public class MenuItemFade : MenuItem
    {
        [Button, BoxGroup("Tween setting")] public float showAlpha;
        [Button, BoxGroup("Tween setting")] public float hideAlpha;
        [Button, BoxGroup("Tween setting")] public Ease easeShow = Ease.Linear;
        [Button, BoxGroup("Tween setting")] public Ease easeHide = Ease.Linear;
        private Image thisImage
        {
            get
            {
                return GetComponent<Image>();
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

            Color _tempColor = thisImage.color;
            _tempColor.a = hideAlpha;
            thisImage.color = _tempColor;

            yield return new WaitForSeconds(DelayShow);
            thisImage.DOFade(showAlpha, TimeShow).SetEase(easeShow).OnComplete(() =>
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
            thisImage.DOFade(hideAlpha, TimeHide).SetEase(easeHide).OnComplete(() =>
            {
                ThisMenuItemState = MenuItemState.Hidden;
            });

        }

        public override void PreviewHide()
        {
            Color _tempColor = thisImage.color;
            _tempColor.a = hideAlpha;
            thisImage.color = _tempColor;

            ThisMenuItemState = MenuItemState.Hidden;
        }

        public override void PreviewShow()
        {
            Color _tempColor = thisImage.color;
            _tempColor.a = showAlpha;
            thisImage.color = _tempColor;

            ThisMenuItemState = MenuItemState.Showed;
        }

        public override void SetThisAsShow()
        {
            showAlpha = thisImage.color.a;
        }

        public override void SetThisAsHide()
        {
            hideAlpha = thisImage.color.a;
        }
    }
}