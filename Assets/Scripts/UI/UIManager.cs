using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Utils;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Network Settings")]
    [SerializeField] private TMP_Dropdown algorithmSelector;
    [SerializeField] private Slider tickTimeSlider;
    [SerializeField] private TMP_Text tickIntervalText;
    [SerializeField] private TMP_Text nextTickText;

    [Header("Performance Metrics")]
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private TMP_Text frameTimeText;

    [Header("Player Statistics")]
    [SerializeField] private TMP_Text localPlayerCoinsText;
    [SerializeField] private TMP_Text localPlayerDamageText;
    [SerializeField] private TMP_Text remotePlayerCoinsText;
    [SerializeField] private TMP_Text remotePlayerDamageText;

    [Header("Network Statistics")]
    [SerializeField] private TMP_Text optimizedBytesTotalText;
    [SerializeField] private TMP_Text unoptimizedBytesTotalText;
    [SerializeField] private TMP_Text optimizedBytesThisTickText;
    [SerializeField] private TMP_Text unoptimizedBytesThisTickText;

    private float fps;
    private float frameTime;
    private float updateInterval = 0.5f; // How often to update FPS
    private float accum = 0.0f; // FPS accumulated over the interval
    private int frames = 0; // Frames drawn over the interval
    private float timeleft; // Left time for current interval

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        timeleft = updateInterval;
        InitializeAlgorithmSelector();
        UpdatePlayerStats(0,0,0,0);
        UpdateNetworkStats(0,0,0,0);
        UpdateTickInterval(tickTimeSlider.value);
    }

    private void InitializeAlgorithmSelector()
    {
        algorithmSelector.ClearOptions();

        var options = new List<TMP_Dropdown.OptionData>();
        foreach (ExtrapolationType type in Enum.GetValues(typeof(ExtrapolationType)))
        {
            var option = new TMP_Dropdown.OptionData(type.ToString());
            options.Add(option);
        }

        algorithmSelector.AddOptions(options);

        // Set default to WeightedAverage
        int defaultIndex = (int)ExtrapolationType.BezierCurve;
        algorithmSelector.value = defaultIndex;

        // Add listener for value changes
        algorithmSelector.onValueChanged.AddListener(OnAlgorithmSelected);

        // Apply initial selection
        OnAlgorithmSelected(defaultIndex);
    }

    private void OnAlgorithmSelected(int index)
    {
        var selectedType = (ExtrapolationType)index;
        NetworkTransform[] networkTransforms = FindObjectsByType<NetworkTransform>(FindObjectsSortMode.None);
        foreach (var transform in networkTransforms)
        {
            transform.SetExtrapolationType(selectedType);
        }
    }

    private void Update()
    {
        UpdateFPSCounter();
    }

    private void UpdateFPSCounter()
    {
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        frames++;

        if (timeleft <= 0.0f)
        {
            fps = accum / frames;
            frameTime = 1000.0f / fps;

            fpsText.text = $"{fps:F2}";
            frameTimeText.text = $"{frameTime:F2}ms";

            timeleft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }

    public void UpdateTickInterval(float interval)
    {
        tickIntervalText.text = $"{interval:F2}s";
        NetworkManager.Instance.SetTickInterval(interval);
    }

    public void UpdateNextTick(float time)
    {
        if(time<0) time = 0;
        nextTickText.text = $"{time:F2}s";
    }

    public void UpdatePlayerStats(int localCoins, int localDamage, int remoteCoins, int remoteDamage)
    {
        if(localCoins!=-1) localPlayerCoinsText.text = $"{localCoins}";
        if(localDamage!=-1) localPlayerDamageText.text = $"{localDamage}";
        if(remoteCoins!=-1) remotePlayerCoinsText.text = $"{remoteCoins}";
        if(remoteDamage!=-1) remotePlayerDamageText.text = $"{remoteDamage}";
    }

    public void UpdateNetworkStats(int optimizedTotal, int unoptimizedTotal, int optimizedThisTick, int unoptimizedThisTick)
    {
        if(optimizedTotal!=-1) optimizedBytesTotalText.text = $"{optimizedTotal}";
        if(unoptimizedTotal!=-1) unoptimizedBytesTotalText.text = $"{unoptimizedTotal}";
        if(optimizedThisTick!=-1) optimizedBytesThisTickText.text = $"{optimizedThisTick}";
        if(unoptimizedThisTick!=-1) unoptimizedBytesThisTickText.text = $"{unoptimizedThisTick}";
    }
}