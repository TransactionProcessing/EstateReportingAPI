FROM stuartferguson/txnprocbase AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src
COPY ["NuGet.Config", "."]
COPY ["EstateReportingAPI/EstateReportingAPI.csproj", "EstateReportingAPI/"]
COPY ["Directory.Packages.props", "."]
COPY . .
RUN dotnet restore "EstateReportingAPI/EstateReportingAPI.csproj"
COPY . .
WORKDIR "/src/EstateReportingAPI"
RUN dotnet build "EstateReportingAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EstateReportingAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EstateReportingAPI.dll"]
