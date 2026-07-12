using UnityEngine;

public class Enemy : MonoBehaviour
{
    public void GetHit()
    {
        Debug.Log(gameObject.name + " has been hit!");
    }
}
