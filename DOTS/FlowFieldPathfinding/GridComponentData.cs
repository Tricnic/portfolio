using Unity.Entities;
using Unity.Mathematics;

namespace Vomisa.FlowField.DOTS
{
    [GenerateAuthoringComponent]
    public struct GridComponentData : IComponentData
    {
        public int2 size;
        public float cellRadius;
        public int2 destinationIndex;
    }
}