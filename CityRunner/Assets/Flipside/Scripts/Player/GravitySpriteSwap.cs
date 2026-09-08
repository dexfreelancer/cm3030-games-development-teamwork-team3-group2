using UnityEngine;

namespace Flipside
{
    /// <summary>Shows one sprite under normal gravity and another while gravity is inverted.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class GravitySpriteSwap : MonoBehaviour
    {
        [SerializeField] Sprite normalSprite;
        [SerializeField] Sprite invertedSprite;
        [SerializeField] PlayerMotor motor;

        SpriteRenderer sr;
        int shownSign;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (motor == null) motor = GetComponentInParent<PlayerMotor>();
            if (normalSprite == null) normalSprite = sr.sprite;
        }

        void LateUpdate()
        {
            if (motor == null || invertedSprite == null) return;
            // Swap halfway through the flip so the colour change lands with the half turn.
            int sign = motor.IsFlipping && motor.FlipProgress < 0.5f ? -motor.GravitySign : motor.GravitySign;
            if (sign == shownSign) return;
            shownSign = sign;
            sr.sprite = sign == -1 ? invertedSprite : normalSprite;
        }
    }
}
