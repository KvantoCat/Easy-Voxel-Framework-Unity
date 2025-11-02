
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class UIProfilerManager : MonoBehaviour
{
    private int _deltaFrame;
    private float _summFrameDelays;
    private float _avgFrameDelay;
    private float _avgFPS;

    private UIDocument _uiProfiler;
    private VisualElement _profilerWindow;

    private Label _versionLabel;
    private Label _performanceLabel;
    private Label _memoryLabel;

    private bool _isShow = true;

    private void Awake()
    {
        _uiProfiler = GetComponent<UIDocument>();
        _profilerWindow = _uiProfiler.rootVisualElement;

        _versionLabel = _profilerWindow.Q<Label>("Version");
        _performanceLabel = _profilerWindow.Q<Label>("Performance");
        _memoryLabel = _profilerWindow.Q<Label>("Memory");
    }

    private void Start()
    {
        StartCoroutine(UpdateDelayed(0.5f));
    }

    private void Update()
    {
        _deltaFrame += 1;
        _summFrameDelays += Time.deltaTime;

        if (_deltaFrame == 10)
        {
            _avgFrameDelay = _summFrameDelays / _deltaFrame;
            _avgFPS = 1.0f / _avgFrameDelay;

            _summFrameDelays = 0.0f;
            _deltaFrame = 0;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            _profilerWindow.style.left = 0.0f;
            _profilerWindow.style.top = 0.0f;

            if (_isShow)
            {
                _profilerWindow.visible = false;
                _isShow = false;
            }
            else
            {
                _profilerWindow.visible = true;
                _isShow = true;
            }
        }
    }

    private IEnumerator UpdateDelayed(float seconds)
    {
        while (true)
        {
            _versionLabel.text = "V.00.00.01";

            _performanceLabel.text = $"FPS: {Math.Round(_avgFPS), -4} ({Math.Round(_avgFrameDelay * 1000.0f)} ms)";

            long managedMemory = GC.GetTotalMemory(false) / (1024 * 1024);
            _memoryLabel.text = $"Usage memory: {managedMemory} Mb";

            yield return new WaitForSeconds(seconds);
        }
    }
}
