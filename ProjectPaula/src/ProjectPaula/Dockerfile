FROM microsoft/dotnet:1.0.0-rc2-core-deps

COPY . /app
WORKDIR /app
RUN dotnet restore

EXPOSE 50000
ENTRYPOINT ["restore_and_run.sh"]