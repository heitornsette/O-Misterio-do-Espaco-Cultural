"""Build the stylized 3D CC01 cutscene source for Blender 5.1+.

Run with:
  blender --background --python Documentation/Blender/create_cc01.py
The script writes the reusable .blend alongside this source file.
"""
import math
import os
import bpy
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
BLEND_OUT = os.path.join(os.path.dirname(__file__), "CC01_Cinematic.blend")
FPS = 24
DURATION = 35.7
LAST_FRAME = round(FPS * DURATION)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def collection(name):
    c = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(c)
    return c


def move_to(obj, coll):
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    coll.objects.link(obj)
    return obj


def material(name, color, roughness=0.72, metallic=0.0, emission=None, alpha=1.0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, alpha)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, alpha)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Alpha"].default_value = alpha
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
        bsdf.inputs["Emission Strength"].default_value = 1.5
    if alpha < 1:
        m.surface_render_method = 'BLENDED'
    return m


def finish_mesh(obj, name, mat, coll, bevel=0.0):
    obj.name = name
    if mat:
        obj.data.materials.append(mat)
    move_to(obj, coll)
    if bevel:
        mod = obj.modifiers.new("Soft pixel-art edge", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        obj.modifiers.new("Weighted corner normals", "WEIGHTED_NORMAL")
    return obj


def cube(name, loc, size, mat, coll, bevel=0.0, parent=None):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0))
    o = bpy.context.object
    finish_mesh(o, name, mat, coll, bevel)
    o.parent = parent
    o.location = loc
    o.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return o


def sphere(name, loc, scale, mat, coll, parent=None, segments=16, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=(0, 0, 0))
    o = bpy.context.object
    finish_mesh(o, name, mat, coll)
    o.parent = parent
    o.location = loc
    o.scale = scale
    for p in o.data.polygons:
        p.use_smooth = True
    return o


def cylinder(name, loc, radius, depth, mat, coll, parent=None, rotation=None, vertices=12, bevel=0.0):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=(0, 0, 0))
    o = bpy.context.object
    finish_mesh(o, name, mat, coll, bevel)
    o.parent = parent
    o.location = loc
    if rotation:
        o.rotation_euler = rotation
    return o


def curve_line(name, points, radius, mat, coll):
    cu = bpy.data.curves.new(name, "CURVE")
    cu.dimensions = '3D'
    cu.resolution_u = 1
    cu.bevel_depth = radius
    cu.bevel_resolution = 2
    spline = cu.splines.new('POLY')
    spline.points.add(len(points) - 1)
    for p, co in zip(spline.points, points):
        p.co = (*co, 1)
    ob = bpy.data.objects.new(name, cu)
    coll.objects.link(ob)
    ob.data.materials.append(mat)
    return ob


