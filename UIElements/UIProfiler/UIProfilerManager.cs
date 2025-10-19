
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
    private bool _isDragging = false;
    private Vector3 _clickPos;
    private float _oldPosX;
    private float _oldPosY;

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
        _profilerWindow.RegisterCallback<PointerDownEvent>(evt =>
        {
            _isDragging = true;
            _clickPos = evt.position;
            _oldPosX = _profilerWindow.resolvedStyle.left;
            _oldPosY = _profilerWindow.resolvedStyle.top;

            _profilerWindow.CapturePointer(evt.pointerId);
        });

        _profilerWindow.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (!_isDragging)
            {
                return;
            }

            Vector2 delta = evt.position - _clickPos;

            _profilerWindow.style.translate = new Translate(delta.x, delta.y);
        });

        _profilerWindow.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (_isDragging)
            {
                _isDragging = false;

                _profilerWindow.ReleasePointer(evt.pointerId);

                Translate translate = _profilerWindow.style.translate.value;
                _profilerWindow.style.left = _oldPosX + translate.x.value;
                _profilerWindow.style.top = _oldPosY + translate.y.value;
                _profilerWindow.style.translate = new Translate(0, 0);
            }
        });

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
