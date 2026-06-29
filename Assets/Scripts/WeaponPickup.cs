using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public bool isAvailable = true;

    public void PickUp()
    {
        isAvailable = false;
        gameObject.SetActive(false);
    }
}