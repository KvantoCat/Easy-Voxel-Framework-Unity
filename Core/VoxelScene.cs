
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace EasyVoxel
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [ImageEffectAllowedInSceneView]
    [ExecuteAlways]

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

        public List<VoxelObject> Objects
        {
            get 
            { 
                return _voxelObjects; 
            }
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            if (FindObjectsByType<VoxelScene>(FindObjectsSortMode.None).Length > 1)
            {
                EditorApplication.delayCall += () =>
                {
                    DestroyImmediate(this);
                    DestroyImmediate(_camera);
                };

#if UNITY_EDITOR
                EditorUtility.DisplayDialog("Object cannot be added", "Unity scene already contains voxel scene", "Ok");
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
            StartCoroutine(DelayedUodate(0.01f));
        }

        private IEnumerator DelayedUodate(float second)
        {
            while (true)
            {
                _voxelObjects = new(FindObjectsByType<VoxelObject>(FindObjectsSortMode.None));

                List<VoxelOctree> octreeLinks = GetVoxelOctreeLinks();
                VTransformStuct[] objectsVTransform = MergeObjectsVTransform(octreeLinks);

                RenderHelp.InitComputeBuffer(ref _ObjectsTransformBuffer, objectsVTransform, 0.0f);

                _voxelRenderMaretial.SetBuffer("TRs", _ObjectsTransformBuffer);
                _voxelRenderMaretial.SetInt("COUNT", _voxelObjects.Count);

                yield return new WaitForSeconds(second);
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
                if (voxelObject.TryGetComponent<MeshFilter>(out var meshFilter))
                {
                    PolygonalTree poligonTree = new(); poligonTree.Build(meshFilter.sharedMesh);
                    voxelObject.Build(poligonTree, (Vector3 vec) => new Color(Random.value, 0.5f, 0.35f));
                }
            }

            if (_voxelObjects.Count > 0)
            {
                InitShaderScene();
            }
        }

        public void InitShaderScene()
        {
            List<VoxelOctree> octreeLinks = GetVoxelOctreeLinks();
            OctreeNode[] objectNodes = MergeObjectsNode(octreeLinks);
            VTransformStuct[] objectsVTransform = MergeObjectsVTransform(octreeLinks);

            RenderHelp.InitComputeBuffer(ref _nodesBuffer, objectNodes, 0.03f);
            RenderHelp.InitComputeBuffer(ref _ObjectsTransformBuffer, objectsVTransform, 0.0f);

            _voxelRenderMaretial.SetBuffer("Nodes", _nodesBuffer);
            _voxelRenderMaretial.SetBuffer("TRs", _ObjectsTransformBuffer);
            _voxelRenderMaretial.SetInt("COUNT", _voxelObjects.Count);
        }

        public OctreeNode[] MergeObjectsNode(List<VoxelOctree> voxelOctreeLinks)
        {
            int nodesCount = 0;

            foreach (VoxelOctree octreeLink in voxelOctreeLinks)
            {
                nodesCount += octreeLink.Nodes.Count;
            }

            OctreeNode[] result = new OctreeNode[nodesCount];

            int i = 0;

            foreach (VoxelOctree octreeLink in voxelOctreeLinks)
            {
                octreeLink.Nodes.CopyTo(result, i);
                i += octreeLink.Nodes.Count;
            }

            return result;
        }

        public VTransformStuct[] MergeObjectsVTransform(List<VoxelOctree> voxelOctreeLinks)
        {
            VTransformStuct[] result = new VTransformStuct[_voxelObjects.Count];

            int i = 0;
            foreach (VoxelObject voxelObject in _voxelObjects)
            {
                Transform transform = voxelObject.transform;

                int j = 0;
                foreach (VoxelOctree voxelOctreeLink in voxelOctreeLinks)
                {
                    if (voxelObject.VoxelOctree == voxelOctreeLink)
                    {
                        break;
                    }

                    j = voxelOctreeLink.Nodes.Count;
                }

                result[i] = new VTransformStuct(transform.localScale.x, transform.position, j, voxelObject.Depth);

                i++;
            }

            return result;
        }

        public List<VoxelOctree> GetVoxelOctreeLinks()
        {
            List<VoxelOctree> voxelOctreeLinks = new();

            foreach (VoxelObject voxelObject in _voxelObjects)
            {
                if (voxelObject.VoxelOctree == null)
                {
                    continue;
                }

                if (voxelOctreeLinks.Count == 0)
                {
                    voxelOctreeLinks.Add(voxelObject.VoxelOctree);

                    continue;
                }

                bool a = false;

                foreach (VoxelOctree octreeLink in voxelOctreeLinks)
                {
                    if (voxelObject.VoxelOctree == octreeLink)
                    {
                        a = true;
                        break;
                    }
                }

                if (!a)
                {
                    voxelOctreeLinks.Add(voxelObject.VoxelOctree);
                }
            }

            return voxelOctreeLinks;
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