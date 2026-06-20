using System.Collections.Generic;
using UnityEngine;

public class MazeInventory : MonoBehaviour
{
    private readonly HashSet<string> keys = new HashSet<string>();

    public void AddKey(string keyId)
    {
        if (string.IsNullOrEmpty(keyId))
        {
            return;
        }

        if (keys.Add(keyId))
        {
            Debug.Log("Picked up key: " + keyId);
        }
    }

    public bool HasKey(string keyId)
    {
        return keys.Contains(keyId);
    }
}