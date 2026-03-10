![Unity](https://img.shields.io/badge/unity-%23000000.svg?style=for-the-badge&logo=unity&logoColor=white)
![Blender](https://img.shields.io/badge/blender-%23F5792A.svg?style=for-the-badge&logo=blender&logoColor=white)
![C%23](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)
---

# Reversed Tower Defense (Tytuł Roboczy)

> Innowacyjne podejście do klasycznego gatunku Tower Defense. Zamiast budować wieże, przejmujesz kontrolę nad armią atakującą! 

Projekt wczesnej fazy koncepcyjnej tworzony w silniku Unity. Gra skupia się na taktycznym planowaniu, optymalizacji zasobów i rywalizacji wieloosobowej.

---

## KONCEPT:
### Rozgrywka i Główny Cel 
W Reversed Tower Defense gracze nie bronią bazy, lecz konfigurują własną armię pojazdów, której celem jest przedarcie się przez linie obronne wroga, zniszczenie wież i dotarcie do celu. 

Tryb Multiplayer:
Gra opiera się na aspekcie rywalizacyjnym. Gracze mierzą się na tych samych mapach, a o ostatecznym zwycięstwie decyduje efektywność:
* Wygrywa gracz, który przejdzie mapę tracąc jak najmniej jednostek lub wydając na to najmniej złota (Golda). 
* Wymaga to ciągłego eksperymentowania ze składem armii i szukania słabych punktów w systemie obronnym mapy.

---

### Defensywa (Wieże Przeciwnika)
Zestawienie struktur obronnych, z którymi będą musiały zmierzyć się Twoje wojska:

* Wieża Podstawowa (Basic Tower): Standardowa jednostka obronna. Cechuje się średnią szybkostrzelnością i średnimi obrażeniami. Posiada mechanikę "skupienia" – atakuje obrany cel tak długo, aż ten nie opuści jej zasięgu.
* Wieża Sonar (Radar Tower): Jednostka wsparcia taktycznego. Wykrywa ukryte pojazdy atakujące i odsłania ich słabe punkty, co znacząco zwiększa obrażenia zadawane im przez pozostałe wieże.
* Wieża Kolcowa (Spike Tower): Specjalizuje się w walce z bliska. Rozstawia uszkadzające kolce i pułapki w swoim bezpośrednim otoczeniu, skutecznie kontrując jednostki odporne na pociski.
* Wieża Armatnia (Cannon Tower): Wymaga ostrożności. Strzela bardzo wolno i ma krótki zasięg, ale rekompensuje to potężnymi, destrukcyjnymi obrażeniami.
* Wieża Plazmowa (Inferno/Plasma Tower): Wieża o rosnącej potędze. Skupia się na jednym celu – im dłużej wiązka plazmy razi ten sam pojazd, tym większe obrażenia zadaje. Idealna kontra na wytrzymałe jednostki.

---

### Ofensywa (Twoja Armia)
Dostępne warianty wozów bojowych, z których gracz komponuje swoją falę uderzeniową:

* Wóz Tank (Czołg): Mobilna tarcza. Jest bardzo powolny i zadaje znikome obrażenia, ale dysponuje ogromną pulą punktów zdrowia (HP). Służy do przyjmowania na siebie ognia, by chronić słabsze jednostki.
* Wóz Podstawowy (Basic): Zbalansowany trzon armii. Posiada średnią pulę HP, zadaje średnie obrażenia i porusza się ze standardową prędkością.
* Wóz Dalekosiężny (Artyleria): Szklane działo. Bardzo mała ilość HP sprawia, że jest niezwykle podatny na ataki, jednak nadrabia to potężnym zasięgiem i solidnymi obrażeniami, pozwalając niszczyć wieże z bezpiecznej odległości.
* Wóz Lustrzany (Mirror Vehicle): Jednostka do zadań specjalnych. Automatycznie odbija wszystkie pociski (projectile) wystrzeliwane przez wieże. Jego główną słabością są obrażenia terytorialne/fizyczne – może zostać łatwo zniszczony przez trapy i kolce.
* Wóz Zasadzka (Kamikaze): Szybka i niebezpieczna jednostka o niskim HP. Jej celem jest dotarcie do wieży i zainicjowanie potężnego wybuchu, który całkowicie ją niszczy. Aby atak się powiódł, pojazd musi uniknąć zniszczenia w drodze do celu (wrażliwy na kolce, pułapki oraz ataki z dystansu po wykryciu przez Sonar).

---

### Autorzy:

* Jakub Błażko
* Michał Lepak
*
