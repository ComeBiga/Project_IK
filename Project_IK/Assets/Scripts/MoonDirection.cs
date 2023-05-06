using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class MoonDirection : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        Shader.SetGlobalVector("_SunDirection", transform.forward);
    }
}
