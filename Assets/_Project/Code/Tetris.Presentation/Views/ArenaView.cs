using Tetris.Core.Fields;
using Tetris.Presentation.Configs;
using UnityEngine;

namespace Tetris.Presentation.Views
{
    public sealed class ArenaView : MonoBehaviour
    {
        [SerializeField] private Material _lineMaterial = default!;
        [SerializeField] private float _lineWidth = 0.03f;
        [SerializeField] private Color _lineColor = new Color(0.6f, 0.8f, 1f, 0.8f);

        private bool _isInitialized;

        public void Initialize(IReadOnlyGameField field, FieldLayoutSettingsSO layout)
        {
            if (_isInitialized)
            {
                throw new System.InvalidOperationException("ArenaView already initialized.");
            }

            if (field == null) throw new System.ArgumentNullException(nameof(field));
            if (layout == null) throw new System.ArgumentNullException(nameof(layout));

            if (_lineMaterial == null)
            {
                throw new System.InvalidOperationException(
                    "ArenaView._lineMaterial not assigned. Set in Inspector.");
            }

            BuildWireframe(field, layout);
            _isInitialized = true;
        }

        private void BuildWireframe(IReadOnlyGameField field, FieldLayoutSettingsSO layout)
        {
            var blockSize = layout.BlockSize;
            var origin = layout.WorldOrigin;

            var min = origin + new Vector3(-0.5f, -0.5f, -0.5f) * blockSize;
            var max = origin + new Vector3(field.SizeX - 0.5f, field.SizeY - 0.5f, field.SizeZ - 0.5f) * blockSize;

            var c000 = new Vector3(min.x, min.y, min.z);
            var c100 = new Vector3(max.x, min.y, min.z);
            var c010 = new Vector3(min.x, max.y, min.z);
            var c110 = new Vector3(max.x, max.y, min.z);
            var c001 = new Vector3(min.x, min.y, max.z);
            var c101 = new Vector3(max.x, min.y, max.z);
            var c011 = new Vector3(min.x, max.y, max.z);
            var c111 = new Vector3(max.x, max.y, max.z);

            CreateEdge("Edge_Bottom_XMin", c000, c100);
            CreateEdge("Edge_Bottom_XMax", c001, c101);
            CreateEdge("Edge_Bottom_ZMin", c000, c001);
            CreateEdge("Edge_Bottom_ZMax", c100, c101);

            CreateEdge("Edge_Top_XMin", c010, c110);
            CreateEdge("Edge_Top_XMax", c011, c111);
            CreateEdge("Edge_Top_ZMin", c010, c011);
            CreateEdge("Edge_Top_ZMax", c110, c111);

            CreateEdge("Edge_Vertical_00", c000, c010);
            CreateEdge("Edge_Vertical_10", c100, c110);
            CreateEdge("Edge_Vertical_01", c001, c011);
            CreateEdge("Edge_Vertical_11", c101, c111);
        }

        private void CreateEdge(string name, Vector3 start, Vector3 end)
        {
            var edgeGO = new GameObject(name);
            edgeGO.transform.SetParent(transform, worldPositionStays: false);

            var line = edgeGO.AddComponent<LineRenderer>();
            line.material = _lineMaterial;
            line.startColor = _lineColor;
            line.endColor = _lineColor;
            line.startWidth = _lineWidth;
            line.endWidth = _lineWidth;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }
    }
}