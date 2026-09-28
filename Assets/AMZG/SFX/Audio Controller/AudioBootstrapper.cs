using UnityEngine;

namespace SFX.Visual
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class AudioBootstrapper : MonoBehaviour
    {
        [SerializeField] private AudioControllerInitModule audioControllerInitModule;

        private static bool isInitialized;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            if (audioControllerInitModule == null)
            {
                Debug.LogError("[AudioBootstrapper] Missing AudioControllerInitModule reference.");
                return;
            }

            audioControllerInitModule.CreateComponent();
            isInitialized = true;
        }
    }
}
