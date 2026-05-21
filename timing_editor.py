#!/usr/bin/env python3
"""
Timing Editor - Visual editor for Unity LEDRing TimingCollection data.

Usage:
    pip install flask
    python timing_editor.py

Paste the exported text (from TimingCollection.Export()) into the left panel,
click Load, then interact with the timing tree on the right.
"""

import json
import re
import sys
import webbrowser
import threading
from typing import Optional

from flask import Flask, request, jsonify

app = Flask(__name__)

# =============================================================================
# Constants
# =============================================================================

VALID_METHODS = ["sec", "trialStart", "trialEnd", "trialInTarget", "IPCConnect"]
VALID_TYPES = ["button", "dropdown", "other"]
METHODS_NEEDING_VALUE = {"sec", "trialStart", "trialEnd", "trialInTarget"}
SEPARATOR = "|JR|"

# Valid names extracted from ControlsParse switch cases in UIUpdate.cs
# name -> type mapping (button / dropdown)
# 后续添加允许外挂字典
VALID_NAMES = {
    "StartButton": "button",
    "WaitButton": "button",
    "FinishButton": "button",
    "SkipButton": "button",
    "ExitButton": "button",
    "ModeSelect": "dropdown",
    "TriggerModeSelect": "dropdown",
    "InfraRedIn": "button",
    "PressLever": "button",
    "DebugButton": "button",
    "IPCRefreshButton": "button",
    "IPCDisconnect": "button",
    "OpenPythonScript": "button",
    "ClosePythonScript": "button",
    "PageUp": "button",
    "PageDown": "button",
    "BackgroundSwitch": "dropdown",
    "TimingBlank": "button",
    "TimingConfigExoprt": "button",
    "IFTimingValue": "button",
    "IFTimingSet": "button",
    "TimingPause": "button",
    "MessagePost": "button",
    "LogeventStart": "button",
    "LogeventEnd": "button",
}

# =============================================================================
# Data Model
# =============================================================================


class TimingNode:
    """Mirrors the C# Timing struct."""

    def __init__(
        self,
        id: int,
        type: str = "button",
        name: str = "",
        hierarchy: int = 0,
        timing_method: str = "",
        parent_id: int = -1,
        parent_name: str = "",
        value: float = 0.0,
    ):
        self.id = id
        self.type = type
        self.name = name
        self.hierarchy = hierarchy
        self.timing_method = timing_method
        self.parent_id = parent_id
        self.parent_name = parent_name
        self.value = value

    @property
    def method_type(self) -> str:
        """Extract the timing method (sec, trialStart, etc.) from timingMethod."""
        try:
            parts = self.timing_method.rstrip(";").split(";")
            if len(parts) < 2:
                return ""
            inner = parts[1]  # {name}:{method}:{value}
            return inner.split(":")[1]
        except (IndexError, AttributeError):
            return ""

    @property
    def method_value_str(self) -> str:
        """Extract the method value from timingMethod."""
        try:
            parts = self.timing_method.rstrip(";").split(";")
            if len(parts) < 2:
                return ""
            inner = parts[1]
            return inner.split(":")[2]
        except (IndexError, AttributeError):
            return ""

    def rebuild_timing_method(self, method_type: str, method_val: str) -> str:
        """Rebuild timingMethod string from components."""
        return f"type_{self.type};{self.name}:{method_type}:{method_val};"

    def to_dict(self) -> dict:
        return {
            "id": self.id,
            "type": self.type,
            "name": self.name,
            "hierarchy": self.hierarchy,
            "timingMethod": self.timing_method,
            "parentId": self.parent_id,
            "parentName": self.parent_name,
            "value": self.value,
            "methodType": self.method_type,
            "methodValue": self.method_value_str,
        }

    @classmethod
    def from_dict(cls, d: dict) -> "TimingNode":
        return cls(
            id=d.get("Id", d.get("id", 0)),
            type=d.get("type", "button"),
            name=d.get("name", ""),
            hierarchy=d.get("hierarchy", 0),
            timing_method=d.get("timingMethod", ""),
            parent_id=d.get("parentId", -1),
            parent_name=d.get("parentName", ""),
            value=d.get("value", 0.0),
        )


