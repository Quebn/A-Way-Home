using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Essence : MonoBehaviour
{
    [SerializeField] private string ID;
    [SerializeField] private int energyRestored;
    private HomePortal homePortal;



    public Vector3 worldPosition => NodeGrid.GetMiddle(this.transform.position);

    public static Dictionary<Vector2, Essence> list;
    
    private void Start()
    {
        Debug.Log($"Essence Position: {worldPosition}");
        Initialize();
    }

    private void Initialize()
    {
        Debug.Assert(list != null, "ERROR: List is null");
        list.Add(worldPosition, this);
    }

    public void OnConsume(Character character)
    {
        this.gameObject.SetActive(false);
        list.Remove(this.worldPosition);
        character.IncrementEnergy(energyRestored);
        character.IncrementEssence(-1);
    }

    public static List<Vector3> GetCurrentDestinations()
    {
        List<Vector3> destinations = new List<Vector3>();
        Debug.Assert(list.Count > 0, "ERROR: No Essences found");
        foreach(KeyValuePair<Vector2, Essence> pair in list)
            destinations.Add(pair.Key);
        return destinations;
    }

    public static void InitializeAll()
    {

    }

    [ContextMenu("Generate Essence ID")]
    private void GenerateID() 
    {
        this.ID = System.Guid.NewGuid().ToString();
    }
}
