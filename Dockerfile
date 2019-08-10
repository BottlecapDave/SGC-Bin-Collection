FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /app

# Install coverlet
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install coverlet.console --global --version 1.4.1

# Copy csproj and restore as distinct layers
COPY src/SouthGloucestershireBinCollection/*.csproj ./SouthGloucestershireBinCollection/

COPY src/Tests/SouthGloucestershireBinCollection.Tests.UnitTests/*.csproj ./Tests/SouthGloucestershireBinCollection.Tests.UnitTests/

COPY src/*.sln ./
RUN dotnet restore

COPY src/SouthGloucestershireBinCollection/ ./SouthGloucestershireBinCollection/

COPY src/Tests/SouthGloucestershireBinCollection.Tests.UnitTests/ ./Tests/SouthGloucestershireBinCollection.Tests.UnitTests/

# Execute unit tests
RUN dotnet test ./Tests/SouthGloucestershireBinCollection.Tests.UnitTestsn/SouthGloucestershireBinCollection.Tests.UnitTests.csproj /p:CollectCoverage=true /p:CoverletOutput="../result/codecoverage/coverage.json"

# Define our environment variables so we can set our package information
ARG PACKAGE_VERSION
ARG NUGET_PACKAGE_API

# Build and pack attributes
RUN dotnet build ./SouthGloucestershireBinCollection/ -c Release -o out /p:Version=$PACKAGE_VERSION
RUN dotnet pack ./SouthGloucestershireBinCollection/ -c Release -o out /p:Version=$PACKAGE_VERSION

# Push packages to nuget
RUN dotnet nuget push ./SouthGloucestershireBinCollection/out/SouthGloucestershireBinCollection.$PACKAGE_VERSION.nupkg -s https://api.nuget.org/v3/index.json -k $NUGET_PACKAGE_API