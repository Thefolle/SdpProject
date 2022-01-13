# Unity ECS

Unity ECS focuses on scalability rather than game complexity.

You can parallelize systems by:

- Ensuring exclusive access: only one system accesses the entities, so it can read and write freely on them;
- When you want to manually manage race conditions on a certain variable, tag it with NativeDisableParallelForRestriction;
- Apply restrictions to the design so that only one entity is accessing a shared variable in write mode;
- Use a buffer when a variable is subjected to parallel read and write; at the end of the frame, schedule a job to copy the result;
- Alternatively, add a flag that is flipped at each frame, so that a variable is alternatively read and written;
- First design the parallel algorithm; second, write the sequential algorithm; third, turn it to a parallel version. Indeed, parallel jobs are way more expensive to maintain;
- If you cannot solve race conditions, split the job into multiple jobs;
- Prefer many but smaller components against few components with many fields; this diminishes the chance that two systems read/write from the same field;
- Avoid command buffers, since they playback commands sequentially anyway; instead, leverage batch operations from entity manager and populate the components of the new entities in separate jobs;
- Use SharedComponents to group related entities to improve cache efficiency; this component groups the entities in the same chunk, which in turn favors data locality. GetComponentFromEntity variables instead perform random accesses, which often result in cache misses; however, they cannot avoided in most cases;
- Avoid floating points: use integers.
