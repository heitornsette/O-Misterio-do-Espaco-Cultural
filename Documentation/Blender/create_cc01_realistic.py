"""Realistic 3D reconstruction of the Unity CC01 opening.

Run from the project root:
    blender --background --python Documentation/Blender/create_cc01_realistic.py

The silent 1280x720 render is kept at 24 fps so the Unity VideoPlayer can
continue to drive the existing Portuguese captions and sound cues.
"""
import math
import os
import bpy
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
OUT_BLEND = os.path.join(os.path.dirname(__file__), "CC01_Cinematic.blend")
FPS = 24
DURATION = 35.7
END_FRAME = round(FPS * DURATION)


def collection(name):
    result = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(result)
    return result


def move_to(obj, coll):
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    coll.objects.link(obj)
    return obj


def set_parent(obj, parent, loc=None, rot=None):
    obj.parent = parent
    if loc is not None:
        obj.location = loc
    if rot is not None:
        obj.rotation_euler = rot
    return obj


def pbr(name, color, roughness=0.5, metallic=0.0, texture=None,
        color_b=None, bump_strength=0.0, bump_distance=0.02):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    shader = nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Roughness"].default_value = roughness
    shader.inputs["Metallic"].default_value = metallic
    if "IOR" in shader.inputs:
        shader.inputs["IOR"].default_value = 1.46
    if texture:
        texcoord = nodes.new("ShaderNodeTexCoord")
        noise = nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = texture
        noise.inputs["Detail"].default_value = 4.0
        noise.inputs["Roughness"].default_value = 0.72
        links.new(texcoord.outputs["Generated"], noise.inputs["Vector"])
        if color_b:
            ramp = nodes.new("ShaderNodeValToRGB")
            ramp.color_ramp.elements[0].position = 0.18
            ramp.color_ramp.elements[0].color = (*color, 1.0)
            ramp.color_ramp.elements[1].position = 0.82
            ramp.color_ramp.elements[1].color = (*color_b, 1.0)
            links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
            links.new(ramp.outputs["Color"], shader.inputs["Base Color"])
        if bump_strength > 0:
            bump = nodes.new("ShaderNodeBump")
            bump.inputs["Strength"].default_value = bump_strength
            bump.inputs["Distance"].default_value = bump_distance
            links.new(noise.outputs["Fac"], bump.inputs["Height"])
            links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    return mat


def emission(name, color, strength=1.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeEmission")
    shader.inputs["Color"].default_value = (*color, 1.0)
    shader.inputs["Strength"].default_value = strength
    mat.node_tree.links.new(shader.outputs["Emission"], out.inputs["Surface"])
    return mat


def volume_material(name, color, density=0.025, anisotropy=0.45):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    volume = nodes.new("ShaderNodeVolumePrincipled")
    volume.inputs["Color"].default_value = (*color, 1.0)
    volume.inputs["Density"].default_value = density
    volume.inputs["Anisotropy"].default_value = anisotropy
    mat.node_tree.links.new(volume.outputs["Volume"], out.inputs["Volume"])
    return mat


def flashlight_scattering_material():
    """A very transparent, noisy surface tint reinforces Eevee's subtle volume beam."""
    mat = bpy.data.materials.new("Flashlight | faint blue scattering")
    mat.use_nodes = True
    mat.surface_render_method = "BLENDED"
    nodes = mat.node_tree.nodes
    nodes.clear()
    links = mat.node_tree.links
    output = nodes.new("ShaderNodeOutputMaterial")
    transparent = nodes.new("ShaderNodeBsdfTransparent")
    glow = nodes.new("ShaderNodeEmission")
    glow.inputs["Color"].default_value = (0.29, 0.51, 0.91, 1.0)
    glow.inputs["Strength"].default_value = 0.72
    mix = nodes.new("ShaderNodeMixShader")
    mix.inputs[0].default_value = 0.012
    links.new(transparent.outputs["BSDF"], mix.inputs[1])
    links.new(glow.outputs["Emission"], mix.inputs[2])
    links.new(mix.outputs["Shader"], output.inputs["Surface"])
    volume = nodes.new("ShaderNodeVolumePrincipled")
    volume.inputs["Color"].default_value = (0.67, 0.78, 0.96, 1.0)
    volume.inputs["Density"].default_value = 0.018
    volume.inputs["Anisotropy"].default_value = 0.6
    links.new(volume.outputs["Volume"], output.inputs["Volume"])
    return mat


def smooth(obj):
    if obj.type == "MESH":
        for poly in obj.data.polygons:
            poly.use_smooth = True
    return obj


def box(name, loc, size, mat, coll, bevel=0.0, parent=None, rot=None):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0))
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to(obj, coll)
    if mat:
        obj.data.materials.append(mat)
    if parent:
        set_parent(obj, parent, loc, rot)
    else:
        obj.location = loc
        if rot:
            obj.rotation_euler = rot
    if bevel > 0:
        mod = obj.modifiers.new("Soft manufactured edges", "BEVEL")
        mod.width = bevel
        mod.segments = 3
        mod.profile = 0.5
        obj.modifiers.new("Weighted highlights", "WEIGHTED_NORMAL")
    return obj


def sphere(name, loc, scale, mat, coll, parent=None, segments=32, rings=20):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=(0, 0, 0))
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to(obj, coll)
    if mat:
        obj.data.materials.append(mat)
    smooth(obj)
    if parent:
        set_parent(obj, parent, loc)
    else:
        obj.location = loc
    return obj


def cylinder(name, loc, radius, depth, mat, coll, parent=None, rot=None,
             vertices=32, bevel=0.0):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                        location=(0, 0, 0))
    obj = bpy.context.object
    obj.name = name
    move_to(obj, coll)
    if mat:
        obj.data.materials.append(mat)
    if parent:
        set_parent(obj, parent, loc, rot)
    else:
        obj.location = loc
        if rot:
            obj.rotation_euler = rot
    smooth(obj)
    if bevel > 0:
        mod = obj.modifiers.new("Rounded edges", "BEVEL")
        mod.width = bevel
        mod.segments = 3
        obj.modifiers.new("Weighted highlights", "WEIGHTED_NORMAL")
    return obj


def curve(name, points, radius, mat, coll, parent=None, resolution=3):
    data = bpy.data.curves.new(name, "CURVE")
    data.dimensions = "3D"
    data.resolution_u = 12
    data.bevel_depth = radius
    data.bevel_resolution = resolution
    spline = data.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for p, coord in zip(spline.points, points):
        p.co = (*coord, 1.0)
    obj = bpy.data.objects.new(name, data)
    coll.objects.link(obj)
    data.materials.append(mat)
    if parent:
        obj.parent = parent
    return obj


def capsule(name, a, b, radius, mat, coll, parent):
    av, bv = Vector(a), Vector(b)
    delta = bv - av
    length = delta.length
    obj = cylinder(name, tuple((av + bv) * 0.5), radius, length, mat, coll,
                   parent=parent, vertices=32)
    obj.rotation_euler = delta.to_track_quat("Z", "Y").to_euler()
    sphere(name + " | joint A", tuple(av), (radius, radius, radius), mat, coll,
           parent, segments=24, rings=16)
    sphere(name + " | joint B", tuple(bv), (radius, radius, radius), mat, coll,
           parent, segments=24, rings=16)
    return obj


def add_text(name, text, loc, size, mat, coll, rotation=(0.0, 0.0, 0.0),
             align="CENTER", parent=None, font=None):
    data = bpy.data.curves.new(name, "FONT")
    data.body = text
    data.size = size
    data.align_x = align
    data.align_y = "CENTER"
    data.extrude = 0.001
    data.bevel_depth = 0.0005
    if font:
        data.font = font
    obj = bpy.data.objects.new(name, data)
    coll.objects.link(obj)
    data.materials.append(mat)
    if parent:
        set_parent(obj, parent, loc, rotation)
    else:
        obj.location = loc
        obj.rotation_euler = rotation
    return obj


def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def key_transform(obj, frame, loc=None, rot=None, scale=None):
    if loc is not None:
        obj.location = loc
        obj.keyframe_insert(data_path="location", frame=frame)
    if rot is not None:
        obj.rotation_euler = rot
        obj.keyframe_insert(data_path="rotation_euler", frame=frame)
    if scale is not None:
        obj.scale = scale
        obj.keyframe_insert(data_path="scale", frame=frame)


