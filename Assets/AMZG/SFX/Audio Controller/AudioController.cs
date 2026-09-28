using System;
using System.Collections.Generic;
using System.Reflection;
using Common.Helper;
using DG.Tweening;
using UnityEngine;
using VTLTools;

namespace SFX.Visual
{
    public class AudioController
    {
        private static AudioController instance;

        private FieldInfo[] fields;
        private const int AUDIO_SOURCES_AMOUNT = 4;

        private GameObject targetGameObject;

        private List<AudioSource> audioSources = new List<AudioSource>();

        private List<AudioSource> activeSoundSources = new List<AudioSource>();
        private List<AudioSource> activeMusicSources = new List<AudioSource>();

        private List<AudioSource> customSources = new List<AudioSource>();
        private List<AudioCaseCustom> activeCustomSourcesCases = new List<AudioCaseCustom>();

        private static bool vibrationState;
        private static float musicVolume;
        private static float soundVolume;

        private static AudioClip[] musicAudioClips;
        public static AudioClip[] MusicAudioClips => musicAudioClips;

        private static Sounds sounds;
        public static Sounds Sounds => sounds;

        private static Music music;
        public static Music Music => music;

        public static OnMusicVolumeChangedCallback OnMusicVolumeChanged;
        public static OnSoundVolumeChangedCallback OnSoundVolumeChanged;
        public static OnVibrationChangedCallback OnVibrationChanged;

        private static AudioListener audioListener;
        public static AudioListener AudioListener => audioListener;

        private static List<AudioMinDelayData> minDelayQueue = new List<AudioMinDelayData>();
        private Tween minDelayQueueUpdateTween;
        private static IDisposable audioSettingSubscription;



        public void Initialise(AudioSettings settings, GameObject targetGameObject)
        {
            if (instance != null)
            {
                Debug.Log("[Audio Controller]: Module already exists!");

                return;
            }

            if (settings == null)
            {
                Debug.LogError("[AudioController]: Audio Settings is NULL! Please assign audio settings scriptable on Audio Controller script.");

                return;
            }

            this.targetGameObject = targetGameObject;

            instance = this;
            fields = typeof(Music).GetFields();
            musicAudioClips = new AudioClip[fields.Length];

            for (int i = 0; i < fields.Length; i++)
            {
                musicAudioClips[i] = fields[i].GetValue(settings.Music) as AudioClip;
            }

            music = settings.Music;
            sounds = settings.Sounds;

            //Create audio source objects
            audioSources.Clear();
            for (int i = 0; i < AUDIO_SOURCES_AMOUNT; i++)
            {
                audioSources.Add(CreateAudioSourceObject(false));
            }

            // Default states are later synchronized via AudioSettingStateChangedEvent.
            vibrationState = true;
            musicVolume = 0.1f;
            soundVolume = .4f;
            Debug.Log($"<color=green>[DA]</color> default music volume: {musicVolume}, default sound volume: {soundVolume} vibration: {vibrationState}");

            // SubscribeAudioSettingEvents();
            StartMinDelayQueueUpdater();
        }

        // private static void SubscribeAudioSettingEvents()
        // {
        //     if (audioSettingSubscription != null)
        //     {
        //         return;
        //     }

        //     audioSettingSubscription = ServiceLocator.Global.EventBus
        //         .SubscribeWithDisposable<AudioSettingStateChangedEvent>(OnAudioSettingStateChanged);
        // }

        // private static void OnAudioSettingStateChanged(AudioSettingStateChangedEvent evt)
        // {
        //     SetMusicVolume(evt.ActiveMusic ? 0.1f : 0f);
        //     SetSoundVolume(evt.ActiveSound ? 1f : 0f);
        //     SetVibrationState(evt.ActiveVibrate);
        // }

        private void StartMinDelayQueueUpdater()
        {
            minDelayQueueUpdateTween?.Kill();
            minDelayQueueUpdateTween = null;

            ScheduleMinDelayQueueTick();
        }

