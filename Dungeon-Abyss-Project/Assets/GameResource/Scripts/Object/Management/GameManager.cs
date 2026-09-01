
using Backend.Util.Management;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Backend.Object.Management
{
    public class GameManager : SingletonGameObject<GameManager>
    {
        private readonly ReactiveProperty<GameState> _state = new(GameState.Ready);
        public static Observable<GameState> OnStateChanged => Instance._state;
        public static GameState CurrentState => Instance._state.Value;

        protected override void OnAwake()
        {
            base.OnAwake();

            Application.targetFrameRate = 60;
        }

        private async UniTask InitializeCore_Internal()
        {
            await AudioManager.InitMixer();
            //TableManager.Init();
        }

        private void StartGameplay_Internal()
        {

            _state.Value = GameState.Playing;
        }

        private void EndGameplay_Internal()
        {

            _state.Value = GameState.Ready;
        }

        private void GameOver_Internal()
        {
            _state.Value = GameState.GameOver;
        }

        private void StageClear_Internal()
        {
            _state.Value = GameState.Clear;
        }


        #region Static Public Methods
        public static void EndGameplay() => Instance.EndGameplay_Internal();
        public static void GameOver() => Instance.GameOver_Internal();
        public static void StageClear() => Instance.StageClear_Internal();
        public static void StartGameplay() => Instance.StartGameplay_Internal();
        public static UniTask InitializeCore() => Instance.InitializeCore_Internal();
        #endregion
    }
}
