# 7 Days to Die AudioSource System Analysis

## Overview

This document provides a comprehensive analysis of how 7 Days to Die handles AudioSources, particularly in multiplayer environments where multiple players need to hear synchronized audio from blocks and entities in 3D space.

## Core Audio Architecture

### Audio.Manager - Central Audio System

The game uses a centralized audio management system through `Audio.Manager` which serves as the primary orchestrator for all audio playback:

```csharp
namespace Audio
{
    public class Manager : IDisposable
    {
        public static Dictionary<string, object> audioData;
        public static Dictionary<string, object> loopingOnEntity;
        public static Dictionary<string, object> loopingOnPosition;
        public static List<AudioSource> playingAudioSources;
        public static Dictionary<string, object> playingOnEntity;
        public static EntityPlayerLocal localPlayer;
        public static Vector3 currentListenerPosition;
        
        // Key methods for multiplayer audio
        public static void BroadcastPlay(Entity entity, string soundGroupName, bool signalOnly);
        public static void BroadcastPlay(Vector3 position, string soundGroupName, float occlusion);
        public static void BroadcastStop(int entityId, string soundGroupName);
        public static void BroadcastStop(Vector3 position, string soundGroupName);
    }
}
```

### Audio.Server - Multiplayer Audio Coordination

The `Audio.Server` class handles server-side audio management for multiplayer scenarios:

```csharp
namespace Audio
{
    public class Server : IDisposable
    {
        public EntityPlayerLocal m_localPlayer;
        public Dictionary<string, object> m_players;
        
        // Core multiplayer audio methods
        public void Play(Entity playOnEntity, string soundGroupName, float occlusion, bool signalOnly);
        public void Play(Vector3 position, string soundGroupName, float occlusion, int entityId);
        public void Stop(int playOnEntityId, string soundGroupName);
        public void Stop(Vector3 position, string soundGroupName);
    }
}
```

## Network Audio Synchronization

### NetPackageAudio - Network Audio Messages

The game uses `NetPackageAudio` for synchronizing audio between client and server:

```csharp
namespace Audio
{
    public class NetPackageAudio : NetPackage
    {
        public int playOnEntityId;
        public string soundGroupName;
        public bool play;
        public Vector3 position;
        public bool playOnEntity;
        public float occlusion;
        public bool signalOnly;
        
        // Setup methods for different audio scenarios
        public NetPackageAudio Setup(int _playOnEntityId, string _soundGroupName, float _occlusion, bool _play, bool _signalOnly);
        public NetPackageAudio Setup(Vector3 _position, string _soundGroupName, float _occlusion, bool _play, int entityId);
    }
}
```

### How Multiplayer Audio Works

1. **Server Authority**: When a block or entity needs to play audio, the server decides when and where the sound should play
2. **Network Broadcasting**: The server sends `NetPackageAudio` messages to all clients in range
3. **Client Playback**: Each client receives the network message and creates local AudioSources
4. **Spatial Audio**: AudioSources are positioned in 3D space so players hear them based on their position

## 3D Spatial Audio Implementation

### AudioSource Configuration for 3D Sound

When creating AudioSources for blocks and entities, the game configures them for proper 3D spatial audio:

```csharp
// Example from RadioManager.cs showing proper 3D AudioSource setup
audioSource.spatialBlend = 1.0f; // Full 3D spatial audio
audioSource.rolloffMode = AudioRolloffMode.Linear;
audioSource.minDistance = 5f;
audioSource.maxDistance = 50f;
audioSource.dopplerLevel = 0f; // Disable doppler for stationary sources
```

### Position-Based Audio Management

The audio system tracks sounds by position and entity ID:

```csharp
public static Dictionary<Vector3, object> loopingOnPosition;
public static Dictionary<int, object> loopingOnEntity;
```

## Audio Synchronization Mechanisms

### Client-Side Synchronization

The game implements several mechanisms to keep audio synchronized across clients:

1. **Time-Based Sync**: AudioSources track playback time and sync to the furthest progressed source
2. **Position Matching**: Audio sources are matched by 3D position with tolerance for network precision
3. **Entity Attachment**: AudioSources can be attached to entity transforms for moving objects

### Example Synchronization Code

```csharp
// From RadioManager.cs - shows synchronization approach
public static void SyncAudioSource(string clipName)
{
    float latestPlayTime = 0;
    List<AudioSource> sources = GetAudioSources(clipName);
    AudioSource sourcePrimary = null;

    // Find the source that's furthest in playback
    foreach (AudioSource source in sources)
    {
        if (source != null && source.isPlaying && source.time >= latestPlayTime)
        {
            latestPlayTime = source.time;
            sourcePrimary = source;
        }
    }

    // Synchronize all other sources to the primary
    foreach (AudioSource source in sources)
    {
        if (source != sourcePrimary && source != null)
        {
            float timeDifference = Mathf.Abs(source.time - latestPlayTime);
            if (timeDifference > 0.1f) // Only sync if difference is significant
            {
                source.time = Mathf.Clamp(latestPlayTime, 0f, source.clip.length - 0.1f);
            }
        }
    }
}
```

## Block-Based Audio (TileEntity Audio)

### Audio Lifecycle for Blocks

When blocks need to play continuous audio (like generators, radios, etc.):

1. **Block Placement**: Audio is initialized when block is placed
2. **World Loading**: Audio is restored when chunks load
3. **Network Sync**: Server broadcasts audio state to joining clients
4. **Block Destruction**: Audio is properly cleaned up