        private void ScheduleMinDelayQueueTick()
        {
            if (targetGameObject == null)
            {
                minDelayQueueUpdateTween = null;
                return;
            }

            minDelayQueueUpdateTween = DOVirtual.DelayedCall(0.1f, () =>
            {
                UpdateMinDelayQueue();
                ScheduleMinDelayQueueTick();
            }).SetTarget(targetGameObject);
        }

        private static void UpdateMinDelayQueue()
        {
            if (minDelayQueue.Count == 0)
            {
                return;
            }

            float now = Time.timeSinceLevelLoad;
            for (int i = 0; i < minDelayQueue.Count; i++)
            {
                if (now >= minDelayQueue[i].enableTime)
                {
                    minDelayQueue.RemoveAt(i);
                    i--;
                }
            }
        }

        public static void CreateAudioListener()
        {
            if (audioListener != null)
                return;

            // Create game object for listener
            GameObject listenerObject = new GameObject("[AUDIO LISTENER]");
            listenerObject.transform.position = Vector3.zero;

            // Mark as non-destroyable
            GameObject.DontDestroyOnLoad(listenerObject);

            // Add listener component to created object
            audioListener = listenerObject.AddComponent<AudioListener>();
        }

        public static bool IsVibrationModuleEnabled()
        {
            return vibrationState;
        }

        public static bool IsAudioModuleEnabled()
        {
            return musicVolume > 0.00005f || soundVolume > 0.00005f;
        }

        public static void PlayRandomMusic()
        {
            if (musicAudioClips.IsNullOrEmpty())
            {
                return;
            }
            PlayMusic(musicAudioClips.GetRandomItem());
        }

        /// <summary>
        /// Stop all active streams
        /// </summary>
        public static void ReleaseStreams()
        {
            ReleaseMusic();
            ReleaseSounds();
            ReleaseCustomStreams();
        }

        /// <summary>
        /// Releasing all active music.
        /// </summary>
        public static void ReleaseMusic()
        {
            int activeMusicCount = instance.activeMusicSources.Count - 1;
            for (int i = activeMusicCount; i >= 0; i--)
            {
                instance.activeMusicSources[i].Stop();
                instance.activeMusicSources[i].clip = null;
                instance.activeMusicSources.RemoveAt(i);
            }
        }

        /// <summary>
        /// Releasing all active sounds.
        /// </summary>
        public static void ReleaseSounds()
        {
            int activeStreamsCount = instance.activeSoundSources.Count - 1;
            for (int i = activeStreamsCount; i >= 0; i--)
            {
                instance.activeSoundSources[i].Stop();
                instance.activeSoundSources[i].clip = null;
                instance.activeSoundSources.RemoveAt(i);
            }
        }

        /// <summary>
        /// Releasing all active custom sources.
        /// </summary>
        public static void ReleaseCustomStreams()
        {
            int activeStreamsCount = instance.activeCustomSourcesCases.Count - 1;
            for (int i = activeStreamsCount; i >= 0; i--)
            {
                if (instance.activeCustomSourcesCases[i].autoRelease)
                {
                    AudioSource source = instance.activeCustomSourcesCases[i].source;
                    instance.activeCustomSourcesCases[i].source.Stop();
                    instance.activeCustomSourcesCases[i].source.clip = null;
                    instance.activeCustomSourcesCases.RemoveAt(i);
                    instance.customSources.Add(source);
                }
            }
        }

        public static void StopStream(AudioCase audioCase, float fadeTime = 0)
        {
            if (audioCase.type == AudioType.Sound)
            {
                instance.StopSound(audioCase.source, fadeTime);
            }
            else
            {
                instance.StopMusic(audioCase.source, fadeTime);
            }
        }

        public static void StopStream(AudioCaseCustom audioCase, float fadeTime = 0)
        {
            ReleaseCustomSource(audioCase, fadeTime);
        }

