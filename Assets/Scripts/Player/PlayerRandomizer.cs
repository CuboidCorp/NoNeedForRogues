using UnityEngine;

public class PlayerRandomizer : MonoBehaviour
{

    #region Transforms

    #region Non Symetric
    public Transform BeltTransform;
    public Transform CapeTransform;
    public Transform EyebrowTransform;
    public Transform FacialHairTransform;
    public Transform HairTransform;
    public Transform HeadTransform;
    public Transform TorsoTransform;
    public Transform LegTransform;

    #endregion

    #region Symetric
    public Transform PauldronTransform;
    public Transform KneeTransform;
    public Transform ElbowTransform;
    public Transform ArmTransform;
    public Transform CalfTransform;
    public Transform FootTransform;
    public Transform HandTransform;
    public Transform ForearmTransform;
    #endregion

    #endregion

    /// <summary>
    /// Supprime tous les enfants sauf un random pr un transform
    /// </summary>
    private void DeleteAllButRandom(Transform parent)
    {
        int nbChild = parent.childCount;
        if (nbChild > 1)
        {
            int randomIndex = Random.Range(0, nbChild);
            for (int i = 0; i < nbChild; i++)
            {
                if (i != randomIndex)
                {
                    Destroy(parent.GetChild(i).gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Randomize tous les enfants des transforms pour avoir un personnage unique
    /// </summary>
    public void Randomize(int seed)
    {
        Random.InitState(seed);
        //On supprime tt sauf un child sur les non symetriques soit Belt, cape, torso,leg,head, hair, facialhair, eyebrow
        DeleteAllButRandom(parent: BeltTransform);
        DeleteAllButRandom(parent: CapeTransform);
        DeleteAllButRandom(parent: TorsoTransform);
        DeleteAllButRandom(parent: LegTransform);
        DeleteAllButRandom(parent: HeadTransform);
        DeleteAllButRandom(parent: HairTransform);
        DeleteAllButRandom(parent: FacialHairTransform);
        DeleteAllButRandom(parent: EyebrowTransform);

        //Pr les symetriques on fait pareil mais pr leurs 2 enfants
        DeleteAllButRandom(parent: PauldronTransform.GetChild(0));
        DeleteAllButRandom(parent: PauldronTransform.GetChild(1));

        DeleteAllButRandom(parent: KneeTransform.GetChild(0));
        DeleteAllButRandom(parent: KneeTransform.GetChild(1));

        DeleteAllButRandom(parent: ElbowTransform.GetChild(0));
        DeleteAllButRandom(parent: ElbowTransform.GetChild(1));

        DeleteAllButRandom(parent: ArmTransform.GetChild(0));
        DeleteAllButRandom(parent: ArmTransform.GetChild(1));

        DeleteAllButRandom(parent: CalfTransform.GetChild(0));
        DeleteAllButRandom(parent: CalfTransform.GetChild(1));

        DeleteAllButRandom(parent: FootTransform.GetChild(0));
        DeleteAllButRandom(parent: FootTransform.GetChild(1));

        DeleteAllButRandom(parent: HandTransform.GetChild(0));
        DeleteAllButRandom(parent: HandTransform.GetChild(1));

        DeleteAllButRandom(parent: ForearmTransform.GetChild(0));
        DeleteAllButRandom(parent: ForearmTransform.GetChild(1));


        //On supprime le script
        Destroy(this);
    }
}
