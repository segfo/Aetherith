using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class WindowMovementTracker : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();

    public Vector3 Velocity { get; private set; }

    private Vector2 lastPosition;
    private float lastTime;

    void Start()
    {
        lastPosition = GetWindowPosition();
        lastTime = Time.time;
    }

    void Update()
    {
        Vector2 currentPos = GetWindowPosition();
        float deltaTime = Time.time - lastTime;

        if (deltaTime > 0f)
        {
            Vector2 delta = (currentPos - lastPosition) / deltaTime;
            Velocity = new Vector3(-delta.x, 0f, -delta.y) * 0.001f; // 揺れ方向の調整
        }

        lastPosition = currentPos;
        lastTime = Time.time;
    }

    private Vector2 GetWindowPosition()
    {
        RECT rect;
        GetWindowRect(GetActiveWindow(), out rect);
        return new Vector2(rect.Left, rect.Top);
    }
}