        private void StopSound(AudioSource source, float fadeTime = 0)
        {
            int streamID = activeSoundSources.FindIndex(x => x == source);
            if (streamID != -1)
            {
                if (fadeTime == 0)
                {
                    activeSoundSources[streamID].Stop();
                    activeSoundSources[streamID].clip = null;
                    activeSoundSources.RemoveAt(streamID);
                }
                else
                {
                    TweenAudioVolume(activeSoundSources[streamID], 0f, fadeTime).OnComplete(() =>
                    {
                        activeSoundSources.Remove(source);
                        source.Stop();
                    });
                }
            }
        }

        private void StopMusic(AudioSource source, float fadeTime = 0)
        {
            int streamID = activeMusicSources.FindIndex(x => x == source);
            if (streamID != -1)
            {
                if (fadeTime == 0)
                {
                    activeMusicSources[streamID].Stop();
                    activeMusicSources[streamID].clip = null;
                    activeMusicSources.RemoveAt(streamID);
                }
                else
                {
                    TweenAudioVolume(activeMusicSources[streamID], 0f, fadeTime).OnComplete(() =>
                    {
                        activeMusicSources.Remove(source);
                        source.Stop();
                    });
                }
            }
        }

        private static void AddMusic(AudioSource source)
        {
            if (!instance.activeMusicSources.Contains(source))
            {
                instance.activeMusicSources.Add(source);
            }
        }

        private static void AddSound(AudioSource source)
        {
            if (!instance.activeSoundSources.Contains(source))
            {
                instance.activeSoundSources.Add(source);
            }
        }

        public static void PlayMusic(AudioClip clip, float volume = 1.0f)
        {
            if (!StaticVariables.IsMusicOn)
            {
                return;
            }
            if (clip == null)
            {
                Debug.LogWarning("[AudioController]: Audio clip is null");
                return;
            }

            AudioSource source = instance.GetAudioSource();

            SetSourceDefaultSettings(source, AudioType.Music);

            source.clip = clip;
            source.volume *= volume;
            source.Play();

            AddMusic(source);

        }

        public static AudioCase PlaySmartMusic(AudioClip clip, float volume = 1.0f, float pitch = 1.0f)
        {
            if (!StaticVariables.IsMusicOn)
            {
                return null;
            }
            if (clip == null)
            {
                Debug.LogError("[AudioController]: Audio clip is null");
                return null;
            }

            AudioSource source = instance.GetAudioSource();

            SetSourceDefaultSettings(source, AudioType.Music);

            source.clip = clip;
            source.volume *= volume;
            source.pitch = pitch;

            AudioCase audioCase = new AudioCase(clip, source, AudioType.Music);

            audioCase.Play();

            AddMusic(source);

            return audioCase;
        }

        public static void PlaySound(AudioClip clip, float volume = 1.0f, float pitch = 1.0f, float minDelay = 0f)
        {
            if (!StaticVariables.IsSoundOn)
            {
                return;
            }
            if (clip == null)
            {
                Debug.LogError("[AudioController]: Audio clip is null");
                return;
            }

            if (minDelay > 0)
            {
                if (minDelayQueue.Exists(data => data.audioHash.Equals(clip.GetHashCode())))
                {
                    return;
                }
                else
                {
                    minDelayQueue.Add(new AudioMinDelayData(clip.GetHashCode(), minDelay));
                }
            }

            AudioSource source = instance.GetAudioSource();

            SetSourceDefaultSettings(source, AudioType.Sound);

            source.clip = clip;
            source.volume *= volume;
            source.pitch = pitch;
            source.Play();

            AddSound(source);
        }

        public static void PlaySoundWithCurve(
    AudioClip clip,
    int totalSounds = 10,
    float totalDuration = 1.5f,
    AnimationCurve intervalCurve = null,
    float volume = 1.0f, float firstDelay = 0f)
        {
            if (!StaticVariables.IsSoundOn) return;
            if (clip == null || instance.targetGameObject == null) return;

            if (intervalCurve == null || intervalCurve.length == 0)
            {
                intervalCurve = AnimationCurve.Linear(0, 1, 1, 1);
            }
            DOVirtual.DelayedCall(firstDelay, () =>
                {
                    PlayNextStep(clip, 0, totalSounds, totalDuration, intervalCurve, volume);
                })
                .SetTarget(instance.targetGameObject);
        }

