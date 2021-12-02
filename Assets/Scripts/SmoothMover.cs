using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.OpenXR.Input;

public class SmoothMover : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var input = new Vector3(Input.GetAxis("horizontal"), 0, Input.GetAxis("vertical"));

        if (input != Vector3.zero)
        {
            transform.forward = input;
        }
    }
}
