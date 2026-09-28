using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;
using AntiStress.UI;


public class Panel<T> : Panel where T : Panel
{
    public readonly UnityEvent onClosed = new UnityEvent();
    public void Close()
    {
        HUDSystem.Instance.Hide<T>();
    }

    // public override void Show(object data = null, bool duplicated = false)
    // {
    // 	base.Show(data, duplicated);
    // 	UnlockTapToHide();
    // }

    private void OnDisable()
    {
        onClosed.Invoke();
    }
}

/** <summary> Base Panel in UI</summary> */
public class Panel : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PopupBase popupBase;
    [SerializeField] private bool hasPopupAnimation = true;
    [SerializeField] private bool preventBgScroll;
    [SerializeField] public bool tapToHide = true;
    public bool cacheTapToHide;
    public bool isAllowScrollCamera = false;
    private bool isHiding;

    private PopupBase ThisPopupBase
    {
        get
        {
            if (popupBase == null)
                popupBase = GetComponent<PopupBase>();
            return popupBase;
        }
    }

    public bool blockTouched = true;

#if UNITY_ANDROID
		[SerializeField] public bool physicBackEnable = true;
#endif

    protected bool duplicated = false;

    public virtual void Show(object data = null, bool duplicated = false)
    {
        this.duplicated = duplicated;
        isHiding = false;

        if (ThisPopupBase != null)
        {
            ThisPopupBase.Show(
                data,
                _hasAnimation: hasPopupAnimation,
                _actionOnCompleteShow: OnPopupShowCompleted,
                _actionOnCompleteHide: OnPopupHideCompleted);
        }
        else
        {
            gameObject.SetActive(true);
            OnPopupShowCompleted();
        }

        UnlockTapToHide();
    }

    protected void UnlockTapToHide()
    {
        cacheTapToHide = tapToHide;
    }

    public virtual void Hide(object data = null)
    {
        // isAllowScrollCamera = true;
        if (isHiding)
            return;

        if (ThisPopupBase != null)
        {
            //SoundManager.instance.PlayEffect(SoundCommand.SFX_CLOSE_POPUP_SCENE);
            if (!ThisPopupBase.IsShow)
            {
                OnPopupHideCompleted();
                return;
            }

            isHiding = true;
            ThisPopupBase.Hide(hasPopupAnimation);
            return;
        }

        if (gameObject)
            gameObject.SetActive(false);

        OnPopupHideCompleted();
    }

    protected virtual void OnPopupShowCompleted()
    {
    }

    protected virtual void OnPopupHideCompleted()
    {
        isHiding = false;
        HUDSystem.Instance.UpdateActiveScroll();
    }

    public virtual void Back()
    {

#if UNITY_ANDROID
			if (PhysicBackEnable)
				HUDSystem.Instance.Hide<Panel>();
#endif
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (HUDSystem.TapToHideLocked)
            return;

        if (cacheTapToHide && eventData.pointerCurrentRaycast.gameObject == gameObject)
        {
            Invoke(nameof(HideDelay), 0.1f);
        }

        UnlockTapToHide();
    }

    private void HideDelay()
    {
        var p = gameObject.GetComponent<Panel>();
        HUDSystem.Instance.Hide(p.GetType());
    }

    public virtual bool PhysicBackEnable
    {
        get
        {
#if UNITY_ANDROID
				return physicBackEnable;
#else
            return false;
#endif
        }
    }

    private void OnEnable()
    {
        if (preventBgScroll)
            StartCoroutine(WaitForFrames(6));
    }

    IEnumerator WaitForFrames(int frameCount)
    {
        for (int i = 0; i < frameCount; i++)
        {
            yield return null;
        }
        HUDSystem.Instance.ScrollingLocked = true;
        HUDSystem.Instance.SwipeLocked = true;
        HUDSystem.IsLock = true;
    }

    private void OnDisable()
    {
        if (preventBgScroll)
        {
            HUDSystem.Instance.ScrollingLocked = false;
            HUDSystem.Instance.SwipeLocked = false;
            HUDSystem.IsLock = false;
        }
    }
}
