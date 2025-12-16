# Download Time Calculator

Download Time Calculator is a modern, dark-themed WPF application designed to help users estimate file download
durations and automate system power management based on network activity. Built with .NET 8.0, it features a sleek
interface with neon green accents and robust functionality for power users.

## Features

### 1. Download Time Calculator

- Calculate estimate download times based on file size and connection speed.
- Supports various units for file size (MB, GB, TB) and speed (KB/s, MB/s, Mbps, Gbps).
- Provides instant results for different connection standards.

### 2. Auto Exit System

- **Automated Actions**: Automatically perform system power actions when network activity drops below a specified
  threshold.
- **Supported Actions**:
  - Shutdown
  - Restart
  - Sleep
  - Hibernate
- **Configurable Thresholds**:
  - **Speed Limit**: Define the minimum download speed (e.g., 100 KB/s) that triggers the timer.
  - **Duration**: Set how long the low speed must persist before the action is executed (e.g., 60 seconds).
- **Network Adapter Selection**: Choose a specific network interface to monitor (e.g., Wi-Fi, Ethernet), preventing
  virtual adapters or local traffic from interfering with accurate readings.
- **Visual Feedback**: Real-time display of current download/upload speeds and a countdown timer before the action is
  executed.

### 3. Modern User Interface

- Dark mode design tailored for comfortable viewing in low-light environments.
- Responsive tabbed navigation.
- Visual indicators and tooltips for ease of use.

## Requirements

- **Operating System**: Windows 10 or Windows 11.
- **Runtime**: .NET 8.0 Desktop Runtime.

## Installation and Build

To build and run the application from source:

1. Ensure you have the .NET 8.0 SDK installed.
2. Clone the repository or download the source code.
3. Open a terminal in the project directory.
4. Run the application using the following command:

```bash
dotnet run
```

## Usage

### Calculator Tab

1. Enter the file size in the **File Size** field and select the unit (e.g., GB).
2. Enter your internet speed in the **Internet Speed** field and select the unit (e.g., Mbps).
3. The estimated download time will appear instantly below.

### Auto Exit Tab

1. **Action**: Select the desired power action (Shutdown, Restart, Sleep, Hibernate).
2. **Adapter**: Choose the network adapter you want to monitor from the dropdown list.
3. **Threshold**: Enter the speed limit below which the action timer should start.
4. **Duration**: Enter the number of seconds the speed must remain low before the action triggers.
5. **Enable**: Toggle the **Enable Auto Exit** switch to start monitoring.

The application will monitor the selected network adapter. If the download speed drops below your defined threshold for
the specified duration, the selected power action will be executed.

## License

This project is open-source and available under the MIT License.
