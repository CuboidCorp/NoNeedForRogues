using System;
using UnityEngine;
using UnityEngine.UIElements;
using Donnees;
using Unity.Netcode;

public class ConfigDonjonUI : MonoBehaviour
{
    private UIDocument doc;
    private VisualElement root;

    [SerializeField] private float interactDistance = 10f;

    private SliderInt nbEtages;
    private IntegerField seed;
    private Vector2IntField minTailleEtage;
    private Vector2IntField maxTailleEtage;
    private SliderInt nbStairs;
    private SliderInt nbChaudrons;
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
        nbChaudrons = root.Q<SliderInt>("nbChaudrons");
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
            Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (!Physics.Raycast(cameraRay, out RaycastHit hit, interactDistance, LayerMask.GetMask("UI")))
            {
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
        cancelChanges.clicked += () => { ResetOptions(); MultiplayerGameManager.Instance.SyncConfigDonjonClientRpc(conf); };
    }

    private void OnDisable()
    {
        doc.panelSettings.SetScreenToPanelSpaceFunction(null);
        saveChanges.clicked -= () => SaveSettings();
        cancelChanges.clicked -= () => { ResetOptions(); MultiplayerGameManager.Instance.SyncConfigDonjonClientRpc(conf); };
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
                nbChaudrons = nbChaudrons.value,
                typeEtage = TypeEtage.Labyrinthe,
                baseDiff = baseDifficulty.value,
                diffScaling = difficultyScaling.value
            };
            labelInfo.style.color = new(Color.green);
            labelInfo.text = "Changements sauvegardés";
            MultiplayerGameManager.Instance.SyncConfigDonjonClientRpc(conf);
        }
    }

    /// <summary>
    /// Remet les options de base
    /// </summary>
    private void ResetOptions()
    {
        conf = new();
        //On charge les valeurs de base de conf dans les autres trucs
        SetConf(conf);
        labelInfo.text = "";
    }

    /// <summary>
    /// Set la configuration du menu des config ui
    /// </summary>
    /// <param name="config"></param>
    public void SetConf(ConfigDonjon config)
    {
        nbEtages.value = config.nbEtages;
        seed.value = config.seed;
        minTailleEtage.value = config.minTailleEtage;
        maxTailleEtage.value = config.maxTailleEtage;
        nbStairs.value = config.nbStairs;
        nbChaudrons.value = config.nbChaudrons;
        baseDifficulty.value = config.baseDiff;
        difficultyScaling.value = config.diffScaling;
    }

    /// <summary>
    /// Verifie si les données entrée sont correctes
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
            labelInfo.text = "Taille etage min doit être inferieure a max";
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
public class ConfigDonjon : INetworkSerializable
{
    public int nbEtages;
    public int seed;
    public Vector2Int minTailleEtage;
    public Vector2Int maxTailleEtage;
    public int nbStairs;
    public int nbChaudrons;
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
        nbChaudrons = 2;
        typeEtage = TypeEtage.Labyrinthe;
        baseDiff = 1;
        diffScaling = 1;
    }

    public override string ToString()
    {
        return $"ConfigDonjon:\n" +
               $"- Nombre d'étages: {nbEtages}\n" +
               $"- Seed: {seed}\n" +
               $"- Nombre de chaudrons: {nbChaudrons}\n" +
               $"- Taille minimale d'un étage: {minTailleEtage.x} x {minTailleEtage.y}\n" +
               $"- Taille maximale d'un étage: {maxTailleEtage.x} x {maxTailleEtage.y}\n" +
               $"- Nombre d'escaliers: {nbStairs}\n" +
               $"- Type d'étage: {typeEtage}\n" +
               $"- Difficulté de base: {baseDiff}\n" +
               $"- Scaling de difficulté: {diffScaling}";
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref nbEtages);
        serializer.SerializeValue(ref seed);
        serializer.SerializeValue(ref minTailleEtage);
        serializer.SerializeValue(ref maxTailleEtage);
        serializer.SerializeValue(ref nbStairs);
        serializer.SerializeValue(ref nbChaudrons);
        serializer.SerializeValue(ref typeEtage);
        serializer.SerializeValue(ref baseDiff);
        serializer.SerializeValue(ref diffScaling);
    }

}

