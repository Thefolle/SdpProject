# Project report

This document is a summary of the work done to develop the traffic simulation project.

## Purpose of the project

The functional requirement of the project consists of a simulator about a city, and in particular its streets and vehicles.
The non-functional requirement is high-scalability in the number of vehicles and in general in the number of entities in the scene. Indeed, the purpose of the project is to evaluate the capabilities of Unity DOTS to support hundreds of thousands of entities in the scene at the same time.

## The underlying technology: Unity DOTS

Unity recently offers two paradigms to build an application:

- Traditional stack: the well-established and mature approach to create three-dimensional (or bi-dimensional) programs in Unity, based on mono behaviours;
- DOTS: the Data-Oriented Technology Stack, a novel paradigm in building 3D (or 2D) applications. This is the target approach of this project.

DOTS is a paradigm that favours scalability in exchange for complexity. Hence, the data-oriented model suits projects that are characterized by a large or huge number of entities that obey to the same algorithm.

The community also talks about hybrid solutions, where mixing a small number of traditional scripts with DOTS is considered acceptable; in the context of this project, instead, the developers tried to stick to the Pure ECS paradigm, although Unity internally creates old-styled scripts anyway for some purposes.

## The first version of the project

The very first phase of the development approached the problem in a very flexible way. Essentially, the whole scene was created through scripts only. On one hand, this solution donated great flexibility since the input could be customized at a fine-grained level; on the other hand, generating all the components of the city on the fly came up to be time-consuming if compared to the results. Hence, the developers at that time acknowledged their lack of experience on Unity and 3D frameworks overall.

## Why switching to the version 2?

After having explored the Unity ecosystem through the version 1 of this project, the developers switched to the next version, whose workflow is discussed in this document. The reason of this change was the need to increase the development process speed in exchange for a quite less customizability of the city.

The former version can be considered as an *architectural spike*, namely a prototype that helps in going deep into the specific domain of a project before proceeding to a complete solution.

## Version 2 characteristics

The second phase of the development process pertained the intent to build and deploy a full-fledged traffic simulator, on top of Unity DOTS. The paramount aspect of the second version is the usage of the physics to allow each component in the scene to interact with each other. The Physics package allows to reach a great degree of realism in the behaviour of the components inside the scene. For instance, cars continuously correct their trajectory along the lane, as in real life, rather than following invisible tracks.

## Why switching to the version 3?

The developers, once again, realized that realism was not a requirement of the project, especially if it thwarts scalability at a very limited threshold. Indeed, using physics means loading Unity with a pletora of computations: collision surfaces and points, raycast and spherecast interpolation, gravity application, force computations and so on. Even if systems were partially but reasonably optimized, the resulting number of cars at steady state was unsatisfying. Systems were not run in parallel and some bugs were in place, but the developers traced back the poor performances to the usage of physics, which is therefore intended for few entities.

## The  simulator

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

Cars receive a random path at spawn time, that guides them from the source street/cross until the destination street/cross; the path is currently filled with streets and crosses choosen randomly.

### The output of the simulator

This section describes the measures taken into account in order to evaluate the simulator, along with their actual values on three different machines (the developers'). It follows a discussion on the collected results.

## Last considerations

### How to enhance the project?

The suggested path comprises the following improvements:

- Wider streets, with more than two lanes;
- Curved streets;
- Uphills and downhills;
- Different types of cars;
- A public transport circuit.

Anyway, the system relies on some conventions about the city organization that have to be taken into account so as to build a district that the simulator can recognize.

Streets and crosses within a district must be mutually linked, by following these conventions:

- Crosses: *in the Unity editor, position the view on the cross to link, such that the global z and the global x are aligned with the local z and local x of the element;*
- Streets: *in the Unity editor, position the view on the street to link, such that the local z points toward the ending cross of the street.*