def smooth_animation(owner):
    ad = getattr(owner, "animation_data", None)
    action = getattr(ad, "action", None) if ad else None
    if not action:
        return
    curves = getattr(action, "fcurves", None)
    if curves is None:
        slot = getattr(ad, "action_slot", None)
        curves = []
        for layer in action.layers:
            for strip in layer.strips:
                if strip.type == "KEYFRAME":
                    bag = strip.channelbag(slot) if slot else None
                    if bag:
                        curves.extend(bag.fcurves)
    for fc in curves:
        for point in fc.keyframe_points:
            point.interpolation = "BEZIER"
            point.handle_left_type = "AUTO_CLAMPED"
            point.handle_right_type = "AUTO_CLAMPED"


def visibility(obj, start, end=None):
    obj.hide_render = True
    obj.keyframe_insert(data_path="hide_render", frame=max(1, start - 1))
    obj.hide_render = False
    obj.keyframe_insert(data_path="hide_render", frame=start)
    if end is not None:
        obj.hide_render = False
        obj.keyframe_insert(data_path="hide_render", frame=end)
        obj.hide_render = True
        obj.keyframe_insert(data_path="hide_render", frame=end + 1)
    ad = obj.animation_data
    if ad and ad.action:
        curves = getattr(ad.action, "fcurves", [])
        for fc in curves:
            if fc.data_path == "hide_render":
                for point in fc.keyframe_points:
                    point.interpolation = "CONSTANT"


def area_light(name, loc, target, color, power, size, coll):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = power
    data.shape = "DISK"
    data.size = size
    data.color = color
    obj = bpy.data.objects.new(name, data)
    coll.objects.link(obj)
    obj.location = loc
    aim(obj, target)
    return obj


def spot_light(name, loc, target, color, power, angle, coll, blend=0.35):
    data = bpy.data.lights.new(name, "SPOT")
    data.energy = power
    data.color = color
    data.spot_size = math.radians(angle)
    data.spot_blend = blend
    data.shadow_soft_size = 0.06
    obj = bpy.data.objects.new(name, data)
    coll.objects.link(obj)
    obj.location = loc
    aim(obj, target)
    return obj


def make_target(name, loc, coll):
    target = bpy.data.objects.new(name, None)
    coll.objects.link(target)
    target.empty_display_type = "SPHERE"
    target.empty_display_size = 0.08
    target.location = loc
    return target


def make_camera(name, start_loc, start_target, lens, coll, focus_distance=8.0):
    data = bpy.data.cameras.new(name)
    data.lens = lens
    data.dof.use_dof = True
    data.dof.focus_distance = focus_distance
    data.dof.aperture_fstop = 5.6
    obj = bpy.data.objects.new(name, data)
    coll.objects.link(obj)
    obj.location = start_loc
    aim(obj, start_target)
    return obj


def camera_keys(camera, start_frame, end_frame, loc0, target0, loc1, target1):
    camera.location = loc0
    aim(camera, target0)
    camera.keyframe_insert(data_path="location", frame=start_frame)
    camera.keyframe_insert(data_path="rotation_euler", frame=start_frame)
    camera.location = loc1
    aim(camera, target1)
    camera.keyframe_insert(data_path="location", frame=end_frame)
    camera.keyframe_insert(data_path="rotation_euler", frame=end_frame)
    smooth_animation(camera)


def detail_frame(name, center, width, height, mat, coll, parent=None):
    outer = box(name + " | frame", center, (width, 0.12, height), mat, coll, 0.035, parent)
    return outer


def make_materials():
    return {
        "plaster": pbr("Wall | midnight blue lime plaster", (0.045, 0.085, 0.19), 0.78, texture=9.0, color_b=(0.075, 0.13, 0.25), bump_strength=0.12, bump_distance=0.018),
        "panel": pbr("Wall | satin blue enamel panels", (0.055, 0.10, 0.22), 0.36, texture=32.0, color_b=(0.09, 0.16, 0.29), bump_strength=0.045, bump_distance=0.006),
        "stone": pbr("Stone | blue limestone", (0.11, 0.16, 0.24), 0.36, texture=5.0, color_b=(0.20, 0.26, 0.33), bump_strength=0.10, bump_distance=0.012),
        "floor": pbr("Floor | honed blue granite", (0.07, 0.11, 0.18), 0.20, 0.10, texture=3.6, color_b=(0.19, 0.24, 0.31), bump_strength=0.06, bump_distance=0.007),
        "grout": pbr("Floor | dark grout", (0.025, 0.04, 0.075), 0.64),
        "wood": pbr("Wood | smoked oak", (0.16, 0.075, 0.035), 0.32, texture=5.0, color_b=(0.38, 0.20, 0.08), bump_strength=0.12, bump_distance=0.012),
        "wood_trim": pbr("Wood | oiled handrail", (0.19, 0.09, 0.04), 0.23, texture=4.0, color_b=(0.44, 0.24, 0.11), bump_strength=0.08, bump_distance=0.008),
        "brass": pbr("Metal | aged brushed brass", (0.34, 0.20, 0.075), 0.24, 0.82, texture=90.0, color_b=(0.52, 0.34, 0.13), bump_strength=0.025, bump_distance=0.002),
        "steel": pbr("Metal | brushed stainless steel", (0.27, 0.34, 0.43), 0.22, 0.86, texture=130.0, color_b=(0.47, 0.55, 0.63), bump_strength=0.025, bump_distance=0.002),
        "black_metal": pbr("Metal | dark anodized", (0.025, 0.035, 0.052), 0.27, 0.72),
        "glass": pbr("Glass | smoked blue", (0.04, 0.10, 0.18), 0.12, 0.15),
        "cotton": pbr("Uniform | blue cotton weave", (0.035, 0.16, 0.32), 0.78, texture=190.0, color_b=(0.085, 0.29, 0.49), bump_strength=0.08, bump_distance=0.003),
        "shirt_detail": pbr("Uniform | seam and pocket fabric", (0.055, 0.21, 0.38), 0.82, texture=180.0, color_b=(0.10, 0.31, 0.47), bump_strength=0.06, bump_distance=0.0025),
        "navy": pbr("Uniform | deep navy twill", (0.018, 0.027, 0.052), 0.86, texture=230.0, color_b=(0.055, 0.072, 0.105), bump_strength=0.07, bump_distance=0.0025),
        "trousers": pbr("Uniform | slate blue trouser twill", (0.055, 0.095, 0.15), 0.82, texture=180.0, color_b=(0.12, 0.18, 0.26), bump_strength=0.055, bump_distance=0.0025),
        "leather": pbr("Leather | black grain", (0.018, 0.022, 0.028), 0.31, texture=100.0, color_b=(0.055, 0.06, 0.065), bump_strength=0.08, bump_distance=0.003),
        "skin": pbr("Skin | warm medium-brown", (0.20, 0.075, 0.038), 0.46, texture=92.0, color_b=(0.34, 0.15, 0.075), bump_strength=0.06, bump_distance=0.003),
        "skin_light": pbr("Skin | facial planes", (0.29, 0.11, 0.055), 0.43, texture=95.0, color_b=(0.38, 0.17, 0.08), bump_strength=0.035, bump_distance=0.002),
        "hair": pbr("Hair | charcoal black", (0.012, 0.012, 0.016), 0.56, texture=120.0, color_b=(0.045, 0.04, 0.035), bump_strength=0.04, bump_distance=0.002),
        "eye_white": pbr("Eyes | warm ivory", (0.65, 0.59, 0.49), 0.28),
        "eye_dark": pbr("Eyes | dark brown", (0.035, 0.016, 0.009), 0.22),
        "coffee": pbr("Coffee | dark roasted surface", (0.035, 0.014, 0.005), 0.18),
        "ceramic": pbr("Coffee | warm white porcelain", (0.72, 0.67, 0.55), 0.18),
        "leaf": pbr("Plants | waxy deep green foliage", (0.012, 0.055, 0.025), 0.37, texture=18.0, color_b=(0.045, 0.16, 0.06), bump_strength=0.09, bump_distance=0.004),
        "soil": pbr("Planter | dark soil", (0.045, 0.025, 0.015), 0.92, texture=25.0, color_b=(0.12, 0.075, 0.035), bump_strength=0.2, bump_distance=0.02),
        "footprint": pbr("Clue | dusty charcoal residue", (0.085, 0.085, 0.075), 0.94, texture=18.0, color_b=(0.14, 0.135, 0.12), bump_strength=0.08, bump_distance=0.0015),
        "feather": pbr("Clue | pale grey feather", (0.44, 0.46, 0.46), 0.6, texture=70.0, color_b=(0.68, 0.67, 0.62), bump_strength=0.04, bump_distance=0.002),
        "moth": pbr("Clue | moth wings", (0.72, 0.65, 0.45), 0.48, texture=25.0, color_b=(0.92, 0.82, 0.60), bump_strength=0.045, bump_distance=0.002),
        "warm_emit": emission("Light | warm amber emission", (1.0, 0.39, 0.10), 2.2),
        "cool_emit": emission("Light | cold blue emission", (0.30, 0.55, 1.0), 1.25),
        "title": emission("Title | warm white lettering", (0.82, 0.88, 1.0), 1.6),
        "title_glow": emission("Title | blue edge glow", (0.20, 0.39, 0.80), 1.1),
    }


