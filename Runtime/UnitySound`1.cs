using System;
using Aurora.Audio;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Aurora.Unity.Audio
{
    /// <summary>
    /// Implements <see cref="Sound{T}"/> by wrapping a loaded <see cref="AudioClip"/>.
    /// </summary>
    /// <typeparam name="T">The type of the audio file identifier. The derived type decides what type to use.</typeparam>
    /// <inheritdoc />
    public class UnitySound<T> : Sound<T> where T : IEquatable<T>
    {
        private AudioClip _audioClip;

        /// <summary>
        /// Gets the wrapped audio clip.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This sound has been disposed.</exception>
        public AudioClip AudioClip
        {
            get
            {
                ThrowIfDisposed();
                return _audioClip;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitySound{T}"/> class.
        /// </summary>
        /// <param name="id">The identifier of this sound.</param>
        /// <param name="audioClip">The audio clip to wrap.</param>
        public UnitySound(T id, AudioClip audioClip) : base(id)
        {
            _audioClip = audioClip;
        }

        /// <inheritdoc />
        public override double Length
        {
            get
            {
                ThrowIfDisposed();
                return _audioClip.length;
            }
        }

        /// <inheritdoc />
        /// <remarks>Destroys the wrapped audio clip only when the program is running; otherwise the program is ending and Unity destroys the audio clip anyway.</remarks>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing && UnityEnvironment.IsPlaying)
                {
                    Object.DestroyImmediate(_audioClip);
                }
                _audioClip = null;
            }
            base.Dispose(disposing);
        }
    }
}
