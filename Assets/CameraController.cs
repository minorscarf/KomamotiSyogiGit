using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] bool isAutoRotate;
    // ここを中心にカメラを回す
    Vector3 lookAtPosition = new Vector3(0, 0, 0);
    // 前回のポジション
    Vector3 prevPosition;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // タイトルで使う
        if(isAutoRotate)
        {
            transform.RotateAround(lookAtPosition, new Vector3(0, 1, 0), 0.01f);
            return;
        }

        // プレイヤーの操作
        if(Input.GetMouseButtonDown(0))
        {
            prevPosition = Input.mousePosition;
        }
        else if(Input.GetMouseButton(0))
        {
            float val = prevPosition.x - Input.mousePosition.x;
            val *= -0.1f;

            transform.RotateAround(lookAtPosition, new Vector3(0, 1, 0), val);
            prevPosition = Input.mousePosition;
        }
    }
}
