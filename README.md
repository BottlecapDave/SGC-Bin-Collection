# SGC-Bin-Collection
[![Nuget](https://img.shields.io/nuget/v/Bottlecap.SouthGloucestershireBinCollection.svg)](https://www.nuget.org/packages/Bottlecap.SouthGloucestershireBinCollection/)

Helper library for getting bin collection information from South Gloucestershire Council.

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

var addresses = await council.GetAddressesAsync(targetAddress);
```

If a single address is returned, then we've successfully matched your target address with the details the council holds. If multiple addresses are returned, then the provided address wasn't specific enough. If no addresses are returned, then the provided address doesn't match the council records. This could be because a certain line is specified, but held differently on their servers.

Once an address has been determined, pass the `Uprn` of the address object to `GetCollectionDatesAsync`

```
const collectionDates = await council.GetCollectionDatesAsync(addresses.First().Uprn);

```

This will return you the next 3 collection dates for `Refuse`, `Garden Waste` and `Recycling`.

Sometimes, SGC have adjustments to the collection dates on their website for events like Christmas. Unfortunately, these aren't done consistently so you can override the `GetDateAdjustmentsAsync` to provide these adjustments in a consistent format which well then be used to adjust the retrieved dates