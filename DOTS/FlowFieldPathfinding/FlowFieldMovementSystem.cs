using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Vomisa.FlowField.DOTS
{
    public class FlowFieldMovementSystem : SystemBase
    {
        private EntityQuery _gridDataQuery;
        private EntityCommandBufferSystem _ecbs;

        protected override void OnCreate()
        {
            _ecbs = World.GetOrCreateSystem<EntityCommandBufferSystem>();
            _gridDataQuery = GetEntityQuery(typeof(GridComponentData));
        }

        protected override void OnUpdate()
        {
            bool isPaused = FlowFieldHelper.IsPaused;
            EntityCommandBuffer.ParallelWriter commandBuffer = _ecbs.CreateCommandBuffer().AsParallelWriter();
            Entity gridEntity = _gridDataQuery.GetSingletonEntity();
            if (EntityManager.HasComponent<NewGridTag>(gridEntity))
                return;

            DynamicBuffer<EntityBufferElement> buffer = EntityManager.GetBuffer<EntityBufferElement>(gridEntity);
            GridComponentData gridData = EntityManager.GetComponentData<GridComponentData>(gridEntity);
            int2 gridSize = gridData.size;
            int2 destinationIndex = gridData.destinationIndex;
            float cellDiameter = gridData.cellRadius * 2f;
            DynamicBuffer<Entity> cellBuffer = buffer.Reinterpret<Entity>();
            NativeArray<CellComponentData> cellDatas = new NativeArray<CellComponentData>(cellBuffer.Length, Allocator.TempJob);
            for (int i = 0; i < cellBuffer.Length; i++)
            {
                cellDatas[i] = EntityManager.GetComponentData<CellComponentData>(cellBuffer[i]);
            }

            Dependency = Entities
                .WithReadOnly(cellDatas)
                .ForEach((Entity entity, int entityInQueryIndex, ref PhysicsVelocity physicsVelocity, ref Translation translation, ref Rotation rotation, in FollowsFlowFieldComponentData followsFlowFieldData) =>
                {
                    int2 cellIndex = FlowFieldHelper.GetCellIndexFromWorldPos(translation.Value, gridSize, cellDiameter);
                    if (isPaused)
                    {
                        physicsVelocity.Linear = float3.zero;
                    }
                    else if (cellIndex.Equals(destinationIndex))
                    {
                        physicsVelocity.Linear.xz = float2.zero;
                    }
                    else
                    {
                        if (translation.Value.y < -5f)
                        { // If it dot falls through the floor, pop it back up over the map
                            float3 position = translation.Value;
                            position.y = 1f;
                            translation.Value = position;
                            physicsVelocity.Linear.y = 0f;
                        }
                        int flatCellIndex = FlowFieldHelper.GetFlatIndex(cellIndex, gridSize.y);
                        float2 moveDirection = cellDatas[flatCellIndex].bestDirection;
                        float moveSpeed = followsFlowFieldData.moveSpeed;
                        if (cellDatas[flatCellIndex].slowsByCost && cellDatas[flatCellIndex].cost < byte.MaxValue && cellDatas[flatCellIndex].cost > 0)
                        {
                            moveSpeed /= cellDatas[flatCellIndex].cost;
                        }

                        physicsVelocity.Linear.xz = moveDirection * moveSpeed;
                    }
                })
                .WithDisposeOnCompletion(cellDatas)
                .ScheduleParallel(Dependency);

            _ecbs.AddJobHandleForProducer(Dependency);
        }
    }
}
