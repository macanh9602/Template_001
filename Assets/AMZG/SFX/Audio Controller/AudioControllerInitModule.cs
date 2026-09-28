#pragma warning disable 0649

using UnityEngine;

namespace SFX.Visual
{
    [CreateAssetMenu(fileName = "AudioControllerInitModule", menuName = "Settings/Audio Controller Init Module")]
    public class AudioControllerInitModule : ScriptableObject
    {
        protected string moduleName;
        [SerializeField] AudioSettings audioSettings;

        [Space]
        [SerializeField] bool playRandomMusic = true;

        public void CreateComponent()
        {
            AudioController audioController = new AudioController();
            GameObject go = new GameObject("Audio Controller");
            DontDestroyOnLoad(go);
            audioController.Initialise(audioSettings, go);

            // Create audio listener
            AudioController.CreateAudioListener();

            if (playRandomMusic)
                AudioController.PlayRandomMusic();
        }

        public AudioControllerInitModule()
        {
            moduleName = "Audio Controller";
        }
    }
}