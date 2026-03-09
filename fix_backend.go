package main

import (
	"bytes"
	"os"
	"strings"
)

func main() {
	data, _ := os.ReadFile("IoTHubBenhmark/src/BackendService/Program.cs")
	content := string(data)
	
	// Replace the async Main method signature
	content = strings.Replace(content, "static async Task<int> Main(string[] args)", "static int Main(string[] args)", 1)
	
	// Replace Option constructor
	old1 := `var scenarioOption = new Option<int>(
                "--scenario",
                getDefaultValue: () => 1,
                description: "Test scenario number (1-5)");`
	
	new1 := `var scenarioOption = new Option<int>("--scenario") 
            { 
                Description = "Test scenario number (1-5)",
                DefaultValueFactory = _ => 1
            };`
	
	content = strings.Replace(content, old1, new1, 1)
	
	// Replace AddOption with Options.Add
	content = strings.Replace(content, "rootCommand.AddOption(scenarioOption);", "rootCommand.Options.Add(scenarioOption);", 1)
	
	// Replace SetHandler with SetAction
	content = strings.Replace(content, "rootCommand.SetHandler(async (int scenario) =>", "rootCommand.SetAction(parseResult =>", 1)
	
	// Add scenario variable extraction
	content = strings.Replace(content, `rootCommand.SetAction(parseResult =>
            {
                Console.WriteLine($"Starting scenario {scenario}...");`, 
	`rootCommand.SetAction(parseResult =>
            {
                var scenario = parseResult.GetValue(scenarioOption);
                Console.WriteLine($"Starting scenario {scenario}...");`, 1)
	
	// Replace SetHandler closing
	content = strings.Replace(content, "}, scenarioOption);", "});", 1)
	
	// Replace InvokeAsync with Parse().Invoke()
	content = strings.Replace(content, "return await rootCommand.InvokeAsync(args);", "return rootCommand.Parse(args).Invoke();", 1)
	
	// Replace all await calls with .GetAwaiter().GetResult()
	content = strings.Replace(content, "await RunScenario1Async(", "RunScenario1Async(", -1)
	content = strings.Replace(content, "await RunScenario2Async(", "RunScenario2Async(", -1)
	content = strings.Replace(content, "await RunScenario3Async(", "RunScenario3Async(", -1)
	content = strings.Replace(content, "await RunScenario4Async(", "RunScenario4Async(", -1)
	content = strings.Replace(content, "await RunScenario5Async(", "RunScenario5Async(", -1)
	
	// Add .GetAwaiter().GetResult() to scenario calls
	lines := strings.Split(content, "\n")
	var buffer bytes.Buffer
	for _, line := range lines {
		if strings.Contains(line, "RunScenario") && strings.Contains(line, "Async(") && strings.Contains(line, ");") && !strings.Contains(line, "GetAwaiter") {
			line = strings.Replace(line, ");", ").GetAwaiter().GetResult();", 1)
		}
		buffer.WriteString(line + "\n")
	}
	
	os.WriteFile("IoTHubBenhmark/src/BackendService/Program.cs", buffer.Bytes(), 0644)
}
