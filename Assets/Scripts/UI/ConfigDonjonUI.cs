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
    private SliderInt baseDifficulty;
    private SliderInt difficultyScaling;
    private Label labelInfo;
    private Button saveChanges;
    private Button cancelChanges;

    private VisualElement cursor; //TEMPORAIRE

    public ConfigDonjon conf;

    public static ConfigDonjonUI Instance;

    private void Awake()
    {
        Instance = this;
        doc = GetComponent<UIDocument>();
        root = doc.rootVisualElement;

        nbEtages = root.Q<SliderInt>("nbEtages");
        seed = root.Q<IntegerField>("seed");
        minTailleEtage = root.Q<Vector2IntField>("minTailleEtage");
        maxTailleEtage = root.Q<Vector2IntField>("maxTailleEtage");
        nbStairs = root.Q<SliderInt>("nbStairs");
        baseDifficulty = root.Q<SliderInt>("baseDiff");
        difficultyScaling = root.Q<SliderInt>("diffScaling");
        saveChanges = root.Q<Button>("saveChanges");
        cancelChanges = root.Q<Button>("cancelChanges");
        labelInfo = root.Q<Label>("labelInfo");

        cursor = root.Q<VisualElement>("cursor"); //TEMPORAIRE

        ResetOptions();

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

            cursor.style.left = pixelUV.x;
            cursor.style.top = pixelUV.y;

            return pixelUV;

        });

        saveChanges.clicked += () => SaveSettings();
        cancelChanges.clicked += () => ResetOptions();
    }

    private void OnDisable()
    {
        doc.panelSettings.SetScreenToPanelSpaceFunction(null);
        saveChanges.clicked -= () => SaveSettings();
        cancelChanges.clicked -= () => ResetOptions();
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
                typeEtage = TypeEtage.Labyrinthe,
                baseDiff = baseDifficulty.value,
                diffScaling = difficultyScaling.value
            };
            labelInfo.style.color = new(Color.green);
            labelInfo.text = "Changements sauvegard�s";
        }
    }

    /// <summary>
    /// Remet les options de base
    /// </summary>
    private void ResetOptions()
    {
        conf = new();
        Debug.Log("Reset " + conf);
        //On charge les valeurs de base de conf dans les autres trucs
        nbEtages.value = conf.nbEtages;
        seed.value = conf.seed;
        minTailleEtage.value = conf.minTailleEtage;
        maxTailleEtage.value = conf.maxTailleEtage;
        nbStairs.value = conf.nbStairs;
        baseDifficulty.value = conf.baseDiff;
        difficultyScaling.value = conf.diffScaling;
        labelInfo.text = "";
    }

    /// <summary>
    /// Verifie si les donn�es entr�e sont correctes
    /// </summary>
    /// <returns>True si ok, false sinon</returns>
    private bool CheckSettings()
    {
        if (minTailleEtage.value.x == 0 || minTailleEtage.value.y == 0 || maxTailleEtage.value.x == 0 || maxTailleEtage.value.y == 0) //TODO : Voir si on peut mettre des valeurs min dans le truc
        {
            labelInfo.style.color = new(Color.red);
            labelInfo.text = "Tailles etages invalides, trop petites";
            return false;
        }

        if (minTailleEtage.value.x > maxTailleEtage.value.x || minTailleEtage.value.y > maxTailleEtage.value.y)
        {
            labelInfo.style.color = new(Color.red);
            labelInfo.text = "Taille etage min doit �tre inferieure a max";
            return false;
        }

        if (nbStairs.value > minTailleEtage.value.x || nbStairs.value > minTailleEtage.value.y)
        {
            labelInfo.style.color = new(Color.red);
            labelInfo.text = "Nb stairs trop grand";
            return false;
        }


        return true;
    }

}

[Serializable]
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

    public override string ToString()
    {
        return $"ConfigDonjon:\n" +
               $"- Nombre d'�tages: {nbEtages}\n" +
               $"- Seed: {seed}\n" +
               $"- Taille minimale d'un �tage: {minTailleEtage.x} x {minTailleEtage.y}\n" +
               $"- Taille maximale d'un �tage: {maxTailleEtage.x} x {maxTailleEtage.y}\n" +
               $"- Nombre d'escaliers: {nbStairs}\n" +
               $"- Type d'�tage: {typeEtage}\n" +
               $"- Difficult� de base: {baseDiff}\n" +
               $"- Scaling de difficult�: {diffScaling}";
    }

}

