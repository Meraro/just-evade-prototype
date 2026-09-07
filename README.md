# Just Evade Prototype - Public Source

Public, portfolio-oriented source for a Unity boss-combat prototype.

## Published Scope

- Runtime and editor scripts for boss combat, player combat, root motion, and Just Evade.
- Design, implementation, and portfolio documentation.
- Input System configuration and Unity package/version metadata.

## Explicitly Not Included

- Proprietary Asset Store and Mixamo models, animations, VFX, and derived animation clips.
- X/Y Bot models and third-party animation-controller dependencies.
- The playable scene and serialized Animator/Profile assets that reference those dependencies.

This repository is an architectural code sample, not a standalone playable Unity project.

## Reproduction

1. Review `Documentation/BossBattle` for the chronological implementation notes.
2. Import legally acquired assets into a local Unity 6 project.
3. Recreate scene references and editable working clips locally; use the included editor scripts to synchronize attack events.