def build_room(env, props, lights, mats, font):
    # Polished tiled floor and quiet, cool architectural shell.
    box("Hall | structural floor slab", (0.0, 1.3, -0.22), (20.0, 18.5, 0.38), mats["grout"], env)
    for x in range(-9, 10, 2):
        for y in range(-6, 10, 2):
            tile_mat = mats["floor"]
            box("Floor | individually honed granite tile",
                (x + 0.99, y + 0.99, -0.015), (1.96, 1.96, 0.06),
                tile_mat, env, bevel=0.012)
    box("Hall | rear wall", (0.0, 9.12, 4.15), (19.2, 0.42, 8.3), mats["plaster"], env, 0.05)
    box("Hall | left return wall", (-9.48, 1.2, 4.0), (0.34, 16.0, 8.0), mats["plaster"], env, 0.04)
    box("Hall | right return wall", (9.48, 1.2, 4.0), (0.34, 16.0, 8.0), mats["plaster"], env, 0.04)
    box("Hall | baseboard", (0.0, 8.84, 0.20), (18.9, 0.12, 0.34), mats["stone"], env, 0.025)
    box("Hall | ceiling beam", (0.0, 8.82, 7.95), (19.0, 0.35, 0.25), mats["stone"], env, 0.04)

    for x in (-8.1, -5.4, -2.7, 0.0, 2.8, 5.6, 8.3):
        box("Hall | fluted stone pilaster", (x, 8.80, 4.1), (0.34, 0.26, 7.55), mats["stone"], env, 0.045)
        box("Hall | pilaster foot", (x, 8.59, 0.52), (0.56, 0.42, 0.72), mats["panel"], env, 0.045)
        box("Hall | pilaster capital", (x, 8.59, 7.65), (0.56, 0.42, 0.42), mats["panel"], env, 0.045)
        for side in (-1, 1):
            box("Hall | fluting highlight", (x + side * 0.095, 8.655, 4.1),
                (0.024, 0.018, 6.65), mats["panel"], env, 0.008)

    # Central entrance, inset panels and glazed transom.
    door_x = -0.9
    box("Entrance | deep alcove", (door_x, 8.76, 2.7), (3.10, 0.16, 5.45), mats["black_metal"], env, 0.04)
    box("Entrance | left leaf", (door_x - 0.70, 8.63, 2.60), (1.30, 0.09, 4.7), mats["panel"], env, 0.025)
    box("Entrance | right leaf", (door_x + 0.70, 8.63, 2.60), (1.30, 0.09, 4.7), mats["panel"], env, 0.025)
    for x in (door_x - 0.70, door_x + 0.70):
        box("Entrance | inset panel", (x, 8.565, 2.8), (0.82, 0.035, 3.75), mats["plaster"], env, 0.03)
        box("Entrance | lower inset", (x, 8.565, 0.88), (0.82, 0.035, 0.75), mats["panel"], env, 0.025)
        cylinder("Entrance | brass handle", (x + (0.48 if x < door_x else -0.48), 8.49, 2.18),
                 0.035, 0.20, mats["brass"], env, rot=(math.pi / 2, 0, 0), bevel=0.008)
    box("Entrance | transom frame", (door_x, 8.61, 5.27), (2.86, 0.12, 0.68), mats["brass"], env, 0.018)
    box("Entrance | blue frosted transom", (door_x, 8.53, 5.27), (2.66, 0.04, 0.47), mats["glass"], env, 0.01)
    box("Entrance | overhead wash", (door_x, 8.50, 5.59), (1.4, 0.05, 0.035), mats["cool_emit"], env)

    # Wall art, framed with realistic glass and restrained abstract color.
    for x, z, color in ((-6.35, 4.45, (0.045, 0.11, 0.21)), (4.65, 4.8, (0.09, 0.12, 0.18))):
        detail_frame("Gallery | bronze frame", (x, 8.54, z), 1.95, 2.15, mats["wood"], env)
        box("Gallery | canvas", (x, 8.465, z), (1.68, 0.025, 1.86), mats["panel"], env, 0.012)
        box("Gallery | abstract blue field", (x, 8.44, z + 0.08), (1.52, 0.014, 1.35),
            pbr("Gallery print | " + str(x), color, 0.92, texture=3.0,
                color_b=(0.23, 0.31, 0.40), bump_strength=0.0), env)
        sphere("Gallery | moon motif", (x - 0.36, 8.42, z + 0.30), (0.24, 0.02, 0.24),
               mats["stone"], env, segments=32, rings=20)
        curve("Gallery | horizon line", [(x - 0.56, 8.41, z - 0.36), (x + 0.58, 8.41, z - 0.36)],
              0.015, mats["brass"], env)

    # Service door to the right stays slightly open; its amber light becomes a story cue at 26s.
    hinge = bpy.data.objects.new("Service door | hinge pivot", None)
    env.objects.link(hinge)
    hinge.location = (7.45, 8.58, 0.0)
    hinge.empty_display_type = "CUBE"
    hinge.empty_display_size = 0.10
    box("Service door | casing", (8.05, 8.70, 1.55), (1.80, 0.26, 3.18), mats["wood"], env, 0.045)
    box("Service door | dark opening", (8.05, 8.53, 1.55), (1.48, 0.05, 2.88), mats["black_metal"], env)
    panel = box("Service door | open leaf", (0.63, -0.12, 1.55), (1.36, 0.12, 2.88),
                mats["wood_trim"], env, 0.025, parent=hinge,
                rot=(0.0, 0.0, math.radians(-14)))
    for z in (0.62, 1.52, 2.42):
        box("Service door | raised panel", (0.63, -0.195, z), (0.94, 0.035, 0.65),
            mats["wood"], env, 0.025, parent=hinge)
    cylinder("Service door | brass lever", (0.99, -0.25, 1.50), 0.025, 0.18,
             mats["brass"], env, parent=hinge, rot=(math.pi / 2, 0, 0), bevel=0.006)
    seam = box("Service door | warm light seam", (8.76, 8.35, 1.56), (0.09, 0.025, 2.68),
               mats["warm_emit"], env)
    visibility(seam, 625, 766)
    door_wash = area_light("Service door | warm spill", (8.45, 6.8, 1.8), (5.2, 1.6, 0.1),
                           (1.0, 0.39, 0.12), 0.0, 2.2, lights)
    for frame, power in ((1, 0.0), (624, 0.0), (625, 520.0), (766, 520.0), (767, 0.0)):
        door_wash.data.energy = power
        door_wash.data.keyframe_insert(data_path="energy", frame=frame)

    # Staircase, exact right-side silhouette from the Unity foyer.
    stair_start = 1.9
    stair_y = 5.4
    for i in range(16):
        x = stair_start + i * 0.375
        z = 0.105 + i * 0.185
        box("Stair | limestone tread", (x, stair_y, z), (0.43, 3.05, 0.18), mats["stone"], env, 0.02)
        box("Stair | polished nosing", (x - 0.19, stair_y - 0.01, z + 0.089),
            (0.04, 3.0, 0.025), mats["steel"], env, 0.009)
        box("Stair | riser", (x + 0.205, stair_y, z + 0.185), (0.035, 3.02, 0.19), mats["panel"], env, 0.006)
    # A continuous concrete soffit supports the flight, so the stairs do not read as floating cubes.
    stair_rise = 15 * 0.185
    stair_run = 15 * 0.375
    stair_angle = math.atan2(stair_rise, stair_run)
    stair_length = math.hypot(stair_run, stair_rise)
    stair_mid_x = stair_start + stair_run * 0.5
    stair_mid_z = (0.015 + 0.105 + stair_rise - 0.09) * 0.5
    box("Stair | solid limestone soffit", (stair_mid_x, stair_y, stair_mid_z),
        (stair_length, 2.92, 0.18), mats["stone"], env, 0.035,
        rot=(0.0, -stair_angle, 0.0))
    for y in (4.03, 6.78):
        capsule("Stair | continuous brass support",
                (stair_start - 0.30, y, 0.72), (stair_start + 5.60, y, 3.82),
                0.035, mats["black_metal"], env, None)
        capsule("Stair | oiled oak handrail",
                (stair_start - 0.32, y, 0.81), (stair_start + 5.62, y, 3.91),
                0.062, mats["wood_trim"], env, None)
        for i in range(0, 16, 2):
            x = stair_start + i * 0.375
            z = 0.67 + i * 0.185
            capsule("Stair | turned baluster", (x, y, z), (x, y, z + 0.76),
                    0.022, mats["brass"], env, None)
    box("Stair | upper landing", (8.05, 5.8, 3.05), (2.45, 4.75, 0.28), mats["stone"], env, 0.035)
    box("Stair | landing edge", (8.05, 3.40, 2.92), (2.42, 0.05, 0.07), mats["brass"], env, 0.012)

    # Benches and planters frame the same lobby lanes as the Unity background.
    for x, y in ((-6.35, 3.20), (3.95, 3.30), (-7.25, -4.55), (7.55, -4.50)):
        box("Bench | oak seat", (x, y, 0.57), (2.15, 0.60, 0.14), mats["wood"], env, 0.055)
        box("Bench | seat highlight", (x, y - 0.02, 0.65), (2.06, 0.52, 0.035), mats["wood_trim"], env, 0.018)
        box("Bench | upholstered pad", (x, y - 0.01, 0.695), (1.92, 0.42, 0.07), mats["navy"], env, 0.032)
        for dx in (-0.79, 0.79):
            box("Bench | steel leg", (x + dx, y, 0.28), (0.10, 0.47, 0.54), mats["black_metal"], env, 0.025)
        for dx in (-0.91, 0.91):
            box("Bench | armrest", (x + dx, y, 0.80), (0.09, 0.54, 0.09), mats["wood_trim"], env, 0.025)

    for x, y in ((-8.0, 2.6), (-3.35, 6.75), (2.5, 7.1), (7.95, 7.25)):
        cylinder("Planter | ceramic body", (x, y, 0.42), 0.38, 0.82, mats["stone"], env, vertices=48, bevel=0.03)
        cylinder("Planter | top lip", (x, y, 0.84), 0.40, 0.09, mats["panel"], env, vertices=48, bevel=0.025)
        cylinder("Planter | soil", (x, y, 0.90), 0.32, 0.045, mats["soil"], env, vertices=40)
        for j in range(9):
            angle = j * math.tau / 9.0
            start = Vector((x, y, 0.91))
            end = Vector((x + math.cos(angle) * 0.43, y + math.sin(angle) * 0.32, 1.62 + (j % 4) * 0.14))
            curve("Plant | curved stem", [tuple(start), tuple((start + end) * 0.58), tuple(end)],
                  0.012, mats["leaf"], env)
            leaf = sphere("Plant | broad leaf", tuple((start + end) * 0.52),
                          (0.11, 0.05, 0.34 + (j % 2) * 0.06), mats["leaf"], env,
                          segments=24, rings=16)
            leaf.rotation_euler[1] = math.radians((j % 3 - 1) * 16)
            leaf.rotation_euler[2] = angle

    # Practical sconces and the cool pool over the staircase.
    for x, z in ((-3.2, 6.45), (1.2, 6.45)):
        box("Sconce | dark brass housing", (x, 8.42, z), (0.42, 0.20, 0.72), mats["black_metal"], env, 0.07)
        box("Sconce | frosted diffuser", (x, 8.30, z), (0.25, 0.07, 0.55), mats["cool_emit"], env, 0.045)
        area_light("Sconce | blue spill", (x, 7.95, z), (x, 2.0, 0.5),
                   (0.28, 0.48, 1.0), 260.0, 2.6, lights)
    area_light("Ceiling | cool moon bounce", (0.0, 2.0, 7.4), (0.0, 3.0, 0.0),
               (0.40, 0.54, 0.84), 780.0, 10.0, lights)
    area_light("Camera side | soft blue fill", (-6.0, -4.0, 4.8), (-1.0, 2.0, 1.2),
               (0.34, 0.47, 0.72), 330.0, 7.0, lights)
    area_light("Stair landing | cold rim", (7.2, 6.6, 5.5), (4.1, 3.8, 1.3),
               (0.30, 0.49, 0.95), 420.0, 3.5, lights)
    pulse = area_light("Stair | sudden blue pulse", (4.1, 7.1, 5.7), (3.2, 3.4, 0.0),
                       (0.58, 0.78, 1.0), 0.0, 2.5, lights)
    for frame, power in ((1, 0.0), (249, 0.0), (251, 950.0), (259, 950.0),
                         (268, 0.0), (END_FRAME, 0.0)):
        pulse.data.energy = power
        pulse.data.keyframe_insert(data_path="energy", frame=frame)
    smooth_animation(pulse.data)


