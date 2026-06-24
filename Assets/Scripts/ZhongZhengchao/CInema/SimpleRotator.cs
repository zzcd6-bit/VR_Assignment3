using UnityEngine;

public class SimpleRotator : MonoBehaviour
{
    [Header("旋转速度设置")]
    [Tooltip("每秒绕 X 轴旋转多少度")]
    public float rotateSpeedX = 0f;

    [Tooltip("每秒绕 Y 轴旋转多少度")]
    public float rotateSpeedY = 50f;

    [Tooltip("每秒绕 Z 轴旋转多少度")]
    public float rotateSpeedZ = 0f;

    private void Update()
    {
        transform.Rotate(
            rotateSpeedX * Time.deltaTime,
            rotateSpeedY * Time.deltaTime,
            rotateSpeedZ * Time.deltaTime,
            Space.Self
        );
    }
}