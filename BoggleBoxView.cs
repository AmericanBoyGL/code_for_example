using System;
using System.Collections.Generic;
using System.Linq;
using Configs;
using Data;
using Game.BottomPanel.Wheel.LetterItem;
using Player;
using Signals;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Zenject;
using DG.Tweening;

namespace Game.BottomPanel.Wheel
{
    public class BoggleBoxView:MonoBehaviour
    {
        private const float TOUCH_ZONE_RATIO = .75f;
        public ICollection<LetterItemController> GetLetters => _itemViews;

        [SerializeField] private RectTransform _circleImg;
        [SerializeField] private LetterItemController _letterItemPref;
        [SerializeField] private Transform _horizontalLetterContainer;
        [SerializeField] private LayoutElement _areaForLetters;
        [SerializeField] private GridLayoutGroup _cellSizeForLetters;
        [SerializeField] private GameObject _lettersAreaForRotation;
        [SerializeField] public List<Image> _cellActiveForLettersTwo;
        [SerializeField] public List<Image> _cellActiveForLettersThree;
        [SerializeField] public List<Image> _cellActiveForLettersFour;
        
        private List<LevelData> _levels = new List<LevelData>();
        public float normalizedPositionItem = 0f; 
        public float itemNormalizedDistanceFromCenter = 0f;
        private BoggleBoxModel _model;
        private LevelData level;

        private bool isInvoke;
        
        private List<LetterItemController> _itemViews = new List<LetterItemController>();

        [Inject] private PlayerController PlayerController { get; }
        [Inject] private BackgroundSettings BackgroundSettings { get; }
        [Inject] private SignalBus SignalBus { get; }
        [Inject] private LevelsConfig LevelsConfig { get; }

        public void Init(BoggleBoxModel model)
        {
            _model = model;
            isInvoke = false;
            _model.Letters.ObserveCountChanged(true).Subscribe(OnChangeLettersCount);
            SignalBus.Subscribe<RemoveLetterFromWheelSignal>(OnRemoveLetterFromWheel);
            SignalBus.Subscribe<ChangeOrderLettersOnWheelSignal>(ChangeOrderLetters);
            SignalBus.Subscribe<ShakeLableAnimationSignal>(WordNotCorrect);
        }

        private void ChangeOrderLetters(ChangeOrderLettersOnWheelSignal obj)
        {
            List<LetterItemController> _letters = new List<LetterItemController>();
            _letters.AddRange(_itemViews);
            
            foreach (var letter in _letters)
            {
                if (DOTween.IsTweening(letter.transform)) continue;
                
                letter.transform.DORotate(letter.transform.eulerAngles + new Vector3(0, 0, 0), 0.5f);
            }
            
            if (DOTween.IsTweening(_lettersAreaForRotation.transform)) return;
            
            _lettersAreaForRotation.transform.DORotate(_lettersAreaForRotation.transform.eulerAngles + new Vector3(0, 0, -90), 0.5f);
            
            //Tween tween = _lettersAreaForRotation.transform.DORotate(_lettersAreaForRotation.transform.eulerAngles + new Vector3(0, 0, -90), 0.5f);
            //tween.Play();
        }

        private void OnRemoveLetterFromWheel(RemoveLetterFromWheelSignal value)
        {
            Vector2 itemPosition;
            foreach (var item in _itemViews)
            {
                itemPosition = item.GetLocalPosition();
                if (itemPosition.x == value.position.x && itemPosition.y == value.position.y)
                {
                    item.Reset();
                    item.ResetFirstSelected();
                }
            }
        }


        private void OnChangeLettersCount(int count)
        {
            if (_itemViews.Count > 0)
            {
                foreach (var item in _itemViews)
                {
                    if (item != null)
                    {
                        item.OnClickLetter -= CurrentViewOnClickLetter;
                        Destroy(item.gameObject);
                    }
                }
            }
            _itemViews.Clear();
            _levels = LevelsConfig.LevelsInfo;
            var levelNumber = PlayerController.GetMapLevel();
            level = _levels[levelNumber - 1];
            
            if(level.boggle.Count == 4)
                PlaceSectorsOnWheel(_cellActiveForLettersTwo);
            
            if(level.boggle.Count == 9)
                PlaceSectorsOnWheel(_cellActiveForLettersThree);
            
            if(level.boggle.Count == 16)
                PlaceSectorsOnWheel(_cellActiveForLettersFour);
        }

