# Work report - version 2

## Why switching to the version 2?

After having explored the Unity ecosystem through the version 1 of this project, we switched to the next version, whose workflow is discussed in this document. The reason of this change was the need to increase the development process speed in exchange for less customizability of the city.

## First design, then develop

First of all, we designed the new traffic simulator system before working on any implementation facet.

We conceived a general city as a matrix made up of square districts, each having predefined characteristics that the user can choose through a Json given in input; the user validates the json file against the pertinent Json schema.

Then, we brainstormed the approach to move cars which is further discussed in [another document](VehicleMovementDesign.md).

The next step consisted of thinking about the inner data structures that represent the city so as to evaluate the minimum path given a start point and an end point. We found that a city can be modeled as a cyclic directed graph. The system builds the graph at the beginning of the simulation. We developed a dedicated library for graph management, since most of the available ones in the community are not compatible with Unity, like the NuGet QuickGraph.

## Last considerations

### How to enhance the project?

We thought about how to permit a future contributor to enhance the project. The suggested path is allowing the user to put in a more fine-grained characterization of the city.
In particular, the version 2 of the system should rely upon its version 1, without modifying it. The user, through the usual json, should specify, besides the matrix as planned for system version 1, a further data structure that associates to each item in the matrix a bunch of characteristics. In other words, the user should be able to override the predefined districts.

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
                    "id": 0,
                    "isOneWay": true,
                    "length": 5,
                    "semiCarriageways": [
                        {
                            "lanesAmount": 1,
                            "hasBusStop": false
                        }
                    ],
                    "startingIntersectionId": 1
                }
            ],
            "intersections": [
                {
                    "id": 1,
                    "isRoundabout": false,
                    "streets": [
                        {
                            "id": 0,
                            "hasSemaphore": false
                        }
                    ]
                }
            ]
        }
    }
}
```

We propose this improvement because it is best effort: the system level of customization increases and contemporary the contributor doesn't have to modify the core of the system.
