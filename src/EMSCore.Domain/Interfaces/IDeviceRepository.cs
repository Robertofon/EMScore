using EMSCore.Domain.Entities;
using EMSCore.Domain.Enums;

namespace EMSCore.Domain.Interfaces;

/// <summary>
/// Repository interface for device operations
/// </summary>
public interface IDeviceRepository
{
    /// <summary>
    /// Gets all devices
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of devices</returns>
    Task<IEnumerable<Device>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a device by its identifier
    /// </summary>
    /// <param name="id">Device identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Device or null if not found</returns>
    Task<Device?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets devices by site identifier
    /// </summary>
    /// <param name="siteId">Site identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of devices at the site</returns>
    Task<IEnumerable<Device>> GetBySiteIdAsync(string siteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets devices by type
    /// </summary>
    /// <param name="type">Device type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of devices of the specified type</returns>
    Task<IEnumerable<Device>> GetByTypeAsync(DeviceType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only active devices
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active devices</returns>
    Task<IEnumerable<Device>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only online devices
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of online devices</returns>
    Task<IEnumerable<Device>> GetOnlineAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new device
    /// </summary>
    /// <param name="device">Device to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Added device</returns>
    Task<Device> AddAsync(Device device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing device
    /// </summary>
    /// <param name="device">Device to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated device</returns>
    Task<Device> UpdateAsync(Device device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates device online status
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="isOnline">Online status</param>
    /// <param name="lastSeenAt">Last seen timestamp</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateOnlineStatusAsync(
        string deviceId, 
        bool isOnline, 
        DateTime? lastSeenAt = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a device
    /// </summary>
    /// <param name="id">Device identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a device exists
    /// </summary>
    /// <param name="id">Device identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if device exists</returns>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets devices that haven't been seen for a specified duration
    /// </summary>
    /// <param name="duration">Duration since last seen</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of offline devices</returns>
    Task<IEnumerable<Device>> GetOfflineDevicesAsync(
        TimeSpan duration, 
        CancellationToken cancellationToken = default);
}