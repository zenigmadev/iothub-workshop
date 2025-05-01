# IoT Hub Test Scenarios Implementation Guide

This document provides instructions for running the IoT Hub test scenarios to evaluate performance with different connection patterns.

## Overview of Test Scenarios

1. **Scenario 1**: Single Connection, Serial Messages to One Device
   - Producer opens ONE connection to IoT Hub
   - Producer sends 500 messages serially (1 second intervals) to ONE device
   - Consumer (device) listens for these messages

2. **Scenario 2**: Single Connection, Parallel Messages to Multiple Devices
   - Producer opens ONE connection to IoT Hub
   - Producer sends messages in PARALLEL to FIVE devices (50 messages each, 250 total)
   - Five consumers (devices) each listen for their 50 messages

3. **Scenario 3**: Single Connection, Serial Messages to Multiple Devices
   - Producer opens ONE connection to IoT Hub
   - Producer sends messages SERIALLY (1000ms intervals) to FIVE devices (50 messages each, 250 total)
   - Five consumers (devices) each listen for their 50 messages

4. **Scenario 4**: Multiple Connections, Parallel Messages to Multiple Devices
   - Producer opens FIVE connections to IoT Hub (one per device)
   - Producer sends messages in PARALLEL to FIVE devices (50 messages each, 250 total)
   - Five consumers (devices) each listen for their 50 messages

5. **Scenario 5**: Multiple Connections, Serial Messages to Multiple Devices
   - Producer opens FIVE connections to IoT Hub (one per device)
   - Producer sends messages SERIALLY (1000ms intervals) to FIVE devices (50 messages each, 250 total)
   - Five consumers (devices) each listen for their 50 messages

## Setup Instructions

### 1. Configure Azure IoT Hub

1. Update the connection strings in both configuration files:
   - `BackendService/appsettings.json`
   - `DeviceSimulator/appsettings.json`

2. Ensure you have created the necessary devices in your IoT Hub with the same keys (device1, device2, etc.)

### 2. Running the Tests

#### Start Device Simulators

For a single device:
```
cd src/DeviceSimulator
dotnet run --deviceId device1
```

For multiple devices:
```
cd src/DeviceSimulator
dotnet run --deviceCount 5 --devicePrefix device
```

#### Run Backend Service Tests

To run a specific scenario:
```
cd src/BackendService
dotnet run --scenario 1
```

Replace the scenario number (1-5) to test different scenarios.

## Analyzing Results

Each scenario will output performance metrics including:
- Total duration
- Messages per second
- Success rate
- Average/Min/Max transmission times

Compare these metrics across scenarios to evaluate the performance impact of:
- Single vs. multiple connections
- Serial vs. parallel message sending
- Number of target devices

## Troubleshooting

- Ensure all connection strings are correctly configured
- Verify that devices exist in your IoT Hub
- Check that device simulators are running before starting backend tests
- Review logs for any connection or authentication errors
