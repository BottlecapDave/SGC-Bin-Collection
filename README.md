# SGC-Bin-Collection
Helper library for getting bin collection information from South Gloucestershire Council

## Building

To build, execute the tests, and deploy to nuget run the following command

```
docker image build ./ -t bottlecap.net.bots:latest --build-arg PACKAGE_VERSION=<<VERSION HERE> --build-arg NUGET_PACKAGE_API=<<NUGET API KEY HERE>
```