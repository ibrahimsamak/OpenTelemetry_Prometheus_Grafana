FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY CrudApp/CrudApp.csproj CrudApp/
RUN dotnet restore CrudApp/CrudApp.csproj

COPY CrudApp/ CrudApp/
RUN dotnet publish CrudApp/CrudApp.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
# Folder for the SQLite file, owned by the non-root "app" user (APP_UID=1654).
RUN mkdir -p /app/data && chown -R $APP_UID /app/data
USER $APP_UID
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__Default="Data Source=/app/data/app.db"
EXPOSE 8080
ENTRYPOINT ["dotnet", "CrudApp.dll"]
