using System;
using Modding;
using UnityEngine;

namespace AdvancedCannon
{
    public class ProjectileCollision : MonoBehaviour
    {
        private void FixedUpdate()
        {
            Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log($"Physical collision registered at {collision.contacts[0].point}.");
            Destroy(gameObject);
        }
    }
}
