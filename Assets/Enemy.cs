using Unity.Cinemachine;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public void GetHit()
    {
        GetComponent<CinemachineImpulseSource>().GenerateImpulse();//cameraShake
        GetComponent<SquashStrech>().TriggerDefaultSquashStretch();//SquashStrech
        HitStopManager.Instance.TriggerDefaultHitStop();//HitStop

        Debug.Log(gameObject.name + " has been hit!");
    }
}
