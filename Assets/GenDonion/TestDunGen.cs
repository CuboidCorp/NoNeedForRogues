using UnityEngine;

public class TestDunGen : MonoBehaviour
{
    [SerializeField]
    private GameObject cubePrefab;
    // Start is called before the first frame update
    void Start()
    {
        GameObject go1 = Instantiate(cubePrefab, new Vector3(0, 0, 0), Quaternion.identity);
        go1.transform.localScale = new Vector3(1, 1, 1);

        GameObject go2 = Instantiate(cubePrefab, new Vector3(0, 0, 0), Quaternion.identity);
        go2.transform.localScale = new Vector3(2, 2, 2);
    }
}
