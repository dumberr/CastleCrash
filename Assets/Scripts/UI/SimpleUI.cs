using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleUI : MonoBehaviour
{
    public CatapultAiming catapult;
    public LimitedProjectiles ammo;
    public Slider powerSlider;
    public TMP_Text ammoText;

    void Update()
    {
        if (catapult != null && powerSlider != null)
            powerSlider.value = catapult.NormalizedPower;

        if (ammo != null && ammoText != null)
            ammoText.text = "Proiectile: " + ammo.RemainingProjectiles;
    }
}