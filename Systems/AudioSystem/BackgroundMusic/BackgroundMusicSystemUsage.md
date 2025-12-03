
---

## Background Music Manager - Usage Documentation

### Overview
The `BackgroundMusicManager` is a persistent system for handling background music in Unity. It manages a playlist of tracks, crossfading between them, and responds to events (via an event bus) to switch playlists dynamically. It uses only two `AudioSource` components internally for seamless crossfades.

**Key Features:**
- Crossfade between different playlists.
- Fade in the initial track when the game starts.
- Supports scene-specific playlists via `PlaylistController` components placed in the scene.
- Uses an event bus to change playlists at runtime (e.g., during a boss fight).
- Integrates with Unity’s AudioMixer for centralized audio control.

### Setup Steps
1. **Create Playlists:**  
   - Go to *Create → Audio → Playlist* to create a `PlaylistSO` ScriptableObject.
   - Assign an array of `AudioClips` and configure shuffle and crossfade duration as needed.
   - Assign an `AudioMixerGroup` if desired.

2. **Place the Manager:**  
   - Include the `BackgroundMusicManager` in your initial scene or a bootstrapping scene.
   - Assign a `defaultPlaylist` in the Inspector, as well as the `AudioMixerGroup`.

3. **Scene-Specific Playlists:**  
   - In your scene, add a `PlaylistController` component.
   - Assign a `PlaylistSO` to it. When the scene loads, it will raise a `ChangePlaylistEvent` to the manager, switching to that playlist automatically.

4. **Changing Playlists at Runtime:**  
   - Use events to switch playlists on the fly (e.g., `ChangePlaylistEvent` or custom events).
   - For example, during a boss fight, raise a `ChangePlaylistEvent` with the boss playlist to the event bus.

### Event Usage Example
```csharp
// Raise a ChangePlaylistEvent to switch to a new playlist
EventBus<ChangePlaylistEvent>.Raise(new ChangePlaylistEvent() { NewPlaylist = myNewPlaylist });
```

If `myNewPlaylist` is null, the `BackgroundMusicManager` falls back to the default playlist.

### Notes
- Ensure that the `BackgroundMusicManager` script is always present and not destroyed between scene loads.  
- Handle any custom events (e.g. `BossFightStartedEvent`) by registering a listener in the manager or elsewhere and calling `SwitchToPlaylist()` as needed.

---

## Example Script: `MusicTester.cs`

This script demonstrates how to manually trigger the next track in the current playlist and how to load a new playlist during runtime.

```csharp
using UnityEngine;
using UnityEngine.UI; // if using UI buttons
using JG.Audio;       // Adjust namespace to match your project

public class MusicTester : MonoBehaviour
{
    [SerializeField] private PlaylistSO newPlaylistToLoad;

    void Start()
    {
        // Example: After some delay, switch to a new playlist
        Invoke(nameof(SwitchToNewPlaylist), 5f);
    }

    private void SwitchToNewPlaylist()
    {
        // Raise event to change the playlist
        EventBus<ChangePlaylistEvent>.Raise(new ChangePlaylistEvent() { NewPlaylist = newPlaylistToLoad });
    }

    public void SkipToNextTrack()
    {
        // To skip to next track, we rely on the manager’s logic:
        // Since the manager automatically switches tracks when one finishes, 
        // we can simulate this by momentarily stopping the currently playing track.

        // However, we don't have a direct "NextTrack" method exposed. 
        // If needed, you can modify the BackgroundMusicManager to expose a public method like:
        // BackgroundMusicManager.Instance.PlayNextTrack(); (make PlayNextTrack public)

        // For now, as a conceptual example:
        // BackgroundMusicManager.Instance.SwitchToPlaylist(BackgroundMusicManager.Instance.CurrentPlaylist);
        // This would trigger a crossfade to the next track in the current playlist.

        // If you add a public method in BackgroundMusicManager:
        // BackgroundMusicManager.Instance.NextTrack();
    }
}
```

**What this does:**  
- When the scene starts, after 5 seconds, it triggers a `ChangePlaylistEvent` to switch to `newPlaylistToLoad`.  
- The `SkipToNextTrack` method conceptually shows how you’d invoke a method to jump to the next track. For clarity, you may add a public method in the `BackgroundMusicManager` to handle this directly.  

Adjust and expand this example as needed to fit your game’s logic and UI.