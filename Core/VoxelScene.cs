
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyVoxel
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [ImageEffectAllowedInSceneView]
    [RequireComponent(typeof(Camera))]
    public class VoxelScene : MonoBehaviour
    {
        [Header("Voxel Scene Settings")]
        [SerializeField] private Material _voxelRenderMaretial;
        [SerializeField] private bool _renderActive = true;
        [SerializeField] private bool _LODActive = true;
        [SerializeField] private float _LODDist = 200.0f;

        [Header("Environment settings")]
        [SerializeField] private Color _skyHorizonColor = Color.white;
        [SerializeField] private Color _skyZenithColor = new(0.0f, 0.5f, 1.0f);
        [SerializeField] private Color _groundColor = new(0.13f, 0.13f, 0.15f);
        [SerializeField] private float _sunFocus = 650.0f;
        [SerializeField] private float _sunIntensity = 3.5f;
        [SerializeField] private Light _light;

        private Camera _camera;
        private ComputeBuffer _nodesBuffer;
        private ComputeBuffer _ObjectsTransformBuffer;
        private List<VoxelObject> _voxelObjects;

        public List<VoxelObject> VoxelObjects
        {
            get { return _voxelObjects; }
        }

        public static VoxelScene Main
        {
            get { return FindFirstObjectByType<VoxelScene>(); }
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            if (FindObjectsByType<VoxelScene>(FindObjectsSortMode.None).Length > 1)
            {

#if UNITY_EDITOR
                EditorApplication.delayCall += () =>
                {
                    DestroyImmediate(this);
                    DestroyImmediate(_camera);
                };

                EditorUtility.DisplayDialog("Object can't be added", "Unity scene already contains voxel scene", "Ok");
#endif

                return;
            }

            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
            Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);

            Shader shader = Shader.Find("VLib/Render");

            if (shader == null)
            {
                throw new System.Exception("VLib/Render shader missing");
            }

            _voxelRenderMaretial = new Material(shader);  
        }

        private void Start()
        {
            StartCoroutine(DelayedUodate());
        }

        private IEnumerator DelayedUodate()
        {
            while (true)
            {
                _voxelObjects = new(FindObjectsByType<VoxelObject>(FindObjectsSortMode.None));

                InitVTransforms();

                yield return null;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_renderActive && _voxelRenderMaretial != null && _camera != null)
            {
                SetShaderParams();

                Graphics.Blit(source, destination, _voxelRenderMaretial);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }

        [ContextMenu("Build Scene")]
        public void BuildScene()
        {
            _voxelObjects = new(FindObjectsByType<VoxelObject>(FindObjectsSortMode.None));

            foreach (VoxelObject voxelObject in _voxelObjects)
            {
                voxelObject.Build();
            }

            InitVTransforms();
            InitNodes();          
        }

        private void InitVTransforms()
        {
            if (_voxelObjects.Count == 0)
            {
                return;
            }

            VTransformStuct[] vTransforms = new VTransformStuct[_voxelObjects.Count];

            int index = 0;
            for (int i = 0; i < _voxelObjects.Count; i++)
            {
                VoxelObject voxelObject = _voxelObjects[i];
                Transform transform = voxelObject.transform;

                Bounds bounds = voxelObject.Bounds;
                Vector3 boundBoxSize = bounds.size;
                float maxBoundBoxSize = Mathf.Max(Mathf.Max(boundBoxSize.x, boundBoxSize.y), boundBoxSize.z);

                vTransforms[i] = new VTransformStuct(
                    transform.localScale.x * maxBoundBoxSize,
                    transform.position + maxBoundBoxSize * transform.localScale.x * bounds.center / 2.0f,
                    index, voxelObject.Depth);

                index += voxelObject.VoxelOctree.Nodes.Count;
            }

            RenderHelp.InitComputeBuffer(ref _ObjectsTransformBuffer, vTransforms, 0.0f);

            _voxelRenderMaretial.SetBuffer("TRs", _ObjectsTransformBuffer);
            _voxelRenderMaretial.SetInt("COUNT", _voxelObjects.Count);
        }

        private void InitNodes()
        {
            if (_voxelObjects.Count == 0)
            {
                return;
            }

            OctreeNode[] nodes;
            int nodesCount = 0;

            foreach (VoxelObject voxelObject in _voxelObjects)
            {
                nodesCount += voxelObject.VoxelOctree.Nodes.Count;
            }

            nodes = new OctreeNode[nodesCount];

            int index = 0;
            foreach (VoxelObject voxelObject in _voxelObjects)
            {
                voxelObject.VoxelOctree.Nodes.CopyTo(nodes, index);
                index += voxelObject.VoxelOctree.Nodes.Count;
            }

            RenderHelp.InitComputeBuffer(ref _nodesBuffer, nodes, 0.03f);

            _voxelRenderMaretial.SetBuffer("Nodes", _nodesBuffer);
        }

        private void SetShaderParams()
        {
            SetCameraParams();

            _voxelRenderMaretial.SetFloat("MaxRenderDist", _camera.farClipPlane);
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
}