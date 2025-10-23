
using UnityEngine;

public class FreeCameraControl : MonoBehaviour
{
    [SerializeField] private float _cameraFlySpeed = 50.0f;
    [SerializeField] private float _cameraSensitivity = 3.5f;
    [SerializeField] private Camera _camera;

    private float _yaw;
    private float _pitch;
    private bool _isCameraLocked = false;

    private void Start()
    {
        if (_camera == null) 
        { 
            return; 
        }

        _yaw = _camera.transform.eulerAngles.y;
        _pitch = _camera.transform.eulerAngles.x;
    }

    private void Update()
    {
        if (_camera == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                _isCameraLocked = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                _isCameraLocked = false;
            }
        }

        if (_isCameraLocked)
        {
            CameraRotationUpdate();
            CameraTransformUpdate();
        }
    }

    private void CameraRotationUpdate()
    {
        float axisX = Input.GetAxis("Mouse X") * _cameraSensitivity;
        float axisY = Input.GetAxis("Mouse Y") * _cameraSensitivity;

        _yaw += axisX;
        _pitch = Mathf.Clamp(_pitch - axisY, -85.0f, 85.0f);

        _camera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0.0f);
    }

    private void CameraTransformUpdate()
    {
        Vector3 forwardProjXZ = Vector3.Normalize(new Vector3(_camera.transform.forward.x, 0.0f, _camera.transform.forward.z));
        Vector3 rightProjXZ = Vector3.Normalize(new Vector3(_camera.transform.right.x, 0.0f, _camera.transform.right.z));

        if (Input.GetKey(KeyCode.Space))
        {
            _camera.transform.position += _cameraFlySpeed * Time.deltaTime * Vector3.up;
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            _camera.transform.position += _cameraFlySpeed * Time.deltaTime * Vector3.down;
        }

        if (Input.GetKey(KeyCode.W))
        {
            _camera.transform.position += _cameraFlySpeed * Time.deltaTime * forwardProjXZ;
        }
        if (Input.GetKey(KeyCode.S))
        {
            _camera.transform.position += _cameraFlySpeed * Time.deltaTime * -forwardProjXZ;
        }

        if (Input.GetKey(KeyCode.D))
        {
            _camera.transform.position += _cameraFlySpeed * Time.deltaTime * rightProjXZ;
        }
        if (Input.GetKey(KeyCode.A))
        {
            _camera.transform.position +=_cameraFlySpeed * Time.deltaTime * -rightProjXZ;
        }
    }
}
