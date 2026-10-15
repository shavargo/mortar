import math

import pytest

from mortar_art import bake_geometry as geo


def test_the_origin_lands_on_the_foot_pivot():
    x, y = geo.project((0.0, 0.0, 0.0))

    assert x == pytest.approx(geo.PIVOT_X)
    assert y == pytest.approx(geo.FRAME_HEIGHT - geo.PIVOT_FROM_BOTTOM)


@pytest.mark.parametrize(
    ("dx", "dy", "expected"),
    [(1, 0, (64, 32)), (0, 1, (-64, 32)), (1, 1, (0, 64)), (-1, -1, (0, -64)), (-1, 0, (-64, -32))],
)
def test_one_grid_step_matches_the_unity_isometric_grid(dx, dy, expected):
    origin = geo.project((0.0, 0.0, 0.0))
    step = geo.project(geo.ground_vector(dx, dy))

    assert step[0] - origin[0] == pytest.approx(expected[0])
    assert step[1] - origin[1] == pytest.approx(expected[1])


@pytest.mark.parametrize("direction", range(8))
def test_a_turned_model_faces_its_grid_direction(direction):
    angle = math.radians(geo.model_rotation_degrees(direction))
    facing = (math.sin(angle), -math.cos(angle))
    gx, gy, _ = geo.ground_vector(*geo.DIRECTION_DELTAS[direction])
    length = math.hypot(gx, gy)

    assert facing[0] == pytest.approx(gx / length, abs=1e-9)
    assert facing[1] == pytest.approx(gy / length, abs=1e-9)


def test_the_facing_offset_turns_every_direction():
    assert geo.model_rotation_degrees(2, facing_offset=90.0) == pytest.approx(0.0)
    assert geo.model_rotation_degrees(0, facing_offset=-45.0) == pytest.approx(315.0)


def test_the_camera_rotation_looks_at_the_origin():
    rx, ry, rz = geo.camera_rotation_radians()
    forward = (-math.sin(rx) * math.sin(rz), math.sin(rx) * math.cos(rz), -math.cos(rx))
    location = geo.camera_location()
    distance = math.sqrt(sum(value * value for value in location))

    assert ry == 0.0
    assert distance == pytest.approx(geo.CAMERA_DISTANCE)
    for axis in range(3):
        assert forward[axis] == pytest.approx(-location[axis] / distance)


def test_the_camera_frames_one_and_a_half_tiles_high_with_the_pivot_shift():
    assert geo.ortho_scale() == pytest.approx(1.5 * math.sqrt(2))
    assert geo.camera_shift() == pytest.approx((0.0, 80 / 192))


def test_renders_are_twice_the_frame_size():
    assert geo.render_size() == (256, 384)


def test_looping_clips_do_not_repeat_their_first_pose():
    assert geo.sample_times(1.0, loop=True) == pytest.approx([index / 10 for index in range(10)])


def test_held_clips_end_on_their_last_pose():
    times = geo.sample_times(0.92, loop=False)

    assert len(times) == 10
    assert times[-1] == pytest.approx(0.92)


def test_very_short_clips_still_get_a_frame():
    assert geo.sample_times(0.01, loop=True) == [0.0]


def test_the_hit_frame_snaps_to_the_nearest_frame():
    assert geo.hit_frame_index(0.42, 10) == 4


def test_the_hit_frame_is_never_the_first_or_past_the_last_frame():
    assert geo.hit_frame_index(0.0, 10) == 1
    assert geo.hit_frame_index(5.0, 10) == 9


def test_attacks_need_two_frames():
    with pytest.raises(ValueError, match="two frames"):
        geo.hit_frame_index(0.3, 1)
