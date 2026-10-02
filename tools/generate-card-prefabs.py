#!/usr/bin/env python3
"""Generate deterministic CardData assets and dedicated Resources prefabs.

Run from the Unity project root.  Existing authored assets are only augmented
with their element template artwork; prefab files are copied only when a card
does not already have a dedicated Resources/CardPrefabs entry.
"""
from __future__ import annotations

import hashlib
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CARDS = ROOT / "Assets" / "Resources" / "Cards"
PREFABS = ROOT / "Assets" / "Resources" / "CardPrefabs"

TEMPLATES = {
    0: "魔法闪耀",  # Light
    1: "火焰护盾",  # Fire
    2: "自然守护",  # Nature
    3: "激流冲刷",  # Water
    4: "暗影侵蚀",  # Shadow
    5: "粮草先行",  # Neutral
}

NEW_CARDS = (
    ("晨辉壁垒", 0, 2, "获得10护盾；若已有护盾，下回合回复10生命", "晨辉壁垒"),
    ("焚烬突袭", 1, 2, "造成18点火焰伤害；若目标有[烧伤]，额外造成12点火焰伤害", "焚烬突袭"),
    ("荆棘复苏", 2, 2, "回复12生命并获得8护盾；本回合受击时反击6点生机伤害", "荆棘复苏"),
    ("霜潮回环", 3, 2, "造成14点冰霜伤害并施加[冰霜]；若目标已有[冰霜]，获得1点能量", "霜潮回环"),
    ("幽影收割", 4, 3, "造成20点暗影伤害；目标每有1个负面效果额外造成6点（最多3个），回复基础结算伤害一半", "幽影收割"),
    ("远行补给", 5, 1, "下回合开始时摸1张牌；若本回合能量已用尽，额外获得1点能量", "远行补给"),
)


def guid_for(path: Path) -> str:
    return hashlib.md5(("card-content:" + path.as_posix()).encode("utf-8")).hexdigest()


def yaml_quote(value: str) -> str:
    return value.replace("\\", "\\\\").replace('"', '\\"')


def read_guid(path: Path) -> str:
    return re.search(r"^guid: ([0-9a-f]{32})$", path.read_text(encoding="utf-8"), re.M).group(1)


def template_art(element: int) -> str:
    text = (PREFABS / f"{TEMPLATES[element]}.prefab").read_text(encoding="utf-8")
    return re.search(r"m_Sprite: \{fileID: 21300000, guid: ([0-9a-f]{32}), type: 3\}", text).group(1)


def ensure_meta(path: Path) -> str:
    meta = Path(str(path) + ".meta")
    if not meta.exists():
        meta.write_text(
            "fileFormatVersion: 2\n"
            f"guid: {guid_for(path.relative_to(ROOT))}\n"
            + ("NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n" if path.suffix == ".asset" else "PrefabImporter:\n  externalObjects: {}\n"),
            encoding="utf-8",
        )
    return read_guid(meta)


def create_card(name: str, element: int, cost: int, description: str, effect_id: str) -> None:
    path = CARDS / f"{name}.asset"
    if path.exists():
        return
    path.write_text(
        "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"
        "--- !u!114 &11400000\nMonoBehaviour:\n"
        "  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n"
        "  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
        "  m_Script: {fileID: 11500000, guid: 031bac9c4f7ae6b4690e7cd93659ee70, type: 3}\n"
        f"  m_Name: {yaml_quote(name)}\n  m_EditorClassIdentifier: \n  cardName: {yaml_quote(name)}\n"
        f"  element: {element}\n  description: {yaml_quote(description)}\n  cost: {cost}\n"
        f"  cardArt: {{fileID: 21300000, guid: {template_art(element)}, type: 3}}\n"
        f"  effectId: {yaml_quote(effect_id)}\n  effect: {{fileID: 0}}\n"
        "  upgradeLevel: 0\n  baseCardName: \n",
        encoding="utf-8",
    )
    ensure_meta(path)