def build_coffee_and_tracks(props, mats, font):
    # The coffee never vanishes: cup, saucer, table and steam stay together.
    x, y = -5.15, -1.95
    box("Coffee table | top", (x, y, 0.78), (1.25, 0.86, 0.12), mats["wood_trim"], props, 0.055)
    box("Coffee table | apron", (x, y, 0.65), (1.02, 0.67, 0.18), mats["wood"], props, 0.035)
    for dx in (-0.47, 0.47):
        for dy in (-0.29, 0.29):
            box("Coffee table | tapered leg", (x + dx, y + dy, 0.35), (0.075, 0.075, 0.60), mats["wood"], props, 0.025)
    cylinder("Coffee | ceramic saucer", (x, y - 0.05, 0.86), 0.20, 0.025, mats["ceramic"], props, bevel=0.008)
    cylinder("Coffee | cup body", (x, y - 0.05, 0.98), 0.13, 0.22, mats["ceramic"], props, vertices=48, bevel=0.018)
    cylinder("Coffee | dark surface", (x, y - 0.05, 1.092), 0.108, 0.008, mats["coffee"], props, vertices=48)
    # Rounded handle in the same ceramic material.
    curve("Coffee | cup handle", [(x + 0.11, y - 0.05, 1.05),
                                   (x + 0.26, y - 0.05, 1.06),
                                   (x + 0.29, y - 0.05, 0.97),
                                   (x + 0.24, y - 0.05, 0.91),
                                   (x + 0.11, y - 0.05, 0.92)],
          0.024, mats["ceramic"], props)
    steam_mat = volume_material("Coffee | translucent steam", (0.68, 0.76, 0.83), 0.035, 0.1)
    for i in range(3):
        stem = curve("Coffee | rising steam", [
            (x - 0.045 + i * 0.04, y - 0.05, 1.12),
            (x - 0.06 + i * 0.05, y - 0.04, 1.28),
            (x - 0.01 + i * 0.035, y - 0.03, 1.42),
            (x - 0.03 + i * 0.05, y - 0.02, 1.58)
        ], 0.013, mats["ceramic"], props)
        key_transform(stem, 1, loc=(0.0, 0.0, 0.0))
        key_transform(stem, 96, loc=(0.015 * (i - 1), 0.0, 0.04))
        key_transform(stem, 169, loc=(-0.01 * (i - 1), 0.0, 0.0))
        smooth_animation(stem)
        visibility(stem, 97, 766)

    # Four dusty, three-toed impressions appear under the real flashlight at 20s.
    tracks = [(-0.48, 0.30, -24), (0.10, 0.82, -30), (0.68, 1.34, -28),
              (1.24, 1.88, -34)]
    for i, (tx, ty, angle) in enumerate(tracks):
        root = bpy.data.objects.new("Evidence | three-toed track %02d" % (i + 1), None)
        props.objects.link(root)
        # Keep the shallow dust impressions just above the rendered tile surface.
        root.location = (tx, ty, 0.020)
        root.rotation_euler[2] = math.radians(angle)
        mark_start = round(FPS * (20.55 + i * 0.72))
        visibility(root, mark_start, 766)
        root.scale = (0.001, 0.001, 0.001)
        root.keyframe_insert(data_path="scale", frame=mark_start)
        root.scale = (1.0, 1.0, 1.0)
        root.keyframe_insert(data_path="scale", frame=mark_start + 13)
        smooth_animation(root)
        for toe, offset in enumerate((-0.13, 0.0, 0.13)):
            mark = sphere("Track %02d | toe %d" % (i + 1, toe + 1),
                          (offset, 0.12 + abs(offset) * 0.28, 0.0),
                          (0.067, 0.125, 0.004), mats["footprint"], props,
                          parent=root, segments=28, rings=16)
            visibility(mark, mark_start, 766)
        sphere("Track %02d | palm" % (i + 1), (0.0, -0.06, 0.0),
               (0.10, 0.085, 0.004), mats["footprint"], props,
               parent=root, segments=28, rings=16)
        visibility(bpy.data.objects["Track %02d | palm" % (i + 1)], mark_start, 766)

    # A pale feather beside the door and a small moth cross the beam near the walk.
    feather_root = bpy.data.objects.new("Clue | fallen feather", None)
    props.objects.link(feather_root)
    feather_root.location = (7.65, 7.35, 0.045)
    feather = sphere("Feather | vanes", (0.0, 0.0, 0.0), (0.07, 0.32, 0.012), mats["feather"], props, feather_root)
    feather.rotation_euler[1] = math.radians(21)
    visibility(feather, 625, 766)
    quill = curve("Feather | central shaft", [(7.65, 7.35, 0.06), (7.79, 7.68, 0.06)],
                  0.008, mats["ceramic"], props)
    for side in (-1, 1):
        for j in range(7):
            t = j / 7.0
            px = 7.65 + 0.14 * t
            py = 7.35 + 0.33 * t
            curve("Feather | barb", [(px, py, 0.058),
                                      (px + side * 0.075 * math.sin(math.pi * t), py + 0.045, 0.056)],
                  0.003, mats["feather"], props, resolution=1)
    visibility(quill, 625, 766)

    moth_root = bpy.data.objects.new("Clue | moth in flashlight beam", None)
    props.objects.link(moth_root)
    moth_root.location = (-1.15, 0.20, 1.45)
    moth_body = sphere("Moth | body", (0.0, 0.0, 0.0), (0.035, 0.025, 0.11), mats["hair"], props, moth_root, 20, 12)
    visibility(moth_body, 625, 752)
    for side in (-1, 1):
        wing = sphere("Moth | wing", (side * 0.09, -0.005, 0.025),
                      (0.12, 0.018, 0.065), mats["moth"], props, moth_root, 24, 14)
        wing.rotation_euler[1] = side * math.radians(14)
        wing.rotation_euler[2] = side * math.radians(20)
        key_transform(wing, 625, rot=(0.0, side * math.radians(10), side * math.radians(16)))
        key_transform(wing, 631, rot=(0.0, side * math.radians(-28), side * math.radians(16)))
        key_transform(wing, 637, rot=(0.0, side * math.radians(10), side * math.radians(16)))
        key_transform(wing, 643, rot=(0.0, side * math.radians(-28), side * math.radians(16)))
        key_transform(wing, 649, rot=(0.0, side * math.radians(10), side * math.radians(16)))
        smooth_animation(wing)
        visibility(wing, 625, 752)
    for frame, loc in ((625, (-1.15, 0.20, 1.45)), (660, (0.10, 0.55, 2.10)),
                       (700, (1.65, 1.10, 1.55)), (740, (3.10, 2.25, 1.85)),
                       (766, (3.80, 3.10, 2.25))):
        key_transform(moth_root, frame, loc=loc)
    smooth_animation(moth_root)
    return tracks


