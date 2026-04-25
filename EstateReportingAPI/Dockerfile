FROM stuartferguson/txnprocbase AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["EstateReportingAPI/NuGet.Config", "."]
COPY ["EstateReportingAPI/EstateReportingAPI.csproj", "EstateReportingAPI/"]
RUN dotnet restore "EstateReportingAPI/EstateReportingAPI.csproj"
COPY . .
WORKDIR "/src/EstateReportingAPI"
RUN dotnet build "EstateReportingAPI.csproj" -c Release -o /app/build

# Sort out certificate stuff here
RUN openssl x509 -inform DER -in /src/EstateReportingAPI/aspnetapp-root-cert.cer -out /src/EstateReportingAPI/aspnetapp-root-cert.crt
RUN cp /src/EstateReportingAPI/aspnetapp-root-cert.crt /usr/local/share/ca-certificates/
RUN update-ca-certificates

FROM build AS publish
RUN dotnet publish "EstateReportingAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EstateReportingAPI.dll"]