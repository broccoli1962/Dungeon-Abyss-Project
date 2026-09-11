using UnityEngine;

namespace DdaIT.Object.UI
{
    public class SingleGaugeBar : CommonGaugeBar
    {
        protected override void ApplyFill(float ratio)
        {
            _fill.fillAmount = ratio;
        }
    }
}
