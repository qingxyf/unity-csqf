"""Author native Unity SD enemy prefabs from the project's original sprite pairs.

Does not edit images. Run after importing Idle.png / Attack.png in
Assets/Art/Roguelike/Enemies/<archetype>/ to refresh GUIDs, sizes and prefab wiring.
"""
from pathlib import Path
import re
import uuid

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"
NAMESPACE = uuid.UUID("82ff19da-9b6c-4674-913b-c5c6646d6158")


def guid(path):
    return uuid.uuid5(NAMESPACE, path.relative_to(ROOT).as_posix()).hex


def write(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("\n".join(line.rstrip() for line in text.splitlines()) + "\n",
                    encoding="utf-8", newline="\n")


def meta(path, folder=False):
    if Path(str(path) + ".meta").exists():
        return
    write(Path(str(path) + ".meta"), f"fileFormatVersion: 2\nguid: {guid(path)}\n" +
          ("folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n" if folder else
           "DefaultImporter:\n  externalObjects: {}\n"))


def common(game_object):
    return f"""  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {game_object}}}
"""


def main():
    art_root = ASSETS / "Art/Roguelike/Enemies"
    prefab_root = ASSETS / "Resources/Enemies"
    for folder in (ASSETS / "Art", ASSETS / "Art/Roguelike", art_root, prefab_root):
        folder.mkdir(parents=True, exist_ok=True)
        meta(folder, True)
    script = ASSETS / "Scripts/Combat/RoguelikeEnemyPresentation.cs"
    write(Path(str(script) + ".meta"), f"fileFormatVersion: 2\nguid: {guid(script)}\nMonoImporter:\n  externalObjects: {{}}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n")
    template = next(path for path in ASSETS.rglob("*.gif.meta")
                    if "guid: 0b66887db53844e488ccfcca5b6e05da" in path.read_text(encoding="utf-8"))
    texture_meta = template.read_text(encoding="utf-8")

    for name, display_name, profile, height in (
        ("DuskScavenger", "暮路拾荒者", 0, 3.0),
        ("CopperplumeDuelist", "铜羽执刃者", 1, 3.2),
        ("EclipseArchivist", "蚀月档案官", 2, 3.45),
        ("RiceKeeper", "蓝色大肥鱼", 1, 3.2),
    ):
        folder = art_root / name
        folder.mkdir(parents=True, exist_ok=True)
        meta(folder, True)
        idle, attack = folder / "Idle.png", folder / "Attack.png"
        with Image.open(idle) as image:
            assert image.mode == "RGBA", f"{idle} needs true alpha"
            # Generated transparency may contain almost invisible edge pixels.
            # Measure the visible silhouette rather than alpha=1 canvas noise.
            bounds = image.getchannel("A").point(lambda value: 255 if value > 32 else 0).getbbox()
            ppu = (bounds[3] - bounds[1]) / height
        for sprite in (idle, attack):
            assert sprite.exists(), sprite
            pose_ppu = ppu
            if name == "RiceKeeper" and sprite == attack:
                # Independently generated attack art uses a wider canvas. Keep
                # its crouched silhouette close to the idle's world height.
                with Image.open(sprite) as image:
                    attack_bounds = image.getchannel("A").point(lambda value: 255 if value > 32 else 0).getbbox()
                    pose_ppu = (attack_bounds[3] - attack_bounds[1]) / (height * 0.94)
            content = re.sub(r"guid: [0-9a-f]+", f"guid: {guid(sprite)}", texture_meta, count=1)
            content = re.sub(r"spritePixelsToUnits: .*", f"spritePixelsToUnits: {pose_ppu:.4f}", content)
            content = re.sub(r"spriteID: .*", f"spriteID: {guid(sprite)}", content)
            content = content.replace("maxTextureSize: 2048", "maxTextureSize: 1024")
            write(Path(str(sprite) + ".meta"), content)

        content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &100000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 400000}}
  - component: {{fileID: 114000}}
  - component: {{fileID: 114001}}
  - component: {{fileID: 610000}}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &400000
Transform:
{common(100000)}  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_Children:
  - {{fileID: 400001}}
  m_Father: {{fileID: 0}}
--- !u!114 &114000
MonoBehaviour:
{common(100000)}  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 0ad014eb3e86c214fb0c87eca0562d87, type: 3}}
  m_Name:
  m_EditorClassIdentifier:
  enemyName: {display_name}
  maxHealth: 100
  currentHealth: 0
  currentShield: 0
  baseAttack: 10
  hpText: {{fileID: 0}}
--- !u!114 &114001
MonoBehaviour:
{common(100000)}  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid(script)}, type: 3}}
  m_Name:
  m_EditorClassIdentifier:
  body: {{fileID: 212000}}
  idleSprite: {{fileID: 21300000, guid: {guid(idle)}, type: 3}}
  attackSprite: {{fileID: 21300000, guid: {guid(attack)}, type: 3}}
  profile: {profile}
--- !u!61 &610000
BoxCollider2D:
{common(100000)}  m_Enabled: 1
  m_Density: 1
  m_Material: {{fileID: 0}}
  m_IsTrigger: 1
  m_UsedByEffector: 0
  m_UsedByComposite: 0
  m_Offset: {{x: 0, y: 0}}
  serializedVersion: 2
  m_Size: {{x: 1.6, y: 2.8}}
  m_EdgeRadius: 0
--- !u!1 &100001
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 400001}}
  - component: {{fileID: 212000}}
  m_Layer: 0
  m_Name: Visual
  m_TagString: Untagged
  m_IsActive: 1
--- !u!4 &400001
Transform:
{common(100001)}  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_Children: []
  m_Father: {{fileID: 400000}}
--- !u!212 &212000
SpriteRenderer:
{common(100001)}  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_Materials:
  - {{fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 4
  m_Sprite: {{fileID: 21300000, guid: {guid(idle)}, type: 3}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {{x: 1, y: 1}}
  m_WasSpriteAssigned: 1
  m_MaskInteraction: 0
  m_SpriteSortPoint: 0
"""
        prefab = prefab_root / f"{name}.prefab"
        write(prefab, content)
        write(Path(str(prefab) + ".meta"), f"fileFormatVersion: 2\nguid: {guid(prefab)}\nPrefabImporter:\n  externalObjects: {{}}\n")
        if name == "RiceKeeper":
            print(f"{name}: height={height} units, ppu={ppu:.1f}, event challenger prefab wired")
            continue
        battle_name = ("BattleContent", "EliteBattleContent", "BossContent")[profile]
        battle = ASSETS / "Prefabs/NodeContent" / f"{battle_name}.prefab"
        text = battle.read_text(encoding="utf-8")
        text = re.sub(r"enemyPrefab: .*", f"enemyPrefab: {{fileID: 100000, guid: {guid(prefab)}, type: 3}}", text)
        text = re.sub(r"cardsPerTurn: \d+", "cardsPerTurn: 2", text)
        if profile == 2:
            text = re.sub(r"\n  isBossBattle: \d+", "", text)
            text = re.sub(r"isEliteBattle: \d+", "isEliteBattle: 0\n  isBossBattle: 1", text)
        write(battle, text)
        print(f"{name}: height={height} units, ppu={ppu:.1f}, prefab and node wired")


if __name__ == "__main__":
    main()
