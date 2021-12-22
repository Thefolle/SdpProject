using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static UnityEngine.Debug;
using Unity.Collections;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class SplineVehicleMovementSystem : SystemBase
{
    /// <summary>
    /// <para>The degree at which cars stop steering to approach toward the track. This parameter is an indicator of the convergence speed of a car to a track.
    /// It is measured in degrees.</para>
    /// </summary>
    private readonly int SteeringDegree = (int)(math.cos(math.radians(75)) * 100);

    /// <summary>
    /// <para>The algorithm exploits this parameter to determine the behaviour of a car w.r.t. its track.</para>
    /// <para>Greater values imply a better convergence in straight lanes, which is worse during bends. Cars may lose their track during bends for big values. Moreover, you should take into account both the width of the car and the width of the lane.</para>
    /// <para>Lower values imply a better convergence in bends, which is slower in straight lanes.</para>
    /// </summary>
    private const int ThresholdDistance = 50;

    /// <summary>
    /// <para>This parameter establishes the range, expressed as distance from the track, in which the car can be considered in the track.</para>
    /// <para>Within the range, the movement algorithm is deactivated and the car proceeds forward. Outside the range, the algorithm works normally.</para>
    /// </summary>
    private const int NegligibleDistance = 20;

    /// <summary>
    /// <para>The constant is equal to cos(80) * 100</para>
    /// </summary>
    private readonly int Cos80 = (int)(math.cos(math.radians(80)) * 100);

    /// <summary>
    /// <para>The constant is equal to cos(60) * 100</para>
    /// </summary>
    private readonly int Cos60 = (int)(math.cos(math.radians(60)) * 100);



    protected override void OnUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;
        double elapsedTime = Time.ElapsedTime;
        float fixedDeltaTime = UnityEngine.Time.fixedDeltaTime;
        if (elapsedTime < 2) return;

        var getTrackComponentDataFromEntity = GetComponentDataFromEntity<TrackComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getPositionComponentDataFromEntity = GetComponentDataFromEntity<Translation>();

        /* capture local variables */
        var steeringDegree = SteeringDegree;
        var cos60 = Cos60;
        var cos80 = Cos80;

        Entities.ForEach((ref Translation translation, ref SplineCarComponentData splineCarComponentData, ref Rotation rotation, in Entity carEntity, in LocalToWorld localToWorld) =>
        {
            
        }).Run();

    }
}

