using System.Text.Json.Serialization;

namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Communication configuration for connecting to a PLC or remote I/O device.
/// Supports Modbus TCP, Modbus RTU, and CAN bus protocols with their
/// respective addressing and timing parameters.
/// </summary>
public sealed class IOConnectionConfig
{
    /// <summary>
    /// The PLC communication protocol: "ModbusTCP", "ModbusRTU", "CANopen",
    /// "EtherNetIP", "Profinet", "EtherCAT".
    /// </summary>
    public string PlcProtocol { get; set; } = "ModbusTCP";

    /// <summary>
    /// The hostname or IP address of the PLC (for TCP-based protocols).
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// The TCP port number. Default 502 for Modbus TCP.
    /// </summary>
    public int Port { get; set; } = 502;

    /// <summary>
    /// Modbus slave/unit ID (1-247). Default 1.
    /// </summary>
    public int SlaveId { get; set; } = 1;

    /// <summary>
    /// PLC rack number (for protocols that use rack/slot addressing).
    /// </summary>
    public int Rack { get; set; }

    /// <summary>
    /// PLC slot number (for protocols that use rack/slot addressing).
    /// </summary>
    public int Slot { get; set; }

    /// <summary>
    /// CAN bus interface name (e.g., "can0", "PCAN_USBBUS1").
    /// Relevant when <see cref="PlcProtocol"/> is "CANopen".
    /// </summary>
    public string CanInterface { get; set; } = string.Empty;

    /// <summary>
    /// CAN bus bit rate in bits per second. Default 500 kbps.
    /// </summary>
    public int BitRate { get; set; } = 500000;

    /// <summary>
    /// CAN channel number (for multi-channel CAN adapters).
    /// </summary>
    public int CanChannel { get; set; }

    /// <summary>
    /// Polling interval in milliseconds for cyclic data reads.
    /// Default 100 ms (10 Hz).
    /// </summary>
    public int PollIntervalMs { get; set; } = 100;

    /// <summary>
    /// Communication timeout in milliseconds. Default 3000 (3 seconds).
    /// </summary>
    public int TimeoutMs { get; set; } = 3000;

    /// <summary>
    /// Number of retry attempts on communication failure. Default 2.
    /// </summary>
    public int RetryCount { get; set; } = 2;

    // ─── Computed helpers ──────────────────────────────────────────

    /// <summary>
    /// Returns true when the protocol is Modbus TCP.
    /// </summary>
    [JsonIgnore]
    public bool IsModbusTcp => PlcProtocol is "ModbusTCP";

    /// <summary>
    /// Returns true when the protocol is Modbus RTU (serial).
    /// </summary>
    [JsonIgnore]
    public bool IsModbusRtu => PlcProtocol is "ModbusRTU";

    /// <summary>
    /// Returns true when the protocol uses CAN bus.
    /// </summary>
    [JsonIgnore]
    public bool IsCanBus => PlcProtocol is "CANopen";

    /// <summary>
    /// Returns true when the protocol uses Ethernet/IP.
    /// </summary>
    [JsonIgnore]
    public bool IsEthernet => PlcProtocol is "ModbusTCP" or "EtherNetIP"
        or "Profinet" or "EtherCAT";

    /// <summary>
    /// Returns the effective connection string (host:port for TCP,
    /// interface name for CAN, port name for RTU).
    /// </summary>
    [JsonIgnore]
    public string ConnectionString => IsEthernet
        ? $"{Host}:{Port}"
        : IsCanBus
            ? CanInterface
            : Host;
}
