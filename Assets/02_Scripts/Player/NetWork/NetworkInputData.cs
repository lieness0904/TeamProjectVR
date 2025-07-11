using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 moveInput;
    public Vector2 lookInput;
public void OnInput(NetworkRunner runner, NetworkInput input)
{
    var data = new NetworkInputData
    {
        moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")),
        lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"))
    };
    input.Set(data);
}
}

