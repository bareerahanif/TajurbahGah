using UnityEngine;

public enum RadicalType
{
    None,
    Contaminated, // Dirty yellow flame
    Sodium,       // Intense golden-yellow
    Potassium,    // Lilac violet
    Calcium,      // Brick-red
    Strontium,    // Crimson red
    Barium,       // Apple green
    Copper        // Blue-green / emerald
}

public enum WireState
{
    Contaminated,
    DippedInHCl,
    Clean,
    LoadedWithSample
}

[System.Serializable]
public struct RadicalFlameData
{
    public RadicalType radical;
    public string displayName;
    [ColorUsage(true, true)] public Color flameColor; // HDR for URP Bloom
    public Color lightColor;
    public float lightIntensity;
}