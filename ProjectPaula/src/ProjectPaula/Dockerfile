FROM microsoft/aspnet:1.0.0-rc1-update1-coreclr

COPY . /app
WORKDIR /app
RUN ["dnu", "restore"]

EXPOSE 50000
ENTRYPOINT ["dnx", "--configuration", "Release", "-p", "project.json", "web-production-docker"]