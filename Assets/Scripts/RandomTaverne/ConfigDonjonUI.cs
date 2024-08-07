using UnityEngine;
using UnityEngine.UIElements;

public class ConfigDonjonUI : MonoBehaviour
{
    private VisualElement root;

    private void Awake()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        //TODO : Les elements qu'il faut mettre dans le menu uxml c'est
        //Slider Nombre d'etages, min 1 max 10
        //Int le seed de base --> Le seed de base est celui de la journée
        //Slider ou un truc pr Vecteur 2d pr minTailleEtage
        //Slider ou un truc pr Vecteur 2d pr maxTailleEtage
        //Slider pr nb Stairs entre 1 et 5
        //Dropdown pr type etage mais y a que laby pr le moment
        //Slider pr la difficulté (Nombre de pieges + Nombre de loot) --> Voir comment faire la diffictulté
        //Slider pr le difficulty scaling
    }

    private void OnEnable()
    {

    }

    private void OnDisable()
    {

    }
}
