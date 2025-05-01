# Azure IoT Hub and Event Hub Integration - Azure Environment Setup Guide

This document provides detailed step-by-step instructions on how to create and configure the necessary Azure resources for the Azure IoT Hub performance test project.

## 1. Azure Account and Subscription

### 1.1. Creating an Azure Account

1. Go to [Azure Portal](https://portal.azure.com)
2. If you don't have an Azure account yet, click on "Create a free account"
3. Enter the required information to create your account
4. Provide your credit card information to verify your account (new accounts are not charged)

### 1.2. Checking Your Azure Subscription

1. Sign in to the Azure Portal
2. Select "Subscriptions" from the left menu
3. Ensure you have an active subscription
4. Note the subscription name, you will use this subscription when creating resources

## 2. Creating and Configuring Azure IoT Hub

### 2.1. Creating an IoT Hub Service

1. Sign in to the Azure Portal
2. Click on "Create a resource" in the upper left corner
3. Type "IoT Hub" in the search box and select "IoT Hub" from the search results
4. Click the "Create" button
5. Fill in the following information in the "Basics" tab:
   - **Subscription**: Select your Azure subscription
   - **Resource group**: Create a new resource group (e.g., "iot-workshop-rg")
   - **IoT Hub Name**: Enter a unique name (e.g., "iothub-workshop-[yourname]")
   - **Region**: Select the region closest to you (e.g., "West Europe")
   - **Pricing and scale tier**: Select "S1: Standard tier" (sufficient for testing)
6. Click the "Review + create" button
7. After validation is successful, click the "Create" button
8. Wait for the deployment to complete (this may take a few minutes)

### 2.2. Getting the IoT Hub Connection String

1. After the deployment is complete, click the "Go to resource" button
2. Select "Shared access policies" from the left menu
3. Click on the "iothubowner" policy
4. Copy the "Primary connection string" value and save it in a secure location
5. This connection string will be used for the "IoTHub:ConnectionString" value in the CloudService application's appsettings.json file

### 2.3. Creating Devices in IoT Hub

1. Within your IoT Hub resource, select "IoT devices" from the left menu
2. Click the "New" button
3. Fill in the following information:
   - **Device ID**: Enter "test-device-1"
   - **Authentication type**: Select "Symmetric key"
   - **Auto-generate keys**: Leave checked
   - **Set as Edge device**: Leave unchecked
   - **Enable this device**: Select "Enable"
4. Click the "Save" button
5. Repeat the same steps to create "test-device-2", "test-device-3", "test-device-4", and "test-device-5" devices

### 2.4. Getting Device Keys

1. Within your IoT Hub resource, select "IoT devices" from the left menu
2. Click on the "test-device-1" device you created
3. Copy the "Primary key" value and save it in a secure location
4. Repeat the same steps for the other devices
5. These keys will be used in the "IoTHub:DeviceKeys" array in the DeviceSimulator application's appsettings.json file

### 2.5. Getting IoT Hub's Event Hub Endpoint Information

1. Within your IoT Hub resource, select "Built-in endpoints" from the left menu
2. Copy the "Event Hub-compatible endpoint" value and save it in a secure location
3. Copy the "Event Hub-compatible name" value and save it in a secure location
4. Verify that the "$Default" consumer group is listed in the "Consumer groups" section
5. This information will be used for Event Hub integration

## 3. Creating and Configuring Azure Event Hub

### 3.1. Creating an Event Hub Namespace

1. Sign in to the Azure Portal
2. Click on "Create a resource" in the upper left corner
3. Type "Event Hubs" in the search box and select "Event Hubs" from the search results
4. Click the "Create" button
5. Fill in the following information:
   - **Subscription**: Select your Azure subscription
   - **Resource group**: Select the resource group you created for IoT Hub (e.g., "iot-workshop-rg")
   - **Namespace name**: Enter a unique name (e.g., "eventhub-workshop-[yourname]")
   - **Location**: Select the same region as your IoT Hub (e.g., "West Europe")
   - **Pricing tier**: Select "Standard"
6. Click the "Review + create" button
7. After validation is successful, click the "Create" button
8. Wait for the deployment to complete (this may take a few minutes)

### 3.2. Creating an Event Hub

1. After the deployment is complete, click the "Go to resource" button
2. Select "Event Hubs" from the left menu
3. Click the "Event Hub" button
4. Fill in the following information:
   - **Name**: Enter "iothub-data"
   - **Partition count**: Enter "4" (for parallel processing)
   - **Message retention**: Enter "1" (in days)
   - **Capture**: Leave "Off"
5. Click the "Create" button

### 3.3. Creating a Consumer Group for Event Hub

1. Click on the "iothub-data" Event Hub you created
2. Select "Consumer groups" from the left menu
3. Enter "analytics" in the "Consumer group" field
4. Click the "Create" button

### 3.4. Creating a SAS Policy for Event Hub

## 4. Azure IoT Hub and Event Hub Integration

### 4.1. Understanding Data Flow from IoT Hub to Event Hub

By default, Azure IoT Hub sends all telemetry data to its built-in Event Hub-compatible endpoint. This is an embedded Event Hub within IoT Hub that can be accessed directly. However, for more advanced scenarios, using an external Event Hub provides more flexibility.

The data flow from IoT Hub to Event Hub works as follows:
1. IoT devices send telemetry data to IoT Hub
2. IoT Hub processes this data and routes it to the built-in Event Hub-compatible endpoint
3. The CloudService application can read this data from this endpoint

### 4.2. Creating Custom Routing Rules (Optional)

For more advanced scenarios, you may want to route data from IoT Hub to a custom Event Hub:

1. Within your IoT Hub resource, select "Message routing" from the left menu
2. Click on the "Custom endpoints" tab
3. Click the "Add" button and select "Event Hubs"
4. Fill in the following information:
   - **Endpoint name**: Enter "CustomEventHub"
   - **Event Hubs namespace**: Select the Event Hub namespace you created
   - **Event Hub**: Select the "iothub-data" Event Hub
5. Click the "Create" button
6. Click on the "Routes" tab
7. Click the "Add" button
8. Fill in the following information:
   - **Name**: Enter "AllMessagesToEventHub"
   - **Data source**: Select "Device Telemetry Messages"
   - **Routing query**: Enter "true" (routes all messages)
   - **Routing endpoint**: Select "CustomEventHub"
   - **Enable route**: Select "Enable"
9. Click the "Save" button