def read_scalar(text: str, field: str) -> str:
    match = re.search(rf"^  {field}: ?(.+)$", text, re.M)
    if not match:
        raise ValueError(f"Missing {field}")
    value = match.group(1).strip()
    return json.loads(value) if value.startswith('"') else value


def read_description(text: str) -> str:
    match = re.search(r"^  description: (.*(?:\r?\n    .*)*)", text, re.M)
    if not match:
        raise ValueError("Missing description")
    value = re.sub(r"\r?\n    ", "", match.group(1)).strip()
    if value.startswith('"'):
        value = value[1:-1] if value.endswith('"') else value[1:]
        value = re.sub(r"\\u([0-9a-fA-F]{4})", lambda item: chr(int(item.group(1), 16)), value)
        value = value.replace(r'\"', '"').replace(r"\\", "\\")
    return value


def card_fields(path: Path) -> tuple[str, int, int, str]:
    text = path.read_text(encoding="utf-8")
    element = re.search(r"^  element: (\d+)$", text, re.M)
    cost = re.search(r"^  cost: (\d+)$", text, re.M)
    if not element or not cost:
        raise ValueError(f"Invalid CardData asset: {path}")
    return read_scalar(text, "cardName"), int(element.group(1)), int(cost.group(1)), read_description(text)


def complete_card_serialization(path: Path, name: str, element: int) -> None:
    text = path.read_text(encoding="utf-8")
    if re.search(r"^  cardArt: \{fileID: 0\}$", text, re.M):
        art = f"cardArt: {{fileID: 21300000, guid: {template_art(element)}, type: 3}}"
        text = re.sub(r"^  cardArt: \{fileID: 0\}$", "  " + art, text, flags=re.M)
    elif not re.search(r"^  cardArt:", text, re.M):
        art = f"cardArt: {{fileID: 21300000, guid: {template_art(element)}, type: 3}}"
        text = re.sub(r"^(  cost: .*\n)", r"\1  " + art + "\n", text, count=1, flags=re.M)

    if re.search(r"^  effectId: ?$", text, re.M):
        text = re.sub(r"^  effectId: ?$", "  effectId: " + yaml_quote(name), text, flags=re.M)
    elif not re.search(r"^  effectId:", text, re.M):
        text = re.sub(r"^(  cardArt: .*\n)", r"\1  effectId: " + yaml_quote(name) + "\n", text, count=1, flags=re.M)

    path.write_text("\n".join(line.rstrip() for line in text.splitlines()) + "\n", encoding="utf-8")


def write_prefab(name: str, element: int, card_guid: str) -> None:
    target = PREFABS / f"{name}.prefab"
    if target.exists():
        return
    template = PREFABS / f"{TEMPLATES[element]}.prefab"
    text = template.read_text(encoding="utf-8")
    template_name = TEMPLATES[element]
    text = text.replace(f"m_Name: {template_name}", f"m_Name: {name}", 1)
    text = re.sub(
        r"cardData: \{fileID: 11400000, guid: [0-9a-f]{32}, type: 2\}",
        f"cardData: {{fileID: 11400000, guid: {card_guid}, type: 2}}",
        text,
        count=1,
    )
    target.write_text(text, encoding="utf-8")
    ensure_meta(target)


CARD_DISPLAY_GUID = "64c9ace11143fd04db352025fbaf0002"
DISPLAY_FIELDS = ("nameText", "descriptionText", "costText", "cardData")


def display_blocks(text: str):
    pattern = re.compile(r"--- !u!114 &(\d+)\nMonoBehaviour:(.*?)(?=--- !u!|\Z)", re.S)
    return [match for match in pattern.finditer(text) if CARD_DISPLAY_GUID in match.group(2)]


def field_value(block: str, field: str) -> str | None:
    match = re.search(rf"^  {field}: (.*)$", block, re.M)
    return match.group(1) if match else None


