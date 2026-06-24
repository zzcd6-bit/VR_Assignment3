using UnityEngine;

public class ShootingTarget : MonoBehaviour
{
    private bool isHit;

    public bool Hit()
    {
        if (isHit)
        {
            return false;
        }

        isHit = true;
        gameObject.SetActive(false);
        return true;
    }
}