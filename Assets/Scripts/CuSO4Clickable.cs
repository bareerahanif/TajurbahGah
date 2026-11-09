
using UnityEngine;

public class CuSO4Clickable : MonoBehaviour
{
    public enum ObjectType
    {
        Dropper,
        Water,
        CopperSulfate
    }

    [SerializeField] private ObjectType objectType;
    [SerializeField] private CuSO4InteractionManager manager;

    public ObjectType Type => objectType;

    public void HandleClick()
    {
        if (manager == null)
        {
            Debug.LogWarning(
                "No Interaction Manager assigned to " + gameObject.name
            );
            return;
        }

        Debug.Log("Experiment object clicked: " + gameObject.name);

        switch (objectType)
        {
            case ObjectType.Dropper:
                manager.SelectDropper();
                break;

            case ObjectType.Water:
                manager.SelectWater();
                break;

            case ObjectType.CopperSulfate:
                manager.AddWaterToSample();
                break;
        }
    }
}
