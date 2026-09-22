"""Small, Editor-independent client for AutoSprite's REST API."""

import json
import os
import time
from urllib.error import HTTPError, URLError
from urllib.parse import quote, urlsplit
from urllib.request import Request, build_opener


BASE_URL = "https://www.autosprite.io/api/v1"


class AutoSpriteError(RuntimeError):
    def __init__(self, code, message, status=None):
        self.code = code
        self.status = status
        super().__init__(f"AutoSprite {code}: {message}")


class AutoSpriteClient:
    def __init__(self, api_key=None, opener=None, sleep=time.sleep, base_url=BASE_URL):
        self.api_key = api_key if api_key is not None else os.environ.get("AUTOSPRITE_API_KEY")
        self.opener = opener or build_opener()
        self.sleep = sleep
        self.base_url = base_url.rstrip("/")

    def _request(self, method, path, payload=None):
        if not self.api_key or self.api_key == "your_key_here":
            raise AutoSpriteError("KEY_MISSING", "set AUTOSPRITE_API_KEY in your local environment")
        body = json.dumps(payload).encode("utf-8") if payload is not None else None
        headers = {"x-api-key": self.api_key, "Accept": "application/json"}
        if body is not None:
            headers["Content-Type"] = "application/json"
        request = Request(self.base_url + path, data=body, headers=headers, method=method)
        for retry in range(3):
            try:
                with self.opener.open(request, timeout=90) as response:
                    data = json.load(response)
                if not isinstance(data, dict):
                    raise AutoSpriteError("BAD_RESPONSE", "expected a JSON object")
                return data
            except HTTPError as error:
                if error.code == 429:
                    retry_after = error.headers.get("Retry-After", "")
                    try:
                        delay = int(retry_after)
                    except ValueError:
                        delay = 0
                    if retry < 2 and 0 < delay <= 30:
                        self.sleep(delay)
                        continue
                try:
                    detail = json.loads(error.read().decode("utf-8")).get("error", {})
                except (ValueError, UnicodeError):
                    detail = {}
                code = detail.get("code", "HTTP_ERROR")
                message = detail.get("message", f"HTTP {error.code}")
                if error.code == 429:
                    message = f"{message}; retry after {error.headers.get('Retry-After', 'unknown')} seconds"
                raise AutoSpriteError(code, message, error.code) from error
            except URLError as error:
                raise AutoSpriteError("NETWORK_ERROR", str(error.reason)) from error
        raise AutoSpriteError("RATE_LIMIT_EXCEEDED", "retry limit reached", 429)

    @staticmethod
    def _id(value):
        if not isinstance(value, str) or not value or "/" in value:
            raise ValueError("invalid AutoSprite id")
        return quote(value, safe="")

    def account(self):
        return self._request("GET", "/account")

    def create_asset(self, name, prompt, quality="turbo", use_prompt_template=True):
        return self._request("POST", "/assets", {
            "name": name, "prompt": prompt, "quality": quality,
            "usePromptTemplate": use_prompt_template,
        })

    def asset(self, asset_id):
        cursor = None
        while True:
            suffix = f"?limit=50&cursor={quote(cursor, safe='')}" if cursor else "?limit=50"
            page = self._request("GET", "/assets" + suffix)
            for item in page.get("assets", []):
                if item.get("id") == asset_id:
                    return item
            if not page.get("hasMore") or not page.get("nextCursor"):
                raise AutoSpriteError("NOT_FOUND", f"asset {asset_id} was not found")
            cursor = page["nextCursor"]

    def create_character(self, name, prompt, quality="turbo", is_humanoid=False):
        return self._request("POST", "/characters", {
            "name": name, "prompt": prompt, "quality": quality,
            "isHumanoid": is_humanoid,
        })

    def generate_animations(self, character_id, animations, frame_size=256,
                            frame_count=25, video_tier="turbo"):
        return self._request("POST", f"/characters/{self._id(character_id)}/spritesheets", {
            "animations": animations, "frameSize": frame_size,
            "frameCount": frame_count, "videoTier": video_tier,
            "withSound": False, "removeBg": "ultra",
        })

    def job(self, job_id):
        return self._request("GET", f"/jobs/{self._id(job_id)}")

    def wait_for_job(self, job_id, timeout=300, interval=5):
        deadline = time.monotonic() + timeout
        while True:
            result = self.job(job_id)
            if result.get("status") == "succeeded":
                return result
            if result.get("status") == "failed":
                raise AutoSpriteError("JOB_FAILED", str(result.get("error", "unknown error")))
            if time.monotonic() + interval > deadline:
                raise AutoSpriteError("JOB_TIMEOUT", f"job {job_id} is still running")
            self.sleep(interval)

    def spritesheet(self, sheet_id):
        return self._request("GET", f"/spritesheets/{self._id(sheet_id)}")

    def regenerate_spritesheet(self, sheet_id, frame_count, frame_size):
        return self._request("POST", f"/spritesheets/{self._id(sheet_id)}/regenerate", {
            "frameCount": frame_count, "frameSize": frame_size,
            "removeBg": "ultra", "compression": "none",
        })

    def download(self, signed_url, max_bytes=50_000_000):
        if urlsplit(signed_url).scheme != "https":
            raise ValueError("AutoSprite download URL must use HTTPS")
        # Signed storage URLs are fetched without the API key.
        request = Request(signed_url, headers={"Accept": "image/png, application/json"})
        try:
            with self.opener.open(request, timeout=90) as response:
                if urlsplit(response.geturl()).scheme != "https":
                    raise ValueError("download redirected away from HTTPS")
                data = response.read(max_bytes + 1)
        except URLError as error:
            raise AutoSpriteError("DOWNLOAD_FAILED", str(error.reason)) from error
        if len(data) > max_bytes:
            raise AutoSpriteError("DOWNLOAD_TOO_LARGE", "download exceeds size limit")
        return data