def make_guard(char, mats, props):
    root = bpy.data.objects.new("Guard | animated root", None)
    char.objects.link(root)
    root.empty_display_type = "CIRCLE"
    root.empty_display_size = 0.45
    root.location = (-3.45, -1.75, 0.0)

    pelvis = bpy.data.objects.new("Guard | pelvis pivot", None)
    char.objects.link(pelvis)
    pelvis.parent = root
    # Keep the trouser cuffs and shoes above the tiled floor in the standing pose.
    pelvis.location = (0.0, 0.0, 1.02)

    torso = bpy.data.objects.new("Guard | torso pivot", None)
    char.objects.link(torso)
    torso.parent = pelvis
    torso.location = (0.0, 0.0, 0.06)
    torso_body = sphere("Guard | tailored uniform shirt", (0.0, 0.0, 0.32),
                        (0.37, 0.235, 0.49), mats["cotton"], char, torso, 40, 28)
    sphere("Guard | shoulder yoke", (0.0, 0.0, 0.63), (0.42, 0.24, 0.18),
           mats["cotton"], char, torso, 40, 24)
    sphere("Guard | neck", (0.0, 0.0, 0.78), (0.105, 0.10, 0.14), mats["skin"], char, torso, 32, 20)
    box("Guard | shirt placket", (0.0, -0.232, 0.35), (0.026, 0.014, 0.47),
        mats["shirt_detail"], char, 0.006, torso)
    for side in (-1, 1):
        sx = side * 0.205
        box("Guard | chest pocket", (sx, -0.215, 0.43), (0.14, 0.035, 0.17),
            mats["shirt_detail"], char, 0.018, torso)
        box("Guard | pocket flap", (sx, -0.238, 0.52), (0.15, 0.027, 0.04),
            mats["cotton"], char, 0.009, torso)
        for z in (0.22, 0.34, 0.46):
            sphere("Guard | shirt button", (0.0, -0.25, z), (0.014, 0.009, 0.014),
                   mats["brass"], char, torso, 16, 10)
        curve("Guard | shoulder seam", [(side * 0.13, -0.19, 0.58),
                                         (side * 0.25, -0.18, 0.67),
                                         (side * 0.39, -0.11, 0.60)],
              0.007, mats["shirt_detail"], char, torso, resolution=1)

    # Belt, radio and baton match the equipment visible in the Unity sprite.
    box("Guard | duty belt", (0.0, 0.0, -0.02), (0.68, 0.45, 0.11), mats["leather"], char, 0.028, pelvis)
    box("Guard | brushed buckle", (0.0, -0.235, -0.02), (0.12, 0.022, 0.075), mats["brass"], char, 0.01, pelvis)
    box("Guard | radio holster", (-0.31, -0.06, 0.05), (0.15, 0.18, 0.21), mats["leather"], char, 0.022, pelvis)
    box("Guard | radio face", (-0.31, -0.16, 0.06), (0.11, 0.035, 0.15), mats["black_metal"], char, 0.014, pelvis)
    for z in (0.09, 0.04, -0.01):
        sphere("Guard | radio indicator", (-0.30, -0.181, z), (0.009, 0.005, 0.009),
               mats["warm_emit"], char, pelvis, 12, 8)
    cylinder("Guard | baton", (0.34, -0.01, -0.02), 0.035, 0.40, mats["black_metal"],
             char, pelvis, rot=(0.0, math.radians(78), 0.0), vertices=24, bevel=0.012)

    # Head, skin detail, moustache and the familiar dark security cap.
    head = bpy.data.objects.new("Guard | head aim pivot", None)
    char.objects.link(head)
    head.parent = torso
    head.location = (0.0, 0.0, 0.82)
    sphere("Guard | face", (0.0, -0.005, 0.025), (0.145, 0.15, 0.19),
           mats["skin"], char, head, 40, 28)
    for side in (-1, 1):
        sphere("Guard | ear", (side * 0.14, 0.0, 0.02), (0.035, 0.045, 0.060),
               mats["skin_light"], char, head, 24, 16)
        sphere("Guard | eye socket", (side * 0.053, -0.139, 0.055), (0.034, 0.016, 0.022),
               mats["hair"], char, head, 24, 16)
        sphere("Guard | eye", (side * 0.053, -0.153, 0.056), (0.021, 0.010, 0.014),
               mats["eye_white"], char, head, 20, 12)
        sphere("Guard | iris", (side * 0.052, -0.162, 0.055), (0.008, 0.005, 0.009),
               mats["eye_dark"], char, head, 16, 10)
        curve("Guard | eyebrow", [(side * 0.025, -0.145, 0.095),
                                  (side * 0.052, -0.153, 0.106),
                                  (side * 0.085, -0.141, 0.098)],
              0.009, mats["hair"], char, head, resolution=2)
        sphere("Guard | moustache lobe", (side * 0.038, -0.148, -0.027),
               (0.045, 0.018, 0.022), mats["hair"], char, head, 24, 14)
    sphere("Guard | nose", (0.0, -0.157, 0.015), (0.027, 0.045, 0.045),
           mats["skin_light"], char, head, 24, 16)
    curve("Guard | mouth", [(-0.026, -0.151, -0.071), (0.0, -0.157, -0.078),
                             (0.026, -0.151, -0.071)], 0.005, mats["hair"], char, head, resolution=2)
    sphere("Guard | cap crown", (0.0, 0.0, 0.19), (0.165, 0.16, 0.075),
           mats["navy"], char, head, 36, 20)
    sphere("Guard | cap band", (0.0, 0.0, 0.15), (0.17, 0.16, 0.035),
           mats["black_metal"], char, head, 36, 18)
    box("Guard | cap visor", (0.0, -0.13, 0.15), (0.27, 0.20, 0.035),
        mats["navy"], char, 0.018, head)
    sphere("Guard | cap badge", (0.0, -0.166, 0.185), (0.034, 0.009, 0.029),
           mats["brass"], char, head, 20, 12)

    # Legs with separate knee pivots, so the walk and first stair steps read clearly.
    leg_rigs = {}
    for side, label in ((-1, "L"), (1, "R")):
        hip = bpy.data.objects.new("Guard | hip " + label, None)
        char.objects.link(hip)
        hip.parent = pelvis
        hip.location = (side * 0.17, 0.0, -0.10)
        capsule("Guard | trouser thigh " + label, (0.0, 0.0, -0.01), (0.0, 0.0, -0.39),
                0.145, mats["trousers"], char, hip)
        knee = bpy.data.objects.new("Guard | knee " + label, None)
        char.objects.link(knee)
        knee.parent = hip
        knee.location = (0.0, 0.0, -0.39)
        sphere("Guard | knee seam " + label, (0.0, -0.005, 0.0), (0.135, 0.14, 0.14),
               mats["trousers"], char, knee, 32, 20)
        capsule("Guard | trouser shin " + label, (0.0, 0.0, -0.03), (0.0, 0.0, -0.38),
                0.105, mats["trousers"], char, knee)
        shoe = sphere("Guard | polished shoe " + label, (0.0, -0.065, -0.44),
                      (0.13, 0.24, 0.085), mats["leather"], char, knee, 32, 20)
        sphere("Guard | shoe toe highlight " + label, (0.0, -0.19, -0.435),
               (0.12, 0.10, 0.045), mats["black_metal"], char, knee, 24, 16)
        leg_rigs["hip_" + label] = hip
        leg_rigs["knee_" + label] = knee

    # Articulated arms: one hand holds the flashlight, the other rests by his belt.
    arm_rigs = {}
    for side, label in ((-1, "L"), (1, "R")):
        shoulder = bpy.data.objects.new("Guard | shoulder " + label, None)
        char.objects.link(shoulder)
        shoulder.parent = torso
        shoulder.location = (side * 0.36, 0.0, 0.59)
        capsule("Guard | uniform sleeve " + label, (0.0, 0.0, 0.0),
                (side * 0.04, 0.0, -0.34), 0.125, mats["cotton"], char, shoulder)
        elbow = bpy.data.objects.new("Guard | elbow " + label, None)
        char.objects.link(elbow)
        elbow.parent = shoulder
        elbow.location = (side * 0.04, 0.0, -0.34)
        sphere("Guard | elbow " + label, (0.0, 0.0, 0.0), (0.10, 0.105, 0.10),
               mats["skin"], char, elbow, 24, 16)
        capsule("Guard | forearm " + label, (0.0, 0.0, 0.0),
                (side * 0.025, 0.0, -0.31), 0.083, mats["skin_light"], char, elbow)
        hand = bpy.data.objects.new("Guard | hand " + label, None)
        char.objects.link(hand)
        hand.parent = elbow
        hand.location = (side * 0.025, -0.005, -0.32)
        sphere("Guard | hand " + label, (0.0, 0.0, 0.0), (0.073, 0.075, 0.085),
               mats["skin"], char, hand, 24, 16)
        for finger in range(4):
            fx = (finger - 1.5) * 0.029
            capsule("Guard | finger " + label + str(finger), (fx, -0.048, -0.005),
                    (fx, -0.055, -0.052), 0.013, mats["skin_light"], char, hand)
        arm_rigs["shoulder_" + label] = shoulder
        arm_rigs["elbow_" + label] = elbow
        arm_rigs["hand_" + label] = hand

    # Collar, shoulder epaulettes, radio wire and restrained uniform markings.
    for side in (-1, 1):
        box("Guard | shoulder epaulette", (side * 0.31, -0.16, 0.58), (0.18, 0.05, 0.06),
            mats["navy"], char, 0.012, torso)
    collar_l = box("Guard | shirt collar left", (-0.07, -0.20, 0.69), (0.13, 0.06, 0.10),
                   mats["shirt_detail"], char, 0.018, torso, rot=(0.0, 0.0, math.radians(-18)))
    collar_r = box("Guard | shirt collar right", (0.07, -0.20, 0.69), (0.13, 0.06, 0.10),
                   mats["shirt_detail"], char, 0.018, torso, rot=(0.0, 0.0, math.radians(18)))
    add_text("Guard | uniform chest tab", "SEG", (0.205, -0.244, 0.44), 0.032,
             mats["brass"], char, rotation=(math.pi / 2, 0.0, 0.0), parent=torso, align="CENTER")

    # Metal flashlight is visibly attached to the right hand; the real spot follows it.
    hand_r = arm_rigs["hand_R"]
    cylinder("Flashlight | anodized body", (0.0, -0.17, 0.015), 0.043, 0.30,
             mats["black_metal"], char, hand_r, rot=(math.pi / 2, 0.0, 0.0), vertices=40, bevel=0.01)
    cylinder("Flashlight | knurled grip", (0.0, -0.14, 0.015), 0.052, 0.11,
             mats["steel"], char, hand_r, rot=(math.pi / 2, 0.0, 0.0), vertices=40, bevel=0.008)
    cylinder("Flashlight | bezel", (0.0, -0.325, 0.015), 0.059, 0.052,
             mats["steel"], char, hand_r, rot=(math.pi / 2, 0.0, 0.0), vertices=48, bevel=0.008)
    sphere("Flashlight | LED lens", (0.0, -0.357, 0.015), (0.042, 0.012, 0.042),
           mats["cool_emit"], char, hand_r, 24, 16)

    beam_target = make_target("Flashlight | animated aim target", (4.5, 5.0, 3.5), props)
    for frame, target in ((1, (4.5, 5.0, 3.5)), (336, (4.5, 5.0, 3.5)),
                          (337, (5.6, 6.6, 3.7)), (460, (5.8, 6.3, 3.1)),
                          (481, (0.4, 0.4, 0.05)), (507, (0.35, 0.65, 0.05)),
                          (618, (1.9, 2.6, 0.1)), (625, (4.0, 4.8, 1.3)),
                          (752, (7.9, 8.0, 1.5)), (766, (7.9, 8.0, 1.5))):
        key_transform(beam_target, frame, loc=target)
    smooth_animation(beam_target)

    spot_data = bpy.data.lights.new("Flashlight | focused 4200K beam", "SPOT")
    spot_data.energy = 0.0
    spot_data.color = (0.72, 0.84, 1.0)
    spot_data.spot_size = math.radians(24.0)
    spot_data.spot_blend = 0.34
    spot_data.shadow_soft_size = 0.035
    spot = bpy.data.objects.new("Flashlight | physical spot", spot_data)
    char.objects.link(spot)
    spot.parent = hand_r
    spot.location = (0.0, -0.37, 0.015)
    spot.rotation_euler = (-math.pi / 2, 0.0, 0.0)
    track_to = spot.constraints.new("TRACK_TO")
    track_to.name = "Aim the flashlight at the current clue"
    track_to.target = beam_target
    track_to.track_axis = "TRACK_NEGATIVE_Z"
    track_to.up_axis = "UP_Y"
    for frame, power in ((1, 0.0), (336, 0.0), (337, 1600.0), (766, 1600.0), (767, 0.0), (END_FRAME, 0.0)):
        spot_data.energy = power
        spot_data.keyframe_insert(data_path="energy", frame=frame)

    # Subtle dust scattering makes the beam visible without obscuring the set.
    bpy.ops.mesh.primitive_cone_add(vertices=48, radius1=1.85, radius2=0.035,
                                    depth=9.0, location=(0, 0, 0))
    beam = bpy.context.object
    beam.name = "Flashlight | fine volumetric shaft"
    move_to(beam, char)
    beam.parent = spot
    beam.location = (0.0, 0.0, -4.55)
    beam.data.materials.append(flashlight_scattering_material())
    visibility(beam, 337, 766)

    root_rig = {"root": root, "pelvis": pelvis, "torso": torso, "head": head,
                "arms": arm_rigs, "legs": leg_rigs, "beam_target": beam_target}
    return root_rig


