FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Dhblog.sln ./
COPY src/Dhblog.Database/Dhblog.Database.csproj src/Dhblog.Database/
COPY src/Dhblog.DataAccess/Dhblog.DataAccess.csproj src/Dhblog.DataAccess/
COPY src/Dhblog.Api/Dhblog.Api.csproj src/Dhblog.Api/
RUN dotnet restore src/Dhblog.Api/Dhblog.Api.csproj
COPY src/ src/
RUN dotnet publish src/Dhblog.Api/Dhblog.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Dhblog.Api.dll"]
