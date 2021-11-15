using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Configs;
using Data;
using Game.CrosswordPanel;
using Game.CrosswordPanel.Letter;
using GeniusPoints;
using Managers;
using Newtonsoft.Json;
using Player;
using Scripts.Game.CrosswordPanel.AnimationEffects.BoostersAnimations;
using Scripts.Game.CrosswordPanel.Letter;
using TMPro;
using UI.Popups.WinLevelPopup;
using Unibit.TournamentsModule;
using UnityEngine;
using UnityEngine.UI;
using WordsLevelInfo;
using Zenject;
using Random = UnityEngine.Random;

namespace Scripts.Game.CrosswordPanel
{
    public class CrosswordManager : MonoBehaviour
    {
        [SerializeField] private CrosswordPanelController _panelController;
        [SerializeField] private List<LevelData> _levels = new List<LevelData>();
        [SerializeField] private MagicWandVisualization _wandVisualization;
        [SerializeField] private RectTransform _gridSize;
        [SerializeField] private BonusWords _bonusWords;
        [SerializeField] private CrosswordPanelView _view;
        [SerializeField] private GameObject _wheelGameObject;
        [SerializeField] private GameObject _boggleGameObject;
        [SerializeField] private List<GameObject> _boggleAreas;
        public LayoutElement letterBoxElementor;
        public Transform GetTargetToBinocularMove() => _view.GetBinocularsHandle();
        
        public event Action OnCompleteInitLevel;
        public event Action OnCompleteLevel;
        public event Action<WordData> OnCompleteWord;
        public event Action<bool> OnBlockScreenPanel;
        public event Action<LetterModelData> OnLetterOpen;
        public event Action OnLoadedSavedWordsEvent;

        private LevelData _currentLevel;
        private int _currentLevelNumber = 1;
        private OpenLetterSaveController _openLetterSaveController;
        
        private int maxBinocularsCount = 2;
        public int MaxBinocularsCount => maxBinocularsCount;

        private int _binocularCounter;
        public int CurrentBinocularCount => _binocularCounter;

        private const int MinLevelToEnableGeoPushes = 7;

        [Inject] private ApplovinManager ApplovinManager { get; }
        [Inject] private AdsManager AdsManager { get; }
        [Inject] private PlayerController PlayerController { get; }

        [Inject] private Client ClientGRPC { get; }
        [Inject] private LevelsConfig LevelsConfig { get; }

        private string _targetWordForTutorial = string.Empty;

        public LevelData CurrentLevelData => _currentLevel;
        public int GetLastLevel() => _levels[_levels.Count - 1].level;

        private int _DEBUG_currentWordIndex;

        public WordData _currentLevelExtraWord { get; private set; }

        public TypeCrossword TypeCrossword = TypeCrossword.Wheel;

        /// <summary>
        /// Send string.empty to make crossword work as usual
        /// </summary>
        /// <param name="word"></param>
        public void SetTargetWordForTutorial(string word)
        {
            _targetWordForTutorial = word;
        }

		private void Update()
        {
            if (Input.GetKey(KeyCode.W)) 
            {
                if (_DEBUG_currentWordIndex > _currentLevel.words.Count) { return; }

                var targetWord = _currentLevel.words[_DEBUG_currentWordIndex];
                OpenTargetWord(targetWord);
                _DEBUG_currentWordIndex++;
            }
        }

        private void Awake()
        {
            _levels = LevelsConfig.LevelsInfo;
            var levelNumber = PlayerController.GetMapLevel();
            var level = _levels[levelNumber - 1];
           
            if (level.boggle.Count != 0)
            {
                foreach (var area in _boggleAreas)
                {
                    area.SetActive(false);
                }
                
                _boggleGameObject.SetActive(true);
                _boggleAreas[(int)Math.Sqrt(level.boggle.Count) - 2].SetActive(true);
                
                _wheelGameObject.SetActive(false);
                TypeCrossword = TypeCrossword.Boggle;
            }
            else
            {
                _boggleGameObject.SetActive(false);
                _wheelGameObject.SetActive(true);
                TypeCrossword = TypeCrossword.Wheel;
            }

        }

