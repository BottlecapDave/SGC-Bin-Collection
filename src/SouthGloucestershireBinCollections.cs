using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SouthGloucestershireBinCollection
{
    public class SouthGloucestershireBinCollections
    {
        private const int REFUSE_NUMBER_OF_DAYS_BETWEEN_PICKUPS = 14;
        private const int RECYLCING_NUMBER_OF_DAYS_BETWEEN_PICKUPS = 7;
        private const int GARDEN_WASTE_NUMBER_OF_DAYS_BETWEEN_PICKUPS = 14;

        private const string BASE_URL = "https://webapps.southglos.gov.uk/";
        private const string WEBSITE_BASE_URL = "https://www.southglos.gov.uk/";
        private const string ADDRESS_URL = "Webservices/SGC.RefuseCollectionService/RefuseCollectionService.svc/getAddresses/{0}";
        private const string ALTERED_DATES_URL = "environment-and-planning/recycling-rubbish-and-waste/check-your-collection-date/";
        private const string COLLECTION_DATES_URL = "Webservices/SGC.RefuseCollectionService/RefuseCollectionService.svc/getCollections/{0}";

        public async Task<IEnumerable<Address>> GetAddressesAsync(Address address)
        {
            var client = new RestClient(BASE_URL);

            var request = new RestRequest(String.Format(ADDRESS_URL, address.Postcode));
            request.AddHeader("Content-Type", "application/json");

            var response = await client.GetAsync<List<Address>>(request);

            return this.FilterAddresses(address, response);
        }

        public async Task<CollectionDates> GetCollectionDatesAsync(string id)
        {
            var client = new RestClient(BASE_URL);
            client.AddHandler("application/json", () => UKJsonSerializer.Default);

            var request = new RestRequest(String.Format(COLLECTION_DATES_URL, id));
            request.AddHeader("Content-Type", "application/json");

            var normalCollectionDates = client.GetAsync<List<InternalCollectionDates>>(request);
            var adjustedDates = this.GetAlteredDatesAsync();

            await Task.WhenAll(normalCollectionDates, adjustedDates);

            if (normalCollectionDates.Result != null)
            {
                var collectionDate = normalCollectionDates.Result.FirstOrDefault();
                if (collectionDate != null)
                {
                    return new CollectionDates()
                    {
                        Refuse1 = this.GetActualDate(collectionDate.R1, adjustedDates.Result, REFUSE_NUMBER_OF_DAYS_BETWEEN_PICKUPS),
                        Refuse2 = this.GetActualDate(collectionDate.R2, adjustedDates.Result, REFUSE_NUMBER_OF_DAYS_BETWEEN_PICKUPS),
                        Refuse3 = this.GetActualDate(collectionDate.R3, adjustedDates.Result, REFUSE_NUMBER_OF_DAYS_BETWEEN_PICKUPS),

                        GardenWaste1 = this.GetActualDate(collectionDate.G1, adjustedDates.Result, GARDEN_WASTE_NUMBER_OF_DAYS_BETWEEN_PICKUPS),
                        GardenWaste2 = this.GetActualDate(collectionDate.G2, adjustedDates.Result, GARDEN_WASTE_NUMBER_OF_DAYS_BETWEEN_PICKUPS),
                        GardenWaste3 = this.GetActualDate(collectionDate.G3, adjustedDates.Result, GARDEN_WASTE_NUMBER_OF_DAYS_BETWEEN_PICKUPS),

                        // RX represents recycling and non-recycling
                        Recycling1 = this.GetActualDate(collectionDate.R1 < collectionDate.C1 ? collectionDate.R1 : collectionDate.C1, adjustedDates.Result, RECYLCING_NUMBER_OF_DAYS_BETWEEN_PICKUPS),
                        Recycling2 = this.GetActualDate(collectionDate.R2 < collectionDate.C2 ? collectionDate.R2 : collectionDate.C2, adjustedDates.Result, RECYLCING_NUMBER_OF_DAYS_BETWEEN_PICKUPS),
                        Recycling3 = this.GetActualDate(collectionDate.R3 < collectionDate.C3 ? collectionDate.R3 : collectionDate.C3, adjustedDates.Result, RECYLCING_NUMBER_OF_DAYS_BETWEEN_PICKUPS),
                    };
                }
            }

            return null;
        }

        private async Task<IEnumerable<Tuple<DateTime, DateTime>>> GetAlteredDatesAsync()
        {
            var client = new WebClient();
            var content = await client.DownloadStringTaskAsync(new Uri(new Uri(WEBSITE_BASE_URL), ALTERED_DATES_URL));

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(content);

            var table = doc.DocumentNode.SelectSingleNode("//table")
                        ?.SelectSingleNode("//tbody")
                        ?.Descendants("tr")
                        ?.Skip(1) // Skip the headers
                        ?.Select(tr => tr.Elements("td").Select(td => td.InnerText.Trim()).ToList());

            var alternativeDates = new List<Tuple<DateTime, DateTime>>();
            if (table != null)
            {
                foreach (var row in table)
                {
                    if (row.Count != 2)
                    {
                        continue;
                    }

                    if (TryParseDate(row[0], out DateTime originalDate) == false ||
                        TryParseDate(row[1], out DateTime newDate) == false)
                    {
                        continue;
                    }

                    alternativeDates.Add(new Tuple<DateTime, DateTime>(originalDate, newDate));
                }
            }

            return alternativeDates;
        }

        private DateTime? GetActualDate(DateTime? targetDate, IEnumerable<Tuple<DateTime, DateTime>> alternativeDates, int numberOfDaysBetweenPickups)
        {
            if (targetDate.HasValue && alternativeDates != null)
            {
                // Check to see if we have a alternative date for the last collection,
                // and if so if the alternative date has yet to occur.
                var passedDate = targetDate.Value.AddDays(numberOfDaysBetweenPickups * -1);
                var passedAlternativeDate = alternativeDates.FirstOrDefault(x => x.Item1 == passedDate);
                if (passedAlternativeDate != null &&
                    passedAlternativeDate?.Item2.Date >= DateTime.UtcNow.Date)
                {
                    return passedAlternativeDate.Item2;
                }

                // If our target date is present in our alternative dates section, then we must
                // use our alternative date.
                var alternativeDate = alternativeDates.FirstOrDefault(x => x.Item1 == targetDate);
                if (alternativeDate != null)
                {
                    return alternativeDate.Item2;
                }
            }

            return targetDate;
        }

        private bool TryParseDate(string content, out DateTime date)
        {
            // Attempt to take any descriptions away from the content so we have the raw date to process.
            var bracketIndex = content.IndexOf("(");
            var trimmedContent = bracketIndex < 0 ? content : content.Substring(0, bracketIndex);

            if (DateTime.TryParse(trimmedContent, out date) == false)
            {
                // If our date failed to parse, it could be attempting to parse into the current year. Try and parse
                // the date for next year
                if (DateTime.TryParse($"{trimmedContent} {DateTime.UtcNow.Year + 1}", out date) == false)
                {
                    return false;
                }
            }

            return true;
        }

        private IReadOnlyList<Address> FilterAddresses(Address address, IEnumerable<Address> availableAddresses)
        {
            var property = address.Property?.ToLowerInvariant();
            var street = address.Street?.ToLowerInvariant();
            var town = address.Town?.ToLowerInvariant();

            if (String.IsNullOrEmpty(property) == false)
            {
                availableAddresses = availableAddresses.Where(x => property.StartsWith(x.Property.ToLowerInvariant()));

                if (availableAddresses.Count() > 1)
                {
                    availableAddresses = availableAddresses.Where(x => property.StartsWith(String.Format("{0} {1}", x.Property, x.Street).ToLowerInvariant()));
                }
            }

            if (availableAddresses.Count() > 1 &&
                String.IsNullOrEmpty(street) == false)
            {
                availableAddresses = availableAddresses.Where(x => street.StartsWith(x.Property.ToLowerInvariant()));

                if (availableAddresses.Count() > 1)
                {
                    availableAddresses = availableAddresses.Where(x => street.StartsWith(String.Format("{0} {1}", x.Property, x.Street).ToLowerInvariant()));
                }
            }

            if (availableAddresses.Count() > 1 &&
                String.IsNullOrEmpty(town) == false)
            {
                availableAddresses = availableAddresses.Where(x => town.StartsWith(x.Property.ToLowerInvariant()));

                if (availableAddresses.Count() > 1)
                {
                    availableAddresses = availableAddresses.Where(x => town.StartsWith(String.Format("{0} {1}", x.Property, x.Street).ToLowerInvariant()));
                }
            }

            return availableAddresses.ToList();
        }
    }
}
