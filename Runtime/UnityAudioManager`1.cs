using System;
using Aurora.Audio;
using UnityEngine;

namespace Aurora.Unity.Audio
{
    /// <summary>
    /// Implements <see cref="AudioManager{T}"/> by playing <see cref="AudioClip"/> instances through <see cref="AudioSource"/> components.
    /// </summary>
    /// <typeparam name="T">The type of the audio file identifier. The derived type decides what type to use.</typeparam>
    /// <inheritdoc />
    public abstract class UnityAudioManager<T> : AudioManager<T> where T : IEquatable<T>
    {
        /// <inheritdoc />
        protected override Playback<T> CreatePlaybackImpl(int id, Sound<T> sound)
        {
            return new UnityPlayback<T>(id, sound);
        }
    }
}