def animate_guard(rig):
    root = rig["root"]
    pelvis = rig["pelvis"]
    torso = rig["torso"]
    head = rig["head"]
    arms = rig["arms"]
    legs = rig["legs"]

    # Idle, coffee/clock glances, then a sudden turn toward the upstairs sound.
    for frame, loc in ((1, (-3.45, -1.75, 0.0)), (228, (-3.45, -1.75, 0.0)),
                       (432, (-3.10, -1.25, 0.0)), (480, (-2.95, -1.05, 0.0)),
                       (624, (-2.95, -1.05, 0.0)), (632, (-2.95, -1.05, 0.0)),
                       (672, (-2.55, -0.48, 0.0)), (720, (-1.48, 0.85, 0.0)),
                       (730, (0.35, 2.20, 0.10)), (742, (0.62, 2.52, 0.28)),
                       (753, (0.91, 2.82, 0.47)), (766, (0.91, 2.82, 0.47))):
        key_transform(root, frame, loc=loc)

    for frame, rotation in ((1, (0.0, 0.0, math.radians(-8))),
                            (97, (0.0, 0.0, math.radians(-28))),
                            (122, (0.0, 0.0, math.radians(-11))),
                            (169, (0.0, 0.0, math.radians(-16))),
                            (228, (0.0, 0.0, math.radians(-16))),
                            (241, (0.0, 0.0, math.radians(78))),
                            (337, (0.0, 0.0, math.radians(112))),
                            (432, (0.0, 0.0, math.radians(132))),
                            (480, (0.0, 0.0, math.radians(-6))),
                            (624, (0.0, 0.0, math.radians(-6))),
                            (625, (0.0, 0.0, math.radians(135))),
                            (766, (0.0, 0.0, math.radians(135)))):
        key_transform(root, frame, rot=rotation)

    # Gentle breathing in the unhurried opening.
    for frame, z in ((1, 0.0), (36, 0.012), (72, 0.0), (108, 0.012),
                     (145, 0.0), (180, 0.012), (216, 0.0), (240, 0.0)):
        key_transform(torso, frame, loc=(0.0, 0.0, z))
    # Head-only glance to clock and coffee; startled turn holds on the stair.
    for frame, rotation in ((1, (0.0, 0.0, 0.0)), (97, (0.0, 0.0, math.radians(-14))),
                            (123, (math.radians(-4), 0.0, 0.0)), (169, (0.0, 0.0, 0.0)),
                            (235, (0.0, 0.0, 0.0)), (241, (math.radians(-6), 0.0, math.radians(-10))),
                            (270, (0.0, 0.0, 0.0)), (337, (0.0, 0.0, 0.0)),
                            (432, (0.0, 0.0, math.radians(-4))), (480, (math.radians(12), 0.0, 0.0)),
                            (624, (math.radians(10), 0.0, 0.0)), (625, (0.0, 0.0, 0.0))):
        key_transform(head, frame, rot=rotation)

    # Small uniform adjustment around 7 seconds.
    shoulder_l = arms["shoulder_L"]
    elbow_l = arms["elbow_L"]
    for frame, rot in ((1, (0.0, 0.0, 0.0)), (168, (0.0, 0.0, 0.0)),
                       (174, (math.radians(-20), 0.0, math.radians(-8))),
                       (188, (math.radians(-34), 0.0, math.radians(-11))),
                       (202, (0.0, 0.0, 0.0)), (240, (0.0, 0.0, 0.0))):
        key_transform(shoulder_l, frame, rot=rot)
    for frame, rot in ((1, (0.0, 0.0, 0.0)), (174, (0.0, 0.0, 0.0)),
                       (188, (math.radians(-16), 0.0, 0.0)), (202, (0.0, 0.0, 0.0))):
        key_transform(elbow_l, frame, rot=rot)

    # The right arm raises the flashlight after the click, sweeps to the tracks, then steadies.
    shoulder_r = arms["shoulder_R"]
    elbow_r = arms["elbow_R"]
    for frame, rot in ((1, (0.0, 0.0, 0.0)), (336, (0.0, 0.0, 0.0)),
                       (350, (math.radians(-42), math.radians(-28), 0.0)),
                       (365, (math.radians(-63), math.radians(-35), 0.0)),
                       (432, (math.radians(-58), math.radians(-30), 0.0)),
                       (480, (math.radians(-22), math.radians(-15), 0.0)),
                       (488, (math.radians(-64), math.radians(-32), 0.0)),
                       (600, (math.radians(-64), math.radians(-32), 0.0)),
                       (624, (math.radians(-28), math.radians(-18), 0.0)),
                       (632, (math.radians(-40), math.radians(-25), 0.0)),
                       (753, (math.radians(-40), math.radians(-25), 0.0)),
                       (766, (math.radians(-40), math.radians(-25), 0.0))):
        key_transform(shoulder_r, frame, rot=rot)
    for frame, rot in ((1, (0.0, 0.0, 0.0)), (336, (0.0, 0.0, 0.0)),
                       (365, (math.radians(-22), 0.0, 0.0)),
                       (432, (math.radians(-17), 0.0, 0.0)),
                       (480, (math.radians(-10), 0.0, 0.0)),
                       (488, (math.radians(-24), 0.0, 0.0)),
                       (624, (math.radians(-24), 0.0, 0.0)),
                       (632, (math.radians(-14), 0.0, 0.0)), (753, (math.radians(-14), 0.0, 0.0))):
        key_transform(elbow_r, frame, rot=rot)

    # The guard crouches for the full evidence line, stands, then takes ten measured steps.
    for frame, loc in ((1, (0.0, 0.0, 1.02)), (480, (0.0, 0.0, 1.02)),
                       (496, (0.0, 0.0, 0.92)), (520, (0.0, 0.0, 0.90)),
                       (600, (0.0, 0.0, 0.90)), (624, (0.0, 0.0, 0.90)),
                       (632, (0.0, 0.0, 0.99)), (645, (0.0, 0.0, 1.02)),
                       (766, (0.0, 0.0, 1.02))):
        key_transform(pelvis, frame, loc=loc)
    for frame, rotation in ((1, (0.0, 0.0, 0.0)), (480, (0.0, 0.0, 0.0)),
                            (496, (math.radians(17), 0.0, math.radians(-5))),
                            (520, (math.radians(19), 0.0, math.radians(-6))),
                            (600, (math.radians(19), 0.0, math.radians(-6))),
                            (624, (math.radians(19), 0.0, math.radians(-6))),
                            (645, (0.0, 0.0, 0.0)), (766, (0.0, 0.0, 0.0))):
        key_transform(torso, frame, rot=rotation)

    for side in ("L", "R"):
        hip = legs["hip_" + side]
        knee = legs["knee_" + side]
        for frame, angle in ((1, 0.0), (480, 0.0), (496, math.radians(18)),
                             (520, math.radians(22)), (600, math.radians(22)),
                             (624, math.radians(22)), (645, 0.0), (766, 0.0)):
            key_transform(hip, frame, rot=(angle, 0.0, 0.0))
        for frame, angle in ((1, 0.0), (480, 0.0), (496, math.radians(-24)),
                             (520, math.radians(-35)), (600, math.radians(-35)),
                             (624, math.radians(-35)), (645, 0.0), (766, 0.0)):
            key_transform(knee, frame, rot=(angle, 0.0, 0.0))

    for index in range(10):
        frame = 633 + index * 12
        phase = 1.0 if index % 2 == 0 else -1.0
        for side, sign in (("L", 1.0), ("R", -1.0)):
            hip = legs["hip_" + side]
            knee = legs["knee_" + side]
            angle = math.radians(22.0) * phase * sign
            key_transform(hip, frame, rot=(angle, 0.0, 0.0))
            key_transform(knee, frame, rot=(math.radians(-24.0) if phase * sign > 0 else 0.0, 0.0, 0.0))
            key_transform(hip, min(frame + 6, 753), rot=(-angle * 0.7, 0.0, 0.0))
            key_transform(knee, min(frame + 6, 753), rot=(math.radians(-8.0), 0.0, 0.0))

    for obj in (root, pelvis, torso, head, shoulder_l, elbow_l, shoulder_r, elbow_r):
        smooth_animation(obj)
    for obj in list(legs.values()):
        smooth_animation(obj)
    return root


