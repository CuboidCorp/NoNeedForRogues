using UnityEngine;

public class TestsFonctions : MonoBehaviour
{
    public void TestChest()
    {
        Debug.Log("TestChest");
    }

    public void TestChest2()
    {
        GameObject gaz = Resources.Load<GameObject>("Pieges/ToxicGaz");
        GameObject gazObj = Instantiate(gaz, new Vector3(-19, -3.6f, -8), Quaternion.identity);
        Destroy(gazObj, 5);
    }
}
