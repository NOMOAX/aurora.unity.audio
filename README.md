# Aurora Unity Audio

![license](https://img.shields.io/github/license/NOMOAX/aurora.unity.audio)
![version](https://img.shields.io/badge/version-1.0.4-blue)
![lowest Unity version](https://img.shields.io/badge/Unity-2021.2%2B-blue)

Unity implementation of Aurora Audio, built on `UnityEngine.AudioSource`.

English | [中文](README.zh.md)

## Dependencies

- [Aurora](https://github.com/NOMOAX/aurora.git)
- [Aurora Audio](https://github.com/NOMOAX/aurora.audio.git)
- [Aurora Unity](https://github.com/NOMOAX/aurora.unity.git)

## Installation

1. Open Unity package manager.
2. Click the `+` button in the upper-left corner, then select `Add package from git URL...`.
3. Input `https://github.com/NOMOAX/aurora.unity.audio.git` and then click the `Add` button.

## Overview

Aurora Audio defines the loading, lifetime and event-dispatch model of audio in an abstract way. This package implements that model on top of `UnityEngine.AudioSource`, so it does not need any initialization and does not introduce a second audio backend.

Every abstract type of Aurora Audio has exactly one implementation here:

| Aurora Audio      | This package           | Backed by                                         |
|-------------------|------------------------|---------------------------------------------------|
| `Sound<T>`        | `UnitySound<T>`        | A loaded `AudioClip`                              |
| `Playback<T>`     | `UnityPlayback<T>`     | One `AudioSource` on a dedicated `GameObject`     |
| `AudioManager<T>` | `UnityAudioManager<T>` | Loads clips, creates playbacks, polls their state |

`T` is the audio file identifier type; the derived class decides what to use (`string`, `int`, an enum, a custom type…).

`AudioManager<T>` is not thread safe, and the `AudioSource` API can only be used on the Unity main thread, so every type in this package must be used on the Unity main thread.

## Sound

`UnitySound<T>` is the audio data that is ready to be played: it wraps an already loaded `AudioClip`.

A sound is created by the audio manager that loads it, inside `UnityAudioManager<T>.CreateSoundAsync`; user code never constructs one, and obtains a sound from the manager instead, usually through `GetSoundAsync`:

```csharp
var sound = await audioManager.GetSoundAsync("Audio/Bgm/MainTheme");
```

The following members are what the rest of the program reads from it:

```csharp
var id = sound.Id; // "Audio/Bgm/MainTheme"
var length = sound.Length; // The length of the audio clip, in seconds
var audioClip = sound.AudioClip; // The wrapped audio clip
```

`Id` must be the identifier the sound was requested with: `CreateSoundAsync` cannot hand back a sound whose `Id` is anything else, and a load fails with `InvalidOperationException` when it does.

Nothing is loaded by this type either: the audio clip is handed to it already loaded. `UnitySound<T>` only tells Aurora Audio how long the sound is, and releases its reference to the clip when it is disposed.

```csharp
sound.Dispose(); // Releases the reference to the audio clip
```

`Dispose` does **not** destroy the wrapped audio clip; the owner of the clip decides when the clip itself is unloaded.

Sounds are disposed by the manager that created them — when the last playback of a sound is released, or by `DisposeSound` and `Dispose` — so user code rarely disposes one itself.

## Playback

`UnityPlayback<T>` plays the audio clip of a `UnitySound<T>` through a dedicated `AudioSource`.

It cannot be created directly (its constructor is `protected internal`): playbacks are created by the audio manager that owns the sound, through `CreatePlayback`, so that Aurora Audio can track them, count them per sound, and raise their change events.

```csharp
var playback = audioManager.CreatePlayback(sound);
```

A newly created playback is in the `None` status; `Play` is `CreatePlayback` followed by `Playback<T>.Play`.

Each playback owns one `GameObject` (named after the playback id) with one `AudioSource` on it, parented under a playback container that lives as long as the program does (see [Playback Container](#playback-container)).

| Member               | `AudioSource`                  | Remarks                                                                 |
|----------------------|--------------------------------|-------------------------------------------------------------------------|
| `Status`             | `isPlaying` / `clip`           | Mapped from `AudioSourceStatus`; never `Default`                        |
| `Volume`             | `volume`                       | Expected to be in the range 0 to 1                                      |
| `Position`           | `time`                         | In seconds                                                              |
| `NormalizedPosition` | `timeSamples` / `clip.samples` | In the range 0 to 1; assigning 1 is corrected to the last sample        |
| `Play()`             | `Play()`                       | Plays from the beginning when `Status` is `None`, resumes when `Paused` |
| `Pause()`            | `Pause()`                      | Keeps the current position                                              |
| `Stop()`             | `Stop()`                       | Resets `Status` to `None`                                               |

Assigning a `NormalizedPosition` outside the `[0, 1]` range throws `ArgumentOutOfRangeException`.

```csharp
var playback = audioManager.Play(sound, 0.5);

playback.StatusChanged += (playback, status) => Debug.Log($"status: {status}");
playback.PositionChanged += (playback, position) => Debug.Log($"position: {position}");

playback.Play();
playback.Pause();
playback.NormalizedPosition = 0.5;
playback.Stop();
```

`Status` throws `InvalidOperationException` when the underlying `AudioSource` has no clip, which should not happen unless the clip was cleared from the outside.

Disposing a playback destroys its `GameObject`.

```csharp
playback.Dispose();
```

Playback change events are raised by `AudioManager<T>.Update`, not by the playback itself, so they are only raised as often as that method is called (see [Audio Manager](#audio-manager)).

## Audio Manager

`UnityAudioManager<T>` is the abstract base class implementing `AudioManager<T>`; the only member a derived class has to implement is `CreateSoundAsync`, which decides how an audio file is loaded.

```csharp
public sealed class ResourceAudioManager : UnityAudioManager<string>
{
    protected override async Task<Sound<string>> CreateSoundAsync(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var audioClip = await new ResourceRequestAwaitable<AudioClip>(
            Resources.LoadAsync<AudioClip>(id),
            cancellationToken
        );

        return new UnitySound<string>(id, audioClip);
    }
}
```

`Aurora Unity Addressables` or any other loading mechanism can be used here just as well; this package does not care where the audio clip comes from.

The manager owns every sound and playback it creates:

```csharp
// Loads the clip if it is not loaded yet, then creates a playback and starts it
var playback = await audioManager.PlayAsync("Audio/Bgm/MainTheme");

// Same, but without waiting and without getting the playback back;
// the playback is disposed automatically by Update once it stops
audioManager.BeginPlayAndForget("Audio/Sfx/Click", 0.8);

// Tracks and releases playbacks; call it once per frame
audioManager.Update();

// Disposes every playback of the sound and unloads the sound itself
audioManager.DisposeSound("Audio/Bgm/MainTheme");

// Disposes every sound and every playback
audioManager.Dispose();
```

`Update` does three things in order:

1. raises the `StatusChanged`, `VolumeChanged`, `PositionChanged` and `NormalizedPositionChanged` events for every playback whose state changed since the previous call;
2. releases playbacks that were disposed from the outside;
3. releases playbacks created by `BeginPlayAndForget` once they stop playing.

A released playback decrements the playback count of its sound; when the count reaches zero, the sound is disposed as well, so a sound that is no longer played does not keep its audio clip alive.

Because `Update` is what drives all of the above, the manager must be updated once per frame:

```csharp
public sealed class AudioSystem : MonoBehaviour
{
    private ResourceAudioManager _audioManager;

    private void Awake()
    {
        _audioManager = new ResourceAudioManager();
    }

    private void Update()
    {
        _audioManager.Update();
    }

    private void OnDestroy()
    {
        _audioManager.Dispose();
    }
}
```

For ordinary methods, see the Aurora Audio documentation: `GetSoundAsync`, `CreatePlayback`, `Play`, `PlayAsync`, `BeginPlayAndForget` and `DisposeSound`. Each has overloads taking a volume and a `CancellationToken`.

## Audio Source Utility

`AudioSourceUtility` provides the operations on `AudioSource` that Unity itself does not offer.

### AudioSourceStatus

`AudioSourceStatus` is the status of an audio source, more precise than `AudioSource.isPlaying` alone:

| Value     | Meaning                                                                                                       |
|-----------|---------------------------------------------------------------------------------------------------------------|
| `Default` | The audio source has no audio clip, so there is nothing to play; this is the default value of the enumeration |
| `None`    | The audio source is not playing: it has not started, it was stopped, or it reached the end of the clip        |
| `Playing` | The audio source is playing                                                                                   |
| `Paused`  | The audio source is paused and can resume from its current position                                           |

### Normalized Position

`AudioSource.time` is expressed in seconds; these two methods convert between it and a normalized position based on the sample count, which is more precise than dividing `time` by the clip length.

```csharp
var normalizedPosition = AudioSourceUtility.GetNormalizedPosition(audioSource); // [0, 1), or 0 when there is no clip

AudioSourceUtility.SetNormalizedPosition(audioSource, 0.5);
AudioSourceUtility.SetNormalizedPosition(audioSource, 1); // Corrected to the last sample
```

## Playback Container

`UnityPlaybackContainer` is an internal singleton that hosts the `GameObject` of every playback, so that playback objects are not scattered around a scene. It is created on demand when the first playback is created, and is parented under the `AuroraContainer` (also internal) runtime container of `Aurora Unity`. It is marked with `DontDestroyOnLoad` and with the `HideFlags.DontSave | HideFlags.NotEditable` flags.

There is nothing to configure here; the container is mentioned only because it shows up in the Hierarchy window while the program is running.
