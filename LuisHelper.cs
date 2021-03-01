// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.3.0

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinanceBot
{
    public static class LuisHelper
    {
        public static async Task<LuisData> ExecuteLuisQuery(IConfiguration configuration, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            try
            {
                //// Create the LUIS settings from configuration.
                var luisApplication = new LuisApplication(
                        configuration["LuisAppId"],
                        configuration["LuisAPIKey"],
                        configuration["LuisAPIHostName"]
                    );
                var recognizer = new LuisRecognizer(luisApplication);

                // The actual call to LUIS
                var recognizerResult = await recognizer.RecognizeAsync(turnContext, cancellationToken);
                var (intent, score) = recognizerResult.GetTopScoringIntent();
                string topintent = intent.ToString().ToLower();
                LuisData ld = new LuisData();
                ld.intent = "bookingFacility";
                ld.intent = topintent;
                ld.FacilityID = recognizerResult.Entities["FacilityID"]?.FirstOrDefault()?.ToString().ToLower() ?? string.Empty;
                ld.EmployeeID = recognizerResult.Entities["EmployeeID"]?.FirstOrDefault()?.ToString().ToLower() ?? string.Empty;
                ld.FacilityType = recognizerResult.Entities["FacilityType"]?.FirstOrDefault()?.ToString().ToLower() ?? string.Empty;
                ld.FacilityPreference = recognizerResult.Entities["FacilityPreference"]?.FirstOrDefault()?.ToString().ToLower() ?? string.Empty;
                ld.PersonAmount = recognizerResult.Entities["PersonAmount"]?.FirstOrDefault()?.ToString().ToLower() ?? string.Empty;
                ld.Floor = recognizerResult.Entities["Floor"]?.FirstOrDefault()?.ToString().ToLower() ?? string.Empty;
                ld.Date = recognizerResult.Entities["datetimeV2"]?.FirstOrDefault()?.ToString().ToLower() ?? string.Empty;

                return ld;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
