# Work report

## Design

We first designed the system to develop before dedicating any implementation effort. The resulting design documents are:

- A [Draft-7 JSON schema](<./City design.json>), useful both for developers and users: the formers model a city and its various parts, like vehicles, semaphores, streets and so on; the latters, instead, can leverage this schema to generate and validate their own city, which they will give as input to the system;
- A BPMN document to write down and furnish the typical use case of the system;
- Several graphs saved as .xgr files to model the behaviour of objects like cars through a state machine.

The tools we exploited to create the system are:

- Unity: the core of the system; in particular, we employed the DOTS-based ECS architecture;
- Blender: the purpose is modeling, sculpting, creating custom meshes and materials of the parts of a city; these parts are imported in Unity as normal gameobjects; in any case, the meshes are very rough since the rendered result of the system is out-of-scope of our project;
- [QuickType](https://app.quicktype.io/?l=csharp): an automatic translator of a Json schema to POJOs (a.k.a. domain classes). This tool brings a great advantage to the project, since we can design the system through the JSON schema and automatically get its representation in C# classes; the C# deserializer is included in the NewtonSoft.json NuGet package.
