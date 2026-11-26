using UnityEngine;

public interface InDamage
{
    void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockbackForce, bool isCrit);
}
