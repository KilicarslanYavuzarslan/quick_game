using UnityEngine;

/// <summary>
/// Tracks score and fires events for UIManager to consume.
/// Stub for Step 5 — full implementation in Step 9.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int Score { get; private set; }
    public int Mistakes { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Full event system + UI wiring added in Step 9
    public void AddScore(int amount)   => Score   += amount;
    public void RegisterMistake()      => Mistakes++;
}
