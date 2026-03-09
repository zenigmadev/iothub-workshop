package main

import (
	"os"
	"strings"
)

func main() {
	data, _ := os.ReadFile("IoTHubBenhmark/src/DeviceSimulator/Program.cs")
	content := string(data)
	
	// Replace the async Main method signature
	content = strings.Replace(content, "static async Task<int> Main(string[] args)", "static int Main(string[] args)", 1)
	
	// Replace Option constructors with property initializers
	old1 := `var deviceIdOption = new Option<string>(
                "--deviceId",
                description: "Device ID to simulate (e.g., device1, device2, etc.)");`
	
	new1 := `var deviceIdOption = new Option<string>("--deviceId") 
            { 
                Description = "Device ID to simulate (e.g., device1, device2, etc.)"
            };`
	
	content = strings.Replace(content, old1, new1, 1)
	
	old2 := `var deviceCountOption = new Option<int>(
                "--deviceCount",
                getDefaultValue: () => 1,
                description: "Number of devices to simulate");`
	
	new2 := `var deviceCountOption = new Option<int>("--deviceCount") 
            { 
                Description = "Number of devices to simulate",
                DefaultValueFactory = _ => 1
            };`
	
	content = strings.Replace(content, old2, new2, 1)
	
	old3 := `var devicePrefixOption = new Option<string>(
                "--devicePrefix",
                getDefaultValue: () => "device",
                description: "Prefix for device IDs when simulating multiple devices");`
	
	new3 := `var devicePrefixOption = new Option<string>("--devicePrefix") 
            { 
                Description = "Prefix for device IDs when simulating multiple devices",
                DefaultValueFactory = _ => "device"
            };`
	
	content = strings.Replace(content, old3, new3, 1)
	
	old4 := `var scenarioOption = new Option<int>(
                "--scenario",
                getDefaultValue: () => 1,
                description: "Test scenario number (1-5)");`
	
	new4 := `var scenarioOption = new Option<int>("--scenario") 
            { 
                Description = "Test scenario number (1-5)",
                DefaultValueFactory = _ => 1
            };`
	
	content = strings.Replace(content, old4, new4, 1)
	
	// Replace AddOption with Options.Add
	content = strings.ReplaceAll(content, "rootCommand.AddOption(", "rootCommand.Options.Add(")
	
	// Replace SetHandler with SetAction and add parameter extraction
	oldHandler := `rootCommand.SetHandler(async (string deviceId, int deviceCount, string devicePrefix, int scenario) =>`
	newHandler := `rootCommand.SetAction(async parseResult =>`
	content = strings.Replace(content, oldHandler, newHandler, 1)
	
	// Add parameter extraction right after SetAction starts
	oldStart := `rootCommand.SetAction(async parseResult =>
            {
                try`
	
	newStart := `rootCommand.SetAction(async parseResult =>
            {
                var deviceId = parseResult.GetValue(deviceIdOption);
                var deviceCount = parseResult.GetValue(deviceCountOption);
                var devicePrefix = parseResult.GetValue(devicePrefixOption);
                var scenario = parseResult.GetValue(scenarioOption);
                
                try`
	
	content = strings.Replace(content, oldStart, newStart, 1)
	
	// Replace SetHandler closing
	content = strings.Replace(content, ", deviceIdOption, deviceCountOption, devicePrefixOption, scenarioOption);", ");", 1)
	
	// Replace InvokeAsync with Parse().Invoke()
	content = strings.Replace(content, "return await rootCommand.InvokeAsync(args);", "return rootCommand.Parse(args).Invoke();", 1)
	
	os.WriteFile("IoTHubBenhmark/src/DeviceSimulator/Program.cs", []byte(content), 0644)
}
