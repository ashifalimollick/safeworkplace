using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace FinanceBot
{
    public class DialogHelper
    {
        string type;

        public DialogHelper()
        {
            this.type = string.Empty;
        }

        public Activity welcomedefault(ITurnContext turnContext)
        {
            var response = generateAdaptiveCard(turnContext.Activity, "welcomeCard.json");
            return response;
        }

        public Activity bookingDetails(ITurnContext turnContext, string FacilityType, string EmployeeID, string Date, string Floor)
        {
            var response = ((Activity)turnContext.Activity).CreateReply();
            var attachment = CustomCreateAdaptiveCardAttachment("bookingdetails.json", FacilityType, EmployeeID, Date, Floor);
            response.Attachments.Add(attachment);
            return response;
        }

        public Activity generateAdaptiveCard(IActivity activity, string cardName)
        {
            var attachment = CreateAdaptiveCardAttachment(cardName);
            var response = ((Activity)activity).CreateReply();
            response.Attachments = new List<Attachment>() { attachment };
            return response;
        }

        // Load attachment from file.
        private Attachment CreateAdaptiveCardAttachment(String cardName)
        {
            // combine path for cross platform support
            string[] paths = { ".", "Cards", cardName };
            string fullPath = Path.Combine(paths);
            var adaptiveCard = File.ReadAllText(fullPath);
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };
        }

        private Attachment CustomCreateAdaptiveCardAttachment(string cardName, string FacilityType, string EmployeeID, string Date, string Floor)
        {
            // combine path for cross platform support
            string[] paths = { ".", "Cards", cardName };
            string fullPath = Path.Combine(paths);
            string adaptiveCard = File.ReadAllText(fullPath);
            adaptiveCard = adaptiveCard.Replace("formatspace1", EmployeeID);
            adaptiveCard = adaptiveCard.Replace("formatspace2", FacilityType);
            adaptiveCard = adaptiveCard.Replace("formatspace3", Floor);
            adaptiveCard = adaptiveCard.Replace("formatspace6", Date);
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };
        }

    }
}
