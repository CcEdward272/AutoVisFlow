using System.Text.Json.Serialization;

namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Describes the physical or virtual connection parameters for an instrument.
/// Supports serial (RS-232/RS-485), TCP/IP (VISA/LXI), and CAN bus connections.
/// Only the fields relevant to the selected <see cref="Protocol"/> are used.
/// </summary>
public sealed class InstrumentConnection
{
    /// <summary>
    /// The communication protocol: "Serial", "TCP", "CAN", "GPIB", "USB", "Virtual".
    /// </summary>
    public string Protocol { get; set; } = "TCP";

    /// <summary>
    /// The interface address. For Serial: COM port name (e.g., "COM3").
    /// For TCP: IP address or hostname. For CAN: interface name.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Serial port name alias for <see cref="Address"/>. Maintained for
    /// backward compatibility with older serialization formats.
    /// </summary>
    public string PortName { get; set; } = string.Empty;

    /// <summary>
    /// Serial baud rate in bits per second. Default 9600.
    /// </summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    /// Serial data bits per frame. Default 8.
    /// </summary>
    public int DataBits { get; set; } = 8;

    /// <summary>
    /// Serial parity mode: "None", "Odd", "Even", "Mark", "Space".
    /// </summary>
    public string Parity { get; set; } = "None";

    /// <summary>
    /// Serial stop bits: "1", "1.5", "2".
    /// </summary>
    public string StopBits { get; set; } = "1";

    /// <summary>
    /// TCP hostname or IP address. Alias for <see cref="Address"/>
    /// when Protocol is "TCP".
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// TCP port number.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Communication timeout in milliseconds. Default 5000 (5 seconds).
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Number of retry attempts on communication failure. Default 2.
    /// </summary>
    public int RetryCount { get; set; } = 2;

    /// <summary>
    /// When true, the receiver looks for <see cref="TerminationChar"/>
    /// to signal end-of-message.
    /// </summary>
    public bool EnableTerminationChar { get; set; } = true;

    /// <summary>
    /// The character or string that marks the end of a message.
    /// Default is newline.
    /// </summary>
    public string TerminationChar { get; set; } = "\n";

    /// <summary>
    /// Returns true when the protocol is a serial type requiring
    /// baud rate, data bits, parity, and stop bits.
    /// </summary>
    [JsonIgnore]
    public bool IsSerial => Protocol is "Serial" or "RS232" or "RS485";

    /// <summary>
    /// Returns true when the protocol is a TCP/IP type.
    /// </summary>
    [JsonIgnore]
    public bool IsTcp => Protocol is "TCP" or "VXI11" or "HiSLIP";
}
