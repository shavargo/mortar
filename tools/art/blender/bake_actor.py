"""Blender side of the actor bake: renders the frames a job file asks for.

bake.py runs: blender --background --factory-startup --python-exit-code 2 --python bake_actor.py -- job.json
"""

import json
import math
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from mortar_art import bake_geometry as geo  # noqa: E402

KEY_LIGHT = ("Key", 4.0, (math.radians(50.0), 0.0, math.radians(10.0)))
FILL_LIGHT = ("Fill", 1.0, (math.radians(70.0), 0.0, math.radians(200.0)))
AMBIENT = (0.32, 0.32, 0.36, 1.0)
IN_PLACE = frozenset({"death"})


class BakeFailure(Exception):
    pass


def main() -> None:
    job = json.loads(Path(sys.argv[sys.argv.index("--") + 1]).read_text(encoding="utf-8"))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    try:
        if job["mode"] == "list":
            list_files(job["files"])
        else:
            bake(job)
    except (BakeFailure, ValueError, RuntimeError, KeyError) as failure:
        print(f"[BAKE-ERROR] {failure}", flush=True)
        sys.exit(2)


def scene_fps() -> float:
    render = bpy.context.scene.render
    return render.fps / render.fps_base


def import_file(path: str) -> list:
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    for action in bpy.data.actions:
        action.use_fake_user = True
    return [obj for obj in bpy.data.objects if obj not in before]


def list_files(paths: list) -> None:
    for path in paths:
        before = set(bpy.data.actions)
        objects = import_file(path)
        name = Path(path).name
        for action in (action for action in bpy.data.actions if action not in before):
            print(f"[CLIP] {name}: {action.name} ({clip_seconds(action):.2f}s)")
        for obj in objects:
            print(f"[OBJECT] {name}: {obj.name} ({obj.type})")


def bake(job: dict) -> None:
    scene = bpy.context.scene
    setup_render(scene)
    pivot, armature = load_model(job)
    for path in job["animations"]:
        for obj in import_file(path):
            bpy.data.objects.remove(obj, do_unlink=True)
    out = Path(job["out"])
    bodies = [obj for obj in pivot.children_recursive
              if obj.type == "MESH" and not obj.hide_render and obj.find_armature() is not None]
    summary = {"directions": job["directions"], "animations": {}}
    for animation, clip_name in job["clips"].items():
        action = find_action(clip_name)
        times = geo.sample_times(clip_seconds(action), animation in geo.LOOPING)
        if job["maxFrames"]:
            times = times[: job["maxFrames"]]
        play(armature, action)
        for direction in job["directions"]:
            pivot.rotation_euler = (0.0, 0.0, math.radians(geo.model_rotation_degrees(direction, job["facing"])))
            start = None
            for index, seconds in enumerate(times):
                pose(scene, action, seconds)
                if animation in IN_PLACE:
                    start = keep_in_place(pivot, bodies, start)
                render_frame(scene, out / animation / str(direction) / f"{index:02d}.png")
            pivot.location = (0.0, 0.0, 0.0)
        hit = geo.hit_frame_index(job["attackHitSeconds"], len(times)) if animation == "attack" else 0
        summary["animations"][animation] = {"frames": len(times), "hitFrame": hit}
    (out / "frames.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")


def load_model(job: dict) -> tuple:
    objects = import_file(job["model"])
    armatures = [obj for obj in objects if obj.type == "ARMATURE"]
    if not armatures:
        raise BakeFailure(f"{Path(job['model']).name} has no armature")
    hide(objects, job["hide"])
    pivot = bpy.data.objects.new("BakePivot", None)
    bpy.context.scene.collection.objects.link(pivot)
    for obj in objects:
        if obj.parent is None:
            obj.parent = pivot
    pivot.scale = (job["scale"], job["scale"], job["scale"])
    return pivot, armatures[0]


def hide(objects: list, names: list) -> None:
    by_name = {obj.name: obj for obj in objects}
    unknown = [name for name in names if name not in by_name]
    if unknown:
        raise BakeFailure(f"unknown objects to hide: {', '.join(unknown)}; the model has {', '.join(sorted(by_name))}")
    for name in names:
        for obj in [by_name[name], *by_name[name].children_recursive]:
            obj.hide_render = True


def find_action(name: str):
    action = bpy.data.actions.get(name)
    if action is None:
        known = ", ".join(sorted(action.name for action in bpy.data.actions))
        raise BakeFailure(f"no clip named '{name}'; available: {known}")
    return action


def clip_seconds(action) -> float:
    start, end = action.frame_range
    return max(0.0, end - start) / scene_fps()


def play(armature, action) -> None:
    data = armature.animation_data or armature.animation_data_create()
    for track in data.nla_tracks:
        track.mute = True
    data.action = action
    slot = next((slot for slot in action.slots if slot.target_id_type == "OBJECT"), None)
    if slot is not None:
        data.action_slot = slot


def pose(scene, action, seconds: float) -> None:
    frame = action.frame_range[0] + seconds * scene_fps()
    whole = math.floor(frame)
    scene.frame_set(whole, subframe=frame - whole)


def keep_in_place(pivot, bodies: list, start: tuple | None) -> tuple:
    pivot.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    centre = ground_centre(bodies)
    start = centre if start is None else start
    pivot.location = (start[0] - centre[0], start[1] - centre[1], 0.0)
    return start


def ground_centre(bodies: list) -> tuple:
    depsgraph = bpy.context.evaluated_depsgraph_get()
    xs, ys = [], []
    for obj in bodies:
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        for vertex in mesh.vertices:
            point = evaluated.matrix_world @ vertex.co
            xs.append(point.x)
            ys.append(point.y)
        evaluated.to_mesh_clear()
    if not xs:
        raise BakeFailure("no visible meshes follow the armature")
    return (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2


def render_frame(scene, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def setup_render(scene) -> None:
    width, height = geo.render_size()
    render = scene.render
    render.engine = eevee_engine()
    render.resolution_x, render.resolution_y, render.resolution_percentage = width, height, 100
    render.film_transparent = True
    render.image_settings.file_format = "PNG"
    render.image_settings.color_mode = "RGBA"
    scene.view_settings.view_transform = "Standard"
    add_camera(scene)
    for name, energy, rotation in (KEY_LIGHT, FILL_LIGHT):
        light = bpy.data.lights.new(name, "SUN")
        light.energy = energy
        obj = bpy.data.objects.new(name, light)
        obj.rotation_euler = rotation
        scene.collection.objects.link(obj)
    add_world(scene)


def eevee_engine() -> str:
    engines = {item.identifier for item in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items}
    return "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"


def add_camera(scene) -> None:
    data = bpy.data.cameras.new("BakeCamera")
    data.type = "ORTHO"
    data.ortho_scale = geo.ortho_scale()
    data.shift_x, data.shift_y = geo.camera_shift()
    data.clip_end = geo.CAMERA_DISTANCE * 3
    camera = bpy.data.objects.new("BakeCamera", data)
    scene.collection.objects.link(camera)
    camera.location = geo.camera_location()
    camera.rotation_euler = geo.camera_rotation_radians()
    scene.camera = camera


def add_world(scene) -> None:
    world = bpy.data.worlds.new("BakeWorld")
    world.use_nodes = True
    background = world.node_tree.nodes["Background"]
    background.inputs["Color"].default_value = AMBIENT
    background.inputs["Strength"].default_value = 1.0
    scene.world = world


if __name__ == "__main__":
    main()
