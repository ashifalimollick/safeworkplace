// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Net;
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace FinanceBot
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class EchoBot : IBot
    {
        protected LuisRecognizer _luis;

        protected IConfiguration Configuration;

        protected DialogHelper dh;

        private readonly EchoBotAccessors _accessors;

        private readonly DialogSet _dialogs;

        private readonly TextToSpeechService _ttsService;

        private readonly IBotTelemetryClient _telemetry;

        public EchoBot(EchoBotAccessors accessors, LuisRecognizer luisRecognizer, IConfiguration configuration, IBotTelemetryClient telemetry)
        {
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));

            _dialogs = new DialogSet(_accessors.ConversationDialogState);

            _luis = luisRecognizer;

            Configuration = configuration;

            _telemetry = telemetry;

            dh = new DialogHelper();

            _ttsService = new TextToSpeechService();

            var waterfallSteps = new WaterfallStep[]
    {
            ZeroStepAsync,
            FirstStepAsync,
            SecondStepAsync,
            ThirdStepAsync,
            FourthStepAsync,
    };

            var waterfallSteps2 = new WaterfallStep[]
    {
            Assessment_ZeroStep,
            Assessment_FirstStep,
    };

            var waterfallSteps3 = new WaterfallStep[]
    {
                    Count_ZeroStep,
                    Count_FirstStep,
    };

            _dialogs.Add(new WaterfallDialog("booking", waterfallSteps));
            _dialogs.Add(new WaterfallDialog("navigation", waterfallSteps2));
            _dialogs.Add(new WaterfallDialog("YesNo", waterfallSteps3));
            _dialogs.Add(new TextPrompt("B1"));
            _dialogs.Add(new TextPrompt("B2"));
            _dialogs.Add(new TextPrompt("B3"));
            _dialogs.Add(new TextPrompt("B4"));
            _dialogs.Add(new TextPrompt("YN1"));
            _dialogs.Add(new TextPrompt("N1"));
        }

        // Add QnAMaker

        /// <summary>
        /// Every conversation turn for our Echo Bot will call this method.
        /// There are no dialogs used, since it's "single turn" processing, meaning a single
        /// request and response.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            //string uid = turnContext.Activity.From.Id;
            //string uname = turnContext.Activity.From.Name;

            var state = await _accessors.UserDataState.GetAsync(turnContext, () => new UserData());
            string usertext = string.Empty;
            string reply = string.Empty;
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                try
                {
                    JToken commandToken = JToken.Parse(turnContext.Activity.Value.ToString());
                    usertext = commandToken["x"].Value<string>();
                    turnContext.Activity.Text = usertext;
                }
                catch (Exception ex)
                {
                    try
                    {
                        usertext = turnContext.Activity.Text.ToString();
                    }
                    catch
                    {
                        usertext = turnContext.Activity.Value.ToString();
                        turnContext.Activity.Text = usertext;
                    }

                }
                try
                {
                    var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
                    var dialogResult = await dialogContext.ContinueDialogAsync(cancellationToken);
                    if (!turnContext.Responded)
                    {
                        switch (dialogResult.Status)
                        {
                            case DialogTurnStatus.Empty:
                                LuisData ld = new LuisData();
                                ld = await LuisHelper.ExecuteLuisQuery(Configuration, turnContext, cancellationToken);
                                switch (ld.intent)
                                {
                                    case "none":
                                        reply = "Sorry did I not understand. I think i am running out of my bot super powers!";
                                        await turnContext.SendActivityAsync(MessageFactory.Text(reply), cancellationToken);
                                        break;

                                    case "welcome":
                                        var response1 = dh.welcomedefault(turnContext);
                                        await turnContext.SendActivityAsync(response1, cancellationToken);
                                        break;

                                    case "bookfacilities":
                                        await dialogContext.BeginDialogAsync("booking", ld, cancellationToken);
                                        break;

                                    case "navigatefacilities":
                                        await dialogContext.BeginDialogAsync("navigation", ld, cancellationToken);
                                        break;

                                    case "endConversation":
                                        reply = "Is there anything else i can help you with";
                                        await turnContext.SendActivityAsync(MessageFactory.Text(reply), cancellationToken);
                                        break;
                                }

                                break;
                            case DialogTurnStatus.Waiting:
                                // The active dialog is waiting for a response from the user, so do nothing.
                                break;
                            case DialogTurnStatus.Complete:
                                await dialogContext.EndDialogAsync();
                                break;
                            default:
                                await dialogContext.CancelAllDialogsAsync();
                                break;
                        }
                    }
                    // Save states in the accessor
                    // Get the conversation state from the turn context.

                    // Set the property using the accessor.
                    await _accessors.UserDataState.SetAsync(turnContext, state);
                    // Save the new state into the conversation state.
                    await _accessors.ConversationState.SaveChangesAsync(turnContext);
                    await _accessors.UserState.SaveChangesAsync(turnContext);
                }
                catch (Exception ex)
                {
                    //dal.InsertErrorLog(state.UserID, "OnTurnAsync", ex.Message.ToString(), "Tech");
                }
            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate && turnContext.Activity.MembersAdded.FirstOrDefault()?.Id == turnContext.Activity.Recipient.Id)
            {
                var response1 = dh.welcomedefault(turnContext);
                await turnContext.SendActivityAsync(response1, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ZeroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _accessors.UserDataState.GetAsync(stepContext.Context, () => new UserData(), cancellationToken);
            try
            {
                var luisdata = (LuisData)stepContext.Options;
                state.FacilityID = luisdata.FacilityID;
                state.FacilityType = luisdata.FacilityType;
                state.EmployeeID = luisdata.EmployeeID;
                state.FacilityPreference = luisdata.FacilityPreference;
                state.PersonAmount = luisdata.PersonAmount;
                state.Floor = luisdata.Floor;
                state.Date = luisdata.Date;
                if (state.FacilityType == string.Empty)
                {
                    string response1 = "Which facility would you like to book?";
                    var response = new PromptOptions { Prompt = MessageFactory.Text(response1) };
                    return await stepContext.PromptAsync("B1", response, cancellationToken);
                }
                else
                {
                    return await stepContext.NextAsync();
                }
            }
            catch (Exception ex)
            {
                string msg = "Error in ZeroStepAsync";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FirstStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _accessors.UserDataState.GetAsync(stepContext.Context, () => new UserData(), cancellationToken);
            if (state.FacilityType == string.Empty)
            {
                state.FacilityType = stepContext.Context.Activity.Text.ToString();
            }
            try
            {
                if (state.EmployeeID == string.Empty)
                {
                    string response1 = "Can I have your Employee ID please?";
                    var response = new PromptOptions { Prompt = MessageFactory.Text(response1) };
                    return await stepContext.PromptAsync("B2", response, cancellationToken);
                }
                else
                {
                    return await stepContext.NextAsync();
                }
            }
            catch (Exception ex)
            {
                string msg = "Error in FirstStepAsync";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        private async Task<DialogTurnResult> SecondStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _accessors.UserDataState.GetAsync(stepContext.Context, () => new UserData(), cancellationToken);
            if (state.EmployeeID == string.Empty)
            {
                state.EmployeeID = stepContext.Context.Activity.Text.ToString();
            }
            try
            {
                string[] output = ValidateID(state.EmployeeID);
                if (output[0] == "VALID")
                {
                    if (state.FacilityType == "office space" && output[1] != "SE")
                    {
                        string reply = "Sorry office space is only for Senior Executives. Would you like to book an office cubicle instead?";
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(reply), cancellationToken);
                        state.FacilityType = string.Empty;
                        return await stepContext.ReplaceDialogAsync("YesNo", null,cancellationToken);
                    }
                    if (state.Date == string.Empty)
                    {
                        string response1 = "For which date would you like to book?";
                        var response = new PromptOptions { Prompt = MessageFactory.Text(response1) };
                        return await stepContext.PromptAsync("B3", response, cancellationToken);
                    }
                    else
                    {
                        return await stepContext.NextAsync();
                    }
                }
                else
                {
                    state.EmployeeID = string.Empty;
                    string reply = "The Employee ID provided is incorrect";
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(reply), cancellationToken);
                    return await stepContext.ReplaceDialogAsync("booking", cancellationToken);
                }
            }
            catch (Exception ex)
            {
                string msg = "Error in SecondStepAsync";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }
        private async Task<DialogTurnResult> ThirdStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _accessors.UserDataState.GetAsync(stepContext.Context, () => new UserData(), cancellationToken);
            try
            {
                if (state.Date == string.Empty)
                {
                    state.Date = stepContext.Context.Activity.Text.ToString();
                }
                string[] output = ValidateID(state.EmployeeID);
                if (output[2] == "VALID")
                {
                    if (state.Floor == string.Empty)
                    {
                        string response1 = "On which floor would you like to book?";
                        var response = new PromptOptions { Prompt = MessageFactory.Text(response1) };
                        return await stepContext.PromptAsync("B4", response, cancellationToken);
                    }
                    else
                    {
                        return await stepContext.NextAsync();
                    }
                }
                else
                {
                    state.Date = string.Empty;
                    string reply = "No "+state.FacilityType+" is available on this date, would you like to book for another date?";
                    state.Date = string.Empty;
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(reply), cancellationToken);
                    return await stepContext.ReplaceDialogAsync("YesNo", cancellationToken);
                }
            }
            catch (Exception ex)
            {
                string msg = "Error in ThirdStepAsync- "+ex.ToString();
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }
        private async Task<DialogTurnResult> FourthStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _accessors.UserDataState.GetAsync(stepContext.Context, () => new UserData(), cancellationToken);
            try
            {
                string f1 = state.Floor;
                if (f1 == "")
                {
                    state.Floor = stepContext.Context.Activity.Text.ToString();
                }
                string[] output = ValidateID(state.EmployeeID);
                if (output[3] == "VALID")
                {
                    string d1 = state.Date;
                    var response1 = dh.bookingDetails(stepContext.Context, state.FacilityType, state.EmployeeID, d1, f1);
                    await stepContext.Context.SendActivityAsync(response1, cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    state.Floor = string.Empty;
                    string reply = "No " + state.FacilityType + " is  available on this floor, would you like to book for another floor?";
                    state.Floor = string.Empty;
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(reply), cancellationToken);
                    return await stepContext.ReplaceDialogAsync("YesNo",null, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                string msg = "Error in FourthStepAsync- "+ex.ToString();
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        private async Task<DialogTurnResult> Assessment_ZeroStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _accessors.UserDataState.GetAsync(stepContext.Context, () => new UserData(), cancellationToken);
            try
            {
                var luisdata = (LuisData)stepContext.Options;
                state.FacilityID = luisdata.FacilityID;
                if (state.FacilityID == string.Empty)
                {
                    string response1 = "Where do you wish to be navigated?";
                    var response = new PromptOptions { Prompt = MessageFactory.Text(response1) };
                    return await stepContext.PromptAsync("N1", response, cancellationToken);
                }
                else
                {
                    return await stepContext.NextAsync();
                }
            }
            catch (Exception ex)
            {
                string msg = "Error in Assessment_ZeroStep";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        private async Task<DialogTurnResult> Assessment_FirstStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _accessors.UserDataState.GetAsync(stepContext.Context, () => new UserData(), cancellationToken);
            if (state.FacilityID == string.Empty)
            {
                state.FacilityID = stepContext.Context.Activity.Text.ToString();
            }
            try
            {
                string msg = state.FacilityID+ " is located on 2nd floor Wing A";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                string msg = "Error in Assessment_FirstStep";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        private async Task<DialogTurnResult> Count_ZeroStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _accessors.UserDataState.GetAsync(stepContext.Context, () => new UserData(), cancellationToken);
            try
            {
                string response1 = "Yes/No";
                var response = new PromptOptions { Prompt = MessageFactory.Text(response1) };
                return await stepContext.PromptAsync("YN1", response, cancellationToken);
            }
            catch (Exception ex)
            {
                string msg = "Error in Count_ZeroStep";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        private async Task<DialogTurnResult> Count_FirstStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _accessors.UserDataState.GetAsync(stepContext.Context, () => new UserData(), cancellationToken);
            try
            {
                string reply= stepContext.Context.Activity.Text.ToString();
                if (reply=="yes")
                {
                    return await stepContext.ReplaceDialogAsync("booking", null, cancellationToken);
                }
                else
                {
                    string msg = "Is there anything else i can help you with ?";
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                string msg = "Error in Count_FirstStep";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }


        public string[] ValidateID(string id)
        {
            if (id == "123456")
            {
                string[] output = new string[] { "VALID", "E", "VALID","INVALID" };
                return output;
            }
            if (id == "234567")
            {
                string[] output = new string[] { "VALID", "SE", "VALID", "VALID" };
                return output;
            }
            if (id == "345678")
            {
                string[] output = new string[] { "INVALID", "E", "VALID", "VALID" };
                return output;
            }
            return null;
        }
    }
}
