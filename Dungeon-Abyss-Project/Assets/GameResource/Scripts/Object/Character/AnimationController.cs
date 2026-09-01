using UnityEngine;

namespace Backend.Object.Character
{
    [RequireComponent(typeof(Animator))]
    public class AnimationController : MonoBehaviour
    {
        protected Animator _animator;

        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void SetAnimationTrigger(int hashCode)
        {
            _animator.SetTrigger(hashCode);
        }

        public void SetAnimationFloat(int hashCode, float value)
        {
            _animator.SetFloat(hashCode, value);
        }

        public void SetAnimationFloat(int hashCode, float value, float dampTime, float deltaTime)
        {
            _animator.SetFloat(hashCode, value, dampTime, deltaTime);
        }

        public void SetAnimationInt(int hashCode, int value)
        {
            _animator.SetInteger(hashCode, value);
        }

        public void SetAnimationBool(int hashCode, bool value)
        {
            _animator.SetBool(hashCode, value);
        }
    }
}