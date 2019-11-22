using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float moveSpeed = 4.0f;
    public float zoomSpeed = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float vMove = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        float hMove = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;

        transform.Translate(hMove, vMove, 0);

        if (Input.GetKey(KeyCode.J))
            Camera.main.orthographicSize += zoomSpeed;
        if (Input.GetKey(KeyCode.K))
            Camera.main.orthographicSize -= zoomSpeed;
    }
}
