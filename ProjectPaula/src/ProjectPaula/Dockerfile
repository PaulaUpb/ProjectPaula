FROM microsoft/dotnet:1.1.9-sdk-1.1.10-jessie

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

# Required for .NET Core 1.1 (https://github.com/dotnet/corefx/issues/15990#issuecomment-279023245)
# This is to prevent deadlocks on a single-core machine
ENV COMPlus_ThreadPool_ForceMinWorkerThreads 8
ENV COMPlus_ThreadPool_ForceMaxWorkerThreads 80

ENTRYPOINT ["dotnet", "run", "--configuration", "Release", "listen-web", "https"]