def aim(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()


def key(obj, frame, loc=None, rot=None, scale=None):
    if loc is not None:
        obj.location = loc
        obj.keyframe_insert(data_path="location", frame=frame)
    if rot is not None:
        obj.rotation_euler = rot
        obj.keyframe_insert(data_path="rotation_euler", frame=frame)
    if scale is not None:
        obj.scale = scale
        obj.keyframe_insert(data_path="scale", frame=frame)


def fcurves(owner):
    """Return animation curves in both legacy and layered Blender actions."""
    ad = getattr(owner, "animation_data", None)
    action = getattr(ad, "action", None) if ad else None
    if not action:
        return []
    legacy = getattr(action, "fcurves", None)
    if legacy is not None:
        return list(legacy)
    curves = []
    slot = getattr(ad, "action_slot", None)
    for layer in action.layers:
        for strip in layer.strips:
            if strip.type == 'KEYFRAME':
                bag = strip.channelbag(slot) if slot else None
                if bag:
                    curves.extend(bag.fcurves)
    return curves


def set_constant_keys(obj):
    for fc in fcurves(obj):
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'
            kp.handle_left_type = 'AUTO_CLAMPED'
            kp.handle_right_type = 'AUTO_CLAMPED'


def visibility_keys(obj, visible_frame, hidden_frame=767):
    obj.hide_render = True
    obj.keyframe_insert(data_path='hide_render', frame=1)
    obj.hide_render = False
    obj.keyframe_insert(data_path='hide_render', frame=visible_frame)
    obj.hide_render = True
    obj.keyframe_insert(data_path='hide_render', frame=hidden_frame)
    for fc in fcurves(obj):
        if fc.data_path == 'hide_render':
            for kp in fc.keyframe_points:
                kp.interpolation = 'CONSTANT'


def add_text(name, body, loc, size, mat, coll, rotation=(0, 0, 0), align='CENTER'):
    cu = bpy.data.curves.new(name, 'FONT')
    cu.body = body
    cu.size = size
    cu.align_x = align
    cu.align_y = 'CENTER'
    cu.extrude = 0.006
    cu.bevel_depth = 0.002
    ob = bpy.data.objects.new(name, cu)
    coll.objects.link(ob)
    ob.location = loc
    ob.rotation_euler = rotation
    ob.data.materials.append(mat)
    return ob


def light(name, typ, loc, color, power, coll, size=3.0, target=None):
    data = bpy.data.lights.new(name, typ)
    data.color = color
    data.energy = power
    if typ == 'AREA':
        data.shape = 'DISK'
        data.size = size
    elif typ == 'SPOT':
        data.spot_size = math.radians(48)
        data.spot_blend = 0.35
        data.shadow_soft_size = 0.3
    o = bpy.data.objects.new(name, data)
    coll.objects.link(o)
    o.location = loc
    if target:
        aim(o, target)
    return o


def create_guard(coll, mats):
    root = bpy.data.objects.new("GUARD_ROOT", None)
    coll.objects.link(root)
    root.empty_display_type = 'CIRCLE'
    root.empty_display_size = 0.45
    root.location = (-3.6, -1.7, 0)
    rig = {}
    def joint(name, parent, loc):
        e = bpy.data.objects.new(name, None)
        coll.objects.link(e)
        e.parent = parent
        e.location = loc
        e.empty_display_type = 'SPHERE'
        e.empty_display_size = 0.08
        rig[name] = e
        return e
    body = joint("RIG_Torso", root, (0, 0, 0))
    pelvis = joint("RIG_Pelvis", root, (0, 0, 0.84))
    head = joint("RIG_Head", body, (0, 0, 1.56))

    cube("Guard | camisa azul", (0, 0, 1.22), (0.68, 0.38, 0.66), mats['shirt'], coll, 0.09, body)
    cube("Guard | gola", (0, -0.005, 1.54), (0.22, 0.28, 0.12), mats['shirt_light'], coll, 0.035, body)
    for x in (-0.18, 0.18):
        cube("Guard | bolso", (x, -0.205, 1.31), (0.18, 0.035, 0.17), mats['shirt_light'], coll, 0.018, body)
        sphere("Guard | botão", (x, -0.228, 1.25), (0.022, 0.012, 0.022), mats['metal'], coll, body, 8, 4)
    cube("Guard | cinto", (0, 0, 0.91), (0.57, 0.42, 0.11), mats['belt'], coll, 0.02, body)
    cube("Guard | fivela", (0, -0.22, 0.91), (0.11, 0.03, 0.07), mats['metal'], coll, 0.01, body)
    cylinder("Guard | cassetete", (0.34, -0.015, 0.96), 0.045, 0.48, mats['navy'], coll, body, vertices=10)
    cube("Guard | rádio", (-0.34, -0.03, 0.98), (0.13, 0.16, 0.21), mats['metal'], coll, 0.025, body)
    cube("Guard | botão do rádio", (-0.34, -0.12, 1.02), (0.035, 0.02, 0.035), mats['warm'], coll, 0.006, body)

    sphere("Guard | rosto", (0, -0.005, 0.20), (0.23, 0.22, 0.28), mats['skin'], coll, head)
    sphere("Guard | orelha E", (-0.22, -0.005, 0.19), (0.055, 0.07, 0.085), mats['skin'], coll, head, 12, 6)
    sphere("Guard | orelha D", (0.22, -0.005, 0.19), (0.055, 0.07, 0.085), mats['skin'], coll, head, 12, 6)
    for x in (-0.085, 0.085):
        sphere("Guard | olho", (x, -0.211, 0.22), (0.025, 0.014, 0.032), mats['eye'], coll, head, 10, 6)
    cube("Guard | nariz", (0, -0.225, 0.15), (0.055, 0.055, 0.065), mats['skin'], coll, 0.015, head)
    sphere("Guard | boné", (0, 0, 0.49), (0.25, 0.23, 0.11), mats['navy'], coll, head)
    cube("Guard | aba", (0, -0.19, 0.47), (0.35, 0.19, 0.045), mats['navy'], coll, 0.02, head)
    cube("Guard | faixa do boné", (0, -0.005, 0.43), (0.39, 0.26, 0.04), mats['shirt_light'], coll, 0.015, head)

    # Arms are articulated through named pivots. Forearms and hand props inherit motion.
    for side, sign in (("L", -1), ("R", 1)):
        shoulder = joint(f"RIG_Shoulder_{side}", body, (sign * 0.34, 0, 1.43))
        upper = cylinder(f"Guard | braço {side}", (sign * 0.045, 0, -0.17), 0.115, 0.38, mats['shirt'], coll, shoulder, vertices=10)
        upper.rotation_euler[1] = sign * math.radians(6)
        sphere(f"Guard | cotovelo {side}", (sign * 0.08, 0, -0.34), (0.115, 0.12, 0.115), mats['skin'], coll, shoulder, 12, 6)
        fore = joint(f"RIG_Forearm_{side}", shoulder, (sign * 0.08, 0, -0.34))
        cylinder(f"Guard | antebraço {side}", (sign * 0.045, 0, -0.16), 0.09, 0.34, mats['skin'], coll, fore, vertices=10)
        hand = joint(f"RIG_Hand_{side}", fore, (sign * 0.045, -0.005, -0.32))
        sphere(f"Guard | mão {side}", (0, -0.01, -0.015), (0.095, 0.075, 0.09), mats['skin'], coll, hand, 12, 6)
        rig[f"shoulder_{side}"] = shoulder
        rig[f"fore_{side}"] = fore
        rig[f"hand_{side}"] = hand

    for side, sign in (("L", -1), ("R", 1)):
        hip = joint(f"RIG_Hip_{side}", pelvis, (sign * 0.16, 0, 0.02))
        cylinder(f"Guard | calça superior {side}", (0, 0, -0.22), 0.15, 0.46, mats['pants'], coll, hip, vertices=10)
        knee = joint(f"RIG_Knee_{side}", hip, (0, 0, -0.43))
        sphere(f"Guard | joelho {side}", (0, 0, 0), (0.135, 0.15, 0.14), mats['pants'], coll, knee, 12, 6)
        cylinder(f"Guard | canela {side}", (0, 0, -0.20), 0.115, 0.40, mats['pants'], coll, knee, vertices=10)
        cube(f"Guard | sapato {side}", (0, -0.07, -0.43), (0.25, 0.39, 0.15), mats['shoe'], coll, 0.04, knee)
        rig[f"hip_{side}"] = hip
        rig[f"knee_{side}"] = knee

    # Flashlight at the raised right hand, with a restrained visible light cone.
    hand = rig['hand_R']
    lamp = cylinder("Guard | lanterna", (0, -0.03, 0.01), 0.065, 0.31, mats['metal'], coll, hand,
                    rotation=(0, math.radians(90), 0), vertices=12)
    lens = sphere("Guard | lente da lanterna", (0.17, -0.03, 0.01), (0.055, 0.072, 0.072), mats['lamp'], coll, hand, 12, 6)
    visibility_keys(lamp, 337)
    visibility_keys(lens, 337)
    beam = bpy.data.objects.new("Light cone | flashlight", None)
    coll.objects.link(beam)
    beam.parent = hand
    beam.location = (0.23, -0.07, -0.02)
    beam.rotation_euler[1] = math.radians(90)
    # A soft stylized cone; visibility is keyed only after the flashlight is switched on.
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=0.045, radius2=0.36, depth=1.75, location=(0, 0, 0))
    cone = bpy.context.object
    finish_mesh(cone, "Light cone | pixel haze", mats['beam'], coll)
    cone.parent = beam
    cone.location = (0.88, 0.0, 0.0)
    cone.rotation_euler[1] = math.radians(90)
    cone.hide_render = True
    spot_data=bpy.data.lights.new("Guard lantern | focused spot",'SPOT')
    spot_data.energy=0
    spot_data.color=(0.67,0.82,1.0)
    spot_data.spot_size=math.radians(48)
    spot_data.spot_blend=0.35
    spot=bpy.data.objects.new("Guard lantern | real light",spot_data)
    coll.objects.link(spot)
    spot.parent=beam
    spot.location=(0.20,-0.03,0.0)
    spot.rotation_euler=(0,math.radians(-90),0)
    for fr,power in ((1,0),(336,0),(337,210),(766,210),(767,0)):
        spot_data.energy=power
        spot_data.keyframe_insert(data_path='energy',frame=fr)
    rig['body'] = body
    rig['pelvis'] = pelvis
    rig['head'] = head
    rig['beam'] = beam
    rig['cone'] = cone
    return root, rig


