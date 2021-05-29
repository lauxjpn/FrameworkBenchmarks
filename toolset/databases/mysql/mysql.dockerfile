FROM buildpack-deps:bionic

ADD create.sql create.sql
ADD my.cnf my.cnf
ADD mysql.list mysql.list

RUN cp mysql.list /etc/apt/sources.list.d/
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 8C718D3B5072E1F5

RUN apt-get update > /dev/null
RUN apt-get install -yqq locales > /dev/null

RUN locale-gen en_US.UTF-8
ENV LANG en_US.UTF-8
ENV LANGUAGE en_US:en
ENV LC_ALL en_US.UTF-8

# https://bugs.mysql.com/bug.php?id=90695
RUN ["/bin/bash", "-c", "debconf-set-selections <<< \"mysql-server mysql-server/lowercase-table-names select Enabled\""]
RUN ["/bin/bash", "-c", "debconf-set-selections <<< \"mysql-community-server mysql-community-server/data-dir select 'Y'\""]
RUN ["/bin/bash", "-c", "debconf-set-selections <<< \"mysql-community-server mysql-community-server/root-pass password secret\""]
RUN ["/bin/bash", "-c", "debconf-set-selections <<< \"mysql-community-server mysql-community-server/re-root-pass password secret\""]
RUN echo "Installing mysql-server version: $(apt-cache policy mysql-server | grep -oP "(?<=Candidate: )(.*)$")"
RUN DEBIAN_FRONTEND=noninteractive apt-get -y install mysql-server > /dev/null

RUN mv /etc/mysql/my.cnf /etc/mysql/my.cnf.orig
RUN cp my.cnf /etc/mysql/my.cnf

RUN rm -rf /ssd/mysql
RUN rm -rf /ssd/log/mysql
RUN cp -R -p /var/lib/mysql /ssd/
RUN cp -R -p /var/log/mysql /ssd/log
RUN mkdir -p /var/run/mysqld

# It may seem weird that we call `service mysql start` several times, but the RUN
# directive is a 1-time operation for building this image. Subsequent RUN calls
# do not see running processes from prior RUN calls; therefor, each command here
# that relies on the mysql server running will explicitly start the server and
# perform the work required.
# RUN chown -R mysql:mysql /var/lib/mysql /var/log/mysql /var/run/mysqld /ssd && \
#     mysqld & \
#     until mysql -uroot -psecret -e "exit"; do sleep 1; done && \
#     mysqladmin -uroot -psecret flush-hosts && \
#     mysql -uroot -psecret < create.sql

RUN chown -R mysql:mysql /var/lib/mysql /var/log/mysql /var/run/mysqld /ssd

HEALTHCHECK CMD mysqladmin --defaults-extra-file=/healthcheck.cnf ping

RUN echo 'TRACE 01: Processes before start and shutdown MySQL if necessary' && \
    ((mysqladmin processlist -uroot -psecret && mysqladmin shutdown -uroot -psecret) || true) && \
    echo 'TRACE 02: Start MySQL' && \
    (mysqld &) && \
    echo 'TRACE 03: Try to connect...' && \
    until mysqladmin ping -uroot -psecret; do sleep 1; done && \
    echo 'TRACE 04: Processes after start' && \
    (mysqladmin processlist -uroot -psecret || true ) && \
    echo 'TRACE 05: Add additional local permissions' && \
    (mysql -uroot -psecret -e "create user 'root'@'127.0.0.1' identified with caching_sha2_password BY 'secret'; grant all privileges on *.* to 'root'@'127.0.0.1';") && \
    echo 'TRACE 06: Execute flush-privileges' && \
    (mysqladmin flush-privileges -uroot -psecret) && \
    echo 'TRACE 07: Execute flush-hosts' && \
    (mysqladmin flush-hosts -uroot -psecret) && \
    echo 'TRACE 08: Import SQL' && \
    (mysql -uroot -psecret < create.sql) && \
    echo 'TRACE 09: Check that socket works' && \
    (mysql -uroot -psecret --table -e "select user, authentication_string, plugin, host FROM mysql.user") && \
    echo 'TRACE 10: Try to connect...' && \
    until mysqladmin ping --protocol=socket --socket=/var/run/mysqld/mysqld.sock -uroot -psecret; do sleep 1; done && \
    echo 'TRACE 11: Processes after start (socket)' && \
    (mysqladmin processlist --protocol=socket --socket=/var/run/mysqld/mysqld.sock -uroot -psecret || true ) && \
    echo 'TRACE 12: Check that socket works' && \
    (mysql --protocol=socket --socket=/var/run/mysqld/mysqld.sock -uroot -psecret --table -e "select @@version") && \
    (mysql --protocol=socket --socket=/var/run/mysqld/mysqld.sock -uroot -psecret --table -e "select * from information_schema.schemata") && \
    echo 'TRACE 13: Try to connect...' && \
    until mysqladmin ping --protocol=tcp --port=3306 -uroot -psecret; do sleep 1; done && \
    echo 'TRACE 14: Processes after start (tcp)' && \
    (mysqladmin processlist --protocol=tcp --port=3306 -uroot -psecret || true ) && \
    echo 'TRACE 15: Check that TCP works' && \
    (mysql --protocol=tcp --port=3306 -uroot -psecret --table -e "select @@version" || true) && \
    (mysql --protocol=tcp --port=3306 -uroot -psecret --table -e "select * from information_schema.schemata" || true) && \
    echo 'TRACE 16: Try to connect (client, tcp)...' && \
    until mysql --protocol=tcp --port=3306 -uroot -psecret -e "exit"; do sleep 1; done && \
    echo 'TRACE 17: Check that TCP works' && \
    (mysql --protocol=tcp --port=3306 -uroot -psecret --table -e "select @@version") && \
    (mysql --protocol=tcp --port=3306 -uroot -psecret --table -e "select * from information_schema.schemata") && \
    echo 'TRACE 18: Shutdown MySQL' && \
    (mysqladmin shutdown -uroot -psecret || true)

RUN chown -R mysql:mysql /var/lib/mysql /var/log/mysql /var/run/mysqld /ssd

CMD ["mysqld"]
