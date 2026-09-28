using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VTLTools.UIAnimation;

namespace AntiStress.UI
{
    public class PopupBase : MonoBehaviour
    {
        [SerializeField, BoxGroup("Popup Reference")] protected Button closeButton;
        private MenuAnimationControl menuAnimationControl;
        protected MenuAnimationControl ThisMenuAnimationControl
        {
            get
            {
                if (menuAnimationControl == null)
                    menuAnimationControl = GetComponent<MenuAnimationControl>();
                return menuAnimationControl;
            }
        }
        protected System.Action actionOnStartShow, actionOnCompleteShow, actionOnStartHide, actionOnCompleteHide;
        protected object data;
        public bool IsShow
        {
            get
            {
                return this.gameObject.activeSelf;
            }
        }

        #region SHOW
        public virtual void Show(object _data = null, float _delay = 0f, bool _hasAnimation = true, Action _actionOnStartShow = null, Action _actionOnCompleteShow = null, Action _actionOnStartHide = null, Action _actionOnCompleteHide = null)
        {
            this.data = _data;
            this.actionOnStartShow = _actionOnStartShow;
            this.actionOnCompleteShow = _actionOnCompleteShow;
            this.actionOnStartHide = _actionOnStartHide;
            this.actionOnCompleteHide = _actionOnCompleteHide;
            var _menuAnimationControl = ThisMenuAnimationControl;
            if (IsShow && _menuAnimationControl != null && (_menuAnimationControl.ThisMenuItemState == MenuItemState.Showing || _menuAnimationControl.ThisMenuItemState == MenuItemState.Showed))
                return;

            this.Init();

            ButtonAddListener();
            if (_menuAnimationControl == null || !_hasAnimation)
            {
                this.gameObject.SetActive(true);
                OnShowStarted();
                OnShowCompleted();
            }
            else
            {
                this.gameObject.SetActive(true);
                _menuAnimationControl.StartShow(_delay, _onShowStarted: OnShowStarted, _onShowCompleted: OnShowCompleted);
            }
        }
        protected virtual void OnShowStarted()
        {
            this.actionOnStartShow?.Invoke();
        }
        protected virtual void OnShowCompleted()
        {
            this.actionOnCompleteShow?.Invoke();
        }
        [Button, BoxGroup("UI preview")]
        public void PreviewShow()
        {
            if (ThisMenuAnimationControl == null)
                return;

            foreach (var _item in ThisMenuAnimationControl.menuItems)
            {
                _item.PreviewShow();
            }
        }

        #endregion

        #region HIDE
        public virtual void Hide(bool _hasAnimation = true)
        {
            if (!IsShow)
                return;
            ButtonRemoveListener();
            var _menuAnimationControl = ThisMenuAnimationControl;
            if (_menuAnimationControl == null || !_hasAnimation)
            {
                OnHideStarted();
                _menuAnimationControl?.PreviewHide();
                OnHideCompleted();
            }
            else
            {
                _menuAnimationControl.StartHide(0f, _onHideStarted: OnHideStarted, _onHideCompleted: OnHideCompleted);
            }
        }
        protected virtual void OnHideStarted()
        {
            this.actionOnStartHide?.Invoke();
        }
        protected virtual void OnHideCompleted()
        {
            this.gameObject.SetActive(false);
            this.actionOnCompleteHide?.Invoke();
        }
        [Button, BoxGroup("UI preview")]
        public void PreviewHide()
        {
            if (ThisMenuAnimationControl == null)
                return;

            foreach (var _item in ThisMenuAnimationControl.menuItems)
            {
                _item.PreviewHide();
            }
        }
        #endregion

        protected virtual void Init()
        {

        }
        protected virtual void ButtonAddListener()
        {
            closeButton?.onClick.AddListener(OnCloseClick);
        }
        protected virtual void ButtonRemoveListener()
        {
            closeButton?.onClick.RemoveListener(OnCloseClick);
        }
        protected virtual void OnCloseClick()
        {
            if (!IsShow)
                return;
            this.Hide();
            //sound
            //haptic
        }
    }
}
