FROM microsoft/dotnet:1.0.0-preview1

# In order to address an issue with running a sqlite3 database on aspnet-docker-linux
# a version of sqlite3 must be installed that is greater than or equal to 3.7.15
# which is not available on the default apt sources list in this image.
# ref: 	https://github.com/aspnet/EntityFramework/issues/3089
# 		https://github.com/aspnet/aspnet-docker/issues/121
RUN printf "deb http://ftp.us.debian.org/debian jessie main\n" >> /etc/apt/sources.list

RUN apt-get -qq update && apt-get -qqy install unzip curl libicu-dev libunwind8 gettext libssl-dev libcurl3-gnutls zlib1g  sqlite3 libsqlite3-dev && rm -rf /var/lib/apt/lists/*

COPY . /app
WORKDIR /app

RUN ["dotnet", "restore", "--configfile", "NuGet.config"]

EXPOSE 50000
ENTRYPOINT ["dotnet", "run", "--configuration", "Release", "-p", "project.json", "listen-web"]