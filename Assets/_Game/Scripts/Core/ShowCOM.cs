using UnityEngine;

public class ShowCOM : MonoBehaviour
{
    public float radius = 0.05f;
    Rigidbody rb;

    void Awake() => rb = GetComponent<Rigidbody>();

    void OnDrawGizmos()
    {
        var r = rb ? rb : GetComponent<Rigidbody>();
        if (!r) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(r.worldCenterOfMass, radius);
        Gizmos.DrawLine(r.worldCenterOfMass, r.worldCenterOfMass + transform.forward * 0.2f);
    }
}