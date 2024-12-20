using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(JG.Audio.BackgroundMusicManager))]
public class BackgroundMusicManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        JG.Audio.BackgroundMusicManager manager = (JG.Audio.BackgroundMusicManager)target;
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Current Playlist:", manager.CurrentPlaylist ? manager.CurrentPlaylist.name : "None");
        EditorGUILayout.LabelField("Current Track:", manager.CurrentTrackName);
    }
}