class TimingTree:
    """Manages the collection of TimingNodes as a tree."""

    def __init__(self):
        self.nodes: dict[int, TimingNode] = {}
        self.max_id = 0

    # ---- Parse / Export ----

    def parse(self, text: str) -> list[str]:
        """Parse exported text. Returns list of errors (empty = success)."""
        text = text.strip()
        if not text:
            self.nodes.clear()
            self.max_id = 0
            return []

        # Try alternate separators
        if "||JSON_RECORD||" in text:
            parts = text.split("||JSON_RECORD||")
        else:
            parts = text.split(SEPARATOR)

        errors = []
        self.nodes.clear()
        self.max_id = 0

        for i, part in enumerate(parts):
            part = part.strip()
            if not part:
                continue
            try:
                d = json.loads(part)
                node = TimingNode.from_dict(d)
                if node.id in self.nodes:
                    errors.append(f"Duplicate Id {node.id} at segment {i}")
                    continue
                self.nodes[node.id] = node
                self.max_id = max(self.max_id, node.id)
            except json.JSONDecodeError as e:
                errors.append(f"JSON parse error at segment {i}: {e}")
                errors.append(f"  Content: {part[:80]}...")

        self._rebuild_parent_names()
        return errors

    def export(self) -> str:
        """Export all nodes to |JR|-separated JSON string (PascalCase for C# compatibility)."""
        return SEPARATOR.join(
            json.dumps(self._to_export_dict(node), ensure_ascii=False)
            for node in self.nodes.values()
        )

    @staticmethod
    def _to_export_dict(node: TimingNode) -> dict:
        """Serialize in PascalCase to match C# Newtonsoft.Json format."""
        return {
            "type": node.type,
            "name": node.name,
            "hierarchy": node.hierarchy,
            "timingMethod": node.timing_method,
            "Id": node.id,
            "parentId": node.parent_id,
            "parentName": node.parent_name,
            "value": node.value,
        }

    # ---- Tree structure ----

    def to_tree_json(self) -> list[dict]:
        """Build nested tree for frontend."""
        children_map: dict[int, list[TimingNode]] = {}
        for node in self.nodes.values():
            children_map.setdefault(node.parent_id, []).append(node)

        def build(node: TimingNode) -> dict:
            d = node.to_dict()
            kids = children_map.get(node.id, [])
            kids.sort(key=lambda x: x.id)
            d["children"] = [build(c) for c in kids]
            return d

        roots = children_map.get(-1, [])
        roots.sort(key=lambda x: x.id)
        return [build(r) for r in roots]

    def from_flat_list(self, nodes_list: list[dict]) -> list[str]:
        """Import from flat list (from frontend). Returns errors."""
        self.nodes.clear()
        self.max_id = 0
        for d in nodes_list:
            node = TimingNode.from_dict(d)
            self.nodes[node.id] = node
            self.max_id = max(self.max_id, node.id)
        self._rebuild_parent_names()
        return self.validate()

    # ---- CRUD operations ----

    def add_node(
        self,
        action: str,
        ref_id: int,
        name: str = "",
        node_type: str = "button",
        method_type: str = "sec",
        method_value: str = "0",
        value: float = 0.0,
        strict: bool = False,
    ) -> tuple[Optional[TimingNode], list[str]]:
        """
        Add a new node.
        action: "root", "child", "before", "after"
        """
        errors = []
        if not name.strip():
            errors.append("Name is required")
        if node_type not in VALID_TYPES:
            # Allow custom types
            pass
        if method_type not in VALID_METHODS:
            errors.append(f"Invalid method: {method_type}. Valid: {', '.join(VALID_METHODS)}")
        if method_type in METHODS_NEEDING_VALUE:
            try:
                float(method_value)
            except ValueError:
                errors.append(f"Method value must be a number for '{method_type}'")

        if errors:
            return None, errors

        if strict:
            errors += self.validate_strict(name, node_type)
        if errors:
            return None, errors

        new_id = self.max_id + 1
        parent_id = -1
        hierarchy = 0

        if action == "root":
            parent_id = -1
            hierarchy = 0

        elif action == "child":
            if ref_id not in self.nodes:
                return None, [f"Reference node {ref_id} not found"]
            parent_id = ref_id
            ref_node = self.nodes[ref_id]
            hierarchy = ref_node.hierarchy + 1

        elif action == "before":
            if ref_id not in self.nodes:
                return None, [f"Reference node {ref_id} not found"]
            ref_node = self.nodes[ref_id]
            # New node takes ref's place under ref's parent
            parent_id = ref_node.parent_id
            hierarchy = ref_node.hierarchy
            # Ref becomes child of new node
            ref_node.parent_id = new_id
            # ref's hierarchy will be recalculated

        elif action == "after":
            if ref_id not in self.nodes:
                return None, [f"Reference node {ref_id} not found"]
            ref_node = self.nodes[ref_id]
            # New node becomes child of ref
            parent_id = ref_id
            hierarchy = ref_node.hierarchy + 1
            # Ref's existing children become children of new node
            for node in self.nodes.values():
                if node.parent_id == ref_id:
                    node.parent_id = new_id

        else:
            return None, [f"Unknown action: {action}"]

        timing_method = f"type_{node_type};{name}:{method_type}:{method_value};"
        node = TimingNode(
            id=new_id,
            type=node_type,
            name=name,
            hierarchy=hierarchy,
            timing_method=timing_method,
            parent_id=parent_id,
            parent_name="",
            value=value,
        )
        self.nodes[new_id] = node
        self.max_id = new_id

        self._rebuild_parent_names()
        self._recalculate_hierarchies()
        return node, []

    def edit_node(self, node_id: int, strict: bool = False, **fields) -> tuple[Optional[TimingNode], list[str]]:
        """Edit an existing node. Returns (updated_node, errors)."""
        if node_id not in self.nodes:
            return None, [f"Node {node_id} not found"]

        node = self.nodes[node_id]
        errors = []

        # Update simple fields
        if "name" in fields:
            name = fields["name"].strip()
            if not name:
                errors.append("Name cannot be empty")
            else:
                node.name = name

        if "type" in fields:
            node.type = fields["type"]

        if "value" in fields:
            try:
                node.value = float(fields["value"])
            except (ValueError, TypeError):
                errors.append("Value must be a number")

        if strict:
            effective_name = fields.get("name", node.name).strip()
            effective_type = fields.get("type", node.type)
            errors += self.validate_strict(effective_name, effective_type)

        # Update parent
        if "parentId" in fields:
            new_parent = int(fields["parentId"])
            if new_parent != -1 and new_parent not in self.nodes:
                errors.append(f"Parent node {new_parent} not found")
            elif new_parent == node_id:
                errors.append("A node cannot be its own parent")
            elif self._would_create_cycle(node_id, new_parent):
                errors.append("This would create a circular reference")
            else:
                node.parent_id = new_parent

        # Rebuild timingMethod from parts
        method_type = fields.get("methodType", node.method_type)
        method_value = fields.get("methodValue", node.method_value_str)
        if method_type not in VALID_METHODS:
            errors.append(f"Invalid method: {method_type}")
        if method_type in METHODS_NEEDING_VALUE:
            try:
                float(method_value)
            except ValueError:
                errors.append(f"Method value must be a number for '{method_type}'")

        if not errors:
            node.timing_method = node.rebuild_timing_method(method_type, method_value)
            self._rebuild_parent_names()
            self._recalculate_hierarchies()

        return node, errors

    def delete_node(self, node_id: int, mode: str) -> tuple[list[dict], list[str]]:
        """
        Delete a node.
        mode: "cascade" - remove node and all descendants
              "promote" - remove node, children move up one level
        Returns (removed_nodes, errors).
        """
        if node_id not in self.nodes:
            return [], [f"Node {node_id} not found"]

        if mode == "cascade":
            removed = self._delete_cascade(node_id)
        elif mode == "promote":
            removed = self._delete_promote(node_id)
        else:
            return [], [f"Unknown delete mode: {mode}"]

        self._rebuild_parent_names()
        self._recalculate_hierarchies()

        if not self.nodes:
            self.max_id = 0

        return [n.to_dict() for n in removed], []

    def _delete_cascade(self, node_id: int) -> list[TimingNode]:
        """Delete node and all its descendants."""
        removed = []
        to_remove = {node_id}

        # Collect all descendants
        changed = True
        while changed:
            changed = False
            for nid, node in self.nodes.items():
                if nid not in to_remove and node.parent_id in to_remove:
                    to_remove.add(nid)
                    changed = True

        for nid in sorted(to_remove):
            removed.append(self.nodes.pop(nid))

        return removed

    def _delete_promote(self, node_id: int) -> list[TimingNode]:
        """Delete node, promote its children to take its place."""
        deleted_node = self.nodes[node_id]
        removed = [self.nodes.pop(node_id)]

        # Children of deleted node: move parentId up to deleted's parent
        # Grandchildren: also shift up (handled by _recalculate_hierarchies later)
        children_to_promote = [
            n for n in self.nodes.values() if n.parent_id == node_id
        ]
        for child in children_to_promote:
            child.parent_id = deleted_node.parent_id

        return removed

    def move_node(self, node_id: int, new_parent_id: int) -> tuple[Optional[TimingNode], list[str]]:
        """Move node (and its subtree) to a new parent. Returns (node, errors)."""
        if node_id not in self.nodes:
            return None, [f"Node {node_id} not found"]
        if new_parent_id != -1 and new_parent_id not in self.nodes:
            return None, [f"Target parent {new_parent_id} not found"]
        if node_id == new_parent_id:
            return None, ["A node cannot be its own parent"]
        if self._would_create_cycle(node_id, new_parent_id):
            return None, ["This would create a circular reference"]

        node = self.nodes[node_id]
        node.parent_id = new_parent_id
        self._rebuild_parent_names()
        self._recalculate_hierarchies()
        return node, []

    @staticmethod
    def validate_strict(name: str, node_type: str) -> list[str]:
        """Validate a name/type pair in strict mode. Returns list of errors."""
        errors = []
        if name not in VALID_NAMES:
            errors.append(f"'{name}' is not a recognized element name")
        else:
            expected_type = VALID_NAMES[name]
            if node_type != expected_type:
                errors.append(f"'{name}' expects type '{expected_type}', got '{node_type}'")
        return errors

    # ---- Validation ----

    def validate(self) -> list[str]:
        """Validate the entire tree. Returns list of errors."""
        errors = []

        if not self.nodes:
            return errors

        # Id uniqueness (guaranteed by dict, but check for negative)
        for nid in self.nodes:
            if nid < 0:
                errors.append(f"Invalid negative Id: {nid}")

        # MaxId consistency
        if self.max_id < max(self.nodes.keys()):
            errors.append(
                f"maxId ({self.max_id}) is less than max node id ({max(self.nodes.keys())})"
            )

        # Per-node checks
        for node in self.nodes.values():
            # Name required
            if not node.name.strip():
                errors.append(f"Node [{node.id}]: name is empty")

            # ParentId must be -1 or exist
            if node.parent_id != -1 and node.parent_id not in self.nodes:
                errors.append(
                    f"Node [{node.id}] '{node.name}': parentId={node.parent_id} does not exist"
                )

            # Self-parent
            if node.parent_id == node.id:
                errors.append(
                    f"Node [{node.id}] '{node.name}': cannot be its own parent"
                )

            # Hierarchy vs parent
            if node.parent_id == -1:
                if node.hierarchy != 0:
                    errors.append(
                        f"Node [{node.id}] '{node.name}': root node should have hierarchy=0, got {node.hierarchy}"
                    )
            else:
                parent = self.nodes.get(node.parent_id)
                if parent and node.hierarchy != parent.hierarchy + 1:
                    errors.append(
                        f"Node [{node.id}] '{node.name}': hierarchy={node.hierarchy} "
                        f"but parent '{parent.name}' has hierarchy={parent.hierarchy}"
                    )

            # timingMethod format
            if node.timing_method:
                if not re.match(
                    r"^type_[^;]*;[^:]+:[^:]+:[^;]*;?$", node.timing_method
                ):
                    errors.append(
                        f"Node [{node.id}] '{node.name}': timingMethod format looks invalid: "
                        f"'{node.timing_method[:60]}'"
                    )
                else:
                    mt = node.method_type
                    if mt and mt not in VALID_METHODS:
                        errors.append(
                            f"Node [{node.id}] '{node.name}': unknown method '{mt}'"
                        )

        # Circular reference check
        for node in self.nodes.values():
            cycle = self._find_cycle(node.id)
            if cycle:
                cycle_str = " -> ".join(str(nid) for nid in cycle)
                errors.append(f"Circular reference detected: {cycle_str}")
                break  # One cycle report is enough

        return errors

    def _would_create_cycle(self, node_id: int, new_parent_id: int) -> bool:
        """Check if setting node_id's parent to new_parent_id would create a cycle."""
        if new_parent_id == -1:
            return False
        # Walk up from new_parent_id; if we reach node_id, it's a cycle
        visited = set()
        current = new_parent_id
        while current != -1 and current not in visited:
            if current == node_id:
                return True
            visited.add(current)
            parent = self.nodes.get(current)
            if parent is None:
                break
            current = parent.parent_id
        return False

    def _find_cycle(self, start_id: int) -> list[int]:
        """Detect cycle starting from a node. Returns cycle path or empty list."""
        path = []
        visited = set()
        current = start_id
        while current != -1:
            if current in visited:
                idx = path.index(current)
                return path[idx:] + [current]
            if current not in self.nodes:
                break
            visited.add(current)
            path.append(current)
            current = self.nodes[current].parent_id
        return []

    # ---- Internal helpers ----

    def _recalculate_hierarchies(self):
        """Recalculate hierarchy for all nodes based on parent relationships."""
        if not self.nodes:
            return

        # Build children map
        children_map: dict[int, list[int]] = {}
        for nid, node in self.nodes.items():
            children_map.setdefault(node.parent_id, []).append(nid)

        # BFS from roots
        queue = [(nid, 0) for nid in children_map.get(-1, [])]
        visited = set()

        while queue:
            nid, depth = queue.pop(0)
            if nid in visited:
                continue
            visited.add(nid)
            if nid in self.nodes:
                self.nodes[nid].hierarchy = depth
            for child_id in children_map.get(nid, []):
                if child_id not in visited:
                    queue.append((child_id, depth + 1))

    def _rebuild_parent_names(self):
        """Auto-fill parentName from parent node's name."""
        for node in self.nodes.values():
            if node.parent_id == -1:
                node.parent_name = ""
            elif node.parent_id in self.nodes:
                node.parent_name = self.nodes[node.parent_id].name
            else:
                node.parent_name = ""


