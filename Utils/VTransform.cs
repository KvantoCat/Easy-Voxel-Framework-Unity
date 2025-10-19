
using UnityEngine;

namespace EasyVoxel
{
    public class VTransform
    {
        private float _scale;
        private Vector3 _position;

        public float Scale
        {
            get { return _scale; }
            set { _scale = value; }
        }

        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public VTransform()
        {
            _scale = 1.0f;
            _position = Vector3.zero;
        }

        public VTransform(float scale, Vector3 position)
        {
            _scale = scale;
            _position = position;
        }
    }
}