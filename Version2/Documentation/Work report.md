# Project report

This document is a summary of the work done to develop the traffic simulation project.

## Glossary

Here follows some definitions that are extensively referenced by this document:

- Traditional stack: the well-established and mature approach to create three-dimensional programs in Unity, based on mono behaviours;
- DOTS: the Data-Oriented Technology Stack, a novel paradigm in building 3D applications. This is the target approach of this project. The community also talks about hybrid solutions, where mixing a small number of traditional scripts with DOTS is considered acceptable; in the context of this project, instead, the developers tried to stick to the Pure ECS paradigm.

## The first version of the project

The very first phase of the development approached the problem in a very flexible way. Essentially, the whole scene was created through scripts only. On one hand, this solution donated great flexibility since the input could be customized at a fine-grained level; on the other hand, generating all the components of the city on the fly came up to be time-consuming if compared to the results. Hence, the developers at that time acknowledged their lack of experience on Unity and 3D frameworks overall.

## Why switching to the version 2?

After having explored the Unity ecosystem through the version 1 of this project, the developers switched to the next version, whose workflow is discussed in this document. The reason of this change was the need to increase the development process speed in exchange for a quite less customizability of the city.

The former version can be considered as an *architectural spike*, namely a prototype that helps in going deep into the specific domain of a project before proceeding to a complete solution.

## Version 2 characteristics

The second phase of the development process pertained the intent to build and deploy a full-fledged traffic simulator, on top of Unity DOTS. The paramount aspect of the second version is the usage of the physics to allow each component in the scene to interact with each other. The Physics package allows to reach a great degree of realism in the behaviour of the components inside the scene. For instance, cars continuously correct their trajectory along the lane, as in real life, rather than following invisible tracks.

## Why switching to the version 3?

The developers, once again, realized that realism was not a requirement of the project, especially if it thwarts scalability at a very limited threshold. Indeed, using physics means loading Unity with a pletora of computations: collision surfaces and points, raycast and spherecast interpolation, gravity application, force computations and so on. Even if systems were partially but reasonably optimized, the resulting number of cars at steady state was unsatisfying. Systems were not run in parallel and some bugs were in place, but the developers traced back the poor performances to the usage of physics, which is therefore intended for few entities.

### What's the input of the simulator?

We conceived a general city as a matrix made up of square districts. Each district has predefined characteristics in terms of density. The scene under simulation can be customized as described beneath.

1. The user writes the city matrix as a json file called `city.json`. The user validates the json file against the [schema](./citySchema.json) in order to probe any syntax error;
2. The user saves the file in `<UnityRootFolder>/Assets/Resources/city.json`, as it is automatically loaded by the simulator so as to build the intended city.

### How does the simulator work?

This paragraph discusses the solutions adopted for each component of the simulator to get to work.

#### The city is a grid of districts

Given the json file that describes the districts selected by the user, the simulator essentially spawns the districts and welds them incrementally.

#### The city is a graph

Once the simulator has instantiated all the streets and crosses of the city, it proceeds to build the internal data structures.
The inner data structure that represent the city is a cyclic directed graph. The developers created a simple dedicated library for graph management, since most of the available ones in the community are not compatible with Unity, like the NuGet QuickGraph.

#### Cars move along splines

Each street is subdivided in forward or backward lanes; each lane contains a certain number of points, declared statically. At runtime, each car proceeds point by point along a trajectory that is the linear interpolation of two successive points (a.k.a., nodes). Cars therefore follow a series of splines that are located across streets and crosses.

Cars receive a random path at spawn time, that guides them from the source street/cross until the destination street/cross; the path is currently computed randomly.

### The output of the simulator

This section describes the measures taken into account in order to evaluate the simulator, along with their actual values on three different machines (the developers'). It follows a discussion on the collected results.

## Last considerations

### How to enhance the project?

We thought about how to permit a future contributor to enhance the project. The suggested path is allowing the user to put in a more fine-grained characterization of the city, as it was intended in the version 1 of this project.
In particular, the future version of the system should rely upon the version 2, without modifying it; in other words, an eventual third version should be built on top of the second one.

As far as our opinion is concerned, the user, through the usual json, will specify, besides the matrix as planned for system version 1, a further data structure that associates to each item in the matrix a bunch of characteristics. In other words, the user will be able to override the predefined districts.

For instance, the input json may follow this structure:

```json
{
    "city": [
            [
                "m-1", "l-2", "l-3", "s-5", ""
            ],
            [
                
            ]
        ],
    "districts": {
        "m-1": {
            "streets": [
                {
                    "isOneWay": true,
                    "length": 5,
                    "semiCarriageways": [
                        {
                            "lanesAmount": 1,
                            "hasBusStop": false
                        }
                    ]
                }
            ],
            "intersections": [
                {
                    "isRoundabout": false,
                    "streets": [
                        {
                            "hasSemaphore": false
                        }
                    ]
                }
            ]
        }
    }
}
```

We propose this improvement because it is best effort: the customizability of the system increases, but at the same time the contributor doesn't have to modify the core of the system too much.
Additionally, the user will be able to add further predefined districts.

Anyway, the system relies on some conventions about the city organization that have to be taken into account so as to build a district that the simulator can recognize.
