using System;
using UnityEngine;

namespace Aurora.Unity.Audio
{
    /// <summary>
    /// Provides utility methods for the <see cref="AudioSource"/> class.
    /// </summary>
    public static class AudioSourceUtility
    {
        /// <summary>
        /// Gets the status of the <see cref="AudioSource"/>.
        /// </summary>
        /// <param name="audioSource">The audio source.</param>
        /// <returns>The status of <paramref name="audioSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="audioSource"/> is <see langword="null"/>.</exception>
        public static AudioSourceStatus GetStatus(AudioSource audioSource)
        {
            if (!audioSource)
            {
                throw new ArgumentNullException(nameof(audioSource));
            }
            if (!audioSource.clip)
            {
                return AudioSourceStatus.Default;
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
            return AudioSourceStatus.None;
        }

        /// <summary>
        /// Gets the playback position of the <see cref="AudioSource"/> as a normalized position.
        /// </summary>
        /// <param name="audioSource">The audio source.</param>
        /// <returns>The normalized position of the playback of <paramref name="audioSource"/>, in the [0, 1) range; or 0 if the <see cref="AudioSource.clip"/> of <paramref name="audioSource"/> is <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="audioSource"/> is <see langword="null"/>.</exception>
        public static double GetNormalizedPosition(AudioSource audioSource)
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
            return (double)audioSource.timeSamples / audioClip.samples;
        }

        /// <summary>
        /// Sets the playback position of the <see cref="AudioSource"/> from a normalized position.
        /// </summary>
        /// <param name="audioSource">The audio source.</param>
        /// <param name="normalizedPosition">The normalized position, in the [0, 1] range; 1 is corrected to the normalized position of the last sample.</param>
        /// <exception cref="ArgumentNullException"><paramref name="audioSource"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="normalizedPosition"/> is not in the [0, 1] range.</exception>
        public static void SetNormalizedPosition(AudioSource audioSource, double normalizedPosition)
        {
            if (!audioSource)
            {
                throw new ArgumentNullException(nameof(audioSource));
            }
            if (normalizedPosition is double.NaN or < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(normalizedPosition), normalizedPosition, null);
            }
            var audioClip = audioSource.clip;
            if (!audioClip)
            {
                return;
            }
            audioSource.timeSamples = Math.Min(
                (int)Math.Round(audioClip.samples * normalizedPosition),
                audioClip.samples - 1
            );
        }
    }
}
