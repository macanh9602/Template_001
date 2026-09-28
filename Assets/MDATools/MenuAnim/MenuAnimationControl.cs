using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VTLTools.UIAnimation
{
    public class MenuAnimationControl : MonoBehaviour
    {
        [TableMatrix]
        public List<MenuItem> menuItems;

        //[ShowInInspector]
        //public bool IsShow
        //{
        //    get => this.gameObject.activeSelf;
        //}

        [ShowInInspector, ReadOnly]
        public MenuItemState ThisMenuItemState
        {
            get;
            protected set;
        }

        public void StartShow(float _delay, Action _onShowStarted, Action _onShowCompleted)
        {
            StartCoroutine(_IEStarShow());
            IEnumerator _IEStarShow()
            {
                ThisMenuItemState = MenuItemState.Showing;

                _onShowStarted.Invoke();
                yield return new WaitForSeconds(_delay);
                foreach (var _item in menuItems)
                {
                    //DPDebug.Log($"<color=green>[DA]</color> Starting show for {_item.name}");
                    _item.StartShow();
                }
                yield return new WaitForSeconds(GetLongestAnimationTime(true));
                _onShowCompleted.Invoke();

                ThisMenuItemState = MenuItemState.Showed;
            }
        }

        public void StartHide(float _delay, Action _onHideStarted, Action _onHideCompleted)
        {
            StartCoroutine(_IEStartHide());
            IEnumerator _IEStartHide()
            {
                ThisMenuItemState = MenuItemState.Hiding;

                _onHideStarted.Invoke();
                yield return new WaitForSeconds(_delay);
                foreach (var _item in menuItems)
                {
                    _item.StartHide();
                }
                yield return new WaitForSeconds(GetLongestAnimationTime(false));
                _onHideCompleted.Invoke();

                ThisMenuItemState = MenuItemState.Hidden;
            }
        }

        public void PretendShow()
        {
            foreach (var _item in menuItems)
            {
                _item.StartShow();
            }
        }

        public void PretendHide()
        {
            foreach (var _item in menuItems)
            {
                _item.StartHide();
            }
        }

        public void PreviewShow()
        {
            foreach (var _item in menuItems)
            {
                _item.PreviewShow();
            }
        }
        public void PreviewHide()
        {
            foreach (var _item in menuItems)
            {
                _item.PreviewHide();
            }
        }

        float GetLongestAnimationTime(bool _isShowTime)
        {
            float _temp = 0;
            foreach (var _item in menuItems)
            {
                if (_isShowTime)
                {
                    if ((_item.DelayShow + _item.TimeShow) > _temp)
                        _temp = _item.DelayShow + _item.TimeShow;
                }
                else
                {
                    if ((_item.DelayHide + _item.TimeHide) > _temp)
                        _temp = _item.DelayHide + _item.TimeHide;
                }
            }
            return _temp;
        }


        [Button]
        public void GetAllMenuItem()
        {
            menuItems.Clear();
            menuItems = Helpers.GetAllChildsComponent<MenuItem>(this.transform);
        }
    }
}

public enum MenuItemState
{
    None,
    Showing,
    Showed,
    Hiding,
    Hidden,
}