        private static void PlayNextStep(AudioClip clip, int currentCount, int total, float duration, AnimationCurve curve, float vol)
        {
            if (currentCount >= total) return;

            PlaySound(clip, vol);

            float progress = (float)currentCount / total;

            float baseInterval = duration / total;
            float dynamicInterval = baseInterval * curve.Evaluate(progress);

            DOVirtual.DelayedCall(dynamicInterval, () =>
            {
                PlayNextStep(clip, currentCount + 1, total, duration, curve, vol);
            }).SetTarget(instance.targetGameObject);
        }

        public static AudioCase PlaySmartSound(AudioClip clip, float volume = 1.0f, float pitch = 1.0f)
        {
            if (!StaticVariables.IsSoundOn)
            {
                return null;
            }
            if (clip == null)
            {
                Debug.LogError("[AudioController]: Audio clip is null");
                return null;
            }

            AudioSource source = instance.GetAudioSource();

            SetSourceDefaultSettings(source, AudioType.Sound);

            source.clip = clip;
            source.volume *= volume;
            source.pitch = pitch;

            AudioCase audioCase = new AudioCase(clip, source, AudioType.Sound);
            audioCase.Play();

            AddSound(source);

            return audioCase;
        }

        public static AudioCaseCustom GetCustomSource(bool autoRelease, AudioType audioType)
        {
            AudioSource source = null;

            if (!instance.customSources.IsNullOrEmpty())
            {
                source = instance.customSources[0];
                instance.customSources.RemoveAt(0);
            }
            else
            {
                source = instance.CreateAudioSourceObject(true);
            }

            SetSourceDefaultSettings(source, audioType);

            AudioCaseCustom audioCase = new AudioCaseCustom(null, source, audioType, autoRelease);

            instance.activeCustomSourcesCases.Add(audioCase);

            return audioCase;
        }

        public static void ReleaseCustomSource(AudioCaseCustom audioCase, float fadeTime = 0)
        {
            int streamID = instance.activeCustomSourcesCases.FindIndex(x => x.source == audioCase.source);
            if (streamID != -1)
            {
                if (fadeTime == 0)
                {
                    instance.activeCustomSourcesCases[streamID].source.Stop();
                    instance.activeCustomSourcesCases[streamID].source.clip = null;
                    instance.activeCustomSourcesCases.RemoveAt(streamID);
                    instance.customSources.Add(audioCase.source);
                }
                else
                {
                    TweenAudioVolume(instance.activeCustomSourcesCases[streamID].source, 0f, fadeTime).OnComplete(() =>
                    {
                        instance.activeCustomSourcesCases.Remove(audioCase);
                        audioCase.source.Stop();
                        instance.customSources.Add(audioCase.source);
                    });
                }
            }
        }

        private AudioSource GetAudioSource()
        {
            int sourcesAmount = audioSources.Count;
            for (int i = 0; i < sourcesAmount; i++)
            {
                if (!audioSources[i].isPlaying)
                {
                    return audioSources[i];
                }
            }

            AudioSource createdSource = CreateAudioSourceObject(false);
            audioSources.Add(createdSource);

            return createdSource;
        }

        private AudioSource CreateAudioSourceObject(bool isCustom)
        {
            AudioSource audioSource = targetGameObject.AddComponent<AudioSource>();
            SetSourceDefaultSettings(audioSource);

            return audioSource;
        }

        private static Tween TweenAudioVolume(AudioSource source, float targetVolume, float duration)
        {
            return DOTween.To(() => source.volume, value => source.volume = value, targetVolume, duration)
                .SetTarget(source);
        }

