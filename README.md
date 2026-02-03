# JGameFramework

JGameFramework is a modular, project-agnostic gameplay foundation for Unity. Each system can be mixed, adapted, or extended in any kind of project.

## Highlights
- Core utilities for cooldowns, curves, settings, UI helpers, and shared tooling.
- Scene, time, and content management layers plus resource loading helpers.
- Audio, notifications, tooltips, UI theming, and cursor management.
- Inventory, stats, weapons, and effectors for gameplay-heavy projects.
- Demo scenes, templates, prefabs, and samples to accelerate prototyping.

## Automated Setup (recommended)
1) Create or open a Unity project.
2) In **Package Manager -> Add package by name**, install `com.unity.nuget.newtonsoft-json` (Unity Newtonsoft JSON).
3) In your project, add a script at `Assets/Editor/JGProjectSetupWindow.cs`.
4) Copy the contents of `JGameFramework/Docs/JGProjectSetupWindow.cs` from this repository into that script.
5) Back in Unity, open **Tools -> JG -> Project Setup** and click **Run All (A->D)** or step through A->D in order.

### What the setup window does
- Initializes a Git repository (if missing) and creates a Unity-ready `.gitignore`.
- Adds required submodules:
  - `https://github.com/JorGra/JG-UnityEssentials.git` -> `Assets/JG-UnityEssentials`
  - `https://github.com/JorGra/JGameFramework.git` -> `Assets/JGameFramework`
- Installs DOTween from your Asset Store cache or a selected `.unitypackage`, then enables its asmdefs.
- Ensures UPM dependencies in `Packages/manifest.json`:
  - `com.eflatun.scenereference` (`https://github.com/starikcetin/Eflatun.SceneReference.git#4.1.1`)
  - `com.mackysoft.serializereference-extensions` (`https://github.com/mackysoft/Unity-SerializeReferenceExtensions.git?path=Assets/MackySoft/MackySoft.SerializeReferenceExtensions`)
- Requests a package resolve so the project is ready to enter Play Mode.

> The window is idempotent; rerun it anytime to resync submodules or packages.

## Repository Layout
- `Systems/` - Modular runtime systems (Audio, Content, Cursor, Inventory, Notifications, Resources, SceneManagement, Stats, TimeManager, Tooltip, UITheming, Weapon, and more).
- `GameTemplate/` - Starting point assets and project scaffolding.
- `DemoScenes/` & `Samples/` - Examples showing how systems fit together.
- `Prefabs/` & `Resources/` - Reusable building blocks.
- `Docs/JGProjectSetupWindow.cs` - The automated setup editor window (copy this into your project to bootstrap).

## Notes
- Git must be available on your PATH for the setup window to initialize the repo and add submodules.
- Keep a DOTween `.unitypackage` in your Unity Asset Store cache or provide it when prompted during Step C.
- After setup, commit the generated `.gitmodules` and updated `Packages/manifest.json` so teammates can sync cleanly.
