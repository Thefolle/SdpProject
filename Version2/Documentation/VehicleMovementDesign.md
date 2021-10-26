# Vehicle movement algorithm

## How does a car self-assign a lane?

First of all, in the project we strived for distributing the center of decisions in each car, rather than centralizing it in streets or districts. That's indeed what happens in real life: a car doesn't know what's the current state of a whole street, quite just its surrounding environment. As a consequence, each car self-assigns the lane to follow; in other words, no other system or component sets the lane that a car should follow.

The assignment should occur when the car spawns and when it passes from a street (cross) to a cross (street).

The car first needs to know what are the available tracks; for the purpose, it may:

- Inspect the tracks both rightwards and leftwards through a couple of raycasts;
- Query the graph with the current street about the available tracks.

## How should a car follow a given lane?

### By exploiting collisions between the bottom of the car and the asphalt

This solution implies a drawback: a car corrects its trajectory only when it has already overrun in another lane.

### By exploiting invisible tracks

1. The car is bound to tracks: this approach consists of treating cars as trains on their tracks; the car is bound to tracks and follows them accurately.
Although this method would have been easier to implement, it differs from the real life where cars continuously adjust their trajectory to follow the lane.
2. The car move along tracks: in this more realistic situation, the car uses its assigned track just as a reference, like normal drivers: if the car is slightly going away from the center of the lane, it doesn't really matter. When the car is far beyond a given threshold from the track, it adjusts the trajectory.

We *choosed the second option* given that it is simple enough to implement, but still realistic.

Unity DOTS offers a range of mechanisms to develop this solution, as explained beneath.

#### The car moves along tracks by discretizing them in nodes

This is a methodology proposed by the community.
Here, cars follow a track build on the fly by interpolating two successive nodes in which the street is split into.
Although this approach is valid and presents many advantages, we preferred to brainstorm a mechanism on our own. Moreover, interpolating successive nodes doesn't exploit the Physics module furnished with DOTS, quite just the traditional UnityEngine; therefore, the approach does not fit the purpose of this project.

#### The car moves along tracks by exploiting raycasts

*We adopted this solution*.
A raycast is essentially a straight line on which Unity probes and reports the intersections of the raycast itself with other entities. In particular, given the distance and the angle of intersection of the car's raycast with a track, the algorithm decides the behaviour of the car in the context of following or changing lane.

---

## How to make cars change lane?

Given the aforementioned algorithm to make cars follow the assigned lane, extending it to make them change lane is natural. Indeed, the system just changes the refenced lane of a car; as a result, the car approaches the newly-assigned track and then moves along it.

## How to prevent cars to crash each other?

An algorithm that makes cars interact *avoids* that them crashes each other. It could be necessary another algorithm that *prevents* cars from slamming into one another, just in case.

## Come gestire gli incroci

Caratteristiche:

- Come far fermare la prima auto di una coda davanti al semaforo rosso: tramite un muro invisibile associato al semaforo; il muro viene abilitato o disabilitato a seconda dello stato del semaforo e l'auto è provvista del raycast di anti-tamponamento;
- Anche gli incroci sono provvisti di binari, ciascuno per ogni tragitto ammissibile fra la fine di una strada e l'inizio di un'altra; ogni binario è provvisto di un id che l'auto segue fedelmente.

## Calcolo del tragitto

### L'auto conosce il tragitto appena appare

In questo scenario, verosimilmente il conducente conosce già quale tragitto seguire per raggiungere la destinazione.
L'auto, appena spawnata, è già provvista della sequenza di incroci e svolte da effettuare fino al punto di despawn.

Questo algoritmo non necessita di alcuna struttura dati, tuttavia configurarlo puo' essere lungo e tedioso. Inoltre, questo approccio è inadatto in quanto statico rispetto al traffico.

### L'auto non conosce il tragitto appena appare

Il conducente sfrutta le indicazioni del navigatore per arrivare a sapere verso dove svoltare in un incrocio.
Un algoritmo calcola, dati il punto di partenza e la destinazione, il tragitto che l'auto deve effettuare. L'auto pertanto conoscerà il tragitto da effettuare sin da subito grazie all'algoritmo, ma chiederà sul momento a ciascun incrocio qual è il binario che collega la strada corrente a quella seguente.

Questo algoritmo necessita di una struttura dati, un grafo, che rappresenti la città a livello delle strade e degli incroci.
L'auto, appena spawnata, riceve da un algoritmo generale il tragitto da seguire in termini di strade e incroci. Ogni incrocio poi risponderà con il binario che collega una coppia di strade.
