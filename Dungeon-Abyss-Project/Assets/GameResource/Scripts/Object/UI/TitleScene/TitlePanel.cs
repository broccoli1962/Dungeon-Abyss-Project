using DdaIT.Object.UI;
using UnityEngine;

namespace DdaIT
{
    public class TitlePanel : UIPanel<TitlePanelPresenter>
    {
        [SerializeField] private CommonButton _gameStartButton;
        [SerializeField] private CommonButton _gameOptionButton;
        [SerializeField] private CommonButton _gameExitButton;

        public CommonButton GameStartButton { get => _gameStartButton; }
        public CommonButton GameOptionButton { get => _gameOptionButton; }
        public CommonButton GameExitButton { get => _gameExitButton; }
    }
}
