using System;
using UnityEngine;

namespace Aurora.Unity.Audio
{
    /// <summary>
    /// Provides extension methods for the <see cref="AudioSource"/> class.
    /// </summary>
    public static class AudioSourceExtensions
    {
        /// <summary>
        /// Gets the current state of the <see cref="AudioSource"/>.
        /// </summary>
        /// <param name="audioSource">The audio source.</param>
        /// <returns>The state of the audio source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="audioSource"/> is <see langword="null"/>.</exception>
        public static AudioSourceStatus GetStatus(this AudioSource audioSource)
        {
            if (!audioSource)
            {
                throw new ArgumentNullException(nameof(audioSource));
            }
            if (!audioSource.clip)
            {
                return AudioSourceStatus.None;
            }
            if (audioSource.isPlaying)
            {
                return AudioSourceStatus.Playing;
            }
            audioSource.UnPause();
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
                return AudioSourceStatus.Paused;
            }
            return AudioSourceStatus.Stopped;
        }

        /// <summary>
        /// Gets the current progress of the <see cref="AudioSource"/>.
        /// </summary>
        /// <param name="audioSource">The audio source.</param>
        /// <returns>The progress of the audio source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="audioSource"/> is <see langword="null"/>.</exception>
        public static double GetProgress(this AudioSource audioSource)
        {
            if (!audioSource)
            {
                throw new ArgumentNullException(nameof(audioSource));
            }
            var audioClip = audioSource.clip;
            if (!audioClip)
            {
                return 0;
            }
            var samples = audioClip.samples;
            if (samples < 2)
            {
                return 1;
            }
            var timeSamples = audioSource.timeSamples;
            return (double)timeSamples / (samples - 1);
        }

        /// <summary>
        /// Sets the current progress of the <see cref="AudioSource"/>.
        /// </summary>
        /// <param name="audioSource">The audio source.</param>
        /// <param name="progress">The progress.</param>
        /// <exception cref="ArgumentNullException"><paramref name="audioSource"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="progress"/> is not in the [0, 1] range.</exception>
        public static void SetProgress(this AudioSource audioSource, double progress)
        {
            if (!audioSource)
            {
                throw new ArgumentNullException(nameof(audioSource));
            }
            if (progress is float.NaN or < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(progress), progress, null);
            }
            var audioClip = audioSource.clip;
            if (!audioClip)
            {
                return;
            }
            var samples = audioClip.samples;
            if (samples < 2)
            {
                return;
            }
            audioSource.timeSamples = (int)((samples - 1) * progress);
        }
    }
}
