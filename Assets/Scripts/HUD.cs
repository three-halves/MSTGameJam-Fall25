using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [SerializeField] private Image _hammerImage;
    [SerializeField] private Color[] hammerChargeColors;
    [SerializeField] private Animator _hammerAnimator;

    // public void UpdateHammer(bool charging, int chargeStageIndex)
    // {
    //     _hammerImage.color = hammerChargeColors[chargeStageIndex];
    //     _hammerAnimator.SetBool("isCharging", charging);
    // }
}