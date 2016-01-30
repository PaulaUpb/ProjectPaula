FROM microsoft/aspnet:1.0.0-rc1-update1-coreclr

COPY . /app
WORKDIR /app
RUN ["dnu", "restore"]

ENTRYPOINT ["dnx", "-p", "project.json", "web-production-docker", "--server.urls http://0.0.0.0:80"]