using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;

namespace VTLTools.UIAnimation
{
    public class MenuItemActive : MenuItem
    {
        public override void StartShow()
        {
            if (this.gameObject.activeSelf)
                StartCoroutine(IEStartShow());
        }

        public override IEnumerator IEStartShow()
        {
            ThisMenuItemState = MenuItemState.Showing;
            yield return new WaitForSeconds(DelayShow);

            gameObject.SetActive(IsShow);
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
            gameObject.SetActive(IsShow);
        }
        public override void PreviewHide()
        {
            ThisMenuItemState = MenuItemState.Hidden;
            gameObject.SetActive(IsShow);
        }

        public override void PreviewShow()
        {
            ThisMenuItemState = MenuItemState.Showed;
            gameObject.SetActive(IsShow);
        }

        public override void SetThisAsShow() { }

        public override void SetThisAsHide() { }
    }
}

