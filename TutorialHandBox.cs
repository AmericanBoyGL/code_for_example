using Configs;
using Data;
using DG.Tweening;
using Game.BottomPanel.LeftButtonsPanel;
using Game.BottomPanel.Wheel;
using Scripts.Game.BottomPanel.RightButtonsPanel;
using Scripts.Game.CrosswordPanel;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using TMPro;

namespace Scripts.Game
{
    public class TutorialHandBox : TutorialObject
    {
        public override int TargetLevel => _targetLevel;
        public override int Step => _tutorialStep;

        [Header("Tutorial settings")]
        [SerializeField]
        private int _tutorialStep;
        [SerializeField]
        private int _targetLevel;

        [SerializeField]
        private int _wordId;

        [Header("Tutorial game objects")]
        [SerializeField]
        private RightButtonsPanelView _rightButtonsPanelView;
        [SerializeField]
        private LeftButtonsPanelView _leftPanelButtonsView;

        [Space]
        [SerializeField] private Image _vignette;
        [SerializeField] private Animator _animatorHand;
        [SerializeField] private GameObject _gameObjectTutorialPart;
        [SerializeField] private float _alphaValue;
        [SerializeField] private RectTransform _rectTransformHand;
        [SerializeField] private float _durationShow = 0.6f;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Canvas _wheelPanel;
        [SerializeField] private BoggleBoxController _boxController;
        [SerializeField] private RectTransform _tutorialPartTr;
        [SerializeField] private RectTransform _bottomPanelTr;
        [SerializeField] private CanvasGroup _helpCanvasGroup;
        [SerializeField] private TextMeshProUGUI tutorHelpText;

        [Inject] private AnimationSettings _animationSettings;
        [Inject] private CrosswordManager _crosswordManager;
        private string _targetWord;
        private static readonly int StartAnimation = Animator.StringToHash("StartAnimation");

        private void Start()
        {
            _crosswordManager.OnCompleteWord += OnCompleteTargetWord;
        }

        protected override void OnStartTutorial()
        {            
            _rightButtonsPanelView.SetButtonsState(false);
            _leftPanelButtonsView.SetButtonsState(false);
            _targetWord = _crosswordManager.GetLevelDataByWordId(_targetLevel).words[_wordId].GetWord();
            tutorHelpText.text = "Swipe to form the word <color=#093ac6>" + _targetWord.ToUpper() + "</color>";
            _crosswordManager.SetTargetWordForTutorial(_targetWord);

            var color = _vignette.color;
            color.a = 0;
            _vignette.color = color;
            _helpCanvasGroup.alpha = 0;

            _rectTransformHand.localScale = Vector3.zero;
            
            _gameObjectTutorialPart.SetActive(true);
            _boxController.OnChangeInputLetters += OnChangeInputLetters;
            _wheelPanel.overrideSorting = true;
            _wheelPanel.sortingOrder = 4;

            _helpCanvasGroup.DOFade(1f, _durationShow);
            _vignette.DOFade(_alphaValue, _durationShow);
            _rectTransformHand.DOScale(Vector3.one, _durationShow)
                .SetEase(_animationSettings.CurvePopupShow)
                .OnComplete(
                    () =>
                    {
                        _animatorHand.SetTrigger(StartAnimation);
                    });
        }

        private void OnChangeInputLetters(string word)
        {
            if (word == "")
                return;

            _boxController.OnChangeInputLetters -= OnChangeInputLetters;
        }

        private void OnCompleteTargetWord(WordData word)
        {
            if (word.GetWord() == _targetWord) { EndTutorial(); }
        }

        private void EndTutorial()
        {
            if (Step == 1)
            {
                _wheelPanel.sortingOrder = 1;
                _rightButtonsPanelView.SetButtonsState(true);
                _leftPanelButtonsView.SetButtonsState(true);
                CrosswordManager.SetTargetWordForTutorial(string.Empty);
            }

            _crosswordManager.OnCompleteWord -= OnCompleteTargetWord;

            FireOnCompleteEvent();
            gameObject.SetActive(false);
        }
    }
}