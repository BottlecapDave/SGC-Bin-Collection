﻿using RestSharp;
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
        private const string ADDRESS_URL = "Webservices/SGC.RefuseCollectionService/RefuseCollectionService.svc/getAddresses/{0}";
        private const string ALTERED_DATES_URL = "environment-and-planning/recycling-rubbish-and-waste/check-your-collection-date/";
        private const string COLLECTION_DATES_URL = "Webservices/SGC.RefuseCollectionService/RefuseCollectionService.svc/getCollections/{0}";

        /// <summary>
        /// Retrieve all south gloucestershire registered addresses for the provided partial address.
        /// </summary>
        /// <param name="address">The partial address to find the address records for.</param>
        /// <returns>The collection of addresses</returns>
        public async Task<IEnumerable<Address>> GetAddressesAsync(Address address)
        {
            var client = new RestClient(BASE_URL);

            var request = new RestRequest(String.Format(ADDRESS_URL, address.Postcode));
            request.AddHeader("Content-Type", "application/json");

            var response = await client.GetAsync<List<Address>>(request);

            return this.FilterAddresses(address, response);
        }

        /// <summary>
        /// Get collecton dates for the provided address id
        /// </summary>
        /// <param name="id">The id of the address to get the collection dates for.</param>
        /// <param name="dateAdjustments">The collection of date adjustments to apply. This is to counteract the fact 
        /// South Gloucestershire council don't always alter their dates for events like christmas</param>
        /// <returns>The collection of collection dates</returns>
        public async Task<CollectionDates> GetCollectionDatesAsync(string id, IEnumerable<DateAdjustment> dateAdjustments)
        {
            var client = new RestClient(BASE_URL);
            client.AddHandler("application/json", () => UKJsonSerializer.Default);

            var request = new RestRequest(String.Format(COLLECTION_DATES_URL, id));
            request.AddHeader("Content-Type", "application/json");

            var normalCollectionDates = await client.GetAsync<List<InternalCollectionDates>>(request);

            if (normalCollectionDates != null)
            {
                var collectionDate = normalCollectionDates.FirstOrDefault();
                if (collectionDate != null)
                {
                    return new CollectionDates()
                    {
                        Refuse1 = this.GetActualDate(collectionDate.R1, dateAdjustments, REFUSE_NUMBER_OF_DAYS_BETWEEN_PICKUPS),
                        Refuse2 = this.GetActualDate(collectionDate.R2, dateAdjustments, REFUSE_NUMBER_OF_DAYS_BETWEEN_PICKUPS),
                        Refuse3 = this.GetActualDate(collectionDate.R3, dateAdjustments, REFUSE_NUMBER_OF_DAYS_BETWEEN_PICKUPS),

                        GardenWaste1 = this.GetActualDate(collectionDate.G1, dateAdjustments, GARDEN_WASTE_NUMBER_OF_DAYS_BETWEEN_PICKUPS),
                        GardenWaste2 = this.GetActualDate(collectionDate.G2, dateAdjustments, GARDEN_WASTE_NUMBER_OF_DAYS_BETWEEN_PICKUPS),
                        GardenWaste3 = this.GetActualDate(collectionDate.G3, dateAdjustments, GARDEN_WASTE_NUMBER_OF_DAYS_BETWEEN_PICKUPS),

                        // RX represents recycling and non-recycling
                        Recycling1 = this.GetActualDate(collectionDate.R1 < collectionDate.C1 ? collectionDate.R1 : collectionDate.C1, dateAdjustments, RECYLCING_NUMBER_OF_DAYS_BETWEEN_PICKUPS),
                        Recycling2 = this.GetActualDate(collectionDate.R2 < collectionDate.C2 ? collectionDate.R2 : collectionDate.C2, dateAdjustments, RECYLCING_NUMBER_OF_DAYS_BETWEEN_PICKUPS),
                        Recycling3 = this.GetActualDate(collectionDate.R3 < collectionDate.C3 ? collectionDate.R3 : collectionDate.C3, dateAdjustments, RECYLCING_NUMBER_OF_DAYS_BETWEEN_PICKUPS),
                    };
                }
            }

            return null;
        }

        private DateTime? GetActualDate(DateTime? targetDate, IEnumerable<DateAdjustment> alternativeDates, int numberOfDaysBetweenPickups)
        {
            if (targetDate.HasValue && alternativeDates != null)
            {
                // Check to see if we have a alternative date for the last collection,
                // and if so if the alternative date has yet to occur.
                var passedDate = targetDate.Value.AddDays(numberOfDaysBetweenPickups * -1);
                var passedAlternativeDate = alternativeDates.FirstOrDefault(x => x.OriginalDate == passedDate);
                if (passedAlternativeDate != null &&
                    passedAlternativeDate?.NewDate.Date >= DateTime.UtcNow.Date)
                {
                    return passedAlternativeDate.NewDate;
                }

                // If our target date is present in our alternative dates section, then we must
                // use our alternative date.
                var alternativeDate = alternativeDates.FirstOrDefault(x => x.OriginalDate == targetDate);
                if (alternativeDate != null)
                {
                    return alternativeDate.NewDate;
                }
            }

            return targetDate;
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