def build_cameras(scene, cameras_coll, props):
    # Each camera starts at the exact Unity beat; gentle dolly moves happen within the beat.
    shots = [
        ("CAM 01 | midnight foyer", 1, 96, (0.0, -15.2, 5.35), (0.0, 2.7, 2.7),
         (0.0, -14.5, 5.05), (0.0, 2.55, 2.65), 29),
        ("CAM 02 | clock and coffee", 97, 168, (-7.1, -8.7, 3.5), (-5.2, 1.3, 2.3),
         (-6.55, -7.9, 3.0), (-5.2, 0.9, 1.7), 38),
        ("CAM 03 | the guard reacts", 169, 240, (-5.0, -8.2, 3.2), (-3.1, -0.3, 1.2),
         (-4.45, -7.4, 2.9), (-3.2, -0.8, 1.25), 42),
        ("CAM 04 | noise above the stairs", 241, 336, (4.7, -7.4, 5.25), (5.2, 5.8, 3.0),
         (4.35, -6.8, 4.7), (5.2, 5.8, 3.0), 39),
        ("CAM 05 | flashlight search", 337, 480, (-5.5, -8.5, 3.5), (-0.8, 1.8, 1.4),
         (-5.1, -7.8, 3.2), (-0.4, 1.6, 1.2), 32),
        ("CAM 06 | three-toed tracks", 481, 624, (0.10, -4.8, 2.05), (0.55, 0.90, 0.08),
         (0.2, -4.15, 1.8), (0.65, 0.95, 0.08), 52),
        ("CAM 07 | toward the stairs", 625, 766, (-6.3, -9.8, 4.8), (-0.7, 1.8, 0.95),
         (-5.7, -8.6, 4.45), (0.5, 2.5, 1.10), 32),
    ]
    result = []
    for name, start, end, loc0, tgt0, loc1, tgt1, lens in shots:
        cam = make_camera(name, loc0, tgt0, lens, cameras_coll)
        camera_keys(cam, start, end, loc0, tgt0, loc1, tgt1)
        result.append(cam)
        marker = scene.timeline_markers.new(name, frame=start)
        marker.camera = cam
    title_cam_data = bpy.data.cameras.new("CAM 08 | title")
    title_cam_data.type = "ORTHO"
    title_cam_data.ortho_scale = 14.0
    title_cam = bpy.data.objects.new("CAM 08 | title", title_cam_data)
    cameras_coll.objects.link(title_cam)
    # Isolate the title camera from the foyer floor while it looks down its local -Z axis.
    title_cam.location = (0.0, -40.0, 0.0)
    title_cam.rotation_euler = (0.0, 0.0, 0.0)
    marker = scene.timeline_markers.new("CAM 08 | title after blackout", frame=767)
    marker.camera = title_cam
    result.append(title_cam)
    return result


