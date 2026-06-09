# BlockHaven City Simulator — Windows EXE sans Unity

BlockHaven City Simulator est maintenant un jeu Windows **C#/.NET 8 WinForms**, sans Unity, sans serveur et sans internet au runtime. GitHub Actions construit automatiquement un `.exe` Windows téléchargeable en artifact.

## Ce que contient le jeu

- Monde voxel/isométrique généré localement avec ville préfaite au spawn.
- Ville style Brookhaven : routes, lampadaires, maison starter, maisons achetables, hôpital, police station, magasin et forêt autour.
- Gameplay Minecraft-like : casser/poser des blocs, inventaire, matériaux, mode survie/créatif.
- Simulation de ville : PNJ avec routine maison → travail → magasin → retour maison, rôles, humeur et argent.
- Économie locale : joueur à 2000$, métiers, salaires, maisons achetables, trésorerie de ville.
- Véhicules : voiture, moto, trafic simple, entrée/sortie.
- Sauvegarde offline exacte dans le dossier de l'exécutable :
  - `Saves/world.json`
  - `Saves/player.json`
  - `Saves/economy.json`
  - `Saves/npcs.json`

## Commandes

- `ZQSD` ou `WASD` : déplacement
- `Shift` : sprint
- Clic gauche : casser le bloc ciblé
- Clic droit : poser le bloc sélectionné
- `1` terre, `2` pierre, `3` bois, `4` planches, `5` verre
- `C` : basculer créatif/survie
- `E` : entrer/sortir d'un véhicule proche
- `B` : acheter une maison proche
- `F1` policier, `F2` médecin, `F3` pompier, `F4` livreur
- `F5` : sauvegarde manuelle

## Télécharger l'exe depuis GitHub Actions

1. Aller dans l'onglet **Actions** du dépôt GitHub.
2. Ouvrir le workflow **Build Windows EXE** le plus récent.
3. Télécharger l'artifact **BlockHavenCitySimulator-windows-x64**.
4. Dézipper et lancer `BlockHavenCitySimulator.exe`.

## Build local Windows

```powershell
dotnet publish src/BlockHavenCitySimulator/BlockHavenCitySimulator.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  --output artifacts/windows-x64
```

Le résultat est dans `artifacts/windows-x64/BlockHavenCitySimulator.exe`.