# Global tree instance (single-user tool)
tree = TimingTree()

# =============================================================================
# API Routes
# =============================================================================


@app.route("/")
def index():
    return HTML_TEMPLATE


@app.route("/api/load", methods=["POST"])
def api_load():
    data = request.get_json()
    text = data.get("text", "")
    errors = tree.parse(text)
    return jsonify({"tree": tree.to_tree_json(), "errors": errors})


@app.route("/api/tree", methods=["GET"])
def api_get_tree():
    return jsonify({"tree": tree.to_tree_json(), "errors": tree.validate()})


@app.route("/api/node/add", methods=["POST"])
def api_node_add():
    data = request.get_json()
    action = data.get("action", "root")
    ref_id = data.get("refId", -1)
    strict = data.get("strict", False)

    node, errors = tree.add_node(
        action=action,
        ref_id=ref_id,
        name=data.get("name", ""),
        node_type=data.get("type", "button"),
        method_type=data.get("methodType", "sec"),
        method_value=str(data.get("methodValue", "0")),
        value=data.get("value", 0.0),
        strict=strict,
    )

    if errors:
        return jsonify({"success": False, "errors": errors}), 400

    return jsonify({
        "success": True,
        "node": node.to_dict() if node else None,
        "tree": tree.to_tree_json(),
        "errors": tree.validate(),
    })


@app.route("/api/node/<int:node_id>", methods=["PUT"])
def api_node_edit(node_id):
    data = request.get_json()
    strict = data.pop("strict", False)
    node, errors = tree.edit_node(node_id, strict=strict, **data)

    if errors and not node:
        return jsonify({"success": False, "errors": errors}), 400

    return jsonify({
        "success": True,
        "node": node.to_dict() if node else None,
        "tree": tree.to_tree_json(),
        "errors": errors or tree.validate(),
    })


@app.route("/api/node/move", methods=["POST"])
def api_node_move():
    data = request.get_json()
    node_id = data.get("nodeId")
    new_parent_id = data.get("newParentId", -1)
    node, errors = tree.move_node(node_id, new_parent_id)
    if errors:
        return jsonify({"success": False, "errors": errors}), 400
    return jsonify({
        "success": True,
        "node": node.to_dict() if node else None,
        "tree": tree.to_tree_json(),
        "errors": tree.validate(),
    })


@app.route("/api/nodes/move-batch", methods=["POST"])
def api_nodes_move_batch():
    data = request.get_json()
    node_ids = data.get("nodeIds", [])
    new_parent_id = data.get("newParentId", -1)
    moved = []
    errors = []
    for nid in node_ids:
        node, errs = tree.move_node(nid, new_parent_id)
        if node:
            moved.append(node.to_dict())
        else:
            errors += errs
    return jsonify({
        "success": len(errors) == 0,
        "moved": moved,
        "tree": tree.to_tree_json(),
        "errors": errors or tree.validate(),
    })


@app.route("/api/valid-names", methods=["GET"])
def api_valid_names():
    return jsonify({"names": VALID_NAMES})


@app.route("/api/node/<int:node_id>", methods=["DELETE"])
def api_node_delete(node_id):
    mode = request.args.get("mode", "cascade")
    if mode not in ("cascade", "promote"):
        return jsonify({"success": False, "errors": [f"Invalid mode: {mode}"]}), 400

    removed, errors = tree.delete_node(node_id, mode)
    if errors:
        return jsonify({"success": False, "errors": errors}), 400

    return jsonify({
        "success": True,
        "removed": removed,
        "tree": tree.to_tree_json(),
        "errors": tree.validate(),
    })


@app.route("/api/export", methods=["GET"])
def api_export():
    text = tree.export()
    errors = tree.validate()
    return jsonify({"text": text, "errors": errors})


@app.route("/api/validate", methods=["GET"])
def api_validate():
    return jsonify({"errors": tree.validate()})


# =============================================================================
# HTML Template
# =============================================================================

