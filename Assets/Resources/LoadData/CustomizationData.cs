using Fusion;

[System.Serializable]
public struct CustomizationData : INetworkStruct
{
    public NetworkString<_64> body;
    public NetworkString<_64> head;
    public NetworkString<_64> top;
    public NetworkString<_64> bottom;
    public NetworkString<_64> shoes;
    public NetworkString<_64> outfit;
    public NetworkString<_64> hairstyle;
    public NetworkString<_64> acc_head;
    public NetworkString<_64> gender;
}