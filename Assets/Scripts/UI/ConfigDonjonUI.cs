using System;
using UnityEngine;
using UnityEngine.UIElements;
using Donnees;

public class ConfigDonjonUI : MonoBehaviour
{
    private UIDocument doc;
    private VisualElement root;

    [SerializeField] private float interactDistance = 100f;

    private SliderInt nbEtages;
    private IntegerField seed;
    private Vector2IntField minTailleEtage;
    private Vector2IntField maxTailleEtage;
    private SliderInt nbStairs;
    private EnumField typeDonjon;
    private SliderInt baseDifficulty;
    private SliderInt difficultyScaling;
    private Label textErreur;
    private Button saveChanges;
    private Button cancelChanges;

    public ConfigDonjon conf;

    public static ConfigDonjonUI Instance;

    private void Awake()
    {
        Instance = this;
        doc = GetComponent<UIDocument>();
        root = doc.rootVisualElement;

        //TODO : Les elements qu'il faut mettre dans le menu uxml c'est
        //Slider Nombre d'etages, min 1 max 10
        //Int le seed de base --> Le seed de base est celui de la journée
        //Slider ou un truc pr Vecteur 2d pr minTailleEtage
        //Slider ou un truc pr Vecteur 2d pr maxTailleEtage
        //Slider pr nb Stairs entre 1 et 5
        //Dropdown pr type etage mais y a que laby pr le moment
        //Slider pr la difficulté (Nombre de pieges + Nombre de loot) --> Voir comment faire la diffictulté
        //Slider pr le difficulty scaling
        //Label pr erreurs et infos aux joueurs
        //Button pr save 
        //Button pr reset

        Reset();

    }

    private void OnEnable()
    {
        doc.panelSettings.SetScreenToPanelSpaceFunction((Vector2 screenPosition) =>
        {
            Vector2 invalidPos = new(float.NaN, float.NaN);

            Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition); //Utiliser l'input de l'autre systeme d'input
            Debug.DrawRay(cameraRay.origin, cameraRay.direction * interactDistance, Color.magenta);

            if (!Physics.Raycast(cameraRay, out RaycastHit hit, interactDistance, LayerMask.GetMask("UI")))
            {
                Debug.Log("Invalid position");
                return invalidPos;
            }

            Vector2 pixelUV = hit.textureCoord;

            pixelUV.y = 1 - pixelUV.y;
            pixelUV.x *= doc.panelSettings.targetTexture.width;
            pixelUV.y *= doc.panelSettings.targetTexture.height;

            return pixelUV;

        });

        saveChanges.clicked += () => SaveSettings();
        cancelChanges.clicked += () => Reset();
    }

    private void OnDisable()
    {
        doc.panelSettings.SetScreenToPanelSpaceFunction(null);
        saveChanges.clicked -= () => SaveSettings();
        cancelChanges.clicked -= () => Reset();
    }

    /// <summary>
    /// Sauvegarde les choix si ils sont valides
    /// </summary>
    private void SaveSettings()
    {
        if (CheckSettings())
        {
            conf = new()
            {
                nbEtages = nbEtages.value,
                seed = seed.value,
                minTailleEtage = minTailleEtage.value,
                maxTailleEtage = maxTailleEtage.value,
                nbStairs = nbStairs.value,
                typeEtage = typeDonjon.value,
                baseDiff = baseDifficulty.value,
                diffScaling = difficultyScaling.value
            };
            textErreur.text = "Changements sauvegardés";
        }
    }

    /// <summary>
    /// Remet les options de base
    /// </summary>
    private void Reset()
    {
        conf = new();
        //On charge les valeurs de base de conf dans les autres trucs
        nbEtages.value = conf.nbEtages;
        seed.value = conf.seed;
        minTailleEtage.value = conf.minTailleEtage;
        maxTailleEtage.value = conf.maxTailleEtage;
        nbStairs.value = conf.nbStairs;
        typeDonjon.value = conf.typeEtage;
        baseDifficulty.value = conf.baseDiff;
        difficultyScaling.value = conf.diffScaling;
        textErreur.text = "";
    }

    /// <summary>
    /// Verifie si les données entrée sont correctes
    /// </summary>
    /// <returns>True si ok, false sinon</returns>
    private bool CheckSettings()
    {
        if (minTailleEtage.value.x == 0 || minTailleEtage.value.y == 0 || maxTailleEtage.value.x == 0 || maxTailleEtage.value.y == 0) //TODO : Voir si on peut mettre des valeurs min dans le truc
        {
            textErreur.text = "Tailles etages invalides, trop petites";
            return false;
        }

        if (minTailleEtage.value.x > maxTailleEtage.value.x || minTailleEtage.value.y > maxTailleEtage.value.y)
        {
            textErreur.text = "Taille etage min doit être inferieure a max";
            return false;
        }

        if (nbStairs.value > minTailleEtage.value.x || nbStairs.value > minTailleEtage.value.y)
        {
            textErreur.text = "Nb stairs trop grand";
            return false;
        }

        if (typeDonjon.value != TypeEtage.Labyrinthe)
        {
            textErreur.text = "Type de donjon non implémenté encore ;(";
            return false;
        }


        return true;
    }

}

[System.Serializable]
public class ConfigDonjon
{
    public int nbEtages;
    public int seed;
    public Vector2Int minTailleEtage;
    public Vector2Int maxTailleEtage;
    public int nbStairs;
    public TypeEtage typeEtage;
    public int baseDiff;
    public int diffScaling;

    public ConfigDonjon()
    {
        nbEtages = 5;
        DateTime now = DateTime.Now;
        seed = now.Year * 10000 + now.Month * 100 + now.Day;
        minTailleEtage = new(10, 10);
        maxTailleEtage = new(20, 20);
        nbStairs = 2;
        typeEtage = TypeEtage.Labyrinthe;
        baseDiff = 1;
        diffScaling = 1;
    }
}

