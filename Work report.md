# Work report

## Design

We first designed the system to develop before dedicating any implementation effort. The resulting design documents are:

- A [Draft-7 JSON schema](<./City design.json>), useful both for developers and users: the formers model a city and its various parts, like vehicles, semaphores, streets and so on; the latters, instead, can leverage this schema to generate and validate their own city, which they will give as input to the system;
- A BPMN document to write down and furnish the typical use case of the system;
