using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Vomisa.FlowField.DOTS
{
    public class RecalculateFlowFieldSystem : SystemBase
    {
        private EntityCommandBufferSystem _ecbs;

        protected override void OnCreate()
        {
            _ecbs = World.GetOrCreateSystem<EntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer commandBuffer = _ecbs.CreateCommandBuffer();
            NativeArray<GridDirection> cardinalDirections = new NativeArray<GridDirection>(GridDirection.CardinalDirections.Length, Allocator.TempJob);
            for (int i = 0; i < GridDirection.CardinalDirections.Length; i++)
            {
                cardinalDirections[i] = GridDirection.CardinalDirections[i];
            }
            NativeArray<GridDirection> allDirections = new NativeArray<GridDirection>(GridDirection.AllDirections.Length, Allocator.TempJob);
            for (int i = 0; i < GridDirection.AllDirections.Length; i++)
            {
                allDirections[i] = GridDirection.AllDirections[i];
            }
            NativeQueue<int2> indicesToCheck = new NativeQueue<int2>(Allocator.TempJob);

            Entities
                .WithAll<RecalculateFlowFieldComponentTag>()
                .ForEach((Entity entity, ref DynamicBuffer<EntityBufferElement> buffer, in GridComponentData gridData) =>
                {
                    //UnityEngine.Debug.Log("[RecalculateFlowFieldSystem] Recalculating flow field");
                    commandBuffer.RemoveComponent<RecalculateFlowFieldComponentTag>(entity);
                    NativeArray<int2> neighborIndices = new NativeArray<int2>(cardinalDirections.Length, Allocator.Temp);
                    NativeArray<int2> allNeighborIndices = new NativeArray<int2>(allDirections.Length, Allocator.Temp);

                    DynamicBuffer<Entity> cellbuffer = buffer.Reinterpret<Entity>();
                    NativeArray<CellComponentData> cellDataContainer = new NativeArray<CellComponentData>(cellbuffer.Length, Allocator.Temp);
                    int2 gridSize = gridData.size;

                    for (int i = 0; i < cellbuffer.Length; i++)
                    {
                        var cellData = GetComponent<CellComponentData>(cellbuffer[i]);
                        cellData.bestCost = ushort.MaxValue;
                        cellData.isDestination = false;
                        cellDataContainer[i] = cellData;
                    }

                    int flatDestinationIndex = FlowFieldHelper.GetFlatIndex(gridData.destinationIndex, gridSize.y);
                    CellComponentData destinationCell = cellDataContainer[flatDestinationIndex];
                    destinationCell.isDestination = true;
                    destinationCell.bestCost = 0;
                    cellDataContainer[flatDestinationIndex] = destinationCell;

                    indicesToCheck.Enqueue(destinationCell.gridIndex);
                    int indicesChecked = 0;
                    int neighborsChecked = 0;
                    int neighborsEnqueued = 0;

                    while (indicesToCheck.Count > 0)
                    { // Calculate integration field
                    int2 cellIndex = indicesToCheck.Dequeue();
                        int flatIndex = FlowFieldHelper.GetFlatIndex(cellIndex, gridSize.y);
                        CellComponentData cellData = cellDataContainer[flatIndex];
                        FlowFieldHelper.GetNeighborIndices(cellIndex, ref cardinalDirections, gridSize, ref neighborIndices);
                        for (int i = 0; i < neighborIndices.Length; i++)
                        {
                            if (neighborIndices[i].x >= 0)
                            {
                                int flatNeighborIndex = FlowFieldHelper.GetFlatIndex(neighborIndices[i], gridSize.y);
                                CellComponentData neighborData = cellDataContainer[flatNeighborIndex];
                                if (neighborData.cost == byte.MaxValue)
                                    continue;

                                neighborsChecked++;
                                int combinedCost = (neighborData.isDestination ? 0 : neighborData.cost) + cellData.bestCost;
                                if (combinedCost < neighborData.bestCost)
                                {
                                    neighborData.bestCost = (ushort)combinedCost;
                                    cellDataContainer[flatNeighborIndex] = neighborData;
                                    indicesToCheck.Enqueue(neighborIndices[i]);
                                    neighborsEnqueued++;
                                }
                            }
                        }
                        indicesChecked++;
                    }

                    for (int i = 0; i < cellDataContainer.Length; i++)
                    { // Generate flow field directions
                    CellComponentData cellData = cellDataContainer[i];
                        FlowFieldHelper.GetNeighborIndices(cellData.gridIndex, ref allDirections, gridSize, ref allNeighborIndices);
                        ushort bestCost = cellData.bestCost;
                        int2 bestDirection = int2.zero;
                        for (int n = 0; n < allNeighborIndices.Length; n++)
                        {
                            if (allNeighborIndices[n].x >= 0)
                            {
                                int flatNeighborIndex = FlowFieldHelper.GetFlatIndex(allNeighborIndices[n], gridSize.y);
                                CellComponentData neighborData = cellDataContainer[flatNeighborIndex];
                                if (neighborData.bestCost < bestCost)
                                {
                                    bestCost = neighborData.bestCost;
                                    bestDirection = allNeighborIndices[n] - cellData.gridIndex;
                                }
                            }
                            cellData.bestDirection = bestDirection;
                            cellDataContainer[i] = cellData;
                        }
                    }

                    for (int i = 0; i < cellbuffer.Length; i++)
                    {
                        commandBuffer.SetComponent(cellbuffer[i], cellDataContainer[i]);
                    }

                    neighborIndices.Dispose();
                    allNeighborIndices.Dispose();
                    cellDataContainer.Dispose();
                    commandBuffer.AddComponent<FlowFieldRecalculationCompleteTag>(entity);
                })
                .WithDisposeOnCompletion(cardinalDirections)
                .WithDisposeOnCompletion(allDirections)
                .WithDisposeOnCompletion(indicesToCheck)
                .Schedule();

            _ecbs.AddJobHandleForProducer(Dependency);
        }
    }
}
