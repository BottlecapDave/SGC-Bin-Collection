# SGC-Bin-Collection
![Nuget](https://img.shields.io/nuget/v/SouthGloucestershireBinCollection.svg)

Helper library for getting bin collection information from South Gloucestershire Council.

Releases can be found on [Nuget](https://www.nuget.org/packages/SouthGloucestershireBinCollection/).

## Building

To build, execute the tests, and deploy to nuget run the following command

```
docker image build ./ -t bottlecap.net.bots:latest --build-arg PACKAGE_VERSION=<<VERSION HERE> --build-arg NUGET_PACKAGE_API=<<NUGET API KEY HERE>
```