        private void SetVolumeForMusicAudioSources(float volume)
        {
            SetAllMusicVolume(volume);
            for (int i = 0; i < activeCustomSourcesCases.Count; i++)
            {
                if (activeCustomSourcesCases[i].type == AudioType.Music)
                {
                    activeCustomSourcesCases[i].source.volume = volume;
                }
            }

        }
        private void SetVolumeForSoundAudioSources(float volume)
        {

            SetAllSoundsVolume(volume);
            for (int i = 0; i < activeCustomSourcesCases.Count; i++)
            {
                if (activeCustomSourcesCases[i].type == AudioType.Sound)
                {
                    activeCustomSourcesCases[i].source.volume = volume;
                }
            }

        }


        public static void SetAllSoundsVolume(float newVolume)
        {
            for (int i = 0; i < instance.activeSoundSources.Count; i++)
            {
                instance.activeSoundSources[i].volume = newVolume;
            }
        }

        public static void SetAllMusicVolume(float newVolume)
        {
            for (int i = 0; i < instance.activeMusicSources.Count; i++)
            {
                instance.activeMusicSources[i].volume = newVolume;
            }
        }

        public static void SetMusicVolume(float volume)
        {
            AudioController.musicVolume = volume;

            instance.SetVolumeForMusicAudioSources(volume);

            OnMusicVolumeChanged?.Invoke(volume);
        }
        public static void SetSoundVolume(float volume)
        {
            AudioController.soundVolume = volume;

            instance.SetVolumeForSoundAudioSources(volume);

            OnSoundVolumeChanged?.Invoke(volume);
        }



        public static float GetMusicVolume()
        {
            return musicVolume;
        }
        public static float GetSoundVolume()
        {
            return soundVolume;
        }
        public static bool IsVibrationEnabled()
        {
            return vibrationState;
        }

        public static void SetVibrationState(bool vibrationState)
        {
            AudioController.vibrationState = vibrationState;

            OnVibrationChanged?.Invoke(vibrationState);
        }

        public static void SetSourceDefaultSettings(AudioSource source, AudioType type = AudioType.Sound)
        {
            float volume = type == AudioType.Sound ? soundVolume : musicVolume;



            if (type == AudioType.Sound)
            {
                source.loop = false;
            }
            else if (type == AudioType.Music)
            {
                source.loop = true;
            }

            source.clip = null;

            source.volume = volume;
            source.pitch = 1.0f;
            source.spatialBlend = 0; // 2D Sound
            source.mute = false;
            source.playOnAwake = false;
            source.outputAudioMixerGroup = null;
        }

        public enum AudioType
        {
            Music = 0,
            Sound = 1
        }

        public delegate void OnMusicVolumeChangedCallback(float volume);
        public delegate void OnSoundVolumeChangedCallback(float volume);
        public delegate void OnVibrationChangedCallback(bool state);


    }

    public struct AudioMinDelayData
    {
        public int audioHash;
        public float enableTime;

        public AudioMinDelayData(int audioHash, float delayDuration)
        {
            this.audioHash = audioHash;
            this.enableTime = Time.timeSinceLevelLoad + delayDuration;
        }
    }
}

// -----------------
// Audio Controller v 0.4
// -----------------

// Changelog
// v 0.4
// • Vibration settings removed
// v 0.3.3
// • Method for separate music and sound volume override
// v 0.3.2
// • Added audio listener creation method
// v 0.3.2
// • Added volume float
// • AudioSettings variable removed (now sounds, music and vibrations can be reached directly)
// v 0.3.1
// • Added OnVolumeChanged callback
// • Renamed AudioSettings to Settings
// v 0.3
// • Added IsAudioModuleEnabled method
// • Added IsVibrationModuleEnabled method
// • Removed VibrationToggleButton class
// v 0.2
// • Removed MODULE_VIBRATION
// v 0.1
// • Added basic version
// • Added support of new initialization
// • Music and Sound volume is combined
