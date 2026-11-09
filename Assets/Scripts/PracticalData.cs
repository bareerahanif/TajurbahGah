using UnityEngine;

/// <summary>
/// One entry per practical (e.g. Pendulum, Ohm's Law, Titration).
/// Create instances via Assets > Create > Lab > Practical Data.
/// </summary>
[CreateAssetMenu(fileName = "NewPractical", menuName = "Lab/Practical Data")]
public class PracticalData : ScriptableObject
{
    public enum Subject { Physics, Chemistry, Biology }

    [Header("Identity")]
    public string practicalName = "Pendulum";
    public Subject subject;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Scene")]
    [Tooltip("Exact scene name to load, must be added in Build Settings.")]
    public string sceneName;

    [Header("Progress (read at runtime, not edited here)")]
    [HideInInspector] public bool completed;
    [HideInInspector] public int starsEarned; // 0-3
    [HideInInspector] public int bestScore;
}
