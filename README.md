# SGC-Bin-Collection
![Nuget](https://img.shields.io/nuget/v/SouthGloucestershireBinCollection.svg)

Helper library for getting bin collection information from South Gloucestershire Council.

Releases can be found on [Nuget](https://www.nuget.org/packages/SouthGloucestershireBinCollection/).

## Building

To build, execute the tests, and deploy to nuget run the following command

```
docker image build ./ -t bottlecap.net.bots:latest --build-arg PACKAGE_VERSION=<<VERSION HERE> --build-arg NUGET_PACKAGE_API=<<NUGET API KEY HERE>
```

## Usage

To use, create an instance of the `SouthGloucestershireBinCollections`

```
var council = new SouthGloucestershireBinCollections();

```

Call `GetAddressesAsync`, providing an address object with as much of the address filled in.

```
var targetAddress = new Address()
{
    // Address details here
};

var addresses = await council.GetAddressesAsync(targetAddress)
```

If a single address is returned, then we've successfully matched your target address with the details the council holds. If multiple addresses are returned, then the provided address wasn't specific enough. If no addresses are returned, then the provided address doesn't match the council records. This could be because a certain line is specified, but held differently on their servers.

Once an address has been determined, pass the `Uprn` of the address object to `GetCollectionDatesAsync`

```

const collectionDates = await council.GetCollectionDatesAsync(addresses.First().Uprn)

```

This will return you the next 3 collection dates for `Refuse`, `Garden Waste` and `Recycling`.
