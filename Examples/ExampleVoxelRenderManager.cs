
using UnityEngine;
using EasyVoxel;

[RequireComponent(typeof(Camera))]
[ExecuteAlways, ImageEffectAllowedInSceneView]
public class ExampleVoxelRenderManager : MonoBehaviour
{
    [SerializeField] private Material _voxelRenderMaretial;
    [SerializeField] private bool _renderActive = true;
    [SerializeField] private float _maxRenderDist = 800.0f;
    [SerializeField] private bool _LODActive = true;
    [SerializeField] private float _LODDist = 200.0f;
    [SerializeField] private float _objectsScale = 50.0f;

    [Header("Object #1")]
    [SerializeField, Range(1, 8)] private int _octreeDepth;
    [SerializeField] private Mesh _testMesh;

    [Header("Object #2")]
    [SerializeField, Range(1, 8)] private int _octreeDepth2;
    [SerializeField] private Mesh _testMesh2;

    [Header("Environment settings")]
    [SerializeField] private Color _skyHorizonColor = Color.white;
    [SerializeField] private Color _skyZenithColor = new(0.0f, 0.5f, 1.0f);
    [SerializeField] private Color _groundColor = new(0.13f, 0.13f, 0.15f);
    [SerializeField] private float _sunFocus = 650.0f;
    [SerializeField] private float _sunIntensity = 3.5f;
    [SerializeField] private Light _light;

    private ComputeBuffer _nodesBuffer;
    private ComputeBuffer _ObjectsTransformBuffer;
    private VoxelScene _scene;
    private Camera _camera;

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);

        _camera = GetComponent<Camera>();
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = Color.black;
        _camera.cullingMask = 0;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_renderActive && _voxelRenderMaretial != null)
        {
            SetShaderParams();

            Graphics.Blit(source, destination, _voxelRenderMaretial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    private void Update()
    {
        if (_voxelRenderMaretial == null)
        {
            _voxelRenderMaretial = new Material(Shader.Find("VLib/Render"));
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            BuildSceneTEST();
        }  
    }

    [ContextMenu("Build Scene Function TEST")]
    private void BuildSceneTEST()
    {
        if (_testMesh == null || _testMesh2 == null)
        {
            return;
        }

        _scene = new();

        PolygonalTree poligonTree = new(); poligonTree.Build(_testMesh);
        PolygonalTree poligonTree2 = new(); poligonTree2.Build(_testMesh2);
        VoxelOctree voxelOctree = new(); voxelOctree.Build(_octreeDepth, poligonTree, (Vector3 v3) => new Color(Random.value, 0.5f, 0.35f));
        VoxelOctree voxelOctree2 = new(); voxelOctree2.Build(_octreeDepth2, poligonTree2, (Vector3 v3) => new Color(0.3f, Random.value, 0.75f));

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                VoxelObject voxelObject = new();

                if (Random.value < 0.5f)
                {
                    voxelObject.VoxelOctree = voxelOctree2;
                }
                else
                {
                    voxelObject.VoxelOctree = voxelOctree;
                }

                voxelObject.Transform = new VTransform(_objectsScale, new Vector3(i, 0.0f, j) * _objectsScale);
                _scene.AddObject(voxelObject);
            }
        }

        _scene.TrowInShader(_voxelRenderMaretial, ref _nodesBuffer, ref _ObjectsTransformBuffer, "Nodes", "TRs", "COUNT");
    }

    private void SetShaderParams()
    {
        SetCameraParams();

        _voxelRenderMaretial.SetFloat("MaxRenderDist", _maxRenderDist);
        _voxelRenderMaretial.SetVector("SkyHorizonColor", _skyHorizonColor);
        _voxelRenderMaretial.SetVector("SkyZenithColor", _skyZenithColor);
        _voxelRenderMaretial.SetVector("GroundColor", _groundColor);
        _voxelRenderMaretial.SetFloat("SunFocus", _sunFocus);
        _voxelRenderMaretial.SetFloat("SunIntensity", _sunIntensity);

        if (_light != null) 
        { 
            _voxelRenderMaretial.SetVector("SunLightDir", _light.transform.forward); 
        }
        else
        {
            _voxelRenderMaretial.SetVector("SunLightDir", Vector3.zero); 
        }

        _voxelRenderMaretial.SetInt("LODUse", _LODActive ? 1 : 0);
        _voxelRenderMaretial.SetFloat("LODDist", _LODDist);
    }

    private void SetCameraParams()
    {
        float screenDepth = _camera.nearClipPlane;
        float screenHeight = screenDepth * Mathf.Tan(_camera.fieldOfView / 2.0f * Mathf.Deg2Rad) * 2.0f;
        float screenWidth = screenHeight * _camera.aspect;
        Vector3 viewParams = new(screenWidth, screenHeight, screenDepth);
        Matrix4x4 localToWorldMatrix = _camera.transform.localToWorldMatrix;

        _voxelRenderMaretial.SetMatrix("LocalToWorldMatrix", localToWorldMatrix);
        _voxelRenderMaretial.SetVector("ViewParams", viewParams);
        _voxelRenderMaretial.SetVector("CameraPos", _camera.transform.position);
    }

    private void OnDisable()
    {
        _nodesBuffer?.Release();
        _ObjectsTransformBuffer?.Release();
    }
}

