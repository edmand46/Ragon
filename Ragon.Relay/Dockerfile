FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env

WORKDIR /App

COPY . ./

RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:9.0

WORKDIR /App

COPY --from=build-env /App/out .

ENTRYPOINT ["Ragon.Relay"]