        private void PlaceSectorsOnWheel(List<Image> cellActiveForLetters)
        {
            var dimension = GetSectorItemDimension();

            var countLetters = (_model.Letters.Count > 0) ? _model.Letters.Count : 1;
            var _angleStep = 360 / countLetters;
            
            var selectedCeilColor = BackgroundSettings.GetCeilColorForLevel(PlayerController.GetPlayerModel().Level.Value);

            var index = 0;
            
            
            for (int i = 0; i < _model.Letters.Count; i++)
            {
                var currentView = Instantiate<LetterItemController>(_letterItemPref, _horizontalLetterContainer);
                var currentAngleStep = _angleStep * i;

                var currentRect = currentView.GetComponent<RectTransform>();
                currentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, dimension);
                currentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, dimension);
                var currentTouchZone = currentView.GetTouchZoneRect();
                currentTouchZone.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, dimension*TOUCH_ZONE_RATIO);
                currentTouchZone.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, dimension*TOUCH_ZONE_RATIO);
                //currentView.GetComponent<RectTransform>().pivot = pivot;
            
            
                currentRect.rotation = Quaternion.Euler(0f, 0f, 0);
                currentView.Init(_model.Letters[i], currentAngleStep, selectedCeilColor, cellActiveForLetters[i]);
                currentView.OnClickLetter += CurrentViewOnClickLetter;
                currentView.OnUpLetter += CurrentViewOnUpLetter;
                currentView.OnPosition += CurrentViewOnMousePosition;
                currentView.OnExitPosition += CurrentViewOnExitMousePosition;
                currentView.OnSelected += CurrentViewOnSelected;
            
                var space = 0.65f;
                if (_model.Letters.Count >= 6)
                    space = 0.7f;
            
                currentView.gameObject.transform.localPosition = RandomCircle(currentView.gameObject.transform.localPosition, _circleImg.rect.width / 2 * space, currentAngleStep);//new Vector3(0, _circleImg.rect.width / 2*0.8f, 0);
                _itemViews.Add(currentView);

                // currentView.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            }
        }

        private void CurrentViewOnSelected(LetterSelectedItemData value)
        {
            if (value.isSelected)
            {
                _model.IsSelected.SetValueAndForceNotify(_itemViews.Count(l=>l.IsSelected().isSelected) >= 1);
                _model.CheckRemoveLetter(value.position);
            }
        }

        private void CurrentViewOnExitMousePosition(Vector2 pos)
        {
            _model.exitPosition.Value = pos;
        }

        private void CurrentViewOnMousePosition(Vector2 pos)
        {
            _model.AddPosition(pos);
        }
        Vector3 RandomCircle(Vector3 center, float radius, float _angleStep)
        {
            float ang = _angleStep;//UnityEngine.Random.Range(0f,1f) * 360;
            Vector3 pos;
            pos.x = center.x + radius * Mathf.Sin(ang * Mathf.Deg2Rad);
            pos.y = center.y + radius * Mathf.Cos(ang * Mathf.Deg2Rad);
            pos.z = 0;
            return pos;
        }
        
        private void CurrentViewOnUpLetter(string letter)
        {
            if (!isInvoke)
            {
                isInvoke = true;
                _model.OnCheckItemUp?.Invoke();
            }
            
            // _model.ClearInputLetters();
            //_model.ClearCoordinates();
            //_itemViews.ForEach(t => t.UpdateAvailableInput(true));
            //_itemViews.ForEach( v=> v.Reset());
        }

        private void WordNotCorrect(ShakeLableAnimationSignal signal)
        {
            isInvoke = false;
            _model.ClearInputLetters();
            _model.ClearCoordinates();
            _itemViews.ForEach(t => t.UpdateAvailableInput(true));
            _itemViews.ForEach( v=> v.Reset());
        }

        private void CurrentViewOnClickLetter(string letter)
        {
            _model.AddInputLetter(letter);
            _itemViews.ForEach(t => t.UpdateAvailableInput(true));
            _itemViews.ForEach(t => t.SetFirstSelect(false));
        }

        private Vector2 GetSectorItemPivot()
        {
            var sectorItemDimension = GetSectorItemDimension();
            var wheelRadius = _circleImg.rect.width / 2;
            var wheelCenter = _circleImg.rect.width - wheelRadius;
            var sectorItemPosition = _circleImg.rect.width - wheelRadius * (1 - itemNormalizedDistanceFromCenter);
            var distanceFromItemToCenter = sectorItemPosition - wheelCenter - sectorItemDimension / 2;

            var normalizedDistanceFromItemToCenter = distanceFromItemToCenter / sectorItemDimension;

            var pivotX = 0.5f;
            var pivotY = -normalizedDistanceFromItemToCenter;

            return new Vector2(pivotX, pivotY);
        }
        
        private float GetSectorItemDimension()
        {
            if (_model.Letters.Count >= 6) normalizedPositionItem = 0.4f;
                    
            var wheelRadius = _circleImg.rect.width / 2;
            var iconDimension = wheelRadius * normalizedPositionItem;

            return iconDimension;
        }


        public void ResetLettersInWheel()
        {
            _itemViews.ForEach( v=> v.Reset());
            _itemViews.ForEach(t => t.UpdateAvailableInput(true));
            
            _itemViews.ForEach(t => t.SetFirstSelect(true));
        }
    }
}