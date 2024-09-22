using UnityEngine;

/// <summary>
/// Piege de pique qui drop du plafond quand activé, se reset automatiquement ou quand desactivé
/// </summary>
public class PiegePiqueDrop : Trap
{
    [SerializeField] private float speedDrop = 2f;
    [SerializeField] private float speedUp = 1f;

    [SerializeField] private Vector3 startPos;
    [SerializeField] private Vector3 endPos;

    [SerializeField] private float delayAtEnd = 1.0f;  // Temps d'attente avant la remontée
    [SerializeField] private float currentDelay = 0.0f; // Temps écoulé depuis que le piège a touché le sol

    private Vector3 targetPos;
    private bool isActive = false;
    private DamageZone dmgZone;

    private void Awake()
    {
        dmgZone = GetComponentInChildren<DamageZone>();
        dmgZone.isActivated = false;
    }

    private void Update()
    {
        if (isActive)
        {
            // Déplace le piège vers la targetPos (endPos ou startPos selon l'état)
            transform.position = Vector3.MoveTowards(transform.position, targetPos, GetCurrentSpeed() * Time.deltaTime);

            // Si le piège atteint sa cible (targetPos)
            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
            {
                if (targetPos == endPos)
                {
                    // Si on a atteint la position au sol, on attend avant de remonter
                    currentDelay += Time.deltaTime;
                    if (currentDelay >= delayAtEnd)
                    {
                        // Début de la remontée après le délai
                        targetPos = startPos;
                    }
                }
                else if (targetPos == startPos)
                {
                    // Si on a atteint la position de départ, on désactive le piège
                    isActive = false;
                    dmgZone.isActivated = false;
                }
            }
        }
    }

    /// <summary>
    /// Renvoie la vitesse actuelle en fonction de la cible.
    /// Si le piège descend, on utilise speedDrop. S'il remonte, speedUp.
    /// </summary>
    private float GetCurrentSpeed()
    {
        return (targetPos == endPos) ? speedDrop : speedUp;
    }

    /// <summary>
    /// Configure la position de départ, position de fin et les vitesses
    /// </summary>
    /// <param name="startPos">Position au plafond</param>
    /// <param name="endPos">Position au sol</param>
    /// <param name="speedDrop">Vitesse de chute</param>
    /// <param name="speedUp">Vitesse de remontée</param>
    /// <param name="damage">Dégâts infligés par le piège</param>
    public void Setup(Vector3 startPos, Vector3 endPos, float speedDrop, float speedUp, float damage)
    {
        this.startPos = startPos;
        this.endPos = endPos;
        this.speedDrop = speedDrop;
        this.speedUp = speedUp;
        transform.position = startPos;
        targetPos = startPos;
        dmgZone.damage = damage;
    }

    public override void ActivateTrap()
    {
        if (!isActive)
        {
            currentDelay = 0.0f;
            isActive = true;
            targetPos = endPos;
            dmgZone.isActivated = true;
        }
    }

    public override void DeactivateTrap()
    {
        isActive = true;
        targetPos = startPos;
    }
}
