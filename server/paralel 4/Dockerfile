FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["paralel 4/paralel 4.csproj", "paralel 4/"]
RUN dotnet restore "paralel 4/paralel 4.csproj"
COPY . .
WORKDIR "/src/paralel 4"
RUN dotnet build "paralel 4.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "paralel 4.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "paralel 4.dll"]