        private void Start()
        {
            Debug.Log("LVL" + PlayerController.GetMaxGameLevel());
            
            _levels = LevelsConfig.LevelsInfo;

            _panelController.ClearPanel();
            _currentLevelNumber = PlayerController.GetMapLevel();

            try
            {
                ClientGRPC.GetMainWords(_currentLevelNumber, OnLoadedSavedWords, () => 
                {
                    InitLevel(_currentLevelNumber);
                    if(_currentLevelNumber != 1)
                    {
                        _openLetterSaveController = new OpenLetterSaveController(_currentLevelNumber);
                        if (_openLetterSaveController.LoadSavedOpenLetters(out var result))
                        {
                            foreach (var letterData in result)
                            {
                                OpenLetter(letterData);
                            }
                        }

                        OnCompleteLevel += DeleteSaveOpenLetters;
                    }
                });
            }
            catch
            {
                InitLevel(_currentLevelNumber);
                if(_currentLevelNumber != 1)
                {
                    _openLetterSaveController = new OpenLetterSaveController(_currentLevelNumber);
                    if (_openLetterSaveController.LoadSavedOpenLetters(out var result))
                    {
                        foreach (var letterData in result)
                        {
                            OpenLetter(letterData);
                        }
                    }

                    OnCompleteLevel += DeleteSaveOpenLetters;
                }
            }
            //GetLetters();
            //

            if (!PlayerController.GetIsAdDisable() && AdsManager.ShowAd)
            {
                ApplovinManager.ShowBanner();
            }

            if (PlayerPrefs.HasKey("TimeBetweenScenes"))
            {
                DateTime startTime = DateTime.Parse(PlayerPrefs.GetString("TimeBetweenScenes"));
                TimeSpan loadTime = DateTime.UtcNow - startTime;
                Debug.Log("Load seconds " + loadTime.Seconds + " Miliseconds " + loadTime.Milliseconds);
            }
        }

        private void OnDestroy()
        {
            PlayerPrefs.SetString("TimeBetweenScenes", DateTime.UtcNow.ToString());
        }

        private void OnLoadedSavedWords(ResponseGet savedWords)
        {
            InitLevel(_currentLevelNumber);
            if (_currentLevelNumber != 1)
            {
                foreach (var word in savedWords.Data)
                {
                    CheckSavedWord(word.Word.Word_, LetterState.OPEN_SAVED);
                }

                _openLetterSaveController = new OpenLetterSaveController(_currentLevelNumber);
                if (_openLetterSaveController.LoadSavedOpenLetters(out var result))
                {

                    foreach (var letterData in result)
                    {
                        OpenLetter(letterData);
                    }
                }

                OnCompleteLevel += DeleteSaveOpenLetters;
                OnLoadedSavedWordsEvent?.Invoke();
            }
        }

        public int CurrentLevel => _currentLevelNumber;

        public List<string> GetLetters()
        {
            return _currentLevel.GetLetters(_currentLevel);
            //return InfoWordLevels.GetLetters(_currentLevel);
        }

        public List<string> GetBoggleLetters()
        {
            var lvl = PlayerController.GetMapLevel();
            return _levels[lvl - 1].boggle.ToList();
        }
        

        public void InitLevel(int levelNumber)
        {
            var level = _levels[levelNumber - 1];//.FirstOrDefault(l => l.level == levelNumber);
            
            if (level != null)
            {
                _currentLevel = level.Clone() as LevelData;
                Debug.Log("max rows = " + _currentLevel.GetMaxRowCount());
                _panelController.InitLevel(_currentLevel, _currentLevel.GetMaxRowCount(), _currentLevel.GetMaxColumnCount(), _gridSize, letterBoxElementor);
            }

            //_panelController.gameObject.SetActive(true);
            PrepareExtraWord();

            OnCompleteInitLevel?.Invoke();            
        }
        private void PrepareExtraWord()
        {
            _currentLevelExtraWord = _panelController.FindExtraWord(_currentLevel);

            if (_currentLevelExtraWord != null)
            {
                _panelController.ActivateCoins(_currentLevelExtraWord);
            }
        }

