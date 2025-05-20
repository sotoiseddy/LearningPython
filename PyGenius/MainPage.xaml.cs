using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace PyGenius
{
    public partial class MainPage : ContentPage
    {
        private readonly string _apiKey = "YOUR_API_KEY"; // Replace with your API key
        private readonly string _baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
     
        private string challenge;

        public MainPage()
        {
            InitializeComponent();
        }

        // 🔹 Generate a Python coding challenge
        private async void OnGenerateChallengeClicked(object sender, EventArgs e)
        {
            lblProgress.IsVisible = true;
            lblProgress.Text = "Generating challenge...";
            btnGenerate.IsEnabled = false;

             challenge = await GetGeminiResponse("Give me a short, beginner-friendly python coding challenge. Only return one of these: 'Create a for loop', 'Create a foreach loop', 'Use an array', 'Define a class', 'Write a method'these are examples but like these u can change the method thing and use like classes etc. and dont repeat the same challange two times in a row AND DO NOT tell to Create a function");
            DisplayChallenge(challenge);

            lblProgress.IsVisible = false;
            btnGenerate.IsEnabled = true;
        }

        // 🔹 Validate user's Python code
        private async void OnSubmitCodeClicked(object sender, EventArgs e)
        {
            string userCode = CodeEditor.Text;
            if (string.IsNullOrWhiteSpace(userCode))
            {
                ChatDisplay.Text = "⚠️ Please enter your Python code!";
                return;
            }

            if (string.IsNullOrEmpty(challenge))
            {
                ChatDisplay.Text = "⚠️ Please generate a challenge first!";
                return;
            }

            lblProgress.IsVisible = true;
            lblProgress.Text = "Checking code...";
            btnSubmit.IsEnabled = false;

            string validationResult = await GetGeminiResponse(
                $"Analyze the following Python code and ONLY return '✅' if the code is syntactically valid and runs without errors AND correctly implements the following C# coding challenge: '{challenge}'. Explain how the Python code fulfills the requirements of the challenge. Otherwise, return '❌' and provide a clear explanation of the errors or why the code does not function as intended OR why it does not correctly implement the given python challenge and  GIVE steps to actualy create it.:\n```{userCode}```"
            );

            DisplayValidationResult(validationResult.Trim());

            lblProgress.IsVisible = false;
            btnSubmit.IsEnabled = true;
        }


        // 🔹 Calls Gemini API with a given prompt
        private async Task<string> GetGeminiResponse(string prompt)
        {
            try
            {
                using var client = new HttpClient();
                var requestBody = new
                {
                    contents = new[] { new { parts = new[] { new { text = prompt } } } }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_baseUrl}?key={_apiKey}", content);

                if (!response.IsSuccessStatusCode)
                    return $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";

                var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                // ✅ Check if "candidates" property exists
                if (!responseJson.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                    return "Error: No valid response from AI.";

                // ✅ Check if "content" property exists
                var contentElement = candidates[0].GetProperty("content");
                if (!contentElement.TryGetProperty("parts", out var parts) || parts.GetArrayLength() == 0)
                    return "Error: No valid text found in response.";

                // ✅ Extract and return text
                return parts[0].GetProperty("text").GetString() ?? "Error: No text returned.";
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }


        // 🔹 Display challenge on the UI
        private void DisplayChallenge(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                ChatDisplay.Text = "⚠️ No challenge received. Try again!";
                return;
            }

            // Reset text color to default (white)
            ChatDisplay.TextColor = Colors.White;

            ChatDisplay.Text = $"💡 Challenge:\n{response}";
            CodeEditor.Text = "";
        }


        // 🔹 Display validation results
        private void DisplayValidationResult(string response)
        {
            if (response.Contains("✅"))
            {
                ChatDisplay.Text += "\n🎉 Correct! Great job!";
                ChatDisplay.TextColor = Colors.Green;
            }
            else
            {
                ChatDisplay.Text += $"\n❌ Incorrect: {response}";
                ChatDisplay.TextColor = Colors.Red;
            }
        }
        private void OnCodeEditorTextChanged(object sender, TextChangedEventArgs e)
        {
            if (e.NewTextValue.EndsWith("\n"))  // Detect Enter key
            {
                int cursorPosition = CodeEditor.CursorPosition;
                string text = CodeEditor.Text;

                // Get the last line of code
                string[] lines = text.Split('\n');
                string lastLine = lines.Length > 1 ? lines[lines.Length - 2] : "";

                // Count leading spaces from the last line
                int spaceCount = lastLine.TakeWhile(char.IsWhiteSpace).Count();

                // Add new indentation
                string indentation = new string(' ', spaceCount);
                CodeEditor.Text = text + indentation;
                CodeEditor.CursorPosition = text.Length + indentation.Length; // Move cursor after indentation
            }
        }
    }
}

