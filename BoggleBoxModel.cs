using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.BottomPanel.Wheel
{
    public class BoggleBoxModel
    {
        private ReactiveCollection<string> _letters;
        private ReactiveCollection<Vector2> _lettersCoordinates;
        private ReactiveCollection<string> _inputedLetters;
        public ReactiveProperty<Vector2> exitPosition;
        public ReactiveProperty<bool> IsSelected;

        public Action OnCheckItemUp;
        public Action<Vector2> OnCheckRemoveLetter;
        public IReadOnlyReactiveCollection<string> Letters => _letters;
        public IReadOnlyReactiveCollection<string> InputedLetters => _inputedLetters;
        public IReadOnlyReactiveCollection<Vector2> LettersCoordinates => _lettersCoordinates;

        public void AddLetter(string value)
        {
            _letters.Add(value);
        }

        public void ClearLetters()
        {
            _letters.Clear();
        }

        public void ReorderLetters()
        {
            for (int i = _letters.Count - 1; i >= 1; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = _letters[j];
                _letters[j] = _letters[i];
                _letters[i] = temp;
            }
        }

        public void AddPosition(Vector2 pos)
        {
            // Debug.Log("Add positions: " + pos.ToString());
            if (!_lettersCoordinates.Contains(pos))
            {
                _lettersCoordinates.Add(pos);
            }
        }

        public void ClearCoordinates()
        {
            _lettersCoordinates.Clear();
        }

        public void AddInputLetter(string value)
        {
            _inputedLetters.Add(value);
        }

        public void ClearInputLetters()
        {
            _inputedLetters.Clear();
        }

        public void RemoveLastLetter()
        {
            if (_inputedLetters.Count > 0)
            {
                _inputedLetters.Remove(_inputedLetters.Last());
            }

            if (_lettersCoordinates.Count > 0)
            {
                _lettersCoordinates.Remove(_lettersCoordinates.Last());
            }
        }

        public BoggleBoxModel()
        {
            _letters = new ReactiveCollection<string>();
            _inputedLetters = new ReactiveCollection<string>();
            _lettersCoordinates = new ReactiveCollection<Vector2>(new List<Vector2>());
            exitPosition = new ReactiveProperty<Vector2>();
            IsSelected = new ReactiveProperty<bool>(false);
        }

        public void CheckRemoveLetter(Vector2 valuePosition)
        {
            OnCheckRemoveLetter?.Invoke(valuePosition);
        }
    }
}