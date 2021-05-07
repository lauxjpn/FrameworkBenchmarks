import json
import MySQLdb
import traceback

from colorama import Fore
from toolset.utils.output_helper import log
from toolset.databases.abstract_database import AbstractDatabase


class Database(AbstractDatabase):

    margin = 1.0 # 1.015

    @classmethod
    def get_connection(cls, config):
        return MySQLdb.connect(config.database_host, "benchmarkdbuser",
                                 "benchmarkdbpass", "hello_world")

    @classmethod
    def get_current_world_table(cls, config):
        results_json = []

        try:
            db = cls.get_connection(config)
            cursor = db.cursor()
            cursor.execute("SELECT * FROM World")
            results = cursor.fetchall()
            results_json.append(json.loads(json.dumps(dict(results))))
            db.close()
        except Exception:
            tb = traceback.format_exc()
            log("ERROR: Unable to load current MariaDB World table.",
                color=Fore.RED)
            log(tb)

        return results_json

    @classmethod
    def test_connection(cls, config):
        try:
            db = cls.get_connection(config)
            cursor = db.cursor()
            cursor.execute("SELECT 1")
            cursor.fetchall()
            db.close()
            return True
        except:
            return False

    @classmethod
    def get_queries(cls, config):
        db = cls.get_connection(config)
        cursor = db.cursor()
        cursor.execute("Show global status where Variable_name in ('Com_select','Com_update')")
        res = 0
        records = cursor.fetchall()
        for row in records:
            res = res + int(int(row[1]) * cls.margin) # MySQL/MariaDB might count inaccurate under load
        return res

    @classmethod
    def get_rows(cls, config):
        db = cls.get_connection(config)
        cursor = db.cursor()
        cursor.execute("""SELECT `r`.`VARIABLE_VALUE` - `u`.`VARIABLE_VALUE` FROM 
                        (SELECT `VARIABLE_VALUE` FROM `INFORMATION_SCHEMA`.`GLOBAL_STATUS` WHERE `VARIABLE_NAME` = 'INNODB_ROWS_READ') r,
                        (SELECT `VARIABLE_VALUE` FROM `INFORMATION_SCHEMA`.`GLOBAL_STATUS` WHERE `VARIABLE_NAME` = 'INNODB_ROWS_UPDATED') u""")
        record = cursor.fetchone()
        return int(int(record[0]) * cls.margin) # MySQL/MariaDB might count inaccurate under load

    @classmethod
    def get_rows_updated(cls, config):
        db = cls.get_connection(config)
        cursor = db.cursor()
        cursor.execute("show session status like 'Innodb_rows_updated'")
        record = cursor.fetchone()
        return int(int(record[1]) * cls.margin) # MySQL/MariaDB might count inaccurate under load

    @classmethod
    def reset_cache(cls, config):
        #The query cache is disabled by default in MariaDB 10.5
        #cursor = self.db.cursor()
        #cursor.execute("RESET QUERY CACHE")
        #self.db.commit()
        return
