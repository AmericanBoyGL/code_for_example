using System.Collections;
using Managers;
using UnityEngine;
using Zenject;

namespace Scripts.UI.AnimationUI.FlyItems.FlyCoins
{
    public class FlyCoinsAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FlyCoinAnimation _flyCoinPrefab;
        [SerializeField] private Transform _startPointDefault;
        
        [SerializeField] private float _delay;
        [SerializeField] private float _duration;
        [SerializeField] private int _countCoinInAnimation;

        [Inject] private MusicManager _musicManager;
        
        private Vector3 _startPosition;
        private Transform _endPoint;
        
        public float AnimationStart(Vector3 startPositionWordCoord, int multiplier)
        {
            if (!gameObject.activeInHierarchy)
                return 0;
            if (_startPointDefault == null)
            {
                Debug.LogWarning("Start point is not assigned");
                return 0;
            }

            _startPosition = _startPointDefault.position; // startPositionWordCoord == Vector3.zero ? _startPointDefault.position : Camera.main.WorldToScreenPoint(startPositionWordCoord);
            _endPoint = transform;
            var currentCountCoins = _countCoinInAnimation * multiplier;

            StartCoroutine(SpawnCoins(currentCountCoins));
            var durationAnimation = currentCountCoins * _delay + _duration; 
            
            _musicManager.PlaySound(SoundsNames.CongratulationCoins);
            StartCoroutine(StopSoundAfterAnimation(durationAnimation));
            
            return durationAnimation;
        }

        private IEnumerator SpawnCoins(int count)
        {
            while (count > 0)
            {
                var coin = Instantiate(_flyCoinPrefab, _endPoint.position, Quaternion.identity, _endPoint);
                
                coin.Initial(_startPosition, _duration);

                count--;
                
                yield return new WaitForSeconds(_delay);
            }
            yield return null;
        }

        private IEnumerator StopSoundAfterAnimation(float delay)
        {
            yield return new WaitForSeconds(delay);
            _musicManager.StopSound();
        }

    }
}