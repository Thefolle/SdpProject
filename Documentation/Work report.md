# Work report - version 2

This document is a summary of the work done to develop the project.

## Glossary

Here follows some definitions that are extensively referenced by this document:

- Traditional stack: the well-established and mature approach to create three-dimensional programs in Unity, based on single behaviours;
- DOTS: the Data-Oriented Technology Stack, a novel paradigm in building 3D applications. This is the target approach of this project. The community also talks about hybrid solutions, where mixing a small number of traditional scripts with DOTS is considered acceptable; in the context of this project, instead, the developers tried to stick to the Pure ECS paradigm.

## Why switching to the version 2?

After having explored the Unity ecosystem through the version 1 of this project, we switched to the next version, whose workflow is discussed in this document. The reason of this change was the need to increase the development process speed in exchange for a quite less customizability of the city.

The former version can be considered as an *architectural spike*, namely a prototype that helps in going deep into the specific domain of the project and the overall DOTS ecosystem.

## First design, then develop

First of all, we designed the new traffic simulator system before working on any implementation facet.

### What's the input of the simulator?

We conceived a general city as a matrix made up of square districts. Each district has predefined characteristics in terms of density, arrangement of streets and crosses, as well as spawners and despawners.

The user writes the city matrix in a json file, following [this schema](). The schema is self-documented, so cast a glance at tooltips and comments inside it for any further detail. The user can validate the json file against the schema to probe any syntax mistake.

Once the user has created the city matrix in a file named for instance `city.json`, he loads it into the Unity assets.

### How does the simulator work?

Then, we brainstormed the approach to move cars which is further discussed in [another document](VehicleMovementDesign.md).

The next step consisted of thinking about the inner data structures that represent the city so as to evaluate the minimum path given a start point and an end point. We found that a city can be modeled as a cyclic directed graph. The system builds the graph at the beginning of the simulation. We developed a dedicated library for graph management, since most of the available ones in the community are not compatible with Unity, like the NuGet QuickGraph.

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
