FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /app

# Install coverlet
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install coverlet.console --global --version 1.4.1

# Copy csproj and restore as distinct layers
COPY src/*.csproj ./

COPY src/Tests/Bottlecap.SouthGloucestershireBinCollection.Tests.UnitTests/*.csproj ./Tests/Bottlecap.SouthGloucestershireBinCollection.Tests.UnitTests/

COPY src/*.sln ./
RUN dotnet restore

COPY src/ ./

COPY src/Tests/Bottlecap.SouthGloucestershireBinCollection.Tests.UnitTests/ ./Tests/Bottlecap.SouthGloucestershireBinCollection.Tests.UnitTests/

# Execute unit tests
RUN dotnet test ./Tests/Bottlecap.SouthGloucestershireBinCollection.Tests.UnitTests/Bottlecap.SouthGloucestershireBinCollection.Tests.UnitTests.csproj /p:CollectCoverage=true /p:CoverletOutput="../result/codecoverage/coverage.json"

# Define our environment variables so we can set our package information
ARG PACKAGE_VERSION
ARG NUGET_PACKAGE_API

# Build and pack attributes
RUN dotnet build ./ -c Release -o out /p:Version=$PACKAGE_VERSION
RUN dotnet pack ./ -c Release -o out /p:Version=$PACKAGE_VERSION

RUN ls ./out

# Push packages to nuget
RUN dotnet nuget push ./out/Bottlecap.SouthGloucestershireBinCollection.$PACKAGE_VERSION.nupkg -s https://api.nuget.org/v3/index.json -k $NUGET_PACKAGE_API