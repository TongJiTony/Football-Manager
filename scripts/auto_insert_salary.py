import cx_Oracle
from datetime import datetime
import schedule
import time

def job():
    today = datetime.today()
    if today.day == 1:  # 只在每月的第一天执行
        # 连接到Oracle数据库
        dsn_tns = cx_Oracle.makedsn('47.103.206.98', '1521', service_name='xe')
        conn = cx_Oracle.connect(user='test_lx', password='test_lx', dsn=dsn_tns)

        cursor = conn.cursor()

        # 获取当前月份的第一天
        first_day_of_month = today.replace(day=1)

        # 查询所有teamid，并获取不重复的playerid的salary求和
        query = """
            SELECT team_id, SUM(salary)
            FROM (
                SELECT DISTINCT team_id, player_id, salary 
                FROM Contracts
            )
            GROUP BY team_id
        """

        cursor.execute(query)
        results = cursor.fetchall()

        # 插入到财务记录表中
        insert_query = """
            INSERT INTO Records (record_id, team_id, TRANSACTION_DATE, amount, description) 
            VALUES (RECORD_SEQ.NEXTVAL, :teamid, :TRANSACTION_DATE, :amount, :description)
        """

        for teamid, total_salary in results:
            cursor.execute(insert_query, [teamid, first_day_of_month, -total_salary, "球员薪水"])

        # 提交事务
        conn.commit()

        # 关闭连接
        cursor.close()
        conn.close()

# 安排任务在每天的01:00执行
schedule.every().day.at("01:00").do(job)

# 持续运行，等待任务执行
while True:
    schedule.run_pending()
    time.sleep(1)