        /**
         * return count letters in opened word (if opened many words in one time
         * - returned count all letters opened in one time)
         */
        public int CheckWord(string text, LetterState state = LetterState.OPEN)
        {
            if (!text.Equals(_targetWordForTutorial, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(_targetWordForTutorial)) { return 0; }

            var wordData = _currentLevel.CheckWord(text);
            int countLetters = 0;

            if (wordData != null)
            {
                _panelController.UpdateWordState(wordData, state);
                _currentLevel.ChangeLettersState(wordData, LetterState.OPEN);
                wordData.CheckCompleteWord();

                if (state != LetterState.OPEN_SAVED && wordData.IsOpened && wordData == _currentLevelExtraWord)
                {
                    //PlayerController.AddCoins(_currentLevelExtraWord.letters.Count, TypeBalance.PER_LEVEL);
                    GameObject centreLetter =
                        _panelController.GetLetterObject(
                            _currentLevelExtraWord.letters[_currentLevelExtraWord.letters.Count / 2]);
                    _bonusWords.Init(_currentLevelExtraWord.letters.Count, false);
                    _bonusWords.StartAnimationLvl(centreLetter.transform.position);
                }
                
                countLetters = wordData.letters.Count;
                countLetters += _currentLevel.CheckCompleteOtherWords();
                OnCompleteWord?.Invoke(wordData);
                if (CheckCompleteLevel())
                {
                    //_panelController.gameObject.SetActive(false);
                    OnCompleteLevel?.Invoke();
                }
            }

            return countLetters;
        }

        public int CheckSavedWord(string text, LetterState state = LetterState.OPEN)
        {
            if (!text.Equals(_targetWordForTutorial, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(_targetWordForTutorial)) { return 0; }

            var wordData = _currentLevel.CheckWord(text, true);
            int countLetters = 0;

            if (wordData != null)
            {
                _panelController.UpdateWordState(wordData, state);
                _currentLevel.ChangeLettersState(wordData, LetterState.OPEN);
                wordData.CheckCompleteWord();

                if (state != LetterState.OPEN_SAVED && wordData.IsOpened && wordData == _currentLevelExtraWord)
                {
                    PlayerController.AddCoins(_currentLevelExtraWord.letters.Count, TypeBalance.PER_LEVEL);
                }

                countLetters = wordData.letters.Count;
                countLetters += _currentLevel.CheckCompleteOtherWords();
                OnCompleteWord?.Invoke(wordData);
                if (CheckCompleteLevel())
                {
                    //_panelController.gameObject.SetActive(false);
                    OnCompleteLevel?.Invoke();
                }
            }

            return countLetters;
        }

        public bool ContainsInCurrentLevel(string text)
        {
            return _currentLevel.ContainsInCurrentLevel(text);
        }

        public bool IsOpenedWordInCurrentLevel(string text)
        {
            return _currentLevel.IsOpenedWordInCurrentLevel(text);
        }

        public void CompleteCurrentLevel()
        {
            foreach (var word in _currentLevel.words)
            {
                _panelController.UpdateWordState(word, LetterState.OPEN);
                _currentLevel.ChangeLettersState(word, LetterState.OPEN);
                word.IsOpened = true;
            }
            //_panelController.gameObject.SetActive(false);
            OnCompleteLevel?.Invoke();
        }

        public void OpenTargetWord(WordData word)
        {
            var targetWord = word.GetWord();

            if (word == null || (targetWord != _targetWordForTutorial && !string.IsNullOrEmpty(_targetWordForTutorial))) { return; }

            OnBlockScreenPanel?.Invoke(true);

            _wandVisualization.AnimationStart(() =>
            {
                _panelController.UpdateWordState(word, LetterState.OPEN_MAGIC_WAND);
                _currentLevel.ChangeLettersState(word, LetterState.OPEN);
                word.IsOpened = true;
                OnCompleteWord?.Invoke(word);
                Action action = () =>
                {
                    if (CheckCompleteLevel())
                    {
                        ClientGRPC.CheckMainWord(word.GetWord(), CurrentLevel, true, OnCheckedWordOnServerCallback);
                        //_panelController.gameObject.SetActive(false);
                        OnCompleteLevel?.Invoke();
                    }
                    else
                    {
                        ClientGRPC.CheckMainWord(word.GetWord(), CurrentLevel, false, OnCheckedWordOnServerCallback);
                    }

                };
                StartCoroutine(WaitDelay(1f, action));
            });
        }

        public void OpenRandomWord()
        {
            var word = _currentLevel.GetRandomWord();
            if (word != null)
            {
                OnBlockScreenPanel?.Invoke(true);
                
                _wandVisualization.AnimationStart(() =>
                {
                    _panelController.UpdateWordState(word, LetterState.OPEN_MAGIC_WAND);
                    _currentLevel.ChangeLettersState(word, LetterState.OPEN);
                    word.IsOpened = true;
                    OnCompleteWord?.Invoke(word);
                    Action action = () =>
                    {
                        if (CheckCompleteLevel())
                        {
                            ClientGRPC.CheckMainWord(word.GetWord(), CurrentLevel,true, OnCheckedWordOnServerCallback);
                            //_panelController.gameObject.SetActive(false);
                            OnCompleteLevel?.Invoke();
                        }
                        else
                        {
                            ClientGRPC.CheckMainWord(word.GetWord(), CurrentLevel,false, OnCheckedWordOnServerCallback);
                        }
                    
                    };
                    StartCoroutine(WaitDelay(1f, action));
                });
            }
        }

        private void OnCheckedWordOnServerCallback(ResponseCheckMain data)
        {

        }

        private IEnumerator WaitDelay(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            OnBlockScreenPanel?.Invoke(false);
            action?.Invoke();
        }

        public void InitNextLevel()
        {
            var levelNumber = PlayerController.GetMapLevel();
            var level = _levels[levelNumber - 1];
            
            if (level.boggle.Count != 0)
            {
                foreach (var area in _boggleAreas)
                {
                    area.SetActive(false);
                }
                _boggleGameObject.SetActive(true);
                _boggleAreas[(int)Math.Sqrt(level.boggle.Count) - 2].SetActive(true);
                
                _wheelGameObject.SetActive(false);
            }
            else
            {
                _boggleGameObject.SetActive(false);
                _wheelGameObject.SetActive(true);
            }
            
            if (_currentLevelNumber < _levels.Count)
            {
                _currentLevelNumber += 1;
            }
            else
            {
                _currentLevelNumber = 1;
            }
            InitLevel(_currentLevelNumber);
        }
        
        public void OpenRandomLetter(LetterState letterState = LetterState.OPEN_LIGHT_BULB)
        {
            var word = _currentLevel.GetRandomWord();
            if (word != null)
            {
                OnBlockScreenPanel?.Invoke(true);

                var closedLetters = word.letters.Where(x => x.state == LetterState.CLOSED).ToList();
                
                // LetterModelData letter = word.letters.FirstOrDefault(l => l.state == LetterState.CLOSED);

                LetterModelData letter = closedLetters.ElementAt(Random.Range(0, closedLetters.Count));

                if (word.letters.Count(q=>q.state == LetterState.OPEN) == (word.letters.Count-1))
                {
                    OnCompleteWord?.Invoke(word);
                    word.IsOpened = true;
                }

                WordData tempWord = new WordData();
                tempWord.letters = new List<LetterModelData>()
                {
                    letter
                };

                _panelController.UpdateWordState(tempWord, letterState);
                _currentLevel.ChangeLetterState(letter, LetterState.OPEN);

                Action action = () =>
                {
                    
                    if (word.IsOpened)
                    {
                        if (CheckCompleteLevel())
                        {
                            Debug.Log("Finish Level");
                            ClientGRPC.CheckMainWord(word.GetWord(), CurrentLevel,true, OnCheckedWordOnServerCallback);
                            OnCompleteLevel?.Invoke();
                        }
                        else
                        {
                            ClientGRPC.CheckMainWord(word.GetWord(), CurrentLevel,false, OnCheckedWordOnServerCallback);
                        }
                    }
                };

                StartCoroutine(WaitDelay(1f, action));
                

                if (letter != null)
                {
                    var saveLetterData = new LetterPositionData {column = letter.column, row = letter.row};
                    _openLetterSaveController.SaveLetter(saveLetterData);
                }
               
            }
        }

        public void OpenTargetLetter(LetterPositionData data, LetterState overrideEffect = LetterState.OPEN_TARGET)
        {
            LetterModelData letter = _currentLevel.GetTargetLetter(data, out var word);

            if (letter == null || word == null)
            {
                Debug.LogError($"[CrosswordManager] Letter with column: {data.column}, row {data.row} not found in this level");
                return;
            }
            
            OnBlockScreenPanel?.Invoke(true);

            if (word.letters.Count(q=>q.state == LetterState.OPEN) == (word.letters.Count-1))
            {
                word.IsOpened = true;
                OnCompleteWord?.Invoke(word);
            }
                
            WordData tempWord = new WordData();
            tempWord.letters = new List<LetterModelData>()
            {
                letter
            };
                
            _panelController.UpdateWordState(tempWord, overrideEffect);
            _currentLevel.ChangeLetterState(letter, LetterState.OPEN);
            
            _panelController.OnSelectTargetLetter(data);
            
            Action action = () =>
            {
                    
                if (word.IsOpened)
                {
                    if (CheckCompleteLevel())
                    {
                        ClientGRPC.CheckMainWord(word.GetWord(), CurrentLevel,true, OnCheckedWordOnServerCallback);
                        OnCompleteLevel?.Invoke();
                    }
                    else
                    {
                        ClientGRPC.CheckMainWord(word.GetWord(), CurrentLevel,false, OnCheckedWordOnServerCallback);
                    }
                }
            };

            StartCoroutine(WaitDelay(1f, action));

            OnLetterOpen?.Invoke(letter);

            var saveLetterData = new LetterPositionData {column = letter.column, row = letter.row};
            _openLetterSaveController.SaveLetter(saveLetterData);
        }

        public void OpenLetter(LetterPositionData data)
        {
            if (_currentLevel.GetLetter(data, out var letter))
            {
                WordData tempWord = new WordData();
                tempWord.letters = new List<LetterModelData>()
                {
                    letter
                };
                
                _panelController.UpdateWordState(tempWord, LetterState.OPEN_SAVED);
                _currentLevel.ChangeLetterState(letter, LetterState.OPEN);
            }
        }

        public int GetMaxLettersCount()
        {
            return _currentLevel.MaxWordLetters();
        }

        public int GetMinLettersCount()
        {
            return _currentLevel.MinWordLetters();
        }

        public int GetMaxLevelNumber()
        {
            return _levels.Max(item => item.level);
        }

        public bool CheckCompleteLevel()
        {
            return _currentLevel.words.FirstOrDefault(w => w.IsOpened == false) == null;
        }

        //public bool CheckCompleteLevelByWords()
        //{
        //    foreach (var w in _currentLevel.words)
        //    {
        //        Debug.Log(w.GetWord());
        //        Debug.Log(w.IsOpened);
        //    }
        //    Debug.Log(_currentLevel.words.FirstOrDefault(w => w.IsOpened == false) == null);
        //    return _currentLevel.words.Where(w => w);
        //}

        public List<LetterPositionData> GetLettersPosition(string text)
        {
            return _currentLevel.GetLettersPosition(text);
        }

        public LevelData GetLevelDataByWordId(int id)
        {
            return _levels[id];
        }

        private void DeleteSaveOpenLetters()
        {
            OnCompleteLevel -= DeleteSaveOpenLetters;
            _openLetterSaveController.DeleteSavedLetters();
        }

        public List<GameObject> GetExtraWordLetterObjects()
        {
            if (_currentLevelExtraWord != null)
                return _panelController.GetWordLetterObjects(_currentLevelExtraWord);
                        
            return null;            
        }
        public List<GameObject> GetEmptyLetterObjects()
        {
            List<GameObject> emptyLetters = new List<GameObject>();

            foreach(WordData word in _currentLevel.words)
            {
                foreach(LetterModelData letter in word.letters)
                {
                    if (letter.state == LetterState.CLOSED)
                    {
                        GameObject letterObject = _panelController.GetLetterObject(letter);
                        if(!emptyLetters.Contains(letterObject))
                            emptyLetters.Add(letterObject);
                    }
                }
            }
            return emptyLetters;
        }
        
        public void SetBinocularsCount(int value)
        {
            _binocularCounter = value;
        }

        public void AddBunocular()
        {
            _binocularCounter++;
        }
    }
}

public enum TypeCrossword
{
    Wheel,
    Boggle
}