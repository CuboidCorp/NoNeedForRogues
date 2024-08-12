using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Classe de base pr les entites pr absraction des données commune
/// </summary>
public abstract class Entity : NetworkBehaviour
{
    [Header("Entity Stats")]

    public int MaxHP = 20;
    [HideInInspector] public float currentHealth;

    public int MaxMana = 20;
    [HideInInspector] public float currentMana;

    public float poids = 20f;

    [Header("PlayerUI")]
    public Slider healthSlider;
    public Slider manaSlider;
    public TMP_Text healthText;
    public TMP_Text manaText;

    public void IntiliazeUi()
    {
        currentHealth = MaxHP;
        currentMana = MaxMana;
        healthSlider.maxValue = MaxHP;
        healthSlider.value = currentHealth;
        healthText.text = currentHealth + "/" + MaxHP;
        manaSlider.maxValue = MaxMana;
        manaSlider.value = currentMana;
        manaText.text = currentMana + "/" + MaxMana;
    }

    public virtual void Damage(float damage)
    {
        currentHealth -= damage;
        healthSlider.value = currentHealth;
        healthText.text = currentHealth + "/" + MaxHP;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void Heal(float heal)
    {
        currentHealth += heal;
        if (currentHealth > MaxHP)
        {
            currentHealth = MaxHP;
        }
        healthSlider.value = currentHealth;
        healthText.text = currentHealth + "/" + MaxHP;
    }

    public void FullHeal()
    {
        currentHealth = MaxHP;
        healthSlider.value = currentHealth;
        healthText.text = currentHealth + "/" + MaxHP;
    }

    protected abstract void Die();

    public virtual void UseMana(float mana)
    {
        currentMana -= mana;
        if (currentMana < 0)
        {
            currentMana = 0;
        }
        manaSlider.value = currentMana;
        manaText.text = currentMana + "/" + MaxMana;
    }

    public virtual void GainMana(float mana)
    {
        currentMana += mana;
        if (currentMana > MaxMana)
        {
            currentMana = MaxMana;
        }
        manaSlider.value = currentMana;
        manaText.text = currentMana + "/" + MaxMana;
    }

    public void FullMana()
    {
        currentMana = MaxMana;
        manaSlider.value = currentMana;
        manaText.text = currentMana + "/" + MaxMana;
    }


}
