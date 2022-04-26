using Unity.Entities;
using Unity.Mathematics;

namespace Vomisa.FlowField.DOTS
{
    public class GridInitializationSystem : SystemBase
    {
        private EntityCommandBufferSystem _ecb;
        private EntityArchetype _cellArchetype;

        protected override void OnCreate()
        {
            _ecb = World.GetOrCreateSystem<EntityCommandBufferSystem>();
            _cellArchetype = EntityManager.CreateArchetype(typeof(CellComponentData));
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer commandBuffer = _ecb.CreateCommandBuffer();
            EntityArchetype cellArchetype = _cellArchetype;

            Entities
                .WithAll<NewGridTag>()
                .ForEach((Entity entity, in GridComponentData gridData) =>
                {
                    commandBuffer.RemoveComponent<NewGridTag>(entity);

                    DynamicBuffer<EntityBufferElement> buffer = commandBuffer.AddBuffer<EntityBufferElement>(entity);
                    DynamicBuffer<Entity> entityBuffer = buffer.Reinterpret<Entity>();

                    float cellRadius = gridData.cellRadius;
                    float cellDiameter = cellRadius * 2f;
                    int2 gridSize = gridData.size;

                    for (int x = 0; x < gridSize.x; x++)
                    {
                        for (int y = 0; y < gridSize.y; y++)
                        {
                            float3 cellWorldPos = FlowFieldHelper.GetWorldPositionFromCellIndex(x, y, cellRadius);
                            byte cellCost = CostFieldHelper.EvaluateCost(cellWorldPos, cellRadius);
                            CellComponentData cellData = new CellComponentData
                            {
                                worldPos = cellWorldPos,
                                gridIndex = new int2(x, y),
                                cost = cellCost,
                                bestCost = ushort.MaxValue,
                                bestDirection = int2.zero
                            };


                            Entity cellEntity = commandBuffer.CreateEntity(cellArchetype);
                            entityBuffer.Add(cellEntity);
                            commandBuffer.SetComponent(cellEntity, cellData);
                        }
                    }
                })
                .WithoutBurst()
                .Run();
        }
    }
}
