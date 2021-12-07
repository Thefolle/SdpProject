# Frame updates

If all the systems perform physics steps, then the frame rate is no more than 50 frames/s.
If all the systems perform steps, the frame rate is the maximum permitted by the machine, but it heavily depends on the workload of the machine, which in turn depends both from the simulation load and the host machine processes.

Each physic step counts for 0.02 simulation seconds, while a step is performed through bare polling.

When the simulation step becomes heavy or when the host machine is overwhelmed, Unity performs more physic steps than steps because it has to recover the simulation time before going on. Systems based on steps, therefore, receive the input only from the last physic step: this phenomenon wreaks havoc in the simulation.
When a step is evaluated quickly, instead, Unity performs one physical step and one step, with no difference between them.
