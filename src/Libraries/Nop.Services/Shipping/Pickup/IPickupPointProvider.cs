﻿using System.Threading.Tasks;
using Nop.Core.Domain.Common;
using Nop.Services.Plugins;
using Nop.Services.Shipping.Tracking;

namespace Nop.Services.Shipping.Pickup
{
    /// <summary>
    /// Represents an interface of pickup point provider
    /// </summary>
    public partial interface IPickupPointProvider : IPlugin
    {
        #region Properties

        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        IShipmentTracker ShipmentTracker { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Get pickup points for the address
        /// </summary>
        /// <param name="address">Address</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the represents a response of getting pickup points
        /// </returns>
        Task<GetPickupPointsResponse> GetPickupPointsAsync(Address address);
        
        #endregion
    }
}