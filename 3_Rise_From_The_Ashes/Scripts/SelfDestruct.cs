using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    public float destructionDelay = 3f;

    void Start()
    {
        Invoke("DestroySelf", destructionDelay);
    }

    void DestroySelf()
    {
        Destroy(gameObject);
    }
}
