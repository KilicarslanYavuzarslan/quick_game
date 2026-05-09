using UnityEngine;
using System;

/// <summary>
/// Tracks score and mistake count during a game session.
/// Fires events whenever values change so UIManager can update
/// the HUD without polling every frame.
///
/// GameManager owns the 3-mistake limit and win/lose logic.
/// ScoreManager only tracks numbers and notifies listeners.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static ScoreManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── State ─────────────────────────────────────────────────────────────────

    public int Score    { get; private set; }
    public int Mistakes { get; private set; }

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired whenever the score changes. Carries the new score value.</summary>
    public event Action<int> OnScoreChanged;

    /// <summary>Fired whenever a mistake is registered. Carries the new mistake count.</summary>
    public event Action<int> OnMistakesChanged;

    // ── Public API ────────────────────────────────────────────────────────────

    public void AddScore(int amount)
    {
        Score += amount;
        OnScoreChanged?.Invoke(Score);
    }

    /// <summary>
    /// Deducts score without allowing it to go below zero.
    /// Returns the actual amount deducted.
    /// </summary>
    public int SpendScore(int amount)
    {
        if (amount <= 0) return 0;

        int deducted = Mathf.Min(Score, amount);
        Score -= deducted;
        OnScoreChanged?.Invoke(Score);
        return deducted;
    }

    public void RegisterMistake()
    {
        Mistakes++;
        OnMistakesChanged?.Invoke(Mistakes);
    }

    public void Reset()
    {
        Score    = 0;
        Mistakes = 0;
        OnScoreChanged?.Invoke(Score);
        OnMistakesChanged?.Invoke(Mistakes);
    }
}
