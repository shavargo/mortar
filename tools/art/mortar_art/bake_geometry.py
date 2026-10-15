"""Camera, direction and timing math shared by the Blender bake script and the sheet packer.

Stdlib only: Blender's bundled Python imports this module. Bake ground axes: grid +x is Blender +X
and grid +y (south) is Blender -Y; one grid cell is one metre.
"""

from __future__ import annotations

import math

DIRECTION_DELTAS = ((0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1), (1, -1), (1, 0), (1, 1))
ANIMATIONS = ("idle", "walk", "attack", "hit", "death")
LOOPING = frozenset({"idle", "walk"})

FRAME_WIDTH = 208
FRAME_HEIGHT = 208
RENDER_SCALE = 2
TILE_PIXELS = 128
PIVOT_X = 104
PIVOT_FROM_BOTTOM = 48
SAMPLE_FPS = 10

CAMERA_PITCH_DEGREES = 30.0
CAMERA_YAW_DEGREES = -45.0
CAMERA_DISTANCE = 20.0

Vector = tuple[float, float, float]


def render_size() -> tuple[int, int]:
    return FRAME_WIDTH * RENDER_SCALE, FRAME_HEIGHT * RENDER_SCALE


def ground_vector(dx: int, dy: int) -> Vector:
    return float(dx), float(-dy), 0.0


def model_rotation_degrees(direction: int, facing_offset: float = 0.0) -> float:
    return (facing_offset - 45.0 * direction) % 360.0


def camera_location() -> Vector:
    pitch = math.radians(CAMERA_PITCH_DEGREES)
    yaw = math.radians(CAMERA_YAW_DEGREES)
    flat = CAMERA_DISTANCE * math.cos(pitch)
    return flat * math.cos(yaw), flat * math.sin(yaw), CAMERA_DISTANCE * math.sin(pitch)


def camera_rotation_radians() -> Vector:
    return math.radians(90.0 - CAMERA_PITCH_DEGREES), 0.0, math.radians(CAMERA_YAW_DEGREES + 90.0)


def ortho_scale() -> float:
    return FRAME_HEIGHT / TILE_PIXELS * math.sqrt(2.0)


def camera_shift() -> tuple[float, float]:
    pivot_row = FRAME_HEIGHT - PIVOT_FROM_BOTTOM
    return (FRAME_WIDTH / 2 - PIVOT_X) / FRAME_HEIGHT, (pivot_row - FRAME_HEIGHT / 2) / FRAME_HEIGHT


def project(point: Vector) -> tuple[float, float]:
    right, up = _camera_axes()
    pixels_per_metre = FRAME_HEIGHT / ortho_scale()
    shift_x, shift_y = camera_shift()
    x = FRAME_WIDTH / 2 + _dot(point, right) * pixels_per_metre - shift_x * FRAME_HEIGHT
    y = FRAME_HEIGHT / 2 - _dot(point, up) * pixels_per_metre + shift_y * FRAME_HEIGHT
    return x, y


def sample_times(duration: float, loop: bool, fps: int = SAMPLE_FPS) -> list[float]:
    count = max(1, round(duration * fps))
    times = [index / fps for index in range(count)]
    return times if loop else times + [duration]


def hit_frame_index(hit_seconds: float, frame_count: int, fps: int = SAMPLE_FPS) -> int:
    if frame_count < 2:
        raise ValueError("an attack needs at least two frames")
    return min(max(round(hit_seconds * fps), 1), frame_count - 1)


def _camera_axes() -> tuple[Vector, Vector]:
    x, y, z = camera_location()
    length = math.sqrt(x * x + y * y + z * z)
    forward = (-x / length, -y / length, -z / length)
    right = _normalize((forward[1], -forward[0], 0.0))
    return right, _cross(right, forward)


def _dot(a: Vector, b: Vector) -> float:
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def _cross(a: Vector, b: Vector) -> Vector:
    return a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]


def _normalize(vector: Vector) -> Vector:
    length = math.sqrt(_dot(vector, vector))
    return vector[0] / length, vector[1] / length, vector[2] / length
