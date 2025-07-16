using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Sockets.Audio
{
    [DefaultExecutionOrder(100)]
    public class SocketAudioController : MonoBehaviour
    {
        private const string _DEFAULT_PLACE_SOUND_ASSET_PATH = "Assets/Hypertonic Games/Ultimate Sockets/Audio/[UltimateSockets] Place Sound.wav";
        private const string _DEFAULT_REMOVE_SOUND_ASSET_PATH = "Assets/Hypertonic Games/Ultimate Sockets/Audio/[UltimateSockets] Remove Sound.wav";

        public bool UseAudio => _useAudio;
        public AudioSource AudioSource => _audioSource;


        [SerializeField]
        private Socket _socket;

        [SerializeField]
        private bool _useAudio = false;

        [SerializeField]
        private AudioSource _audioSource;

        [SerializeField]
        private AudioClip _placeAudioClip;

        [SerializeField]
        private AudioClip _removeAudioClip;


        #region  Unity Functions
        private void Awake()
        {
            if (!UseAudio)
                return;

            if (AudioSource == null)
            {
                Debug.LogErrorFormat(this, "Socket {0} has audio enabled but has no audio source.", _socket.name);
                return;
            }
        }

        private void OnEnable()
        {
            _socket.OnItemPlaced += HandleItemPlaced;
            _socket.OnItemRemoved += HandleItemRemoved;
            _socket.StackableItemController.OnItemAddedToCloneStack += HandleItemAddedToCloneStack;
            _socket.StackableItemController.OnItemAddedToInstanceStack += HandleItemAddedToInstanceStack;

        }

        private void OnDisable()
        {
            _socket.OnItemPlaced -= HandleItemPlaced;
            _socket.OnItemRemoved -= HandleItemRemoved;
            _socket.StackableItemController.OnItemAddedToCloneStack -= HandleItemAddedToCloneStack;
            _socket.StackableItemController.OnItemAddedToInstanceStack -= HandleItemAddedToInstanceStack;
        }

        #endregion Unity Functions

        #region Public Functions

        public void HandleUseAudioChanged()
        {
            if (UseAudio)
            {
                HandleAudioEnabled();
            }
            else
            {
                HandleAudioDisabled();
            }
        }

        public void SetSocket(Socket socket)
        {
            _socket = socket;
        }

        public void AddAudioSource()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }


        #endregion Public Functions

        #region  Private Functions

        private void HandleAudioEnabled()
        {
            if (AudioSource == null)
            {
                AddAudioSource();
            }
        }

        private void HandleAudioDisabled()
        {
            if (AudioSource != null)
            {
                RemoveAudioSource();
            }
        }

        private void RemoveAudioSource()
        {
            if (Application.isPlaying)
            {
                Destroy(_audioSource);
            }
            else
            {
                DestroyImmediate(_audioSource);
            }
        }

        private void HandleItemPlaced(Socket socket, PlaceableItem placeableItem)
        {
            if (placeableItem.StackableItemController.Spawning)
            {
                return;
            }

            if (!socket.IsSetup)
            {
                return;
            }

            PlayPlacedSFX();
        }

        private void HandleItemRemoved(Socket socket, PlaceableItem placeableItem)
        {
            if (!UseAudio)
                return;

            _audioSource.clip = _removeAudioClip;

            _audioSource.Play();
        }

        private void HandleItemAddedToCloneStack()
        {
            PlayPlacedSFX();
        }

        private void HandleItemAddedToInstanceStack()
        {
            if (_socket.StackableItemController.Settings.InstanceStackType == Enums.InstanceStackType.FIFO)
            {
                PlayPlacedSFX();
            }
        }

        private void PlayPlacedSFX()
        {
            if (!UseAudio)
                return;

            _audioSource.clip = _placeAudioClip;

            _audioSource.Play();
        }

        #endregion  Private Functions

        #region Editor Functions

#if UNITY_EDITOR

        public void LoadDefaultSoundClips()
        {
            Debug.Log("Loading default sound clips");

            LoadDefaultPlaceSound();
            LoadDefaultRemoveSound();
        }

        private void LoadDefaultPlaceSound()
        {
            _placeAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(_DEFAULT_PLACE_SOUND_ASSET_PATH);

            if (_placeAudioClip == null)
            {
                Debug.LogWarning("Failed to load the default place sound effect at path: " + _DEFAULT_PLACE_SOUND_ASSET_PATH, this);
            }
        }

        private void LoadDefaultRemoveSound()
        {
            _removeAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(_DEFAULT_REMOVE_SOUND_ASSET_PATH);

            if (_removeAudioClip == null)
            {
                Debug.LogWarning("Failed to load the default remove sound effect at path: " + _DEFAULT_REMOVE_SOUND_ASSET_PATH);
            }
        }
#endif

        #endregion Editor Functions
    }
}
