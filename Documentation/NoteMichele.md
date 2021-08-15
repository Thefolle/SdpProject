#Note Michele

Gli id delle strade e degli incroci devono andare da 0 al numero di strade ed incroci. es: se si ha solo una strada, questa DEVE avere id = 0, se se ne hanno 2, devono avere 0 ed 1 come id.

Se starting o ending intersection non c'è: VA MESSO, ma con valore = -1.

Incroci quadrati: si possono incrociare strade a 3 corsie con strade a 3 corsie. 2 corsie con 2 corsie. ecc.

Prima strada lungo X

Guarda la sua startingIntersectionId
-> Se non esiste la crei
0-> Genera tutte le strade che ha quella intersezione
  -> Per ogni strada vedi la startigIntersectionId
    -> Se non esiste la crei e vai a 0
  -> Per ogni strada vedi la endingIntersectionId
    -> Se non esiste la crei e vai a 0