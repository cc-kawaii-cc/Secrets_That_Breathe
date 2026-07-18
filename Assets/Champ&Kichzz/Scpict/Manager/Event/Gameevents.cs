using System;
using UnityEngine;
public static class GameEvents
{
    public static event Action<GameObject> OnPlayerReady;
    public static void PlayerReady(GameObject player) => OnPlayerReady?.Invoke(player);
    public static event Action<GameState> OnGameStateChanged;
    public static void GameStateChanged(GameState s) => OnGameStateChanged?.Invoke(s);
    public static event Action<string> OnClueFound;
    public static void ClueFound(string clueId) => OnClueFound?.Invoke(clueId);
    public static event Action<string> OnInteracted;
    public static void Interacted(string objectName) => OnInteracted?.Invoke(objectName);
    public static event Action OnCutsceneStart;
    public static event Action OnCutsceneEnd;
    public static void CutsceneStart() => OnCutsceneStart?.Invoke();
    public static void CutsceneEnd() => OnCutsceneEnd?.Invoke();
    public static void ClearAll()
    {
        OnPlayerReady = null;
        OnGameStateChanged = null;
        OnClueFound = null;
        OnInteracted = null;
        OnCutsceneStart = null;
        OnCutsceneEnd = null;
    }
}