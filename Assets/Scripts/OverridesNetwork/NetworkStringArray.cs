using Unity.Netcode;

/// <summary>
/// Cette struct permet de s�rialiser un tableau de string. pr les envoyer sur le r�seau.
/// </summary>
public struct NetworkStringArray : INetworkSerializable
{
    public string[] Array;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int length = 0;
        if (!serializer.IsReader)
            length = Array.Length;

        serializer.SerializeValue(ref length);

        if (serializer.IsReader)
            Array = new string[length];

        for (int n = 0; n < length; n++)
            serializer.SerializeValue(ref Array[n]);
    }
}
