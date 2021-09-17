# Algoritmo di movimento dell'auto all'interno di una strada

## Come far seguire ad un veicolo una corsia

### Sfruttando le collisioni tra auto e asfalto

Una strada s ha n corsie in un senso ed m nell'altro senso. Ogni corsia è decorata con un IComponentData provvisto di un indice per distinguerla dalle altre corsie di s. s è un empty object fatto di corsie e due marciapiedi eventualmente.

Il veicolo v segue o cambia corsia cercando di non entrare nelle altre corsie.

Caratteristiche:

- Molte collisioni vengono triggerate, mettendo a dura prova il motore di Unity come voluto;
- L'auto aggiusta l'andatura solo quando almeno una ruota è fuori corsia, cioè tardi o troppo tardi;

### Sfruttando dei binari invisibili

Al centro di ogni corsia corre un binario statico e invisibile, munito di indice univoco, a cui il veicolo cerca di avvicinarsi più possibile.

### Sfruttando i raycast

Si montano un paio di raycast nel veicolo, in direzione trasversale e verso il basso. L'auto correggerà l'andatura in base all'oggetto toccato dal raycast.
Questa soluzione:

- Permette di correggere l'andatura dell'auto prima dello sconfinamento;
- E' un metodo retroattivo piuttosto che deterministico;
- Permette di modulare la sterzata in base al punto di intersezione del raycast;
- Le auto sbandano continuamente a destra e a sinistra;

## Come far cambiare corsia ad un auto

### Griglia

Puoi suddividere la strada in una griglia. La griglia sarà virtuale

### Libertà

I raycast di mantenimento nella corsia vengono disattivati finché uno di loro non interseca la corsia voluta; a quel punto, i raycast vengono riattivati. L'auto ottiene una certa angolatura di sterzata all'inizio e solo i raycast di tamponamento rimangono attivi.

- L'arrivo in corsia è innaturale se usato in unione con i raycast per il mantenimento in corsia; questa caratteristica in realtà non importa visto che la parte grafica ha minore importanza;

### Binari

L'algoritmo assegna all'auto un diverso binario, ergo l'auto cercherà di minimizzare la sua distanza da esso; il risultato sarà il cambio di corsi.

## Metodo per non far investire le auto

Si possono usare tre RayCastHit per ogni auto: due laterali e uno davanti.

## Metodo per far traslare l'auto

Sfruttando le coordinate locali dell'auto (struct LocalToWorld).

## Come gestire gli incroci

Caratteristiche:

- Come far fermare la prima auto di una coda davanti al semaforo rosso: tramite un muro invisibile associato al semaforo; il muro viene abilitato o disabilitato a seconda dello stato del semaforo e l'auto è provvista del raycast di anti-tamponamento;
- Anche gli incroci sono provvisti di binari, ciascuno per ogni tragitto ammissibile fra la fine di una strada e l'inizio di un'altra; ogni binario è provvisto di un id che l'auto segue fedelmente.

## Calcolo del tragitto

### L'auto conosce già il tragitto

In questo scenario, verosimilmente il conducente conosce già quale tragitto seguire per raggiungere a destinazione.
L'auto, appena spawnata, è già provvista della sequenza di incroci e svolte da effettuare fino al punto di despawn.

Questo algoritmo non necessita di alcuna struttura dati, tuttavia configurarlo puo' essere lungo e tedioso. Inoltre, questo approccio è inadatto in quanto statico rispetto al traffico.

### L'auto non conosce il tragitto

Il conducente sfrutta le indicazioni del navigatore per arrivare a sapere verso dove svoltare in un incrocio.
Un algoritmo calcola, dati il punto di partenza e la destinazione, il tragitto che l'auto deve effettuare. L'auto pertanto conoscerà il tragitto da effettuare sin da subito grazie all'algoritmo, ma chiederà sul momento a ciascun incrocio qual è il binario che collega la strada corrente a quella seguente.

Questo algoritmo necessita di una struttura dati, un grafo, che rappresenti la città a livello delle strade e degli incroci.
L'auto, appena spawnata, riceve da un algoritmo generale il tragitto da seguire in termini di strade e incroci. Ogni incrocio poi risponderà con il binario che collega una coppia di strade.
