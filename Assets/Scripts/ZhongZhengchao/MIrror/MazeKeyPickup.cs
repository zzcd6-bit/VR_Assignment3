using UnityEngine;

public class MazeKeyPickup : MonoBehaviour
{
    public string keyId = "GoldKey";
    public bool pickupOnTouch = false;
    public AudioSource pickupSound;

    private bool pickedUp = false;

    public void Interact()
    {
        if (pickedUp)
        {
            return;
        }

        MazeInventory inventory = FindObjectOfType<MazeInventory>();

        if (inventory == null)
        {
            Debug.LogWarning("No MazeInventory found on player.");
            return;
        }

        PickUp(inventory);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!pickupOnTouch)
        {
            return;
        }

        MazeInventory inventory = other.GetComponentInParent<MazeInventory>();

        if (inventory != null)
        {
            PickUp(inventory);
        }
    }

    private void PickUp(MazeInventory inventory)
    {
        if (pickedUp)
        {
            return;
        }

        pickedUp = true;
        inventory.AddKey(keyId);

        if (pickupSound != null)
        {
            pickupSound.Play();
        }

        Debug.Log("Picked up key: " + keyId);

        Destroy(gameObject, 0.08f);
    }
}