def main():
    clear_scene()
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.eevee.taa_render_samples = 16
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.fps = FPS
    scene.frame_start = 1
    scene.frame_end = LAST_FRAME
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGB'
    scene.render.film_transparent = False
    scene.render.filepath = "/tmp/cc01_frames/CC01_"
    scene.view_settings.view_transform = 'AgX'
    scene.view_settings.look = 'AgX - Medium High Contrast'
    scene.world = bpy.data.worlds.new("CC01 | night world")
    scene.world.color = (0.006, 0.012, 0.045)
    scene.render.image_settings.color_depth = '8'
    scene.camera = None
    scene.unit_settings.system = 'METRIC'

    env = collection("01_ENVIRONMENT")
    char = collection("02_CHARACTERS")
    props = collection("03_CLUES_AND_PROPS")
    lights = collection("04_LIGHTS")
    cameras = collection("05_CAMERAS")
    title_coll = collection("06_TITLE_CARD")

    mats = {
        'wall': material("Night blue | stone", (0.035, 0.065, 0.17), 0.82),
        'wall_light': material("Blue | stone trim", (0.09, 0.16, 0.31), 0.76),
        'floor': material("Polished blue marble", (0.065, 0.105, 0.22), 0.24, 0.18),
        'floor_tile': material("Floor tile edge", (0.13, 0.20, 0.32), 0.32),
        'navy': material("Deep navy", (0.035, 0.06, 0.14), 0.78),
        'wood': material("Warm wood", (0.30, 0.14, 0.07), 0.62),
        'wood_light': material("Wood edge", (0.56, 0.30, 0.12), 0.54),
        'metal': material("Brushed metal", (0.30, 0.39, 0.49), 0.32, 0.58),
        'lamp': material("Emergency lamp", (0.70, 0.88, 1.0), 0.28, emission=(0.28, 0.62, 1.0)),
        'warm': material("Service door warm light", (1.0, 0.58, 0.18), 0.35, emission=(1.0, 0.32, 0.06)),
        'shirt': material("Guard uniform | blue", (0.22, 0.47, 0.68), 0.82),
        'shirt_light': material("Uniform seams", (0.48, 0.73, 0.86), 0.72),
        'pants': material("Uniform | navy trousers", (0.045, 0.075, 0.16), 0.88),
        'belt': material("Uniform | belt", (0.09, 0.10, 0.13), 0.55),
        'shoe': material("Uniform | shoes", (0.035, 0.045, 0.07), 0.68),
        'skin': material("Skin | warm brown", (0.43, 0.22, 0.13), 0.78),
        'eye': material("Eyes", (0.012, 0.012, 0.02), 0.34),
        'paper': material("Cold white", (0.72, 0.83, 0.93), 0.64),
        'beam': material("Lantern haze", (0.65, 0.79, 0.95), 0.3, emission=(0.07, 0.13, 0.24), alpha=0.035),
        'moth': material("Moth | pale wings", (0.88, 0.85, 0.67), 0.72),
        'feather': material("Grey feather", (0.55, 0.61, 0.63), 0.72),
        'plant': material("Leaves", (0.045, 0.19, 0.11), 0.85),
        'pot': material("Planter stone", (0.23, 0.29, 0.36), 0.86),
        'paint': material("Muted wall art", (0.10, 0.20, 0.34), 0.92),
        'title': material("Title | porcelain white", (0.82, 0.92, 1.0), 0.4, emission=(0.48, 0.73, 1.0)),
        'title_shadow': material("Title | blue shadow", (0.12, 0.28, 0.57), 0.5, emission=(0.07, 0.18, 0.48)),
    }

    # Broad hall, frontal three-quarter diorama proportions.
    cube("Hall | reflective floor", (0, 1.2, -0.19), (19.0, 19.0, 0.38), mats['floor'], env, 0.06)
    cube("Hall | rear wall", (0, 9.3, 4.6), (19.0, 0.45, 9.2), mats['wall'], env, 0.06)
    cube("Hall | left return", (-9.35, 1.0, 4.5), (0.40, 17.0, 9.0), mats['wall'], env, 0.05)
    cube("Hall | right return", (9.35, 1.0, 4.5), (0.40, 17.0, 9.0), mats['wall'], env, 0.05)
    cube("Hall | crown molding", (0, 8.98, 8.85), (18.6, 0.65, 0.22), mats['wall_light'], env, 0.04)
    for x in (-8.0, -4.5, -1.6, 1.4, 4.2, 8.0):
        cube("Hall | stone pilaster", (x, 8.94, 4.45), (0.38, 0.22, 8.75), mats['wall_light'], env, 0.045)
        cube("Hall | pilaster base", (x, 8.78, 0.42), (0.62, 0.45, 0.64), mats['wall_light'], env, 0.04)
        cube("Hall | pilaster cap", (x, 8.78, 8.42), (0.62, 0.45, 0.38), mats['wall_light'], env, 0.04)
    # Floor seams add scale without turning into a graphic grid.
    for x in range(-8, 9, 2):
        cube("Floor | fine seam", (x, 1.0, 0.014), (0.018, 16.0, 0.012), mats['floor_tile'], env)
    for y in range(-6, 9, 2):
        cube("Floor | fine seam", (0, y, 0.016), (18.0, 0.018, 0.012), mats['floor_tile'], env)

    # Central elevator and framed artwork.
    cube("Elevator | alcove", (0, 8.97, 3.0), (2.8, 0.12, 5.6), mats['navy'], env, 0.06)
    cube("Elevator | doors", (0, 8.82, 2.88), (2.05, 0.08, 4.7), mats['wall'], env, 0.04)
    cube("Elevator | seam", (0, 8.76, 2.88), (0.025, 0.025, 4.65), mats['wall_light'], env)
    cube("Elevator | indicator", (0.83, 8.72, 5.55), (0.15, 0.06, 0.26), mats['warm'], env, 0.02)
    for x, z in ((-6.5, 4.4), (4.7, 4.8)):
        cube("Gallery | frame", (x, 8.72, z), (2.25, 0.18, 2.45), mats['wood'], env, 0.055)
        cube("Gallery | canvas", (x, 8.60, z), (1.96, 0.08, 2.16), mats['paint'], env, 0.02)
        # Abstract, low-contrast shapes keep the room visually authored but avoid fake text.
        sphere("Gallery | painted moon", (x - 0.43, 8.53, z + 0.38), (0.30, 0.025, 0.30), mats['wall_light'], env, segments=20)
        cube("Gallery | painted horizon", (x, 8.53, z - 0.48), (1.55, 0.035, 0.18), mats['wall_light'], env, 0.02)

    # Service door stands ajar, with a narrow warm light leak.
    door_x, door_y = 8.05, 8.45
    cube("Service door | frame", (door_x, door_y, 1.55), (1.8, 0.28, 3.15), mats['wood'], env, 0.05)
    cube("Service door | dark opening", (door_x, door_y - 0.17, 1.55), (1.48, 0.08, 2.82), mats['navy'], env)
    door = cube("Service door | ajar", (door_x - 0.52, door_y - 0.30, 1.55), (1.40, 0.13, 2.82), mats['wood_light'], env, 0.025)
    door.rotation_euler[2] = math.radians(-20)
    cube("Service door | warm gap", (door_x + 0.50, door_y - 0.22, 1.54), (0.16, 0.035, 2.68), mats['warm'], env)
    cube("Service door | handle", (door_x - 0.88, door_y - 0.42, 1.45), (0.07, 0.08, 0.28), mats['metal'], env, 0.02)
    # A small warm interior glimpse gives the door depth.
    for z in (0.8, 1.4, 2.0):
        cube("Storage | shelf glimpse", (8.25, 8.0, z), (0.72, 0.50, 0.08), mats['wood'], env, 0.015)

    # Staircase, visible handrails and upper landing.
    stair_start_x, stair_y, n_steps = 1.8, 5.5, 16
    for i in range(n_steps):
        x = stair_start_x + i * 0.37
        z = 0.13 + i * 0.19
        cube(f"Stair | tread {i+1:02d}", (x, stair_y, z), (0.48, 3.1, 0.24), mats['wall_light'], env, 0.025)
        cube(f"Stair | nosing {i+1:02d}", (x - 0.19, stair_y - 0.02, z + 0.12), (0.06, 3.0, 0.035), mats['paper'], env, 0.01)
    for y in (4.05, 6.95):
        pts = [(stair_start_x - 0.25, y, 0.72), (stair_start_x + 5.5, y, 3.75)]
        curve_line("Stair | warm handrail", pts, 0.075, mats['wood_light'], env)
        for i in range(0, 16, 2):
            x = stair_start_x + i * 0.37
            z = 0.62 + i * 0.19
            cylinder("Stair | baluster", (x, y, z), 0.035, 0.72, mats['metal'], env, rotation=(0, 0, 0), vertices=8)
    cube("Stair | upper landing", (8.0, 6.15, 3.25), (2.8, 4.7, 0.35), mats['wall_light'], env, 0.04)

    # Benches, planters, and emergency fixtures.
    for x, y in ((-6.0, 3.0), (4.0, 3.3), (-7.3, -4.8), (7.6, -4.5)):
        cube("Hall | bench seat", (x, y, 0.64), (2.2, 0.72, 0.16), mats['wood'], env, 0.055)
        cube("Hall | bench top", (x, y - 0.03, 0.73), (2.14, 0.68, 0.07), mats['wood_light'], env, 0.025)
        for dx in (-0.82, 0.82):
            cube("Hall | bench leg", (x+dx, y, 0.31), (0.12, 0.54, 0.62), mats['navy'], env, 0.02)
    for x, y in ((-8.1, 3.1), (-3.4, 6.8), (2.2, 7.0), (7.8, 7.0)):
        cube("Planter | pot", (x, y, 0.44), (0.75, 0.72, 0.88), mats['pot'], env, 0.09)
        cylinder("Planter | soil", (x, y, 0.91), 0.29, 0.05, mats['navy'], env, vertices=16)
        for j in range(7):
            angle = j * math.tau / 7
            start = (x, y, 0.92)
            end = (x + math.cos(angle)*0.52, y + math.sin(angle)*0.38, 1.72 + (j%3)*0.18)
            curve_line("Planter | leaf stem", [start, end], 0.035, mats['plant'], env)
            sphere("Planter | leaf", (end[0], end[1], end[2]), (0.12, 0.08, 0.38), mats['plant'], env, segments=10, rings=6)

    # Coffee is a persistent clue on a real side table in the opening shot.
    table_x, table_y = -5.3, -2.2
    cube("Coffee table | top", (table_x, table_y, 0.80), (1.15, 0.82, 0.12), mats['wood_light'], props, 0.06)
    cube("Coffee table | apron", (table_x, table_y, 0.65), (0.95, 0.66, 0.19), mats['wood'], props, 0.035)
    for dx in (-0.43, 0.43):
        for dy in (-0.27, 0.27):
            cube("Coffee table | leg", (table_x+dx, table_y+dy, 0.34), (0.09, 0.09, 0.62), mats['wood'], props, 0.02)
    cylinder("Coffee | cup", (table_x, table_y, 0.98), 0.16, 0.28, mats['paper'], props, vertices=16, bevel=0.02)
    cylinder("Coffee | dark surface", (table_x, table_y, 1.13), 0.125, 0.012, mats['wood'], props, vertices=16)
    curve_line("Coffee | handle", [(table_x+0.14, table_y, 1.08),(table_x+0.30,table_y,1.08),(table_x+0.30,table_y,0.94),(table_x+0.14,table_y,0.94)], 0.035, mats['paper'], props)

    # Three-toed tracks, dust and feather. The marks point toward the stairs.
    track_positions = [(-0.1, 0.1), (0.45, 0.62), (0.98, 1.12), (1.44, 1.66), (1.88, 2.17)]
    tracks=[]
    for i, (x,y) in enumerate(track_positions):
        for toe in (-1,0,1):
            tracks.append(sphere(f"Track {i+1} | toe {toe+1}", (x + toe*0.14, y, 0.028), (0.072, 0.14, 0.018), mats['paper'], props, segments=10, rings=6))
        tracks.append(sphere(f"Track {i+1} | palm", (x, y-0.12, 0.025), (0.12, 0.11, 0.018), mats['paper'], props, segments=10, rings=6))
    for track in tracks:
        visibility_keys(track,481)
    feather_root = bpy.data.objects.new("Clue | drifting grey feather", None)
    props.objects.link(feather_root)
    feather_root.location = (7.55, 7.48, 0.12)
    feather = sphere("Clue | feather body", (0,0,0), (0.10,0.42,0.025), mats['feather'], props, feather_root, 12, 6)
    feather.rotation_euler[1] = math.radians(18)
    feather_quill=curve_line("Clue | feather quill", [(7.55,7.48,0.15),(7.55,7.80,0.17)], 0.012, mats['paper'], props)
    visibility_keys(feather,625)
    visibility_keys(feather_quill,625)

    # Moth crosses the beam briefly near the end; it is a clue, not a reveal of Moema.
    moth_root = bpy.data.objects.new("Clue | small moth", None)
    props.objects.link(moth_root)
    moth_root.location = (-1.6, 1.8, 1.3)
    moth_body=sphere("Moth | body", (0,0,0), (0.07,0.06,0.18), mats['wood'], props, moth_root, 10, 6)
    wing_l = sphere("Moth | left wing", (-0.14, -0.01, 0.03), (0.17,0.04,0.11), mats['moth'], props, moth_root, 10, 6)
    wing_r = sphere("Moth | right wing", (0.14, -0.01, 0.03), (0.17,0.04,0.11), mats['moth'], props, moth_root, 10, 6)
    for moth_part in (moth_body,wing_l,wing_r):
        visibility_keys(moth_part,625,767)
    for fr, loc in ((625,(-1.6,1.8,1.3)), (660,(0.0,2.1,1.85)), (700,(2.0,2.5,1.55)), (740,(3.4,3.1,1.8))):
        key(moth_root, fr, loc=loc)
    set_constant_keys(moth_root)
    # Feather shifts only slightly from air movement near the door.
    key(feather_root, 1, loc=(7.55,7.48,0.12), rot=(0,0,math.radians(18)))
    key(feather_root, 700, loc=(7.55,7.48,0.12), rot=(0,0,math.radians(15)))
    key(feather_root, 740, loc=(7.58,7.48,0.15), rot=(0,0,math.radians(23)))

    # Digital clock, built into the back wall as an emissive display.
    clock_back = cube("Digital clock | backing", (-5.9,8.69,6.65), (2.35,0.12,0.62), mats['navy'], props, 0.035)
    clock_glow = material("Digital clock | glow", (0.57,0.82,1.0), 0.28, emission=(0.20,0.52,1.0))
    clock_text = add_text("Digital clock | 02:00", "02:00", (-5.9,8.60,6.66), 0.42, clock_glow, props, rotation=(math.radians(90),0,0))

    root, rig = create_guard(char, mats)
    # Guard turns toward the staircase, raises the lamp, crouches at the clue and climbs.
    route = [(1,(-3.6,-1.7,0.0)), (228,(-3.6,-1.7,0.0)), (240,(-3.6,-1.7,0.0)),
             (336,(-3.6,-1.7,0.0)), (432,(-2.9,-0.9,0.0)), (480,(-2.7,-0.5,0.0)),
             (624,(1.4,4.35,0.04)), (730,(4.6,5.35,1.8)), (750,(4.9,5.5,1.95)), (766,(5.2,5.7,2.15))]
    for fr, loc in route:
        key(root, fr, loc=loc)
    for fr, rz in ((1,0),(228,0),(240,math.radians(35)),(336,math.radians(35)),(432,math.radians(58)),(480,math.radians(35)),(624,math.radians(35)),(766,math.radians(35))):
        key(root, fr, rot=(0,0,rz))
    set_constant_keys(root)
    # Look/glance beats.
    for fr, rot in ((1,(0,0,0)), (97,(0,0,math.radians(-12))), (150,(0,0,0)), (228,(0,0,0)), (240,(0,0,math.radians(-16))), (336,(0,0,math.radians(-4))), (432,(0,0,math.radians(8))), (480,(0,0,math.radians(-8))), (624,(0,0,math.radians(-5)))):
        key(rig['head'], fr, rot=rot)
    set_constant_keys(rig['head'])
    # Uniform adjustment, lamp gesture, crouch and walking rhythm.
    for fr, rot in ((1,(0,0,0)),(150,(0,0,0)),(170,(math.radians(-25),0,math.radians(-10))),(190,(0,0,0)),(336,(0,0,0)),(360,(math.radians(-68),0,math.radians(18))),(420,(math.radians(-65),0,math.radians(14))),(432,(math.radians(-30),0,0)),(480,(0,0,0)),(624,(0,0,0))):
        key(rig['shoulder_R'], fr, rot=rot)
    set_constant_keys(rig['shoulder_R'])
    for fr, rot in ((1,(0,0,0)),(336,(0,0,0)),(360,(math.radians(-22),0,0)),(420,(math.radians(-18),0,0)),(432,(0,0,0))):
        key(rig['fore_R'], fr, rot=rot)
    set_constant_keys(rig['fore_R'])
    for fr, z in ((1,0),(336,0),(360,math.radians(-8)),(390,math.radians(12)),(420,math.radians(-6)),(480,0),(624,0)):
        key(rig['beam'], fr, rot=(0,0,z))
    set_constant_keys(rig['beam'])
    for fr, z in ((1,0),(480,0),(490,math.radians(-16)),(600,math.radians(-16)),(624,0),(766,0)):
        key(rig['pelvis'], fr, loc=(0,0,z))
    set_constant_keys(rig['pelvis'])
    # Step-synced leg swing while approaching, then articulated climb.
    for base in (432,456,480,504,528,552,576,600,624):
        ph=(base-432)/24.0*math.tau
        key(rig['hip_L'], base, rot=(math.sin(ph)*0.42,0,0))
        key(rig['hip_R'], base, rot=(-math.sin(ph)*0.42,0,0))
        key(rig['knee_L'], base, rot=(max(0,math.sin(ph))*0.32,0,0))
        key(rig['knee_R'], base, rot=(max(0,-math.sin(ph))*0.32,0,0))
    for base, ph in ((624,0),(642,0.55),(660,0),(678,0.55),(696,0),(714,0.55),(730,0)):
        key(rig['hip_L'], base, rot=(ph,0,0))
        key(rig['hip_R'], base, rot=(-ph,0,0))
        key(rig['knee_L'], base, rot=(max(0,ph)*0.45,0,0))
        key(rig['knee_R'], base, rot=(max(0,-ph)*0.45,0,0))
    for name in ('hip_L','hip_R','knee_L','knee_R'):
        set_constant_keys(rig[name])

    # Cues and markers live in scene frame time, so camera cuts remain editable.
    # Emergency lights and a dim warm door light.
    for x in (-3.0, 1.2):
        cube("Emergency lamp | housing", (x,8.45,6.25), (0.42,0.18,0.9), mats['navy'], env, 0.05)
        cube("Emergency lamp | diffuser", (x,8.32,6.25), (0.27,0.08,0.64), mats['lamp'], env, 0.025)
        light("Emergency lamp | blue spill", 'AREA', (x,7.7,6.0), (0.24,0.47,1.0), 300, lights, 4.0, (x,1,1))
    light("Blue moon fill", 'AREA', (0,2,9.0), (0.24,0.36,1.0), 850, lights, 12.0, (0,2,0))
    light("Soft front fill", 'AREA', (-6,-3,5.4), (0.38,0.54,0.88), 450, lights, 7.0, (0,3,1.5))
    light("Service gap | warm spill", 'AREA', (8.1,7.2,1.6), (1.0,0.42,0.12), 180, lights, 2.0, (5,2,0.2))
    light("Stair landing | cool spill", 'AREA', (7.3,6.7,5.1), (0.36,0.60,1.0), 260, lights, 3.0, (4,3,1))

    # Two suspense cues are animated as a visual pulse rather than strobing.
    stair_flash = light("Stair emergency pulse", 'AREA', (4.4,7.2,4.4), (0.55,0.78,1.0), 0, lights, 2.3, (3,3,0))
    for fr, power in ((1,0),(228,0),(240,800),(246,250),(252,0),(LAST_FRAME,0)):
        stair_flash.data.energy=power
        stair_flash.data.keyframe_insert(data_path="energy", frame=fr)
    for fc in fcurves(stair_flash.data):
        for kp in fc.keyframe_points: kp.interpolation='BEZIER'

    # Cameras are separate authored shots; cuts follow the original CC01 beats.
    def camera(name, loc, target, lens=46):
        data=bpy.data.cameras.new(name)
        ob=bpy.data.objects.new(name,data)
        cameras.objects.link(ob)
        ob.location=loc
        data.lens=lens
        aim(ob,target)
        return ob
    cam_wide=camera("CAM_01 | Establishing foyer", (0,-24,8.2),(0,3.7,3.8),39)
    cam_guard=camera("CAM_02 | Guard and coffee",(-4.0,-13.5,4.4),(-4.0,-0.2,1.4),50)
    cam_stair=camera("CAM_03 | Impact at stair",(3.6,-13.0,6.1),(4.0,4.8,3.3),48)
    cam_lamp=camera("CAM_04 | Lantern gesture",(-1.5,-11.7,3.5),(0.2,2.0,1.1),49)
    cam_tracks=camera("CAM_05 | Tracks in the dust",(0.3,-7.5,2.3),(1.0,1.5,0.2),52)
    cam_walk=camera("CAM_06 | Toward the upper floor",(2.1,-19,6.1),(3.0,4.0,2.5),43)
    # Keep the title camera and its black card behind the wide camera so the
    # card cannot accidentally occlude any of the room shots.
    cam_title=camera("CAM_07 | Title card",(0,-100,3),(0,-99,3),48)
    for fr, cam, name in ((1,cam_wide,"01 | Midnight"),(97,cam_guard,"02 | Guard"),(241,cam_stair,"03 | Impact"),(337,cam_lamp,"04 | Lantern"),(481,cam_tracks,"05 | Evidence"),(625,cam_walk,"06 | The trail"),(767,cam_title,"07 | Title")):
        marker=scene.timeline_markers.new(name,frame=fr)
        marker.camera=cam
    scene.camera=cam_wide

    # Camera pushes are subtle and contained to the existing foyer.
    for cam, start, end, delta in ((cam_guard,97,230,(0.0,0.4,-0.12)),(cam_stair,241,330,(0.15,0.0,0.0)),
                                   (cam_lamp,337,475,(0.20,0.1,0.0)),(cam_tracks,481,620,(0.1,0.2,-0.1)),
                                   (cam_walk,625,766,(0.0,0.45,0.0))):
        p=cam.location.copy()
        key(cam,start,loc=p)
        key(cam,end,loc=(p.x+delta[0],p.y+delta[1],p.z+delta[2]))
        set_constant_keys(cam)

    # The last shot uses a camera-mounted black card so the 3D set can remain
    # editable and the title stays cleanly isolated in the final render.
    scene.world.use_nodes=True
    bg=scene.world.node_tree.nodes.get('Background')
    bg.inputs['Color'].default_value=(0.006,0.015,0.05,1.0)
    bg.inputs['Strength'].default_value=0.7
    cam_title.data.type='ORTHO'
    cam_title.data.ortho_scale=12.0
    black=material("Title | absolute black",(0,0,0),1.0,emission=(0,0,0))
    card=cube("Title | black card",(0,0,-4.2),(30,18,0.04),black,title_coll,parent=cam_title)
    shadow=add_text("Title | blue offset","O MISTÉRIO DO\nESPAÇO CULTURAL",(-0.025,-0.025,-4.14),0.66,mats['title_shadow'],title_coll)
    title=add_text("Title | O Mistério do Espaço Cultural","O MISTÉRIO DO\nESPAÇO CULTURAL",(0,0,-4.13),0.66,mats['title'],title_coll)
    shadow.parent=cam_title
    title.parent=cam_title
    for o in (shadow,title):
        o.scale=(0.92,0.92,0.92)
        o.keyframe_insert(data_path='scale',frame=783)
        o.scale=(1,1,1)
        o.keyframe_insert(data_path='scale',frame=800)
        o.keyframe_insert(data_path='scale',frame=LAST_FRAME)
        set_constant_keys(o)

    # Opening and end fades are encoded as world exposure keyframes for render.
    # The environment is held black between the final cut and the title reveal.
    scene.view_settings.exposure=-1.0
    scene.view_settings.keyframe_insert(data_path='exposure',frame=1)
    scene.view_settings.exposure=0.0
    scene.view_settings.keyframe_insert(data_path='exposure',frame=60)
    for fc in fcurves(scene):
        for kp in fc.keyframe_points:
            kp.interpolation='BEZIER'

    # Mild vignette-like contrast is achieved through lighting and composition;
    # no blur/posterization is used so silhouettes remain crisp at 720p.
    scene.render.image_settings.file_format='PNG'
    scene.frame_set(1)
    os.makedirs(os.path.dirname(BLEND_OUT),exist_ok=True)
    os.makedirs("/tmp/cc01_frames",exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
    print(f"CC01 Blender source saved: {BLEND_OUT}; frames={scene.frame_end}; fps={scene.render.fps}")


if __name__ == "__main__":
    main()
