import sys
import datetime as dt
import argparse
import logging as log
import re
import copy
from enum import Enum
import nxm_local_db as db
from nxm_client import Client, NxmURI, NxmParams

log.basicConfig(format='%(asctime)s - %(message)s',
                datefmt='%d-%b-%y %H:%M:%S',
                level=log.DEBUG)


def readfile(fname):
    with open(fname, "r") as f:
        return f.read()


def run(options):
    # Set and parse local files
    db.local_file = options.db_file
    community_posts = db.get_all_posts()
    comment_template =  readfile(".nxm/sticky_comment_global")
    summary = readfile(".nxm/summary")
    description = readfile(".nxm/description")
    patreon = readfile(".nxm/patreon")
    special_comment_3067 = readfile(".nxm/sticky_comment_3067")
    # Setup http client
    cookie = options.cookie if options.cookie else read_cookiefile(options.cookiefile)
    if cookie is None:
        log.error("NXM Cookie not provided.")
        sys.exit(2)
    version = options.version if options.version else get_assembly_version(options.assembly_info)
    nxm = Client(NxmURI.base, cookie=cookie, user_agent=options.user_agent, request_delay=options.automation_delay)
    tags = {
        "$GLOB_FRAMEWORK_VERSION": version,
        "$GLOB_GAME_VERSION": "U11",
        "$GLOB_PATREON": patreon,
        "$GLOB_LATEST_UPDATE": f"{version} - The \"Execution Update\" (close-range shots)",
        "$GLOB_TIME": f"""{dt.datetime.now().strftime("%m/%d/%Y, %H:%M:%S")}(UTC)""",
        "$GLOB_TAG": options.post_tag,
        "$STICKY_3067": special_comment_3067,
        "$2555_ADDON_TEXT": "Thanks for supporting Modular Firearms framework!"
    }
    # Update Mod Page
    log.info("Mod Summary Generated")
    summary = parse_global_tags(tags, summary)
    log.debug(summary)
    description = parse_global_tags(tags, description)
    description = bot_description(description, options.post_tag)
    log.info("Mod Description Generated")
    log.debug(description)
    mod_details(nxm, description, summary, version, options)
    # Update community sticky posts
    post_sticky_comments(nxm, community_posts, comment_template, tags)
    for post in community_posts:
        log.info(f"https://www.nexusmods.com/bladeandsorcery/mods/{post[0]}")

def parse_global_tags(tags, body):
    new_body = copy.deepcopy(body)
    for tag, replacment in tags.items():
        replacment = replacment if "GLOB" in tag else ""
        new_body = new_body.replace(tag, replacment)
    return new_body


def post_sticky_comments(client, community_posts, post_body_template, tags):
    for comment in community_posts:
        mod_id = comment[0]
        post_id = comment[1]
        post_body = copy.deepcopy(post_body_template)
        for tag, replacment in tags.items():
            replacment = replacment if "GLOB" in tag or str(mod_id) in tag else ""
            post_body = post_body.replace(tag, replacment)
        log.info(f"Body generated for post {post_id} under mod {mod_id}")
        log.debug(post_body)
        sticky_comment(client, post_id, post_body)


def mod_details(client, body, summary, version, options):
    request_body = {
        "id": options.mod,
        "game_id": options.game,
        "type": options.type,
        "category_id": options.category,
        "name": options.name,
        "author": options.author,
        "summary": summary,
        "version": version,
        "description": body,
        "language_id": 0,
        "tag_gore": 0,
        "tag_nudity": 0,
        "tag_skimpy": 0,
        "tag_sexualised": 0,
        "tag_profanity": 0,
        "suggested_category_id": None,
        "suggested_new_category": "",
        "classtags": "",
        "new_game_name": "",
        }
    # Send POST request to update Mod Details
    response = client.post(NxmURI.mods, NxmParams.save, data=request_body)
    if response.status_code != 200:
        log.debug(response.content)
        log.error(f"Response Status Code: {response.status_code}", sys.exit(1))     
    response_json = response.json()
    log.debug(response_json)
    if response_json['status']:
        log.info("Update Request Successful")
        return
    log.error(f"Request Error: {response_json['message']}", sys.exit(2))


def sticky_comment(client, post_id, body):
    request_body = {"comment_id": post_id, "post": body, "use_emo": 0}
    response = client.post(NxmURI.forum, NxmParams.edit, data=request_body)
    if response.status_code != 200:
        log.debug(response.content)
        log.error(f"Response Status Code: {response.status_code}", sys.exit(1))     
    response_json = response.json()
    log.debug(response_json)
    if response_json['errors'] == '':
        log.info("Comment Update Successful")
        return
    log.error(f"Comment Request Error: {response_json['errors']}", sys.exit(2))


def read_cookiefile(fname):
    if fname is None:
        return None
    with open(fname, 'r') as f:
        return f.read().strip()


def get_assembly_version(assembly_file) -> str:
    if assembly_file is None:
        return '0.0.0'
    pattern: str = r'AssemblyFileVersion\("(.*?)"\)'
    with open(assembly_file, 'r') as fh:
        fs: str = fh.read()
    match = re.search(pattern, fs)
    if match:
        major, minor, patch = match.groups()[0].split('.')[:3]
        return f'{major}.{minor}.{patch}'


def bot_description(body, tag, *args):
    args_body = [str(a) for a in args]
    args_body = " \n".join(args_body)
    return f"""{body}\n{args_body}""" \
    f"""\n{tag} - {dt.datetime.now().strftime("%m/%d/%Y, %H:%M:%S")}(UTC)"""


if __name__ == "__main__":
    DEFAULT_TAG = """[i]THIS POST WAS AUTOGENERATED[/i] 🤖"""
    DEFAULT_USR_AGNT = r'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36'
    parser = argparse.ArgumentParser()
    parser.add_argument("-c", "--cookie", type=str, default=None)
    parser.add_argument("-m", "--mod", type=int, required=True)
    parser.add_argument("-v", "--version", type=str, default=None)
    parser.add_argument("-n", "--name", type=str, required=True)
    parser.add_argument("--cookiefile", type=str, default=None)
    parser.add_argument("--user_agent", type=str, default=DEFAULT_USR_AGNT)
    parser.add_argument("--post_tag", type=str, default=DEFAULT_TAG)
    parser.add_argument("--assembly_info", type=str, default=None)
    parser.add_argument("--author", type=str, default="SwordFisherL42")
    parser.add_argument("--game", type=int, default=2673)
    parser.add_argument("--type", type=int, default=1)
    parser.add_argument("--category", type=int, default=7)
    parser.add_argument("--automation_delay", type=int, default=5, help="Time between API calls")
    parser.add_argument("--db_file", type=str, default=".nxm/nxm.db")
    run(parser.parse_args())