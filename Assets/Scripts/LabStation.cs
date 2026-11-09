using UnityEngine;

public enum StationType
{
    HClBeaker,
    SaltDish,
    BunsenBurnerTip,
    TableRestPoint
}

public class LabStation : MonoBehaviour
{
    [Header("Station Setup")]
    public StationType stationType;
    public RadicalType saltContained = RadicalType.None;

    [Tooltip("Target transform where the WIRE ROOT should position and rotate before animating.")]
    public Transform wireApproachPoint;

    private void OnTriggerEnter(Collider other)
    {
        PlatinumWireTool wire = other.GetComponentInParent<PlatinumWireTool>();
        if (wire != null)
        {
            wire.NotifyStationEntered(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlatinumWireTool wire = other.GetComponentInParent<PlatinumWireTool>();
        if (wire != null)
        {
            wire.NotifyStationExited(this);
        }
    }
}