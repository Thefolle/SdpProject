using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

public struct SpawnerComponentData : IComponentData
{
    /// <summary>
    /// <para>Variable that helps in implementing a certain spawn strategy.</para>
    /// <para>Each type of entity should be associated with one 
    /// or more slots between 0 and <see cref="TurnWindowLength"/> - 1. <see cref="Turn"/> therefore determines which
    /// slot is currently selected.</para>
    /// </summary>
    public int Turn;
    public static int TurnWindowLength = 6;

    //public void NextTurn()
    //{
    //    Turn = (Turn + 1) % TurnWindowLength;
    //}

    /// <summary>
    /// <para>The last time instant at which this spawner attempted to spawn.</para>
    /// </summary>
    public double LastTimeTriedToSpawn;

}
