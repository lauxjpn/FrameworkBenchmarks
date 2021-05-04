FROM buildpack-deps:bionic

ADD create.sql create.sql
ADD my.cnf my.cnf
ADD mariadb.list mariadb.list

RUN cp mariadb.list /etc/apt/sources.list.d/
RUN apt-key adv --recv-keys --keyserver hkp://keyserver.ubuntu.com:80 0xF1656F24C74CD1D8

RUN apt-get update > /dev/null
RUN apt-get install -yqq locales > /dev/null

RUN locale-gen en_US.UTF-8
ENV LANG en_US.UTF-8
ENV LANGUAGE en_US:en
ENV LC_ALL en_US.UTF-8

# https://bugs.mysql.com/bug.php?id=90695
RUN ["/bin/bash", "-c", "debconf-set-selections <<< \"mariadb-server mysql-server/lowercase-table-names select Enabled\""]
RUN ["/bin/bash", "-c", "debconf-set-selections <<< \"mariadb-server mysql-server/data-dir select 'Y'\""]
RUN ["/bin/bash", "-c", "debconf-set-selections <<< \"mariadb-server mysql-server/root-pass password secret\""]
RUN ["/bin/bash", "-c", "debconf-set-selections <<< \"mariadb-server mysql-server/re-root-pass password secret\""]
RUN echo "Installing mariadb-server version: $(apt-cache policy mariadb-server | grep -oP "(?<=Candidate: )(.*)$")"
RUN DEBIAN_FRONTEND=noninteractive apt-get -y install mariadb-server > /dev/null

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
RUN chown -R mysql:mysql /var/lib/mysql /var/log/mysql /var/run/mysqld /ssd && \
    mysqld & \
    until mysql -uroot -psecret -e "exit"; do sleep 1; done && \
    mysqladmin -uroot -psecret flush-hosts && \
    mysql -uroot -psecret < create.sql

CMD chown -R mysql:mysql /var/lib/mysql /var/log/mysql /var/run/mysqld /ssd && mysqld
