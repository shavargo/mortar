from mortar_art import actors


def test_every_actor_in_content_has_a_bake_entry():
    manifest = actors.load_manifest()

    assert sorted(actors.required_actor_ids() - set(manifest.actors)) == []


def test_committed_models_match_their_recorded_hashes():
    manifest = actors.load_manifest()

    assert actors.hash_models() == manifest.files


def test_every_model_pack_has_a_licence():
    packs = [path for path in actors.MODELS_ROOT.iterdir() if path.is_dir()]

    assert packs
    assert [pack.name for pack in packs if not (pack / actors.LICENCE_NAME).is_file()] == []
