# taptap

A Unity 2D project using the Universal Render Pipeline (URP).

## Requirements

- Unity Hub
- Unity Editor **6000.6.2f1**, as recorded in `ProjectSettings/ProjectVersion.txt`
- Git

## Open the project

1. Clone this repository.
2. In Unity Hub, add the cloned project folder.
3. Open it with Unity Editor 6000.6.2f1. Unity restores packages and imports assets on the first launch.
4. Open `Assets/Scenes/SampleScene.unity`.

## Repository layout

- `Assets/`: scenes, settings, scripts, and assets, including their `.meta` files.
- `Packages/manifest.json`: Unity Package Manager dependencies.
- `Packages/packages-lock.json`: resolved package versions.
- `ProjectSettings/`: shared Unity project settings and editor version.

## Version control

Commit assets together with their `.meta` files. Commit package manifests and
lockfile changes when dependencies change. The project uses Force Text asset
serialization and Visible Meta Files.

Unity caches, logs, local user settings, build output, and generated IDE project
files are ignored and recreated locally. Keep credentials and signing keys out
of the repository.