def build_title(scene, title_cam, title_coll, mats, font):
    # Camera-mounted title card preserves the timing and exact title from Unity.
    card = box("Title | absolute black field", (0.0, 0.0, -3.2), (18.0, 11.0, 0.05),
               None, title_coll, parent=title_cam)
    black = bpy.data.materials.new("Title | pure black")
    black.diffuse_color = (0.0, 0.0, 0.0, 1.0)
    black.use_nodes = True
    sh = black.node_tree.nodes.get("Principled BSDF")
    sh.inputs["Base Color"].default_value = (0.0, 0.0, 0.0, 1.0)
    sh.inputs["Roughness"].default_value = 1.0
    sh.inputs["Emission Color"].default_value = (0.0, 0.0, 0.0, 1.0)
    sh.inputs["Emission Strength"].default_value = 0.0
    card.data.materials.append(black)
    title = add_text("Title | O Mistério do Espaço Cultural",
                     "O MISTÉRIO DO\nESPAÇO CULTURAL", (0.0, 0.0, -3.12), 0.70,
                     mats["title"], title_coll, parent=title_cam, font=font)
    title.data.space_line = 1.25
    title.data.extrude = 0.008
    title.data.bevel_depth = 0.002
    shadow = add_text("Title | blue accent",
                      "O MISTÉRIO DO\nESPAÇO CULTURAL", (-0.035, -0.035, -3.13), 0.70,
                      mats["title_glow"], title_coll, parent=title_cam, font=font)
    shadow.data.space_line = 1.25
    shadow.data.extrude = 0.008
    shadow.data.bevel_depth = 0.002
    for obj in (title, shadow):
        key_transform(obj, 783, scale=(0.90, 0.90, 0.90))
        key_transform(obj, 795, scale=(1.0, 1.0, 1.0))
        key_transform(obj, END_FRAME, scale=(1.0, 1.0, 1.0))
        smooth_animation(obj)


def configure_scene(scene):
    # Blender 5.1 exposes the current Eevee renderer under BLENDER_EEVEE.
    scene.render.engine = "BLENDER_EEVEE"
    if hasattr(scene, "eevee"):
        scene.eevee.taa_render_samples = 12
        if hasattr(scene.eevee, "use_raytracing"):
            scene.eevee.use_raytracing = False
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.fps = FPS
    scene.frame_start = 1
    scene.frame_end = END_FRAME
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGB"
    scene.render.film_transparent = False
    scene.render.filepath = "/tmp/cc01_realistic_frames/CC01_"
    scene.render.image_settings.color_depth = "8"
    scene.render.filepath = "/tmp/cc01_realistic_frames/CC01_"
    scene.render.use_file_extension = True
    scene.render.resolution_percentage = 100
    scene.render.image_settings.color_mode = "RGB"
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.render.use_compositing = True
    scene.render.use_sequencer = False
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    scene.camera = None
    scene.render.resolution_percentage = 100
    scene.unit_settings.system = "METRIC"
    scene.render.threads_mode = "FIXED"
    scene.render.threads = 8
    scene.render.image_settings.color_depth = "8"

    world = bpy.data.worlds.new("CC01 | blue night ambience")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.012, 0.024, 0.055, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.16
    scene.world = world

    # Blender 5 uses a compositor node group assigned to the scene.
    scene.use_nodes = True
    tree = bpy.data.node_groups.new("CC01 | subtle lens halation", "CompositorNodeTree")
    tree.interface.new_socket(name="Image", in_out="OUTPUT", socket_type="NodeSocketColor")
    render_layers = tree.nodes.new("CompositorNodeRLayers")
    render_layers.scene = scene
    glare = tree.nodes.new("CompositorNodeGlare")
    glare.inputs["Type"].default_value = "Fog Glow"
    glare.inputs["Quality"].default_value = "Medium"
    glare.inputs["Threshold"].default_value = 1.4
    glare.inputs["Size"].default_value = 0.32
    glare.inputs["Strength"].default_value = 0.18
    group_out = tree.nodes.new("NodeGroupOutput")
    tree.links.new(render_layers.outputs["Image"], glare.inputs["Image"])
    tree.links.new(glare.outputs["Image"], group_out.inputs["Image"])
    scene.compositing_node_group = tree
    scene.render.use_compositing = True


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    configure_scene(scene)

    env = collection("01 | Cultural foyer")
    char = collection("02 | Security guard")
    props = collection("03 | Coffee and clues")
    lights = collection("04 | Practical lights")
    cameras_coll = collection("05 | Cinematography")
    title_coll = collection("06 | Title card")

    mats = make_materials()
    font_path = os.path.join(ROOT, "Assets", "Fonts", "RobotoMono-Bold.ttf")
    font = bpy.data.fonts.load(font_path, check_existing=True) if os.path.exists(font_path) else None

    build_room(env, props, lights, mats, font)
    build_coffee_and_tracks(props, mats, font)
    guard_rig = make_guard(char, mats, props)
    animate_guard(guard_rig)
    cameras = build_cameras(scene, cameras_coll, props)
    title_cam = cameras[-1]
    build_title(scene, title_cam, title_coll, mats, font)

    # Four-second opening fade, then hard cut to black just before the title.
    scene.view_settings.exposure = -4.5
    scene.view_settings.keyframe_insert(data_path="exposure", frame=1)
    scene.view_settings.exposure = 0.0
    scene.view_settings.keyframe_insert(data_path="exposure", frame=97)
    scene.view_settings.exposure = 0.0
    scene.view_settings.keyframe_insert(data_path="exposure", frame=766)
    scene.view_settings.exposure = -12.0
    scene.view_settings.keyframe_insert(data_path="exposure", frame=767)
    scene.view_settings.exposure = -12.0
    scene.view_settings.keyframe_insert(data_path="exposure", frame=782)
    scene.view_settings.exposure = 0.0
    scene.view_settings.keyframe_insert(data_path="exposure", frame=783)
    for fc in getattr(scene.animation_data.action, "fcurves", []):
        for point in fc.keyframe_points:
            point.interpolation = "BEZIER"
            point.handle_left_type = "AUTO_CLAMPED"
            point.handle_right_type = "AUTO_CLAMPED"

    scene.camera = cameras[0]
    scene.frame_set(1)
    os.makedirs(os.path.dirname(OUT_BLEND), exist_ok=True)
    os.makedirs(os.path.dirname(scene.render.filepath), exist_ok=True)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    print("Saved realistic CC01 Blender scene:", OUT_BLEND)
    print("Timeline:", scene.frame_start, scene.frame_end, "at", FPS, "fps")


if __name__ == "__main__":
    main()
