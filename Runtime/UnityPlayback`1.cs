using System;
using System.Globalization;
using Aurora.Audio;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Aurora.Unity.Audio
{
    /// <summary>
    /// Implements <see cref="Playback{T}"/> by playing the audio clip of a <see cref="UnitySound{T}"/> through an <see cref="AudioSource"/> component.
    /// </summary>
    /// <typeparam name="T">The type of the audio file identifier. The derived type decides what type to use.</typeparam>
    /// <inheritdoc />
    public class UnityPlayback<T> : Playback<T> where T : IEquatable<T>
    {
        private AudioSource _audioSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityPlayback{T}"/> class.
        /// </summary>
        /// <param name="id">The identifier of this playback.</param>
        /// <param name="sound">The sound this playback is created from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="sound"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The <paramref name="sound"/> is not a <see cref="UnitySound{T}"/>.</exception>
        /// <exception cref="InvalidOperationException">The program is not running, so the game object that hosts this playback cannot be created.</exception>
        protected internal UnityPlayback(int id, Sound<T> sound) : base(id, sound)
        {
            if (sound is not UnitySound<T> unitySound)
            {
                throw new ArgumentException($"The {nameof(sound)} is not a {nameof(UnitySound<T>)}", nameof(sound));
            }
            UnityPlaybackContainer.EnsureInitialized();
            var gameObject = new GameObject(id.ToString(NumberFormatInfo.InvariantInfo));
            gameObject.hideFlags |= HideFlags.DontSave | HideFlags.NotEditable;
            gameObject.transform.SetParent(UnityPlaybackContainer.Instance.transform);
            _audioSource      = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = unitySound.AudioClip;
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">The <see cref="AudioSource.clip"/> of the underlying audio source is <see langword="null"/>.</exception>
        public override PlaybackStatus Status
        {
            get
            {
                ThrowIfDisposed();
                return AudioSourceUtility.GetStatus(_audioSource) switch
                {
                    AudioSourceStatus.Default => throw new InvalidOperationException(),
                    AudioSourceStatus.None    => PlaybackStatus.None,
                    AudioSourceStatus.Playing => PlaybackStatus.Playing,
                    AudioSourceStatus.Paused  => PlaybackStatus.Paused,
                    _                         => throw new ArgumentOutOfRangeException()
                };
            }
        }

        /// <inheritdoc />
        public override double Volume
        {
            get
            {
                ThrowIfDisposed();
                return _audioSource.volume;
            }
            set
            {
                ThrowIfDisposed();
                _audioSource.volume = (float)value;
            }
        }

        /// <inheritdoc />
        public override double Position
        {
            get
            {
                ThrowIfDisposed();
                return _audioSource.time;
            }
            set
            {
                ThrowIfDisposed();
                _audioSource.time = (float)value;
            }
        }

        /// <inheritdoc />
        public override double NormalizedPosition
        {
            get
            {
                ThrowIfDisposed();
                return AudioSourceUtility.GetNormalizedPosition(_audioSource);
            }
            set
            {
                ThrowIfDisposed();
                AudioSourceUtility.SetNormalizedPosition(_audioSource, value);
            }
        }

        /// <inheritdoc />
        public override void Play()
        {
            ThrowIfDisposed();
            _audioSource.Play();
        }

        /// <inheritdoc />
        public override void Pause()
        {
            ThrowIfDisposed();
            _audioSource.Pause();
        }

        /// <inheritdoc />
        public override void Stop()
        {
            ThrowIfDisposed();
            _audioSource.Stop();
        }

        /// <inheritdoc />
        /// <remarks>Destroys the game object of this playback only when the program is running; otherwise the program is ending and Unity destroys the game object anyway.</remarks>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing && UnityEnvironment.IsPlaying)
                {
                    _audioSource.clip = null;
                    Object.DestroyImmediate(_audioSource.gameObject);
                }
                _audioSource = null;
            }
            base.Dispose(disposing);
        }
    }
}
