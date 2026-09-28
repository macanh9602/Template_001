using UnityEngine;
using DG.Tweening;
using AudioType = SFX.Visual.AudioController.AudioType;

namespace SFX.Visual
{
    [System.Serializable]
    public class AudioCase
    {
        public AudioSource source;

        public AudioType type;

        public AudioCallback onAudioEnded;
        private Tween endTween;

        public AudioCase(AudioClip clip, AudioSource source, AudioType type, AudioCallback callback = null)
        {
            this.source = source;
            this.type = type;

            this.source.clip = clip;
        }

        public AudioCase OnComplete(AudioCallback callback)
        {
            onAudioEnded = callback;

            if (source == null || source.clip == null)
            {
                return this;
            }

            endTween?.Kill();

            endTween = DOVirtual.DelayedCall(source.clip.length, () =>
            {
                onAudioEnded?.Invoke();
            }).SetTarget(source);

            return this;
        }

        public virtual void Play()
        {
            source.Play();
        }

        public void Stop()
        {
            source.Stop();

            endTween?.Kill();
            endTween = null;
        }

        public void FadeOut(float value, float time, bool stop = false)
        {
            TweenAudioVolume(value, time)
                .OnComplete(() =>
                {
                    if (stop)
                    {
                        source.Stop();
                    }
                });
        }

        public void FadeIn(float value, float time)
        {
            TweenAudioVolume(value, time);
        }

        private Tween TweenAudioVolume(float targetVolume, float duration)
        {
            return DOTween.To(() => source.volume, volume => source.volume = volume, targetVolume, duration)
                .SetTarget(source);
        }

        public delegate void AudioCallback();
    }
}

// -----------------
// Audio Controller v 0.3.3
// -----------------
