using UnityEngine;

namespace Aurora.Unity.Audio
{
    [DoNotDestroyOnLoad]
    [WithHideFlags(HideFlags.DontSave | HideFlags.NotEditable)]
    internal sealed class UnityAudioManagerOwner : SingletonBehaviour<UnityAudioManagerOwner>
    {
        internal static void EnsureInitialized()
        {
            if (Instance)
            {
                return;
            }
            CreateInstance();
            Instance.transform.SetParent(UnityEnvironment.AuroraContainer);
        }
    }
}