def remove_empty_card_displays(text: str) -> str:
    for match in reversed(display_blocks(text)):
        component_id, block = match.group(1), match.group(2)
        if all(field_value(block, field) == "{fileID: 0}" for field in DISPLAY_FIELDS):
            text = text[:match.start()] + text[match.end():]
            text = text.replace(f"  - component: {{fileID: {component_id}}}\n", "", 1)
    return text


def update_game_object_name(text: str, game_object_id: str, name: str) -> str:
    pattern = re.compile(
        rf"(--- !u!1 &{game_object_id}\nGameObject:(?:(?!^--- !u!).)*?^  m_Name: )[^\r\n]*",
        re.S | re.M,
    )
    return pattern.sub(lambda match: match.group(1) + json.dumps(name, ensure_ascii=True), text, count=1)


def update_tmp_text(text: str, component_id: str, value: str) -> str:
    pattern = re.compile(
        rf"(--- !u!114 &{component_id}\nMonoBehaviour:(?:(?!^--- !u!).)*?^  m_text: )[^\r\n]*",
        re.S | re.M,
    )
    return pattern.sub(lambda match: match.group(1) + json.dumps(value, ensure_ascii=True), text, count=1)


def sync_prefab(name: str, card_guid: str, cost: int, description: str) -> None:
    path = PREFABS / f"{name}.prefab"
    text = remove_empty_card_displays(path.read_text(encoding="utf-8"))
    displays = display_blocks(text)
    if len(displays) != 1:
        raise ValueError(f"Expected one CardDisplay in {path}, found {len(displays)}")

    display = displays[0].group(2)
    game_object = field_value(display, "m_GameObject")
    game_object_id = re.search(r"fileID: (\d+)", game_object or "").group(1)
    text = update_game_object_name(text, game_object_id, name)

    text = re.sub(
        r"(cardData: \{fileID: 11400000, guid: )[0-9a-f]{32}(, type: 2\})",
        lambda match: match.group(1) + card_guid + match.group(2),
        text,
        count=1,
    )
    for field, value in (("nameText", name), ("descriptionText", description), ("costText", str(cost))):
        component = field_value(display, field)
        component_id = re.search(r"fileID: (\d+)", component or "").group(1)
        text = update_tmp_text(text, component_id, value)

    path.write_text("\n".join(line.rstrip() for line in text.splitlines()) + "\n", encoding="utf-8")


def rebuild_prefab_from_element_template(name: str, element: int) -> None:
    path = PREFABS / f"{name}.prefab"
    template = PREFABS / f"{TEMPLATES[element]}.prefab"
    if path == template:
        return

    # Resource prefabs share their element layout.  Rebuilding from the
    # canonical element template repairs legacy layout-only prefabs and keeps
    # every card on the same display contract without changing its .meta GUID.
    path.write_text(template.read_text(encoding="utf-8"), encoding="utf-8")


def rename_divine_smite_prefab() -> None:
    old = PREFABS / "神圣惩戒.prefab"
    new = PREFABS / "神圣惩击.prefab"
    if old.exists() and not new.exists():
        old.rename(new)
        Path(str(old) + ".meta").rename(Path(str(new) + ".meta"))
        text = new.read_text(encoding="utf-8")
        new.write_text(text.replace("m_Name: 神圣惩戒", "m_Name: 神圣惩击", 1), encoding="utf-8")


def main() -> None:
    for name, element, cost, description, effect_id in NEW_CARDS:
        create_card(name, element, cost, description, effect_id)

    rename_divine_smite_prefab()
    for card_path in sorted(CARDS.glob("*.asset")):
        name, element, cost, description = card_fields(card_path)
        complete_card_serialization(card_path, name, element)
        card_guid = ensure_meta(card_path)
        write_prefab(name, element, card_guid)
        rebuild_prefab_from_element_template(name, element)
        sync_prefab(name, card_guid, cost, description)


if __name__ == "__main__":
    main()
