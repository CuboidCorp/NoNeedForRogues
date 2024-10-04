using UnityEngine;

/// <summary>
/// Piege de pique qui drop du plafond quand activ�, se reset automatiquement ou quand desactiv�
/// </summary>
public class PiegePiqueDrop : Trap
{
    [SerializeField] private float speedDrop = 2f;
    [SerializeField] private float speedUp = 1f;

    [SerializeField] private Vector3 startPos;
    [SerializeField] private Vector3 endPos;

    [SerializeField] private float delayAtEnd = 1.0f;  // Temps d'attente avant la remont�e
    [SerializeField] private float currentDelay = 0.0f; // Temps �coul� depuis que le pi�ge a touch� le sol

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
            // D�place le pi�ge vers la targetPos (endPos ou startPos selon l'�tat)
            transform.position = Vector3.MoveTowards(transform.position, targetPos, GetCurrentSpeed() * Time.deltaTime);

            // Si le pi�ge atteint sa cible (targetPos)
            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
            {
                if (targetPos == endPos)
                {
                    // Si on a atteint la position au sol, on attend avant de remonter
                    currentDelay += Time.deltaTime;
                    if (currentDelay >= delayAtEnd)
                    {
                        // D�but de la remont�e apr�s le d�lai
                        targetPos = startPos;
                    }
                }
                else if (targetPos == startPos)
                {
                    // Si on a atteint la position de d�part, on d�sactive le pi�ge
                    isActive = false;
                    dmgZone.isActivated = false;
                }
            }
        }
    }

    /// <summary>
    /// Renvoie la vitesse actuelle en fonction de la cible.
    /// Si le pi�ge descend, on utilise speedDrop. S'il remonte, speedUp.
    /// </summary>
    private float GetCurrentSpeed()
    {
        return (targetPos == endPos) ? speedDrop : speedUp;
    }

    /// <summary>
    /// Configure la position de d�part, position de fin et les vitesses
    /// </summary>
    /// <param name="startPos">Position au plafond</param>
    /// <param name="endPos">Position au sol</param>
    /// <param name="speedDrop">Vitesse de chute</param>
    /// <param name="speedUp">Vitesse de remont�e</param>
    /// <param name="damage">D�g�ts inflig�s par le pi�ge</param>
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
