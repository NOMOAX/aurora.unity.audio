# Aurora Unity Audio

![许可](https://img.shields.io/github/license/NOMOAX/aurora.unity.audio)
![版本](https://img.shields.io/badge/version-1.0.5-blue)
![最低 Unity 版本](https://img.shields.io/badge/Unity-2021.2%2B-blue)

Aurora Audio 的 Unity 实现，建立在 `UnityEngine.AudioSource` 之上。

[English](README.md) | 中文

## 依赖

- [Aurora](https://github.com/NOMOAX/aurora.git)
- [Aurora Audio](https://github.com/NOMOAX/aurora.audio.git)
- [Aurora Unity](https://github.com/NOMOAX/aurora.unity.git)

## 安装

1. 打开 Unity package manager。
2. 点击左上角的 `+` 按钮，然后选择 `Add package from git URL...`。
3. 填入 `https://github.com/NOMOAX/aurora.unity.audio.git` 并点击 `Add` 按钮。

## 概述

Aurora Audio 以抽象的方式定义了音频的加载、生命周期与事件分发模型。本包在 `UnityEngine.AudioSource` 之上实现这套模型，因此不需要任何初始化，也不会引入第二套音频后端。

Aurora Audio 里的每个抽象类型在本包中都有一个对应实现：

| Aurora Audio      | 本包                   | 由什么支撑                               |
|-------------------|------------------------|------------------------------------------|
| `Sound<T>`        | `UnitySound<T>`        | 一个已加载的 `AudioClip`                 |
| `Playback<T>`     | `UnityPlayback<T>`     | 专用 `GameObject` 上的一个 `AudioSource` |
| `AudioManager<T>` | `UnityAudioManager<T>` | 加载音频、创建播放、轮询播放状态         |

`T` 是音频文件标识的类型，由派生类决定用什么（`string`、`int`、枚举、自定义类型……）。

`AudioManager<T>` 不保证线程安全，而 `AudioSource` 的 API 只能在 Unity 主线程上使用，因此本包的所有类型都必须在 Unity 主线程上使用。

## 音频数据

`UnitySound<T>` 是已经可以播放的音频数据：它包装一个已经加载好的 `AudioClip`。

音频由加载它的音频管理器创建，也就是在 `UnityAudioManager<T>.CreateSoundAsync` 里创建；外部代码不要自己 `new` 一个，而是从管理器那里取，通常用 `GetSoundAsync`：

```csharp
var sound = await audioManager.GetSoundAsync("Audio/Bgm/MainTheme");
```

程序其余部分会从它身上读到的成员有：

```csharp
var id = sound.Id; // "Audio/Bgm/MainTheme"
var length = sound.Length; // 音频长度，单位为秒
var audioClip = sound.AudioClip; // 被包装的音频剪辑
```

`Id` 必须等于请求该音频时使用的标识：`CreateSoundAsync` 不能返回 `Id` 与之不符的音频，否则加载会以 `InvalidOperationException` 失败。

本类型同样不负责加载：音频剪辑是加载好之后才交给它的。`UnitySound<T>` 只负责告诉 Aurora Audio 这个音频有多长，并在被释放时解除对音频剪辑的引用。

```csharp
sound.Dispose(); // 解除对音频剪辑的引用
```

`Dispose` 并 **不会**销毁被包装的音频剪辑；剪辑本身什么时候卸载，由它的持有者决定。

音频由创建它的管理器释放——某个音频的最后一个播放被释放时，或者调用 `DisposeSound`、`Dispose` 时——所以外部代码很少自己释放它。

## 播放

`UnityPlayback<T>` 通过一个专用的 `AudioSource` 播放 `UnitySound<T>` 的音频剪辑。

它不能直接创建（构造函数是 `protected internal`）：播放由持有该音频的音频管理器通过 `CreatePlayback` 创建，这样 Aurora Audio 才能跟踪它们、按音频计数，并触发它们的变化事件。

```csharp
var playback = audioManager.CreatePlayback(sound);
```

新建出来的播放处于 `None` 状态；`Play` 相当于先 `CreatePlayback`、再调用 `Playback<T>.Play`。

每个播放都拥有一个 `GameObject`（名字就是播放的 id）和其上的一个 `AudioSource`，它们被挂在播放容器之下，容器的生命周期与程序一致（见 [播放容器](#播放容器)）。

| 成员                 | 对应的 `AudioSource`           | 说明                                                      |
|----------------------|--------------------------------|-----------------------------------------------------------|
| `Status`             | `isPlaying` / `clip`           | 由 `AudioSourceStatus` 映射而来；永远不会是 `Default`     |
| `Volume`             | `volume`                       | 取值应在 0 到 1 之间                                      |
| `Position`           | `time`                         | 单位为秒                                                  |
| `NormalizedPosition` | `timeSamples` / `clip.samples` | 取值 0 到 1；写入 1 会被修正到最后一个采样                |
| `Play()`             | `Play()`                       | `Status` 为 `None` 时从头播，为 `Paused` 时从当前位置继续 |
| `Pause()`            | `Pause()`                      | 保留当前位置                                              |
| `Stop()`             | `Stop()`                       | 把 `Status` 重置为 `None`                                 |

写入 `[0, 1]` 之外的 `NormalizedPosition` 会抛出 `ArgumentOutOfRangeException`。

```csharp
var playback = audioManager.Play(sound, 0.5);

playback.StatusChanged += (playback, status) => Debug.Log($"状态：{status}");
playback.PositionChanged += (playback, position) => Debug.Log($"位置：{position}");

playback.Play();
playback.Pause();
playback.NormalizedPosition = 0.5;
playback.Stop();
```

当底层 `AudioSource` 没有音频剪辑时，`Status` 会抛出 `InvalidOperationException`；正常情况下不会发生，除非剪辑被外部清空。

释放一个播放会销毁它的 `GameObject`。

```csharp
playback.Dispose();
```

播放的各项变化事件是由 `AudioManager<T>.Update` 触发的，而不是播放自己触发的，因此它们的触发频率取决于该方法被调用的频率（见 [音频管理器](#音频管理器)）。

## 音频管理器

`UnityAudioManager<T>` 是实现 `AudioManager<T>` 的抽象基类；派生类唯一必须实现的是 `CreateSoundAsync`，它决定音频文件怎么加载。

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

这里同样可以使用 `Aurora Unity Addressables` 或任何其他加载方式；本包不关心音频剪辑来自哪里。

管理器持有它创建的每一个音频与每一个播放。

```csharp
// 未加载则先加载，然后创建一个播放并开始播放
var playback = await audioManager.PlayAsync("Audio/Bgm/MainTheme");

// 同上，但不等待、也拿不到播放；
// 该播放停止后会被 Update 自动释放
audioManager.BeginPlayAndForget("Audio/Sfx/Click", 0.8);

// 跟踪并释放播放；每帧调用一次
audioManager.Update();

// 释放该音频的所有播放，并卸载音频本身
audioManager.DisposeSound("Audio/Bgm/MainTheme");

// 释放所有音频与所有播放
audioManager.Dispose();
```

`Update` 按顺序做三件事：

1. 为每个自上次调用以来状态发生变化的播放触发 `StatusChanged`、`VolumeChanged`、`PositionChanged`、`NormalizedPositionChanged` 事件；
2. 释放被外部主动释放的播放；
3. 释放由 `BeginPlayAndForget` 创建、且已经停止播放的播放。

被释放的播放会把所属音频的播放计数减一；计数归零时音频也会被释放，因此不再播放的音频不会一直持有它的音频剪辑。

由于上述一切都依赖 `Update` 驱动，管理器必须每帧更新一次。

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

其余方法的说明见 Aurora Audio 的文档：`GetSoundAsync`、`CreatePlayback`、`Play`、`PlayAsync`、`BeginPlayAndForget`、`DisposeSound`。它们都有接受音量与 `CancellationToken` 的重载。

## 音频源工具

`AudioSourceUtility` 提供 Unity 自己没有提供的 `AudioSource` 操作。

### AudioSourceStatus

`AudioSourceStatus` 是音频源的状态，比单看 `AudioSource.isPlaying` 更精确：

| 值        | 含义                                                         |
|-----------|--------------------------------------------------------------|
| `Default` | 音频源没有音频剪辑，没有东西可播；这是该枚举的默认值         |
| `None`    | 音频源没有在播放：尚未开始播放、被停止，或者播放到了剪辑末尾 |
| `Playing` | 音频源正在播放                                               |
| `Paused`  | 音频源已暂停，可以从当前位置继续                             |

### 归一化位置

`AudioSource.time` 的单位是秒；下面两个方法在它和基于采样数的归一化位置之间互转，比用 `time` 除以音频长度更精确。

```csharp
var normalizedPosition = AudioSourceUtility.GetNormalizedPosition(audioSource); // [0, 1)，没有剪辑时为 0

AudioSourceUtility.SetNormalizedPosition(audioSource, 0.5);
AudioSourceUtility.SetNormalizedPosition(audioSource, 1); // 会被修正到最后一个采样
```

## 播放容器

`UnityPlaybackContainer` 是一个 `internal` 单例，用来承载所有播放的 `GameObject`，让播放对象不散落在场景里。它在创建第一个播放时按需建立，并挂在 `Aurora Unity` 的 `AuroraContainer`（同样是 `internal`）运行时容器之下。它带有 `DontDestroyOnLoad` 以及 `HideFlags.DontSave | HideFlags.NotEditable` 标记。

这里没有任何需要配置的东西；提到它只是因为程序运行时它会在 Hierarchy 窗口里出现。
