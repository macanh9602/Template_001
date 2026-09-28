using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ToggleType
{
    Sound,
    Music,
    Vibrate
}

namespace UnityEngine.UI
{
    public class UIToggleSpriteSwap : UIBaseInteractive, IPointerClickHandler
    {
        [SerializeField] private ToggleType type;
        [SerializeField]
        private Image avatar;
        [SerializeField]
        private Sprite spriteOn;
        [SerializeField]
        private Sprite spriteOff;
        [SerializeField]
        private bool isOn = true;
        private float pressedY { get; set; }
        public Toggle.ToggleEvent OnValueChange;

        public ToggleType ToggleType { get { return type; } }

        private void Start()
        {
            if (avatar == null)
            {
                avatar = GetComponent<Image>();
            }
            //avatar.sprite = isOn ? spriteOn : spriteOff;
            onFingerDown.AddListener(OnFingerDown);
            onFingerUp.AddListener(OnFingerUp);
        }

        public void SetUp(bool isOn)
        {
            IsOn = isOn;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            IsOn = !IsOn;
            OnValueChange.Invoke(IsOn);
        }

        public bool IsOn
        {
            get { return isOn; }
            set
            {
                isOn = value;
                if (avatar != null)
                {
                    avatar.sprite = value ? spriteOn : spriteOff;
                }
            }
        }

        private int tweenColorID;

        private void OnFingerDown()
        {
            if (avatar.transform != null)
            {
                DOTween.Kill(tweenColorID, true);
                DOTween.Kill(avatar, true);
            }

            pressedY = avatar.transform.localPosition.y;
            tweenColorID = avatar.DOColor(new Color(0.8f, 0.8f, 0.8f), 0.1f).intId;
            avatar.transform.DOLocalMoveY(avatar.transform.localPosition.y - 5, 0.1f);
            avatar.transform.DOScale(Vector3.one * 0.98f, 0.1f);

        }

        private void OnFingerUp()
        {
            if (avatar.transform != null)
            {
                DOTween.Kill(tweenColorID, true);
                DOTween.Kill(avatar, true);
            }
            tweenColorID = avatar.DOColor(Color.white, 0.1f).intId;
            avatar.transform.DOLocalMoveY(pressedY, 0.1f);
            avatar.transform.DOScale(Vector3.one, 0.1f);
            //tweenColorID = LeanTween.color(avatar.rectTransform, Color.white, 0.1f).setIgnoreTimeScale(true).id;
            //LeanTween.moveLocalY(avatar.gameObject, pressedY, 0.1f).setIgnoreTimeScale(true);
            //LeanTween.scale(avatar.gameObject, Vector3.one, 0.1f).setIgnoreTimeScale(true);
        }

        public void Toggle()
        {
            IsOn = !IsOn;
        }
    }

}
