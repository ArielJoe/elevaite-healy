using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Healy.Controllers
{
    public class AzureAIChatController : Controller
    {
        private readonly ChatClient _chatClient;
        private static readonly List<ChatMessage> _conversationHistory = new List<ChatMessage>
    {
        new SystemChatMessage("You are a professional fitness advisor.")
    };

        public AzureAIChatController(ChatClient chatClient)
        {
            _chatClient = chatClient;
        }

        // GET: Display the chat interface
        public IActionResult Index()
        {
            // Pass the conversation history to the view (optional, for display)
            ViewBag.Conversation = _conversationHistory;
            return View();
        }

        // POST: Handle user input and get response from Azure OpenAI
        [HttpPost]
        public async Task<IActionResult> SendMessage(string userMessage)
        {
            if (string.IsNullOrEmpty(userMessage))
            {
                return BadRequest("Message cannot be empty.");
            }

            // Add user message to conversation history
            _conversationHistory.Add(new UserChatMessage(userMessage));

            // Call Azure OpenAI service
            var response = await _chatClient.CompleteChatAsync(_conversationHistory);

            // Extract the assistant's response
            var assistantMessage = response.Value.Content[0].Text;

            // Add assistant's response to conversation history
            _conversationHistory.Add(new AssistantChatMessage(assistantMessage));

            // Return the assistant's response as JSON (for AJAX) or redirect to refresh the view
            return Json(new { message = assistantMessage });
        }
    }
}