HTML_TEMPLATE = r"""<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Timing Editor</title>
<style>
:root {
    --bg: #1a1a2e;
    --surface: #16213e;
    --surface2: #0f3460;
    --border: #2a2a4a;
    --text: #e0e0e0;
    --text-dim: #888;
    --accent: #4fc3f7;
    --accent2: #81c784;
    --danger: #ef5350;
    --warn: #ffb74d;
    --node-btn: #4fc3f7;
    --node-dropdown: #81c784;
    --node-other: #b0b0b0;
    --radius: 6px;
    --shadow: 0 2px 8px rgba(0,0,0,0.3);
}
* { box-sizing: border-box; margin: 0; padding: 0; }
body {
    font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
    background: var(--bg);
    color: var(--text);
    height: 100vh;
    overflow: hidden;
    display: flex;
    flex-direction: column;
}
header {
    background: var(--surface);
    padding: 8px 16px;
    border-bottom: 1px solid var(--border);
    display: flex;
    align-items: center;
    gap: 12px;
    flex-shrink: 0;
}
header h1 { font-size: 16px; font-weight: 600; }
header .badge {
    font-size: 11px;
    padding: 2px 8px;
    border-radius: 10px;
    background: var(--surface2);
    color: var(--text-dim);
}
.main {
    display: flex;
    flex: 1;
    overflow: hidden;
}
.panel-left {
    width: 320px;
    flex-shrink: 0;
    display: flex;
    flex-direction: column;
    border-right: 1px solid var(--border);
    background: var(--surface);
}
.panel-left .section {
    padding: 8px 12px;
    border-bottom: 1px solid var(--border);
}
.panel-left .section-title {
    font-size: 11px;
    text-transform: uppercase;
    color: var(--text-dim);
    margin-bottom: 6px;
    letter-spacing: 0.5px;
}
.panel-left textarea {
    width: 100%;
    height: 180px;
    background: var(--bg);
    color: var(--text);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 8px;
    font-family: 'Cascadia Code', 'Fira Code', 'Consolas', monospace;
    font-size: 12px;
    resize: vertical;
    outline: none;
}
.panel-left textarea:focus { border-color: var(--accent); }
.btn-row {
    display: flex;
    gap: 6px;
    flex-wrap: wrap;
}
.btn {
    padding: 6px 14px;
    border: 1px solid var(--border);
    border-radius: var(--radius);
    background: var(--surface2);
    color: var(--text);
    cursor: pointer;
    font-size: 12px;
    white-space: nowrap;
    transition: background 0.15s;
}
.btn:hover { background: #1a4980; }
.btn.primary { background: #1565c0; border-color: #1565c0; }
.btn.primary:hover { background: #1976d2; }
.btn.export-btn { background: #2e7d32; border-color: #2e7d32; }
.btn.export-btn:hover { background: #388e3c; }
.errors-box {
    flex: 1;
    overflow-y: auto;
    padding: 8px 12px;
    font-size: 12px;
    font-family: monospace;
}
.error-item {
    padding: 4px 0;
    border-bottom: 1px solid #2a2a2a;
}
.error-item.err { color: var(--danger); }
.error-item.warn { color: var(--warn); }
.error-item.ok { color: var(--accent2); }
.panel-right {
    flex: 1;
    overflow: auto;
    padding: 20px 24px;
}
.tree-container { min-width: max-content; }

/* ── Tree lines ── */
.tree-container ul {
    list-style: none;
    padding: 0 0 0 28px;
    margin: 0;
}
.tree-container > ul {
    padding-left: 0;
}
.tree-container li {
    position: relative;
    padding: 0;
    margin: 0;
}
/* vertical line from parent */
.tree-container li::before {
    content: '';
    position: absolute;
    left: 8px;
    top: 0;
    bottom: 0;
    width: 1px;
    background: linear-gradient(to bottom, #3a3a5a 0%, #3a3a5a 60%, transparent 100%);
}
.tree-container > ul > li::before {
    display: none;
}
.tree-container li:last-child::before {
    height: 16px;
}

/* ── Node card ── */
.tree-card-wrapper {
    position: relative;
    padding: 2px 0;
}
/* horizontal connector line */
.tree-container li .tree-card-wrapper::before {
    content: '';
    position: absolute;
    left: -17px;
    top: 14px;
    width: 14px;
    height: 1px;
    background: #3a3a5a;
}
.tree-container > ul > li > .tree-card-wrapper::before {
    display: none;
}

.tree-card {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 5px 10px 5px 6px;
    background: #1e2040;
    border: 1px solid #2a2a50;
    border-left: 3px solid #555;
    border-radius: 2px 6px 6px 2px;
    cursor: pointer;
    user-select: none;
    white-space: nowrap;
    transition: all 0.12s ease;
    box-shadow: 0 1px 3px rgba(0,0,0,0.2);
}
.tree-card:hover {
    background: #252850;
    border-color: #4a4a7a;
    box-shadow: 0 2px 8px rgba(0,0,0,0.35);
    transform: translateX(2px);
}
.tree-card.selected {
    background: #1a3050;
    border-color: var(--accent);
    border-left-width: 3px;
    box-shadow: 0 0 0 1px rgba(79,195,247,0.15), 0 2px 8px rgba(0,0,0,0.3);
}
.tree-card.type-button   { border-left-color: #4fc3f7; }
.tree-card.type-dropdown { border-left-color: #81c784; }
.tree-card.type-other    { border-left-color: #888; }

/* Drag states */
.tree-card.dragging { opacity: 0.4; }
.tree-card.drag-over { border-color: var(--accent); background: #1a3050; box-shadow: 0 0 12px rgba(79,195,247,0.3); }
.tree-card-wrapper.drop-target > .tree-card { border-color: var(--accent2); background: #1a3520; }

/* Strict mode toggle */
.strict-toggle {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    cursor: pointer;
    font-size: 11px;
    color: var(--text-dim);
    margin-left: auto;
    user-select: none;
}
.strict-toggle input { accent-color: var(--accent); }
.strict-toggle input:checked + .strict-label { color: var(--accent); }

/* Datalist */
input[list]::-webkit-calendar-picker-indicator { filter: invert(0.7); }

.tree-card .arrow {
    width: 16px;
    height: 16px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 8px;
    color: var(--text-dim);
    transition: transform 0.15s;
    flex-shrink: 0;
}
.tree-card .arrow.collapsed { transform: rotate(-90deg); }
.tree-card .arrow.leaf { visibility: hidden; }

.tree-card .node-id-chip {
    display: inline-block;
    font-size: 10px;
    font-family: 'Cascadia Code', 'Fira Code', 'Consolas', monospace;
    font-weight: 600;
    color: var(--text-dim);
    background: #ffffff08;
    padding: 1px 5px;
    border-radius: 3px;
    min-width: 28px;
    text-align: center;
}

.tree-card .node-name {
    font-weight: 600;
    font-size: 13px;
    color: #e8e8f0;
}

.tree-card .node-tag {
    display: inline-block;
    font-size: 9px;
    font-weight: 600;
    letter-spacing: 0.4px;
    text-transform: uppercase;
    padding: 2px 6px;
    border-radius: 3px;
}
.tree-card .node-tag.type-tag.button   { background: #1565c028; color: #4fc3f7; }
.tree-card .node-tag.type-tag.dropdown { background: #2e7d3228; color: #81c784; }
.tree-card .node-tag.type-tag.other    { background: #555;     color: #aaa; }

.tree-card .node-method-group {
    display: inline-flex;
    align-items: center;
    gap: 2px;
    font-family: 'Cascadia Code', 'Fira Code', 'Consolas', monospace;
    font-size: 11px;
}
.tree-card .node-method   { color: #e0a050; }
.tree-card .node-colon    { color: #666; }
.tree-card .node-method-val { color: #7eb8da; }

.tree-card .node-value-tag {
    font-size: 10px;
    color: #999;
    font-family: monospace;
}

.tree-card .node-parent-ref {
    font-size: 10px;
    color: #555;
    font-style: italic;
    margin-left: auto;
}

.tree-card .node-actions-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: #555;
    margin-left: 2px;
}
/* Context Menu */
.context-menu {
    position: fixed;
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    box-shadow: var(--shadow);
    z-index: 1000;
    min-width: 200px;
    padding: 4px 0;
}
.context-menu .menu-item {
    padding: 7px 16px;
    font-size: 12px;
    cursor: pointer;
    transition: background 0.1s;
}
.context-menu .menu-item:hover { background: var(--surface2); }
.context-menu .menu-sep {
    height: 1px;
    background: var(--border);
    margin: 4px 8px;
}
.context-menu .menu-item.danger { color: var(--danger); }
/* Modal */
.modal-overlay {
    display: none;
    position: fixed;
    top: 0; left: 0; right: 0; bottom: 0;
    background: rgba(0,0,0,0.6);
    z-index: 2000;
    justify-content: center;
    align-items: center;
}
.modal-overlay.active { display: flex; }
.modal {
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: 8px;
    padding: 20px 24px;
    min-width: 420px;
    max-width: 520px;
    box-shadow: var(--shadow);
}
.modal h2 { font-size: 15px; margin-bottom: 16px; }
.modal .field { margin-bottom: 12px; }
.modal .field label {
    display: block;
    font-size: 11px;
    color: var(--text-dim);
    text-transform: uppercase;
    margin-bottom: 4px;
    letter-spacing: 0.5px;
}
.modal .field input,
.modal .field select {
    width: 100%;
    padding: 7px 10px;
    background: var(--bg);
    color: var(--text);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    font-size: 13px;
    outline: none;
}
.modal .field input:focus,
.modal .field select:focus { border-color: var(--accent); }
.modal .field-row { display: flex; gap: 10px; }
.modal .field-row .field { flex: 1; }
.modal .preview {
    font-size: 11px;
    color: var(--text-dim);
    font-family: monospace;
    background: var(--bg);
    padding: 6px 8px;
    border-radius: var(--radius);
    word-break: break-all;
}
.modal .btn-row { justify-content: flex-end; margin-top: 16px; }
/* Toast */
.toast {
    position: fixed;
    bottom: 20px;
    right: 20px;
    padding: 10px 20px;
    border-radius: var(--radius);
    font-size: 13px;
    z-index: 3000;
    box-shadow: var(--shadow);
    animation: slideIn 0.2s ease;
}
.toast.error { background: #c62828; color: #fff; }
.toast.success { background: #2e7d32; color: #fff; }
@keyframes slideIn { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
/* Empty state */
.empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 100%;
    color: var(--text-dim);
    gap: 12px;
}
.empty-state .icon { font-size: 48px; }
.empty-state p { font-size: 14px; }
.empty-state .hint { font-size: 12px; color: #555; }
</style>
</head>
<body>

<header>
    <h1>Timing Editor</h1>
    <span class="badge" id="nodeCount">0 nodes</span>
    <label class="strict-toggle" title="Strict mode: names must match recognized elements">
        <input type="checkbox" id="strictModeCheck" onchange="onStrictModeChange()" />
        <span class="strict-label">Strict</span>
    </label>
</header>

<div class="main">
    <!-- Left Panel -->
    <div class="panel-left">
        <div class="section">
            <div class="section-title">Import / Export</div>
            <textarea id="rawInput" placeholder="Paste exported TimingCollection text here...
Format: {json}|JR|{json}|JR|..."></textarea>
            <div class="btn-row" style="margin-top: 6px;">
                <button class="btn primary" onclick="loadData()">Load</button>
                <button class="btn export-btn" onclick="exportData()">Export</button>
                <button class="btn" onclick="validateData()">Validate</button>
                <button class="btn" id="addRootBtn" onclick="addRootNode()">+ Add Root</button>
            </div>
        </div>
        <div class="errors-box" id="errorsBox">
            <div style="color: var(--text-dim);">Load data to begin...</div>
        </div>
    </div>

    <!-- Right Panel: Tree -->
    <div class="panel-right" id="treePanel">
        <div class="tree-container" id="treeContainer"></div>
        <div class="empty-state" id="emptyState">
            <div class="icon">🌲</div>
            <p>No timing data loaded</p>
            <p class="hint">Paste exported text in the left panel and click Load</p>
        </div>
    </div>
</div>

<!-- Context Menu -->
<div class="context-menu" id="contextMenu" style="display:none;">
    <div class="menu-item" data-action="addChild">Add Child</div>
    <div class="menu-item" data-action="insertBefore">Insert Before</div>
    <div class="menu-item" data-action="insertAfter">Insert After</div>
    <div class="menu-sep"></div>
    <div class="menu-item" data-action="edit">Edit</div>
    <div class="menu-sep"></div>
    <div class="menu-item danger" data-action="deleteCascade">Delete (Cascade)</div>
    <div class="menu-item danger" data-action="deletePromote">Delete (Promote Children)</div>
</div>

<!-- Edit Modal -->
<div class="modal-overlay" id="editModal">
    <div class="modal">
        <h2 id="modalTitle">Edit Node</h2>
        <div class="field">
            <label>Name</label>
            <input type="text" id="editName" placeholder="Node name" list="validNamesList" />
            <datalist id="validNamesList"></datalist>
        </div>
        <div class="field-row">
            <div class="field">
                <label>Type</label>
                <select id="editType">
                    <option value="button">button</option>
                    <option value="dropdown">dropdown</option>
                    <option value="other">other</option>
                </select>
            </div>
            <div class="field">
                <label>Value (node)</label>
                <input type="number" id="editValue" step="any" value="0" />
            </div>
        </div>
        <div class="field-row">
            <div class="field">
                <label>Method</label>
                <select id="editMethod" onchange="onMethodChange()">
                    <option value="sec">sec</option>
                    <option value="trialStart">trialStart</option>
                    <option value="trialEnd">trialEnd</option>
                    <option value="trialInTarget">trialInTarget</option>
                    <option value="IPCConnect">IPCConnect</option>
                </select>
            </div>
            <div class="field" id="methodValueField">
                <label>Method Value</label>
                <input type="text" id="editMethodValue" value="0" />
            </div>
        </div>
        <div class="field">
            <label>Parent ID (-1 = root)</label>
            <input type="number" id="editParentId" value="-1" step="1" />
        </div>
        <div class="field">
            <label>Generated timingMethod</label>
            <div class="preview" id="editPreview"></div>
        </div>
        <input type="hidden" id="editId" />
        <input type="hidden" id="editMode" value="edit" />
        <!-- editMode: "edit" or "addChild" or "insertBefore" or "insertAfter" or "root" -->
        <input type="hidden" id="editRefId" value="-1" />
        <div class="btn-row">
            <button class="btn" onclick="closeModal()">Cancel</button>
            <button class="btn primary" onclick="saveNode()">Save</button>
        </div>
    </div>
</div>

<script>
// =========================================================================
// Global State
// =========================================================================
let currentTree = [];
let selectedNodeId = null;
let selectedNodeIds = new Set();
let contextNodeId = null;
let collapsedNodes = new Set();
let clickTimer = null;
let strictMode = false;
let validNames = {};  // name -> type mapping for strict mode

// =========================================================================
// API Helpers
// =========================================================================
async function api(method, url, body) {
    const opts = { method, headers: { 'Content-Type': 'application/json' } };
    if (body) opts.body = JSON.stringify(body);
    const resp = await fetch(url, opts);
    const text = await resp.text();
    let data;
    try {
        data = JSON.parse(text);
    } catch {
        throw new Error('Server returned non-JSON response (status ' + resp.status + ')');
    }
    if (!resp.ok && data.errors && data.errors.length) {
        showToast(data.errors[0], 'error');
    }
    return data;
}

function showToast(msg, type) {
    const t = document.createElement('div');
    t.className = 'toast ' + type;
    t.textContent = msg;
    document.body.appendChild(t);
    setTimeout(() => t.remove(), 3000);
}

// =========================================================================
// Load / Export / Validate
// =========================================================================
async function loadData() {
    const text = document.getElementById('rawInput').value;
    // Push undo before replacing data (if there is existing data)
    if (currentTree && currentTree.length > 0) {
        await pushUndo();
    }
    const data = await api('POST', '/api/load', { text });
    currentTree = data.tree || [];
    updateTree();
    updateErrors(data.errors || []);
}

async function exportData() {
    const data = await api('GET', '/api/export');
    document.getElementById('rawInput').value = data.text || '';
    updateErrors(data.errors || []);
    showToast('Exported to text area', 'success');
}

async function validateData() {
    const data = await api('GET', '/api/validate');
    updateErrors(data.errors || []);
    if (!data.errors || data.errors.length === 0) {
        showToast('Validation passed', 'success');
    }
}

// =========================================================================
// Tree Rendering
// =========================================================================
function updateTree() {
    const container = document.getElementById('treeContainer');
    const empty = document.getElementById('emptyState');
    const countEl = document.getElementById('nodeCount');

    if (!currentTree || currentTree.length === 0) {
        container.innerHTML = '';
        if (empty) empty.style.display = 'flex';
        if (countEl) countEl.textContent = '0 nodes';
        return;
    }

    if (empty) empty.style.display = 'none';
    let html = '<ul>';
    for (const node of currentTree) {
        html += renderNode(node);
    }
    html += '</ul>';
    container.innerHTML = html;

    // Count total nodes
    let count = 0;
    function countNodes(nodes) {
        for (const n of nodes) { count++; countNodes(n.children || []); }
    }
    countNodes(currentTree);
    if (countEl) countEl.textContent = count + ' nodes';
}

function renderNode(node) {
    const hasChildren = node.children && node.children.length > 0;
    const collapsed = collapsedNodes.has(node.id);
    const arrowClass = hasChildren ? (collapsed ? 'collapsed' : '') : 'leaf';
    const typeClass = node.type || 'other';
    const method = node.methodType || '?';
    const methodVal = node.methodValue || '';

    let cardClass = 'tree-card type-' + typeClass;
    if (selectedNodeIds.has(node.id)) cardClass += ' selected';

    let html = '<li><div class="tree-card-wrapper">';
    html += '<div class="' + cardClass + '" data-id="' + node.id
        + '" draggable="true"'
        + ' onclick="onNodeClick(event, ' + node.id + ')"'
        + ' oncontextmenu="onNodeContextMenu(event, ' + node.id + ')"'
        + ' ondblclick="onNodeDblClick(' + node.id + ')"'
        + ' ondragstart="onDragStart(event, ' + node.id + ')"'
        + ' ondragend="onDragEnd(event, ' + node.id + ')"'
        + ' ondragover="onDragOver(event, ' + node.id + ')"'
        + ' ondragleave="onDragLeave(event, ' + node.id + ')"'
        + ' ondrop="onDrop(event, ' + node.id + ')">';

    // expand arrow
    html += '<span class="arrow ' + arrowClass + '">&#9654;</span>';

    // ID chip
    html += '<span class="node-id-chip">#' + node.id + '</span>';

    // name
    html += '<span class="node-name">' + esc(node.name) + '</span>';

    // type badge
    html += '<span class="node-tag type-tag ' + typeClass + '">' + esc(node.type) + '</span>';

    // method group
    html += '<span class="node-method-group">';
    html += '<span class="node-method">' + esc(method) + '</span>';
    if (methodVal) {
        html += '<span class="node-colon">:</span>';
        html += '<span class="node-method-val">' + esc(methodVal) + '</span>';
    }
    html += '</span>';

    // node value (if non-zero)
    if (node.value !== 0 && node.value !== undefined && node.value !== null) {
        html += '<span class="node-value-tag">v=' + esc(String(node.value)) + '</span>';
    }

    // parent reference
    if (node.parentId >= 0 && node.parentName) {
        html += '<span class="node-parent-ref">&xrarr; ' + esc(node.parentName) + '</span>';
    } else if (node.parentId >= 0) {
        html += '<span class="node-parent-ref">&xrarr; #' + node.parentId + '</span>';
    }

    html += '</div>'; // .tree-card

    // children
    if (hasChildren) {
        html += '<ul style="' + (collapsed ? 'display:none;' : '') + '">';
        for (const child of node.children) {
            html += renderNode(child);
        }
        html += '</ul>';
    }

    html += '</div></li>'; // .tree-card-wrapper + li
    return html;
}

function esc(s) {
    if (!s) return '';
    return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

// =========================================================================
// Interaction Handlers
// =========================================================================
function onNodeClick(event, nodeId) {
    event.stopPropagation();

    // If a click timer is pending, this is the second click of a double-click
    if (clickTimer) {
        clearTimeout(clickTimer);
        clickTimer = null;
        return;
    }

    const ctrl = event.ctrlKey || event.metaKey;

    // Delay single-click action to distinguish from double-click
    clickTimer = setTimeout(() => {
        clickTimer = null;
        if (ctrl) {
            // Multi-select toggle
            if (selectedNodeIds.has(nodeId)) {
                selectedNodeIds.delete(nodeId);
            } else {
                selectedNodeIds.add(nodeId);
            }
        } else {
            // Single select: clear multi-selection
            selectedNodeIds.clear();
            selectedNodeIds.add(nodeId);
            // Toggle expand/collapse
            if (collapsedNodes.has(nodeId)) {
                collapsedNodes.delete(nodeId);
            } else {
                collapsedNodes.add(nodeId);
            }
        }
        selectedNodeId = selectedNodeIds.size === 1 ? nodeId : null;
        updateTree();
    }, 250);
}

function onNodeDblClick(nodeId) {
    if (clickTimer) {
        clearTimeout(clickTimer);
        clickTimer = null;
    }
    // Double-click selects single node
    selectedNodeIds.clear();
    selectedNodeIds.add(nodeId);
    selectedNodeId = nodeId;
    updateTree();
    openEditModal('edit', nodeId);
}

function onNodeContextMenu(event, nodeId) {
    event.preventDefault();
    event.stopPropagation();
    contextNodeId = nodeId;
    // If right-clicking a non-selected node, select it
    if (!selectedNodeIds.has(nodeId)) {
        selectedNodeIds.clear();
        selectedNodeIds.add(nodeId);
        selectedNodeId = nodeId;
        updateTree();
    }
    showContextMenu(event.clientX, event.clientY, true);
}

// Tree panel right-click (empty area)
document.getElementById('treePanel').addEventListener('contextmenu', function(e) {
    if (e.target === this || e.target.id === 'treeContainer' || e.target.closest('.empty-state')) {
        e.preventDefault();
        contextNodeId = null;
        showContextMenu(e.clientX, e.clientY, false);
    }
});

// =========================================================================
// Drag and Drop
// =========================================================================
let dragNodeId = null;

function onDragStart(event, nodeId) {
    dragNodeId = nodeId;
    // If dragging a selected node, highlight all selected as dragging
    const cards = document.querySelectorAll('.tree-card');
    cards.forEach(c => {
        const id = parseInt(c.dataset.id);
        if (selectedNodeIds.has(id)) c.classList.add('dragging');
    });
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/plain', String(nodeId));
    event.stopPropagation();
}

function onDragEnd(event, nodeId) {
    document.querySelectorAll('.tree-card.dragging').forEach(el => el.classList.remove('dragging'));
    document.querySelectorAll('.drag-over, .drop-target').forEach(el => {
        el.classList.remove('drag-over');
        if (el.parentElement) el.parentElement.classList.remove('drop-target');
    });
    dragNodeId = null;
}

function onDragOver(event, nodeId) {
    event.preventDefault();
    if (dragNodeId === nodeId) return;
    event.dataTransfer.dropEffect = 'move';
    const card = event.target.closest('.tree-card');
    if (card && !card.classList.contains('drag-over')) {
        card.classList.add('drag-over');
    }
}

function onDragLeave(event, nodeId) {
    const card = event.target.closest('.tree-card');
    if (card) card.classList.remove('drag-over');
}

async function onDrop(event, nodeId) {
    event.preventDefault();
    event.stopPropagation();
    const card = event.target.closest('.tree-card');
    if (card) card.classList.remove('drag-over');

    if (dragNodeId === null || dragNodeId === nodeId) return;

    // Collect nodes to move: if dragged node is in multi-selection, move all selected
    let nodesToMove;
    if (selectedNodeIds.size > 1 && selectedNodeIds.has(dragNodeId)) {
        nodesToMove = [...selectedNodeIds];
        // Filter out nodes that are descendants of other nodes in the set
        nodesToMove = nodesToMove.filter(id => {
            return !nodesToMove.some(otherId => otherId !== id && _isDescendantOf(id, otherId));
        });
    } else {
        nodesToMove = [dragNodeId];
    }

    // Validate: no node would create a cycle
    for (const id of nodesToMove) {
        if (id === nodeId) {
            showToast('Cannot move a node onto itself', 'error');
            return;
        }
        if (_wouldCreateLocalCycle(id, nodeId)) {
            showToast('Cannot move: would create a circular reference', 'error');
            return;
        }
    }

    await pushUndo();
    let data;
    if (nodesToMove.length === 1) {
        data = await api('POST', '/api/node/move', {
            nodeId: nodesToMove[0],
            newParentId: nodeId
        });
    } else {
        data = await api('POST', '/api/nodes/move-batch', {
            nodeIds: nodesToMove,
            newParentId: nodeId
        });
    }
    if (data && data.tree) {
        currentTree = data.tree;
        updateTree();
        updateErrors(data.errors || []);
        showToast('Moved ' + nodesToMove.length + ' node(s)', 'success');
    } else {
        popUndo();
    }
}

function _wouldCreateLocalCycle(nodeId, newParentId) {
    const visited = new Set();
    function walk(id) {
        if (id === -1) return false;
        if (id === nodeId) return true;
        if (visited.has(id)) return false;
        visited.add(id);
        const node = findNode(currentTree, id);
        if (!node) return false;
        return walk(node.parentId);
    }
    return walk(newParentId);
}

function _isDescendantOf(nodeId, ancestorId) {
    let current = nodeId;
    const visited = new Set();
    while (current !== -1 && !visited.has(current)) {
        visited.add(current);
        const node = findNode(currentTree, current);
        if (!node) return false;
        if (node.parentId === ancestorId) return true;
        current = node.parentId;
    }
    return false;
}

// Close context menu on click elsewhere
document.addEventListener('click', function() {
    document.getElementById('contextMenu').style.display = 'none';
});

// =========================================================================
// Undo / Redo
// =========================================================================
const MAX_HISTORY = 100;
let undoStack = [];
let redoStack = [];
let undoGuard = false;  // prevents pushUndo during undo/redo operations

async function pushUndo() {
    if (undoGuard) return;
    try {
        const resp = await fetch('/api/export');
        const data = await resp.json();
        if (data.text !== undefined) {
            undoStack.push(data.text);
            if (undoStack.length > MAX_HISTORY) undoStack.shift();
            redoStack = [];  // new action invalidates redo
        }
    } catch (err) {
        console.error('pushUndo failed:', err);
    }
}

function popUndo() {
    if (undoStack.length > 0) undoStack.pop();
}

async function performUndo() {
    if (undoStack.length === 0) return;
    undoGuard = true;
    // Save current state to redo
    try {
        const resp = await fetch('/api/export');
        const data = await resp.json();
        if (data.text !== undefined) {
            redoStack.push(data.text);
        }
    } catch (err) { /* ignore */ }
    // Restore previous state
    const prevState = undoStack.pop();
    const result = await api('POST', '/api/load', { text: prevState });
    if (result && result.tree) {
        currentTree = result.tree;
        selectedNodeIds.clear();
        selectedNodeId = null;
        updateTree();
        updateErrors(result.errors || []);
        showToast('Undo (' + undoStack.length + ' steps remaining)', 'success');
    }
    undoGuard = false;
}

async function performRedo() {
    if (redoStack.length === 0) return;
    undoGuard = true;
    // Save current state to undo
    try {
        const resp = await fetch('/api/export');
        const data = await resp.json();
        if (data.text !== undefined) {
            undoStack.push(data.text);
        }
    } catch (err) { /* ignore */ }
    // Restore next state
    const nextState = redoStack.pop();
    const result = await api('POST', '/api/load', { text: nextState });
    if (result && result.tree) {
        currentTree = result.tree;
        selectedNodeIds.clear();
        selectedNodeId = null;
        updateTree();
        updateErrors(result.errors || []);
        showToast('Redo (' + redoStack.length + ' steps remaining)', 'success');
    }
    undoGuard = false;
}

// Keyboard shortcuts for undo/redo
document.addEventListener('keydown', function(e) {
    if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.tagName === 'SELECT') return;
    if ((e.ctrlKey || e.metaKey) && e.key === 'z' && !e.shiftKey) {
        e.preventDefault();
        performUndo();
    }
    if ((e.ctrlKey || e.metaKey) && (e.key === 'y' || (e.key === 'z' && e.shiftKey))) {
        e.preventDefault();
        performRedo();
    }
});

// =========================================================================
// Context Menu
// =========================================================================
function showContextMenu(x, y, onNode) {
    const menu = document.getElementById('contextMenu');
    menu.style.display = 'block';
    menu.style.left = Math.min(x, window.innerWidth - 220) + 'px';
    menu.style.top = Math.min(y, window.innerHeight - 280) + 'px';

    // Show/hide items based on context
    const items = menu.querySelectorAll('.menu-item');
    items.forEach(item => {
        const action = item.dataset.action;
        item.style.display = '';
        if (!onNode && action !== 'addChild') {
            // On empty area: "addChild" becomes "Add Root"
            if (action === 'addChild') {
                item.textContent = 'Add Root';
            } else {
                item.style.display = 'none';
            }
        } else if (onNode && action === 'addChild') {
            item.textContent = 'Add Child';
        }
    });
}

document.getElementById('contextMenu').addEventListener('click', function(e) {
    const action = e.target.dataset.action;
    if (!action) return;
    this.style.display = 'none';

    switch (action) {
        case 'addChild':
            if (contextNodeId !== null) {
                openEditModal('addChild', contextNodeId);
            } else {
                openEditModal('root', -1);
            }
            break;
        case 'insertBefore':
            openEditModal('insertBefore', contextNodeId);
            break;
        case 'insertAfter':
            openEditModal('insertAfter', contextNodeId);
            break;
        case 'edit':
            openEditModal('edit', contextNodeId);
            break;
        case 'deleteCascade':
            deleteNode(contextNodeId, 'cascade');
            break;
        case 'deletePromote':
            deleteNode(contextNodeId, 'promote');
            break;
    }
});

// =========================================================================
// Edit Modal
// =========================================================================
function openEditModal(mode, refId) {
    const modal = document.getElementById('editModal');
    const title = document.getElementById('modalTitle');
    document.getElementById('editMode').value = mode;
    document.getElementById('editRefId').value = refId;

    // Apply strict mode constraints
    const typeSelect = document.getElementById('editType');
    const nameInput = document.getElementById('editName');
    if (strictMode) {
        nameInput.setAttribute('list', 'validNamesList');
        typeSelect.disabled = true;
        typeSelect.style.opacity = '0.6';
    } else {
        nameInput.removeAttribute('list');
        typeSelect.disabled = false;
        typeSelect.style.opacity = '';
    }

    if (mode === 'edit') {
        title.textContent = 'Edit Node [' + refId + ']';
        // Re-enable parentId for edit mode
        document.getElementById('editParentId').readOnly = false;
        document.getElementById('editParentId').style.opacity = '';
        // Find node in tree
        const node = findNode(currentTree, refId);
        if (node) {
            document.getElementById('editId').value = node.id;
            document.getElementById('editName').value = node.name;
            document.getElementById('editType').value = node.type || 'button';
            document.getElementById('editValue').value = node.value || 0;
            document.getElementById('editMethod').value = node.methodType || 'sec';
            document.getElementById('editMethodValue').value = node.methodValue || '0';
            document.getElementById('editParentId').value = node.parentId;
        }
    } else {
        const titles = {
            'addChild': 'Add Child of [' + refId + ']',
            'insertBefore': 'Insert Before [' + refId + ']',
            'insertAfter': 'Insert After [' + refId + ']',
            'root': 'Add Root Node',
        };
        title.textContent = titles[mode] || 'Add Node';
        document.getElementById('editId').value = '';
        document.getElementById('editName').value = '';
        document.getElementById('editType').value = 'button';
        document.getElementById('editValue').value = '0';
        document.getElementById('editMethod').value = 'sec';
        document.getElementById('editMethodValue').value = '0';
        // Pre-fill parentId based on mode
        let parentId = -1;
        if (mode === 'addChild' || mode === 'insertAfter') {
            parentId = refId;
        } else if (mode === 'insertBefore') {
            const refNode = findNode(currentTree, refId);
            parentId = refNode ? refNode.parentId : -1;
        }
        document.getElementById('editParentId').value = parentId;
        // Disable parentId for add modes (server determines it)
        document.getElementById('editParentId').readOnly = true;
        document.getElementById('editParentId').style.opacity = '0.5';
    }

    onMethodChange();
    updatePreview();
    modal.classList.add('active');
}

function onNameChange() {
    const name = document.getElementById('editName').value.trim();
    if (strictMode && validNames[name]) {
        document.getElementById('editType').value = validNames[name];
    }
    updatePreview();
}

function closeModal() {
    document.getElementById('editModal').classList.remove('active');
}

function onMethodChange() {
    const method = document.getElementById('editMethod').value;
    const field = document.getElementById('methodValueField');
    if (method === 'IPCConnect') {
        field.style.display = 'none';
        document.getElementById('editMethodValue').value = '0';
    } else {
        field.style.display = '';
    }
    updatePreview();
}

function updatePreview() {
    const type = document.getElementById('editType').value;
    const name = document.getElementById('editName').value || '?';
    const method = document.getElementById('editMethod').value;
    const mval = document.getElementById('editMethodValue').value || '0';
    document.getElementById('editPreview').textContent =
        'type_' + type + ';' + name + ':' + method + ':' + mval + ';';
}

document.getElementById('editName').addEventListener('input', onNameChange);
document.getElementById('editType').addEventListener('change', updatePreview);
document.getElementById('editMethod').addEventListener('change', function() { onMethodChange(); });
document.getElementById('editMethodValue').addEventListener('input', updatePreview);

function findNode(nodes, id) {
    for (const n of nodes) {
        if (n.id === id) return n;
        const found = findNode(n.children || [], id);
        if (found) return found;
    }
    return null;
}

async function saveNode() {
    const mode = document.getElementById('editMode').value;
    const refId = parseInt(document.getElementById('editRefId').value);
    const fields = {
        name: document.getElementById('editName').value,
        type: document.getElementById('editType').value,
        methodType: document.getElementById('editMethod').value,
        methodValue: document.getElementById('editMethodValue').value,
        value: parseFloat(document.getElementById('editValue').value) || 0,
    };

    await pushUndo();

    let data;
    try {
        if (mode === 'edit') {
            const nodeId = parseInt(document.getElementById('editId').value);
            fields.parentId = parseInt(document.getElementById('editParentId').value);
            fields.strict = strictMode;
            data = await api('PUT', '/api/node/' + nodeId, fields);
        } else {
            data = await api('POST', '/api/node/add', {
                action: mode === 'root' ? 'root' : (mode === 'addChild' ? 'child' : mode === 'insertBefore' ? 'before' : 'after'),
                refId: refId,
                strict: strictMode,
                ...fields,
            });
        }
    } catch (err) {
        console.error('Save error:', err);
        popUndo();
        showToast('Save failed: network or server error', 'error');
        return;
    }

    if (!data) {
        popUndo();
        showToast('No response from server', 'error');
        return;
    }

    if (data.tree) {
        currentTree = data.tree;
        updateTree();
    }
    updateErrors(data.errors || []);

    if (data.success !== false) {
        closeModal();
        showToast('Saved', 'success');
    } else {
        popUndo();
    }
}

// =========================================================================
// Strict Mode
// =========================================================================
async function onStrictModeChange() {
    strictMode = document.getElementById('strictModeCheck').checked;
    if (strictMode && Object.keys(validNames).length === 0) {
        await populateDatalist();
    }
}

async function populateDatalist() {
    try {
        const resp = await fetch('/api/valid-names');
        const data = await resp.json();
        validNames = data.names || {};
        const datalist = document.getElementById('validNamesList');
        datalist.innerHTML = '';
        for (const name of Object.keys(validNames).sort()) {
            const opt = document.createElement('option');
            opt.value = name;
            datalist.appendChild(opt);
        }
    } catch (err) {
        console.error('Failed to load valid names:', err);
    }
}

// Preload valid names on startup
populateDatalist();

// =========================================================================
// Delete
// =========================================================================
async function deleteNode(nodeId, mode) {
    // If multiple nodes selected and nodeId is one of them, delete all selected
    const idsToDelete = selectedNodeIds.size > 1 && selectedNodeIds.has(nodeId)
        ? [...selectedNodeIds]
        : [nodeId];

    const label = mode === 'cascade' ? 'Cascade delete' : 'Delete (promote children)';
    if (!confirm(label + ' ' + idsToDelete.length + ' node(s)?')) return;

    await pushUndo();

    let totalRemoved = 0;
    for (const id of idsToDelete) {
        const data = await api('DELETE', '/api/node/' + id + '?mode=' + mode);
        if (data.tree) {
            currentTree = data.tree;
            totalRemoved += (data.removed || []).length;
            selectedNodeIds.delete(id);
        }
        // If node was already deleted by a previous cascade, skip
        if (data.errors && data.errors.some(e => e.includes('not found'))) continue;
    }
    if (totalRemoved > 0) {
        if (selectedNodeIds.size === 0) selectedNodeId = null;
        updateTree();
        updateErrors([]);
        showToast('Deleted ' + totalRemoved + ' node(s)', 'success');
    } else {
        popUndo();
    }
}

// =========================================================================
// Add Root (button)
// =========================================================================
function addRootNode() {
    openEditModal('root', -1);
}

// =========================================================================
// Error Display
// =========================================================================
function updateErrors(errors) {
    const box = document.getElementById('errorsBox');
    if (!errors || errors.length === 0) {
        box.innerHTML = '<div class="error-item ok">No errors detected</div>';
        return;
    }
    let html = '';
    for (const e of errors) {
        html += '<div class="error-item err">' + esc(e) + '</div>';
    }
    box.innerHTML = html;
}
</script>
</body>
</html>"""

# =============================================================================
# Entry Point
# =============================================================================


def main():
    host = "127.0.0.1"
    port = 5500

    print(f"Timing Editor starting at http://{host}:{port}")
    print("Press Ctrl+C to stop.")

    # Open browser after a brief delay
    def open_browser():
        webbrowser.open(f"http://{host}:{port}")

    threading.Timer(1.0, open_browser).start()

    app.run(host=host, port=port, debug=False)


if __name__ == "__main__":
    main()
