using Unity.Entities;
using Unity.Mathematics;

namespace Vomisa.FlowField.DOTS
{
    public struct CellComponentData : IComponentData
    {
        public float3 worldPos;
        public int2 gridIndex;
        public byte cost;
        public ushort bestCost;
        public int2 bestDirection;
        public bool isDestination;
        public bool slowsByCost;

        public string CostToString()
        {
            return cost == byte.MaxValue ? "X" : cost.ToString();
        }

        public string BestCostToString()
        {
            return bestCost == ushort.MaxValue ? "X" : bestCost.ToString();
        }
    }
}