### Position-Based Audio Sources

For blocks that don't inherit from MonoBehaviour, the game creates standalone AudioSource GameObjects:

```csharp
// Create standalone AudioSource at world position
GameObject audioGameObject = new GameObject($"BlockAudioSource_{soundName}_{blockID}");
audioGameObject.transform.position = blockWorldPosition;
AudioSource audioSource = audioGameObject.AddComponent<AudioSource>();
```

## Entity-Based Audio

### Moving Audio Sources

For entities like vehicles, drones, or NPCs that move:

1. **Transform Attachment**: AudioSources are parented to the entity's transform
2. **Automatic Following**: Unity's transform system handles position updates
3. **Network Updates**: Entity movement updates automatically move attached audio

```csharp
// Attach AudioSource to entity transform for automatic movement
if (entityTransform != null)
{
    audioGameObject = new GameObject($"EntityAudioSource_{soundName}");
    audioGameObject.transform.SetParent(entityTransform);
    audioGameObject.transform.localPosition = Vector3.zero;
}
```

## Audio Occlusion and Distance

### Occlusion Calculation

The game calculates occlusion for realistic audio based on world geometry:

```csharp
public static float CalculateOcclusion(Vector3 positionOfSound, Vector3 positionOfEars)
{
    // Raycasting to determine if sound is blocked by walls/terrain
    // Returns occlusion multiplier (0.0 = fully occluded, 1.0 = no occlusion)
}
```

### Distance-Based Attenuation

AudioSources use Unity's built-in distance attenuation:
- `minDistance`: Distance where sound is at full volume
- `maxDistance`: Distance where sound becomes inaudible
- `rolloffMode`: How sound volume decreases with distance

## Server vs Client Audio Responsibilities

### Server Responsibilities

1. **Authority**: Decides when audio should start/stop
2. **Broadcasting**: Sends audio commands to all clients in range
3. **State Management**: Tracks which entities/blocks have active audio
4. **Range Culling**: Only sends audio to players within audible range

### Client Responsibilities

1. **Local Playback**: Creates and manages local AudioSource instances
2. **3D Positioning**: Positions audio correctly in 3D space relative to player
3. **Synchronization**: Syncs with other audio sources playing the same sound
4. **Cleanup**: Properly destroys AudioSources when no longer needed

## Performance Considerations

### Audio Culling

The game implements several optimization strategies:

1. **Distance Culling**: Don't send audio to players too far away
2. **Maximum Sources**: Limit total number of concurrent AudioSources
3. **Priority System**: More important sounds take priority over less important ones
4. **LOD System**: Reduce audio quality/complexity at distance

### Memory Management

```csharp
public static void AddPlayingAudioSource(AudioSource _src);
public static void RemovePlayingAudioSource(AudioSource _src);
```

The game tracks all active AudioSources and properly cleans them up to prevent memory leaks.

## Integration with Game Systems

### MinEvent System Integration

Audio integrates with the game's event system through `MinEventActionPlaySound`:

```csharp
public class MinEventActionPlaySound : MinEventActionSoundBase, IMinEventAction
{
    public void Execute(MinEventParams _params)
    {
        // Handles playing sounds through the event system
        // Used by blocks, entities, and gameplay events
    }
}
```

### GameManager Audio Interface

The `GameManager` provides high-level audio methods that handle both single-player and multiplayer scenarios:

```csharp
public void PlaySoundAtPositionServer(Vector3 _pos, string _audioClipName, AudioRolloffMode _mode, int _distance);
public void PlaySoundAtPositionClient(Vector3 _pos, string _audioClipName, AudioRolloffMode _mode, int _distance);
```

## Best Practices for Custom Audio Implementation

### 1. Use Game's Audio System

Always work with the existing `Audio.Manager` system rather than creating independent AudioSources:

```csharp
// Good: Use game's system
Audio.Manager.BroadcastPlay(entity, soundGroupName, false);

// Avoid: Direct AudioSource creation without game knowledge
audioSource.Play(); // Game won't know about this
```

### 2. Handle Network Synchronization

For multiplayer compatibility, ensure audio is properly networked:

```csharp
// Server-side audio initiation
if (GameManager.Instance.World.IsRemote()) 
{
    Audio.Manager.BroadcastPlay(position, soundGroupName, occlusion);
}
```

### 3. Proper Cleanup

Always clean up AudioSources when entities/blocks are destroyed:

```csharp
public void OnDestroy()
{
    if (AudioSourceObject != null)
    {
        Audio.Manager.RemovePlayingAudioSource(AudioSourceObject);
        UnityEngine.Object.Destroy(AudioSourceObject.gameObject);
    }
}
```

### 4. Synchronization for Continuous Audio

For looping audio that needs to stay synchronized across players:

```csharp
// Register for synchronization
if (audioSource.loop)
{
    RegisterForPeriodicSync(audioSource, soundGroupName);
}
```

## Conclusion

The 7 Days to Die audio system is designed around a client-server architecture where:

1. **Server has authority** over when and where audio plays
2. **Network messages synchronize** audio across all clients
3. **3D spatial audio** ensures realistic sound positioning
4. **Automatic cleanup** prevents memory leaks and orphaned audio
5. **Synchronization systems** keep looping audio aligned across players

Understanding this architecture is crucial for implementing custom audio features that work correctly in multiplayer environments. The key is to work with the game's existing systems rather than bypassing them, ensuring compatibility and proper network synchronization.