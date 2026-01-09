using Microsoft.EntityFrameworkCore;
using EMSCore.Domain.Entities;
using EMSCore.Domain.Interfaces;
using EMSCore.Infrastructure.Data;

namespace EMSCore.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for device operations using EF Core
/// Provides CRUD operations and specialized queries for device management
/// </summary>
public class DeviceRepository : IDeviceRepository
{
    private readonly EMSDbContext _context;

    /// <summary>
    /// Initializes a new instance of the DeviceRepository
    /// </summary>
    /// <param name="context">EF Core database context</param>
    public DeviceRepository(EMSDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all devices with their associated site information
    /// Uses AsNoTracking for read-only operations to improve performance
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of all devices with site data</returns>
    public async Task<IEnumerable<Device>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .Include(d => d.Site) // Eager load site information
            .AsNoTracking() // Read-only, no change tracking needed
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a specific device by its unique identifier
    /// Includes site information for complete device context
    /// </summary>
    /// <param name="id">Unique device identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Device with site information, or null if not found</returns>
    public async Task<Device?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .Include(d => d.Site) // Include site for complete device information
            .AsNoTracking() // Read-only operation
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves all devices located at a specific site
    /// Useful for site-based device management and monitoring
    /// </summary>
    /// <param name="siteId">Site identifier to filter devices</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of devices at the specified site</returns>
    public async Task<IEnumerable<Device>> GetBySiteIdAsync(string siteId, CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .Where(d => d.SiteId == siteId) // Filter by site
            .Include(d => d.Site) // Include site information
            .AsNoTracking() // Read-only operation
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves all devices of a specific type (e.g., "Battery", "Solar Panel", "Inverter")
    /// Enables type-based device categorization and management
    /// </summary>
    /// <param name="type">Device type to filter by</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of devices of the specified type</returns>
    public async Task<IEnumerable<Device>> GetByTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .Where(d => d.Type == type) // Filter by device type
            .Include(d => d.Site) // Include site information
            .AsNoTracking() // Read-only operation
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves only active devices (IsActive = true)
    /// Excludes decommissioned or disabled devices from operations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of active devices</returns>
    public async Task<IEnumerable<Device>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .Where(d => d.IsActive) // Only active devices
            .Include(d => d.Site) // Include site information
            .AsNoTracking() // Read-only operation
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves devices that are currently online and active
    /// Critical for real-time monitoring and operational status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of online and active devices</returns>
    public async Task<IEnumerable<Device>> GetOnlineAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .Where(d => d.IsOnline && d.IsActive) // Both online and active
            .Include(d => d.Site) // Include site information
            .AsNoTracking() // Read-only operation
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a new device to the system
    /// Automatically sets creation and update timestamps
    /// </summary>
    /// <param name="device">Device entity to add</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Added device with generated ID and loaded site information</returns>
    public async Task<Device> AddAsync(Device device, CancellationToken cancellationToken = default)
    {
        // Set audit timestamps
        device.CreatedAt = DateTime.UtcNow;
        device.UpdatedAt = DateTime.UtcNow;
        
        // Add device to context
        _context.Devices.Add(device);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Load the site for the returned device to provide complete information
        await _context.Entry(device)
            .Reference(d => d.Site)
            .LoadAsync(cancellationToken);
            
        return device;
    }

    /// <summary>
    /// Updates an existing device in the system
    /// Automatically updates the modification timestamp
    /// </summary>
    /// <param name="device">Device entity with updated information</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated device with loaded site information</returns>
    public async Task<Device> UpdateAsync(Device device, CancellationToken cancellationToken = default)
    {
        // Update modification timestamp
        device.UpdatedAt = DateTime.UtcNow;
        
        // Update device in context
        _context.Devices.Update(device);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Load the site for the returned device to provide complete information
        await _context.Entry(device)
            .Reference(d => d.Site)
            .LoadAsync(cancellationToken);
            
        return device;
    }

    /// <summary>
    /// Updates the online status of a device efficiently
    /// Used for heartbeat updates and connection status tracking
    /// Avoids loading the entire device entity for performance
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="isOnline">New online status</param>
    /// <param name="lastSeenAt">Optional timestamp of last contact (defaults to now)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if device was found and updated, false otherwise</returns>
    public async Task<bool> UpdateOnlineStatusAsync(
        string deviceId, 
        bool isOnline, 
        DateTime? lastSeenAt = null, 
        CancellationToken cancellationToken = default)
    {
        // Find the device to update
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);
            
        if (device == null)
            return false; // Device not found
            
        // Update status and timestamps
        device.IsOnline = isOnline;
        device.LastSeenAt = lastSeenAt ?? DateTime.UtcNow;
        device.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Removes a device from the system
    /// This will cascade delete related measurements due to FK constraints
    /// </summary>
    /// <param name="id">Device identifier to delete</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if device was found and deleted, false otherwise</returns>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        // Find the device to delete
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            
        if (device == null)
            return false; // Device not found
            
        // Remove device (cascade delete will handle related measurements)
        _context.Devices.Remove(device);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Checks if a device exists in the system
    /// Efficient existence check without loading the entire entity
    /// </summary>
    /// <param name="id">Device identifier to check</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if device exists, false otherwise</returns>
    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .AnyAsync(d => d.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves devices that have been offline for longer than the specified duration
    /// Critical for identifying communication issues and maintenance needs
    /// Includes devices that are marked offline or haven't been seen recently
    /// </summary>
    /// <param name="duration">Time duration to consider a device offline</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of devices that are considered offline</returns>
    public async Task<IEnumerable<Device>> GetOfflineDevicesAsync(
        TimeSpan duration, 
        CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(duration);
        
        return await _context.Devices
            .Where(d => d.IsActive && // Only consider active devices
                       (!d.IsOnline || // Either marked as offline
                        (d.LastSeenAt.HasValue && d.LastSeenAt.Value < cutoffTime))) // Or not seen recently
            .Include(d => d.Site) // Include site information for context
            .AsNoTracking() // Read-only operation
            .ToListAsync(cancellationToken);
    }
}