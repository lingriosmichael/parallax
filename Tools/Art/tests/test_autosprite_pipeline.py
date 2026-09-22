"""Offline contract and sizing tests for the AutoSprite art pipeline."""

import io
import json
import sys
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch
from urllib.error import HTTPError

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import autosprite_pipeline as pipeline
from autosprite_client import AutoSpriteClient, AutoSpriteError


class FakeResponse(io.BytesIO):
    def __init__(self, data, url="https://storage.example/signed"):
        super().__init__(data)
        self.url = url

    def geturl(self):
        return self.url


class FakeOpener:
    def __init__(self, responses):
        self.responses = iter(responses)
        self.requests = []

    def open(self, request, timeout):
        self.requests.append(request)
        result = next(self.responses)
        if isinstance(result, Exception):
            raise result
        return result


def image_bytes(background="alpha", size=(200, 200), box=(55, 20, 145, 190)):
    colour = (0, 0, 0, 0) if background == "alpha" else (255, 255, 255, 255)
    image = Image.new("RGBA", size, colour)
    ImageDraw.Draw(image).rectangle(box, fill=(110, 65, 20, 255))
    buffer = io.BytesIO()
    image.save(buffer, format="PNG")
    return buffer.getvalue()


def spec():
    return {"type": "static", "name": "Test post", "prompt": "A side-view stone post",
            "target": {"width": 100, "height": 200, "ppu": 100,
                       "pivot": "bottom centre", "path": "Assets/_Game/Art/Test_Post.png"}}


class ClientTests(unittest.TestCase):
    def test_static_request_uses_header_and_expected_body(self):
        opener = FakeOpener([FakeResponse(b'{"id":"asset_1","baseImageUrl":"https://storage.example/a"}')])
        client = AutoSpriteClient(api_key="test-secret", opener=opener)
        result = client.create_asset("Post", "stone post", quality="ultra")
        self.assertEqual(result["id"], "asset_1")
        request = opener.requests[0]
        self.assertEqual(request.get_method(), "POST")
        self.assertEqual(request.get_header("X-api-key"), "test-secret")
        self.assertEqual(json.loads(request.data)["quality"], "ultra")
        self.assertEqual(request.full_url, "https://www.autosprite.io/api/v1/assets")

    def test_download_does_not_send_api_key_to_storage(self):
        opener = FakeOpener([FakeResponse(b"png")])
        client = AutoSpriteClient(api_key="test-secret", opener=opener)
        self.assertEqual(client.download("https://storage.example/signed"), b"png")
        self.assertIsNone(opener.requests[0].get_header("X-api-key"))
        with self.assertRaises(ValueError):
            client.download("http://storage.example/insecure")

    def test_rate_limit_retries_and_api_error_is_readable(self):
        limited = HTTPError("https://www.autosprite.io/api/v1/account", 429, "rate",
                            {"Retry-After": "2"}, io.BytesIO(b'{"error":{"code":"RATE_LIMIT_EXCEEDED","message":"slow down"}}'))
        opener = FakeOpener([limited, FakeResponse(b'{"credits":12}')])
        sleeps = []
        client = AutoSpriteClient(api_key="test-secret", opener=opener, sleep=sleeps.append)
        self.assertEqual(client.account()["credits"], 12)
        self.assertEqual(sleeps, [2])
        denied = HTTPError("https://www.autosprite.io/api/v1/assets", 402, "credits", {},
                           io.BytesIO(b'{"error":{"code":"INSUFFICIENT_CREDITS","message":"no credits"}}'))
        client = AutoSpriteClient(api_key="test-secret", opener=FakeOpener([denied]))
        with self.assertRaisesRegex(AutoSpriteError, "INSUFFICIENT_CREDITS: no credits"):
            client.create_asset("Post", "stone post")

    def test_animation_and_regeneration_request_shapes(self):
        opener = FakeOpener([FakeResponse(b'{"workflows":[],"creditsUsed":5}'),
                             FakeResponse(b'{"jobId":"job_2"}')])
        client = AutoSpriteClient(api_key="test-secret", opener=opener)
        client.generate_animations("char_1", [{"kind": "walk"}], 256, 10)
        client.regenerate_spritesheet("ss_1", 10, 256)
        self.assertEqual(json.loads(opener.requests[0].data)["withSound"], False)
        self.assertEqual(json.loads(opener.requests[1].data)["compression"], "none")
        self.assertTrue(opener.requests[1].full_url.endswith("/spritesheets/ss_1/regenerate"))

    def test_placeholder_key_blocks_requests(self):
        opener = FakeOpener([])
        client = AutoSpriteClient(api_key="your_key_here", opener=opener)
        with self.assertRaisesRegex(AutoSpriteError, "KEY_MISSING"):
            client.account()
        self.assertEqual(opener.requests, [])


