# BlockHaven City Simulator

Projet Unity 2022+ solo offline mélangeant construction voxel, ville préfaite style Brookhaven, économie locale, PNJ à routines, véhicules et sauvegarde locale.

## Fonctionnalités principales

- Monde voxel procédural par chunks optimisés, avec forêts, montagnes, rivière et zone ville.
- Ville préfaite au spawn : routes, lampadaires, maison starter, maisons achetables, hôpital, police station et magasin.
- Gameplay Minecraft-like : casser/poser des blocs, matériaux terre/pierre/bois/planches, mode créatif et survie simple.
- Gameplay city simulator : PNJ citoyens/policiers/médecins/commerçants avec routine maison → travail → magasin → maison.
- Économie locale : joueur commence à 2000$, prix de maisons/voitures, salaires de métiers.
- Véhicules : voiture, moto, trafic PNJ simple, entrée/sortie avec `E`.
- UI runtime inspirée Roblox : HUD argent + aide commandes.
- Sauvegarde offline automatique dans `Application.persistentDataPath/Saves` : `world.json`, `player.json`, `economy.json`, `npcs.json`.

## Commandes

- `ZQSD` ou `WASD` : déplacement FPS
- `Shift` : sprint
- `Space` : saut
- Clic gauche : casser un bloc
- Clic droit : poser un bloc
- `1`-`4` : changer de matériau
- `C` : basculer créatif/survie
- `E` : entrer/sortir véhicule
- `F1`-`F4` : choisir un métier

## Build Windows

Unity n'est pas inclus dans ce dépôt. Pour générer l'exécutable Windows :

1. Ouvrir le dossier avec Unity 2022.3 LTS ou plus récent.
2. Laisser Unity importer les packages.
3. Utiliser le menu `BlockHaven > Build Windows EXE`.
4. L'exécutable est généré dans `Builds/Windows/BlockHavenCitySimulator.exe`.

Le jeu ne dépend d'aucun serveur et fonctionne entièrement en local.
