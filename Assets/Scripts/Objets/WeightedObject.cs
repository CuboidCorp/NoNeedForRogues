using Unity.Netcode;

public class WeightedObject : NetworkBehaviour
{
    public float weight = 1;

    public NetworkVariable<bool> isHeld = new(false);
}
