using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptRangement : MonoBehaviour
{
    [SerializeField] private Transform[] parentsATrier;
    [SerializeField] private float espacement = 5;
    [SerializeField] private int tailleLigne = 10;
    [SerializeField] private float distanceCercle = 50;
    public void OnTri()
    {
        GameObject holder = GameObject.Find("Rangement");
        float angle = 0;
        for (int i = 0; i < parentsATrier.Length; i++) //On place les trucs dans un cercle avec un angle de 360/nbParents
        {
            parentsATrier[i].parent = holder.transform;
            parentsATrier[i].SetLocalPositionAndRotation(new Vector3(distanceCercle, 0, 0), Quaternion.identity);
            parentsATrier[i].localScale = Vector3.one;
            holder.transform.localRotation = Quaternion.Euler(0, angle, 0);
            angle += 360 / parentsATrier.Length;
            int cptEnfant = 0;
            int cptLigne = 0;
            foreach (Transform child in parentsATrier[i])
            {
                child.SetLocalPositionAndRotation(new Vector3((cptEnfant % tailleLigne) * espacement, 0, cptLigne * espacement), Quaternion.identity);
                child.localScale = Vector3.one;
                cptEnfant++;
                if (cptEnfant % tailleLigne == 0)
                {
                    cptLigne++;
                }
            }

            parentsATrier[i].parent = null;
        }


    }
}
