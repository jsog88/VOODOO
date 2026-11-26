using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [Header("Paneles y Botón HUD")]
    [Tooltip("Panel principal del menú de pausa")]
    public GameObject pauseMenuPanel;
    [Tooltip("Opcional: sub-panel de audio si lo tienes separado de los botones principales")]
    public GameObject audioSubPanel;
    [Tooltip("Opcional: botón de pausa visible en el HUD para ocultarlo al pausar")]
    public GameObject hudPauseButton;

    [Header("Sliders de Audio (Opcionales)")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Configuración de Escena")]
    [Tooltip("Índice de la escena del Menú Principal en Build Profiles (normalmente 0)")]
    public int mainMenuBuildIndex = 0;

    private bool _isPaused;
    private CamaraTerceraPersona _camera;

    private const string MasterKey = "Audio_Master";
    private const string MusicKey = "Audio_Music";
    private const string SfxKey = "Audio_Sfx";

    private void Awake()
    {
        _camera = FindFirstObjectByType<CamaraTerceraPersona>();
    }

    private void Start()
    {
        _isPaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (audioSubPanel != null) audioSubPanel.SetActive(false);

        InicializarSlidersAudio();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_isPaused)
            {
                if (audioSubPanel != null && audioSubPanel.activeSelf)
                {
                    CerrarSubPanelAudio();
                }
                else
                {
                    Continuar();
                }
            }
            else
            {
                AbrirPausa();
            }
        }
    }

    // Conectar al OnClick() del Botón de Pausa en el HUD
    public void AbrirPausa()
    {
        // Impide pausar si ya está abierta la subida de nivel, victoria o derrota
        if (_isPaused || Time.timeScale == 0f) return;

        _isPaused = true;
        Time.timeScale = 0f;

        if (_camera != null) _camera.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (hudPauseButton != null) hudPauseButton.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (audioSubPanel != null) audioSubPanel.SetActive(false);
    }

    // Conectar al botón "Continuar"
    public void Continuar()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (audioSubPanel != null) audioSubPanel.SetActive(false);
        if (hudPauseButton != null) hudPauseButton.SetActive(true);

        if (_camera != null) _camera.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Conectar al botón "Reiniciar"
    public void ReiniciarNivel()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Conectar al botón "Salir al Menú"
    public void SalirAlMenu()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuBuildIndex);
    }

    // Opcionales si usas un botón "Audio" que despliega otro panel
    public void AbrirSubPanelAudio()
    {
        if (audioSubPanel != null) audioSubPanel.SetActive(true);
    }

    public void CerrarSubPanelAudio()
    {
        if (audioSubPanel != null) audioSubPanel.SetActive(false);
    }

    // --- CONTROL Y GUARDADO DE AUDIO ---

    public void CambiarVolumenGeneral(float valor)
    {
        AudioListener.volume = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat(MasterKey, valor);
    }

    public void CambiarVolumenMusica(float valor)
    {
        PlayerPrefs.SetFloat(MusicKey, valor);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(valor);
        }
    }

    public void CambiarVolumenEfectos(float valor)
    {
        PlayerPrefs.SetFloat(SfxKey, valor);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxVolume(valor);
        }
    }

    private void InicializarSlidersAudio()
    {
        float savedMaster = PlayerPrefs.GetFloat(MasterKey, 1f);
        float savedMusic = PlayerPrefs.GetFloat(MusicKey, 0.5f);
        float savedSfx = PlayerPrefs.GetFloat(SfxKey, 0.85f);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(savedMaster);
            masterVolumeSlider.onValueChanged.AddListener(CambiarVolumenGeneral);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(savedMusic);
            musicVolumeSlider.onValueChanged.AddListener(CambiarVolumenMusica);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(savedSfx);
            sfxVolumeSlider.onValueChanged.AddListener(CambiarVolumenEfectos);
        }

        CambiarVolumenGeneral(savedMaster);
        CambiarVolumenMusica(savedMusic);
        CambiarVolumenEfectos(savedSfx);
    }
}