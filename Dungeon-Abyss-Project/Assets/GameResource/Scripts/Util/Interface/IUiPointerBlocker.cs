using UnityEngine;

namespace DdaIT.Util.Interface
{
    public interface IUiPointerBlocker
    {
        bool BlockPointer(Vector2 screenPos);
    }
}