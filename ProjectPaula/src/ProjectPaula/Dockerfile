FROM microsoft/dotnet:1.0.0-preview1

COPY . /app
WORKDIR /app

RUN ["dotnet", "restore"]

EXPOSE 50000
ENTRYPOINT ["dotnet", "run", "--configuration", "Release", "-p", "project.json", "listen-web"]