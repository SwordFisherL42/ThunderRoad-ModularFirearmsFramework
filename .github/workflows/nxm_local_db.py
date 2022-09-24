import sqlite3
import logging

local_file = './nxm.db'


def make_table():
    connection = sqlite3.connect(local_file)
    connection.cursor().execute('CREATE TABLE community_posts (mod_id INTEGER PRIMARY KEY UNIQUE, comment_id INTEGER NOT NULL);')
    connection.commit()
    connection.close()


def insert_post(mod_id, comment_id):
    connection = sqlite3.connect(local_file)
    try:
        connection.cursor().execute(f'INSERT INTO community_posts (mod_id, comment_id) VALUES ({mod_id}, {comment_id});')
        connection.commit()
    except sqlite3.IntegrityError as e:
        logging.error('ID Already in table.')
    connection.close()


def get_all_posts():
    connection = sqlite3.connect(local_file)
    rows = connection.cursor().execute('SELECT * FROM community_posts;').fetchall()
    connection.close()
    return rows


def get_post(mod_id):
    connection = sqlite3.connect(local_file)
    post_id = connection.cursor().execute(f'SELECT * FROM community_posts WHERE mod_id == {mod_id};').fetchone()[1]
    connection.close()
    return post_id


def print_table():
    rows = get_all_posts()
    for row in rows:
        logging.info(f"MOD_ID:{row[0]}  POST_ID:{row[1]}")
