using Fusion;
using UnityEngine;

public enum InputButton
{
}

public struct NetInput : INetworkInput
{
    public NetworkButtons Buttons;
    public Vector2 Direction;
    public Vector2 LookDelta;
    public float horizontal;
    public float vertical;
    public Quaternion cameraRotation;
}