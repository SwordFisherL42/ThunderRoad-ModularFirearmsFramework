import logging as log
import requests
import sys
import time
from enum import Enum


class Client:
    def __init__(self, api_source: Enum, cookie: str = None, user_agent: str = None, request_delay: int = 5) -> None:
        self.source = api_source.value
        self.headers = {'user-agent': user_agent, 'cookie': cookie}
        self.session = requests.Session()
        self.session.headers.update(self.headers)
        self.delay = request_delay

    def get(self, uri: Enum, params: Enum, *args, **kwargs) -> requests.Response:
        try:
            time.sleep(self.delay)
            r = self.session.get(f"{self.source}{uri.value}", *args, params=params.value, **kwargs)
            return r
        except Exception as e:
            log.error(f"Requests GET Error: {e.__traceback__}", sys.exit(1))

    def post(self, uri: Enum, params: Enum, *args, **kwargs) -> requests.Response:
        try:
            time.sleep(self.delay)
            r = self.session.post(f"{self.source}{uri.value}", *args,  params=params.value, **kwargs)
            return r
        except Exception as e:
            log.error(f"Requests POST Error: {e.__traceback__}", sys.exit(1))

    def set_cookie(self, cookie: str) -> None:
        self.headers['cookie'] = cookie
        self.session.headers.update(self.headers)

    def set_user_agent(self, user_agent: str) -> None:
        self.headers['user-agent'] = user_agent
        self.session.headers.update(self.headers)


class NxmURI(Enum):
    base = "https://www.nexusmods.com/Core/Libs/Common/Managers"
    mods = "/Mods"
    forum = "/Forum"


class NxmParams(Enum):
    save = "Save"
    edit = "EditComment"