class PipelineTests(unittest.TestCase):
    def test_manifest_and_custom_targets(self):
        target = pipeline.target_for({"slot": "A_OBJ_Checkpoint.png"})
        self.assertEqual((target.width, target.height, target.pivot), (295, 393, "bottom centre"))
        self.assertAlmostEqual(target.ppu, 196.667)
        self.assertEqual(pipeline.validate_spec(spec()).path, "Assets/_Game/Art/Test_Post.png")
        bad = spec()
        bad["target"]["path"] = "../outside.png"
        with self.assertRaisesRegex(ValueError, "Assets/_Game/Art"):
            pipeline.validate_spec(bad)

    def test_credit_ledger_survives_retry_and_caps_at_twenty(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "ledger.json"
            ledger = pipeline.Ledger(path)
            for _ in range(4):
                ledger.reserve("animation", 5)
            self.assertEqual(pipeline.Ledger(path).reserved, 20)
            with self.assertRaisesRegex(ValueError, "20-credit cap"):
                pipeline.Ledger(path).reserve("static", 1)

    def test_white_and_alpha_sources_fit_target_and_keep_pivot(self):
        target = pipeline.validate_spec(spec())
        for background in ("white", "alpha"):
            with self.subTest(background=background):
                output, metrics = pipeline.prepare_static(image_bytes(background), target)
                self.assertEqual(output.size, (100, 200))
                self.assertEqual(metrics["world_units"], (1.0, 2.0))
                self.assertEqual(output.getchannel("A").getbbox()[3], 200)
                self.assertEqual(metrics["background"], background)

    def test_missing_foreground_and_extreme_aspect_fail(self):
        target = pipeline.validate_spec(spec())
        empty = io.BytesIO()
        Image.new("RGBA", (100, 100), (0, 0, 0, 0)).save(empty, format="PNG")
        with self.assertRaisesRegex(ValueError, "no visible foreground"):
            pipeline.prepare_static(empty.getvalue(), target)
        with self.assertRaisesRegex(ValueError, "aspect differs"):
            pipeline.prepare_static(image_bytes(box=(5, 90, 195, 105)), target)

    def test_downloaded_jpeg_is_staged_as_real_png(self):
        source = Image.open(io.BytesIO(image_bytes("white"))).convert("RGB")
        jpeg = io.BytesIO()
        source.save(jpeg, format="JPEG", quality=95)
        with tempfile.TemporaryDirectory() as directory:
            pipeline.stage_static(jpeg.getvalue(), pipeline.validate_spec(spec()),
                                  Path(directory), 1)
            with Image.open(Path(directory) / "raw_v1.png") as staged:
                self.assertEqual(staged.format, "PNG")

    def test_static_generation_stages_files_and_resumes_without_new_charge(self):
        class FakeClient:
            creates = 0
            def create_asset(self, name, prompt, quality, use_prompt_template):
                self.creates += 1
                return {"id": "asset_1", "baseImageUrl": "https://storage.example/raw"}
            def asset(self, asset_id):
                return {"id": asset_id, "baseImageUrl": "https://storage.example/raw"}
            def download(self, url):
                return image_bytes("white")

        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            spec_path = root / "spec.json"
            spec_path.write_text(json.dumps(spec()))
            args = SimpleNamespace(command="static", spec=spec_path, work_root=root,
                                   execute=True)
            fake = FakeClient()
            with patch.object(pipeline, "AutoSpriteClient", return_value=fake):
                with redirect_stdout(io.StringIO()):
                    pipeline._run(args)
                    folder = pipeline.workspace_for(pipeline.target_for(spec()), root)
                    self.assertTrue((folder / "candidate_v1.png").exists())
                    self.assertTrue((folder / "contact_v1.png").exists())
                    (folder / "raw_v1.png").unlink()
                    pipeline._run(args)
            self.assertEqual(fake.creates, 1)
            self.assertEqual(pipeline.Ledger(folder / "ledger.json").reserved, 1)

    def test_unbudgeted_animation_options_are_rejected(self):
        animation = {**spec(), "type": "animation", "character_id": "char_1",
                     "animations": [{"kind": "walk", "firstFrameQuality": "pro"}]}
        animation["target"] = {**animation["target"], "width": 256, "height": 256}
        with self.assertRaisesRegex(ValueError, "unbudgeted"):
            pipeline.validate_spec(animation)
        animation["animations"] = [{"kind": "walk"}]
        animation["target"]["width"] = 100
        with self.assertRaisesRegex(ValueError, "cell dimensions"):
            pipeline.validate_spec(animation)

    def test_downloaded_sheet_grid_and_atlas_are_checked(self):
        sheet_image = Image.new("RGBA", (512, 256), (100, 50, 20, 255))
        buffer = io.BytesIO()
        sheet_image.save(buffer, format="PNG")

        class FakeClient:
            def spritesheet(self, sheet_id):
                return {"frameWidth": 256, "frameHeight": 256,
                        "frameCount": 2, "columns": 2,
                        "sheetUrl": "https://storage.example/png",
                        "atlasUrl": "https://storage.example/json"}
            def download(self, url):
                return buffer.getvalue() if url.endswith("png") else b'{"frames":[]}'

        with tempfile.TemporaryDirectory() as directory:
            path = pipeline.stage_sheet(FakeClient(), "ss_1", Path(directory), 256)
            self.assertEqual(path.read_bytes(), buffer.getvalue())
            self.assertTrue(path.with_suffix(".json").exists())
            with self.assertRaisesRegex(ValueError, "frame size differs"):
                pipeline.stage_sheet(FakeClient(), "ss_1", Path(directory), 128)


if __name__ == "__main__":
    unittest.main()
