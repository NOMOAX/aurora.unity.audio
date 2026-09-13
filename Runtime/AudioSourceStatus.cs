using UnityEngine;

namespace Aurora.Unity.Audio
{
    /// <summary>
    /// The status of an audio source.
    /// </summary>
    public enum AudioSourceStatus
    {
        /// <summary>
        /// The audio source has no audio clip, so there is nothing to play. This is the default value of the enumeration.
        /// </summary>
        Default,

        /// <summary>
        /// The audio source is not playing, for one of the following reasons:
        /// <list type="bullet">
        /// <item><description>It has not started playing yet.</description></item>
        /// <item><description>It is stopped by <see cref="AudioSource.Stop"/>.</description></item>
        /// <item><description>It reached the end of the audio clip.</description></item>
        /// </list>
        /// </summary>
        None,

        /// <summary>
        /// The audio source is playing.
        /// </summary>
        Playing,

        /// <summary>
        /// The audio source is paused and can resume from its current position.
        /// </summary>
        Paused
    }
}
