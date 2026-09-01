namespace Backend.Object.Character.Monster.Normal
{
    public class NormalAnimationController : AnimationController
    {
        private MonsterStatus _monsterStatus;
        protected override void Awake()
        {
            base.Awake();

            _monsterStatus = GetComponent<MonsterStatus>();
        }

        private void Update()
        {
        }
    }
}