# Algoritmo di movimento dell'auto all'interno di una strada

## Come far seguire ad un veicolo una corsia

### Sfruttando le collisioni tra auto e asfalto

Una strada s ha n corsie in un senso ed m nell'altro senso. Ogni corsia è decorata con un IComponentData provvisto di un indice per distinguerla dalle altre corsie di s. s è un empty object fatto di corsie e due marciapiedi eventualmente.

Il veicolo v segue o cambia corsia cercando di non entrare nelle altre corsie.

Caratteristiche:

- Molte collisioni vengono triggerate, mettendo a dura prova il motore di Unity come voluto;
- L'auto aggiusta l'andatura solo quando almeno una ruota è fuori corsia, cioè tardi o troppo tardi;

### Sfruttando dei binari invisibili

Ogni corsia ha dei binari statici e invisibili a cui il veicolo cerca di avvicinarsi più possibile. Per semplificare l'algoritmo, questo approccio potrà modificare solo la velocità angolare dell'auto.

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

## Metodo per non far investire le auto

Si possono usare tre RayCastHit per ogni auto: due laterali e uno davanti.

## Metodo per far traslare l'auto

Sfruttando le coordinate locali dell'auto (struct LocalToWorld).
