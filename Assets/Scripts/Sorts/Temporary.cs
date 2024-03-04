using System.Collections;
using UnityEngine;

public class Temporary : MonoBehaviour
{
    /// <summary>
    /// Detruit l'objet après un certain temps
    /// </summary>
    /// <param name="time">Le temps de survie de l'objet</param>
    /// <returns>Quand l'objet est détruir</returns>
    public IEnumerator DestroyIn(float time)
    {
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }
}
