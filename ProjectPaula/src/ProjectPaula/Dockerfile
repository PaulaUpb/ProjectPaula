FROM microsoft/dotnet:1.0.0-rc2-core-deps

COPY . /app
WORKDIR /app

EXPOSE 50000
ENTRYPOINT ["sh", "restore_and_run.sh"]